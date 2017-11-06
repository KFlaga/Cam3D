using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;
using CamAlgorithms.Calibration;

namespace CamAlgorithms
{
    public abstract class ImageRectificationComputer
    {
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }

        public CameraPair CalibData { get; set; }
        public List<Vector2Pair> MatchedPairs { get; set; }
        public abstract void ComputeRectificationMatrices();

        // Rectification matrix H for left camera, such that rectified point p_rect = H * p_img
        public Matrix<double> RectificationLeft { get; set; }
        // Rectification matrix H for right camera, such that rectified point p_rect = H * p_img
        public Matrix<double> RectificationRight { get; set; }
    }

    [XmlRoot("Rectification")]
    public class ImageRectification : IXmlSerializable
    {
        private class RectifiedImageCorners
        {
            public RectifiedImageCorners(Matrix<double> recitifcation, double imgWidth, double imgHeight)
            {
                TopLeft = new Vector2(recitifcation * new DenseVector(new double[3] { 0.0, 0.0, 1.0 }));
                TopRight = new Vector2(recitifcation * new DenseVector(new double[3] { imgWidth - 1, 0.0, 1.0 }));
                BotLeft = new Vector2(recitifcation * new DenseVector(new double[3] { 0.0, imgHeight - 1, 1.0 }));
                BotRight = new Vector2(recitifcation * new DenseVector(new double[3] { imgWidth - 1, imgHeight - 1, 1.0 }));
            }

            public Vector2 TopLeft { get; private set; }
            public Vector2 TopRight { get; private set; }
            public Vector2 BotLeft { get; private set; }
            public Vector2 BotRight { get; private set; }
        }

        [XmlIgnore]
        public ImageRectificationComputer RectificationComputer { get; set; }

        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
        [XmlIgnore]
        public CameraPair CalibData { get; set; }

        // Rectification matrix H for left camera, such that rectified point p_rect = H * p_img
        public Matrix<double> RectificationLeft { get; set; }
        // Rectification matrix H for right camera, such that rectified point p_rect = H * p_img
        public Matrix<double> RectificationRight { get; set; }

        public Matrix<double> RectificationLeft_Inverse { get; set; }
        public Matrix<double> RectificationRight_Inverse { get; set; }

        // Pairs of matched points - needed for uncalibrated methods
        [XmlIgnore]
        public List<Vector2Pair> MatchedPairs { get; set; }

        public double Quality { get; set; }

        private RectifiedImageCorners _leftCorners;
        private RectifiedImageCorners _rightCorners;

        public ImageRectification() { }
        public ImageRectification(ImageRectificationComputer rectComp)
        {
            RectificationComputer = rectComp;
        }

        public void ComputeRectificationMatrices()
        {
            RectificationComputer.ImageHeight = ImageHeight;
            RectificationComputer.ImageWidth = ImageWidth;
            RectificationComputer.CalibData = CalibData;
            RectificationComputer.MatchedPairs = MatchedPairs;

            RectificationComputer.ComputeRectificationMatrices();

            RectificationLeft = RectificationComputer.RectificationLeft;
            RectificationRight = RectificationComputer.RectificationRight;

            _leftCorners = new RectifiedImageCorners(RectificationLeft, ImageWidth, ImageHeight);
            _rightCorners = new RectifiedImageCorners(RectificationRight, ImageWidth, ImageHeight);
            EnsureCorrectHorizontalOrder();

            RectificationLeft_Inverse = RectificationLeft.Inverse();
            RectificationRight_Inverse = RectificationRight.Inverse();

            Quality = ComputeRectificationQuality();
        }

        private void EnsureCorrectHorizontalOrder()
        {
            EnsureCorrectHorizontalOrder(ref _leftCorners, RectificationLeft);
            EnsureCorrectHorizontalOrder(ref _rightCorners, RectificationRight);
        }

        private void EnsureCorrectHorizontalOrder(ref RectifiedImageCorners corners, Matrix<double> rectification)
        {
            // Now sometimes we have verticaly mirrored image - pixels appears in reversed
            // x order. So we need to check if right edge - left edge is negative or not.
            // If it is so, then reverse x order -> negate first row and add ImageWidth to 3rd element
            // so then x_rect_rev = (W - (H11x + H12y + H13)) / (H31x + H32y + H33) = W / (H31x + H32y + H33) - x_rect
            // Assuming H31 and H32 are quite small x_rect_rev = W/H33 - x_rect
            if(corners.TopRight.X - _leftCorners.TopLeft.X < 0)
            {
                rectification[0, 0] = -rectification[0, 0];
                rectification[0, 1] = -rectification[0, 1];
                rectification[0, 2] = -rectification[0, 2] + ImageWidth / rectification[2, 2];
                corners = new RectifiedImageCorners(rectification, ImageWidth, ImageHeight);

                // Find lowest x and move it to 0 : x_final = (W - (H11x + H12y + H13) - Xmin) / (H31x + H32y + H33) = x_rect_rev - Xmin/H33
                // if H33 < 0 , then we need +Xmin
                double minX = Math.Min(corners.TopLeft.X,
                    Math.Min(corners.TopRight.X,
                    Math.Min(corners.BotRight.X,
                    corners.BotLeft.X)));

                rectification[0, 2] = rectification[0, 2] - minX * rectification[2, 2];
                corners = new RectifiedImageCorners(rectification, ImageWidth, ImageHeight);
            }
        }

        public double ComputeRectificationQuality()
        {
            double yErr = ComputeNonhorizontalityError();

            double perpErr = ComputeCenterNonPerpendicularityError(_leftCorners, _rightCorners);

            double ratioErr = ComputeEdgesRatioError(_leftCorners, _rightCorners);

            return 1.0 / (yErr + perpErr + ratioErr);
        }


        private double ComputeNonhorizontalityError()
        {
            double error = 0;
            for(int i = 0; i < MatchedPairs.Count; ++i)
            {
                var pair = MatchedPairs[i];

                // rectify points pair
                var rectLeft = RectificationLeft * pair.V1.ToMathNetVector3();
                var rectRight = RectificationRight * pair.V2.ToMathNetVector3();

                // get error -> squared difference of y-coord
                double yError = new Vector2(rectLeft).Y - new Vector2(rectRight).Y;
                error += yError * yError;
            }
            return Math.Sqrt(error) / MatchedPairs.Count;
        }

        private double ComputeCenterNonPerpendicularityError(RectifiedImageCorners leftCorners, RectifiedImageCorners rightCorners)
        {
            // Perpendicularity of lines through centers of rectified edges
            // 1.1) find edge centers
            var edgeTopCenterLeft = (leftCorners.TopLeft + leftCorners.TopRight) * 0.5;
            var edgeBotCenterLeft = (leftCorners.BotLeft + leftCorners.BotRight) * 0.5;
            var edgeLeftCenterLeft = (leftCorners.TopLeft + leftCorners.BotLeft) * 0.5;
            var edgeRightCenterLeft = (leftCorners.TopRight + leftCorners.BotRight) * 0.5;
            var edgeTopCenterRight = (rightCorners.TopLeft + rightCorners.TopRight) * 0.5;
            var edgeBotCenterRight = (rightCorners.BotLeft + rightCorners.BotRight) * 0.5;
            var edgeLeftCenterRight = (rightCorners.TopLeft + rightCorners.BotLeft) * 0.5;
            var edgeRightCenterRight = (rightCorners.TopRight + rightCorners.BotRight) * 0.5;
            // 1.2) find vectors joining centers
            var verticalJoinLeft = edgeBotCenterLeft - edgeTopCenterLeft;
            var horizontalJoinLeft = edgeRightCenterLeft - edgeLeftCenterLeft;
            var verticalJoinRight = edgeBotCenterRight - edgeTopCenterRight;
            var horizontalJoinRight = edgeRightCenterRight - edgeLeftCenterRight;
            // 1.3) perpedicularuty value??

            return 0.0;
        }

        private double ComputeEdgesRatioError(RectifiedImageCorners leftCorners, RectifiedImageCorners rightCorners)
        {
            // Ratio of width and height of base and recitifed images (that is ratio of edges length) :
            //      it should be as small as possible and similar for both cameras
            // 2.1) find edges lengths
            var edgeTopLeftLength = (leftCorners.TopRight - leftCorners.TopLeft).Length();
            var edgeBotLeftLength = (leftCorners.BotRight - leftCorners.BotLeft).Length();
            var edgeLeftLeftLength = (leftCorners.BotLeft - leftCorners.TopLeft).Length();
            var edgeRightLeftLength = (leftCorners.BotRight - leftCorners.TopRight).Length();
            var edgeTopRightLength = (rightCorners.TopRight - rightCorners.TopLeft).Length();
            var edgeBotRightLength = (rightCorners.BotRight - rightCorners.BotLeft).Length();
            var edgeLeftRightLength = (rightCorners.BotLeft - rightCorners.TopLeft).Length();
            var edgeRightRightLength = (rightCorners.BotRight - rightCorners.TopRight).Length();
            // 2.2) find lengths ratios
            var ratioWidthLeft = edgeTopLeftLength / edgeBotLeftLength;
            var ratioHeightLeft = edgeLeftLeftLength / edgeRightLeftLength;
            var ratioWidthRight = edgeTopRightLength / edgeBotRightLength;
            var ratioHeightRight = edgeLeftRightLength / edgeRightRightLength;
            // 2.3) what to do with that?
            // Idea: sum of squares of each ratio : so they are small
            //       + sum of quares of ratios of left/right image, so they are similar 
            double errInternal = (1 - ratioWidthLeft) * (1 - ratioWidthLeft) +
                (1 - ratioHeightLeft) * (1 - ratioHeightLeft) +
                (1 - ratioWidthRight) * (1 - ratioWidthRight) +
                (1 - ratioHeightRight) * (1 - ratioHeightRight);
            double rw = (1 - (ratioWidthLeft / ratioWidthRight));
            double rh = (1 - (ratioHeightLeft / ratioHeightRight));
            double errCorss = rw * rw + rh * rh;

            return Math.Sqrt(errInternal + errCorss);
        }

        public XmlSchema GetSchema() { return null; }

        public virtual void ReadXml(XmlReader reader)
        {
            XmlSerialisation.ReadXmlAllProperties(reader, this);
        }

        public virtual void WriteXml(XmlWriter writer)
        {
            XmlSerialisation.WriteXmlNonIgnoredProperties(writer, this);
            //writer.WriteElementString("ImageHeight", ImageHeight.ToString());
            //writer.WriteElementString("ImageWidth", ImageWidth.ToString());
            //writer.WriteElementString("Quality", Quality.ToString());

            //writer.WriteStartElement("RectificationLeft");
            //new MatrixXmlSerializer(RectificationLeft).WriteXml(writer);
            //writer.WriteEndElement();

            //writer.WriteStartElement("RectificationLeft_Inverse");
            //new MatrixXmlSerializer(RectificationLeft_Inverse).WriteXml(writer);
            //writer.WriteEndElement();

            //writer.WriteStartElement("RectificationRight");
            //new MatrixXmlSerializer(RectificationRight).WriteXml(writer);
            //writer.WriteEndElement();

            //writer.WriteStartElement("RectificationRight_Inverse");
            //new MatrixXmlSerializer(RectificationRight_Inverse).WriteXml(writer);
            //writer.WriteEndElement();
        }
    }
}
