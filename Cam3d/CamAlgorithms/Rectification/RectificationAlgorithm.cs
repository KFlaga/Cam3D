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
    public abstract class IRectificationAlgorithm  // TODO: change name to IRectificationAlgorithm
    {
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }

        public CameraPair Cameras { get; set; }
        public List<Vector2Pair> MatchedPairs { get; set; }

        // Rectification matrix H for left camera, such that rectified point p_rect = H * p_img
        public Matrix<double> RectificationLeft { get; set; }
        // Rectification matrix H for right camera, such that rectified point p_rect = H * p_img
        public Matrix<double> RectificationRight { get; set; }
		
        public abstract void ComputeRectificationMatrices();
    }

    [XmlRoot("Rectification")]
    public class RectificationAlgorithm : IXmlSerializable, IParameterizable
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
        public IRectificationAlgorithm RectificationComputer { get; set; }

        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
        [XmlIgnore]
        public CameraPair Cameras { get; set; }

        // Rectification matrix H for left camera, such that rectified point p_rect = H * p_img
        public Matrix<double> RectificationLeft { get; set; }
        // Rectification matrix H for right camera, such that rectified point p_rect = H * p_img
        public Matrix<double> RectificationRight { get; set; }

        public Matrix<double> RectificationLeftInverse { get; set; }
        public Matrix<double> RectificationRightInverse { get; set; }

        // Pairs of matched points - needed for uncalibrated methods
        [XmlIgnore]
        public List<Vector2Pair> MatchedPairs { get; set; }
        
        public RectificationAlgorithm() { }
        public RectificationAlgorithm(IRectificationAlgorithm rectComp)
        {
            RectificationComputer = rectComp;
        }

        public void ComputeRectificationMatrices()
        {
            RectificationComputer.ImageHeight = ImageHeight;
            RectificationComputer.ImageWidth = ImageWidth;
            RectificationComputer.Cameras = Cameras;
            RectificationComputer.MatchedPairs = MatchedPairs;

            RectificationComputer.ComputeRectificationMatrices();
            Matrix<double> HtL, HtR;
            ComputeScalingMatrices(RectificationComputer.RectificationLeft, RectificationComputer.RectificationRight, out HtL, out HtR);

            RectificationLeft = HtL * RectificationComputer.RectificationLeft;
            RectificationRight = HtR * RectificationComputer.RectificationRight;
            
            EnsureCorrectHorizontalOrder(RectificationLeft);
            EnsureCorrectHorizontalOrder(RectificationRight);

            RectificationLeftInverse = RectificationLeft.Inverse();
            RectificationRightInverse = RectificationRight.Inverse();    
        }

        private void EnsureCorrectHorizontalOrder(Matrix<double> rectification)
        {
            RectifiedImageCorners corners = new RectifiedImageCorners(rectification, ImageWidth, ImageHeight);
            // Now sometimes we have verticaly mirrored image - pixels appears in reversed
            // x order. So we need to check if right edge - left edge is negative or not.
            // If it is so, then reverse x order -> negate first row and add ImageWidth to 3rd element
            // so then x_rect_rev = (W - (H11x + H12y + H13)) / (H31x + H32y + H33) = W / (H31x + H32y + H33) - x_rect
            // Assuming H31 and H32 are quite small x_rect_rev = W/H33 - x_rect
            if(corners.TopRight.X - corners.TopLeft.X < 0)
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

        public void ComputeScalingMatrices(Matrix<double> Hl, Matrix<double> Hr, out Matrix<double> HtL, out Matrix<double> HtR)
        {
            // Scale and move images (after rectification) so that they have lowest
            // coordinates (0,0) and same width/height as original image
            RectifiedImageCorners left = new RectifiedImageCorners(Hl, ImageWidth, ImageHeight);
            RectifiedImageCorners right = new RectifiedImageCorners(Hr, ImageWidth, ImageHeight);

            // (scale and y-translation must be same for both images to perserve rectification)
            // Scale so that both images fits to (imgHeight*2, imgWidth*2)
            // If it fits w/o scaling, scale so that bigger image have width imgWidth
            // Translate in y so that left(0,0) is transformed into left'(0,x)
            // Translate in x (independently) so that img(0,0) is transformed into img'(y,0)
            // 1) Find max/min x/y
            double minX_L = Math.Min(left.BotLeft.X, Math.Min(left.BotRight.X, Math.Min(left.TopLeft.X, left.TopRight.X)));
            double minY_L = Math.Min(left.BotLeft.Y, Math.Min(left.BotRight.Y, Math.Min(left.TopLeft.Y, left.TopRight.Y)));
            double maxX_L = Math.Max(left.BotLeft.X, Math.Max(left.BotRight.X, Math.Max(left.TopLeft.X, left.TopRight.X)));
            double maxY_L = Math.Max(left.BotLeft.Y, Math.Max(left.BotRight.Y, Math.Max(left.TopLeft.Y, left.TopRight.Y)));

            double minX_R = Math.Min(right.BotLeft.X, Math.Min(right.BotRight.X, Math.Min(right.TopLeft.X, right.TopRight.X)));
            double minY_R = Math.Min(right.BotLeft.Y, Math.Min(right.BotRight.Y, Math.Min(right.TopLeft.Y, right.TopRight.Y)));
            double maxX_R = Math.Max(right.BotLeft.X, Math.Max(right.BotRight.X, Math.Max(right.TopLeft.X, right.TopRight.X)));
            double maxY_R = Math.Max(right.BotLeft.Y, Math.Max(right.BotRight.Y, Math.Max(right.TopLeft.Y, right.TopRight.Y)));

            double wr = (maxX_L - minX_L) / (maxX_R - minX_R);
            double hr = (maxY_L - minY_L) / (maxY_R - minY_R);
            double maxWidth = Math.Max(maxX_L - minX_L, maxX_R - minX_R);
            double maxHeight = Math.Max(maxY_L - minY_L, maxY_R - minY_R);

            // 2) Scale image so that images fills old size
            double scaleX = ImageWidth / maxWidth;
            double scaleY = ImageHeight / maxHeight;
            double scale = Math.Min(scaleX, scaleY);

            HtL = DenseMatrix.CreateIdentity(3);
            HtR = DenseMatrix.CreateIdentity(3);
            HtL.At(0, 0, scaleX);
            HtL.At(1, 1, scaleY);
            HtR.At(0, 0, scaleX);
            HtR.At(1, 1, scaleY);
            // 3) Translate in y so that minY on larger image = 0
            double transY = -Math.Min(minY_L, minY_R) * scaleY;
            // Translate in x (independently) so that minX on both images = 0
            double transX_L = -minX_L * scaleX;
            double transX_R = -minX_R * scaleX;
            HtL.At(0, 2, transX_L);
            HtL.At(1, 2, transY);
            HtR.At(0, 2, transX_R);
            HtR.At(1, 2, transY);
            
            left = new RectifiedImageCorners(HtL*Hl, ImageWidth, ImageHeight);
            right = new RectifiedImageCorners(HtR*Hr, ImageWidth, ImageHeight);
        }

        public XmlSchema GetSchema() { return null; }

        public virtual void ReadXml(XmlReader reader)
        {
            XmlSerialisation.ReadXmlAllProperties(reader, this);
        }

        public virtual void WriteXml(XmlWriter writer)
        {
            XmlSerialisation.WriteXmlNonIgnoredProperties(writer, this);
        }

        public List<IAlgorithmParameter> Parameters { get; private set; } = new List<IAlgorithmParameter>();

        public void InitParameters()
        {
            Parameters = new List<IAlgorithmParameter>();

            DictionaryParameter computersParam = new DictionaryParameter("Rectification Algorithm", "RectificationComputer");
            computersParam.ValuesMap = new Dictionary<string, object>()
            {
                { "Zhang-Loop", new Rectification_ZhangLoop() },
                { "Fusiello-Trucco-Verri", new Rectification_FusielloTruccoVerri() },
                { "Fusiello-Irsara", new Rectification_FussieloIrsara() { UseInitialCalibration = false } },
                { "Fusiello-Irsara with initial calibration", new Rectification_FussieloIrsara() { UseInitialCalibration = true } }
            };
            computersParam.DefaultValue = computersParam.ValuesMap["Zhang-Loop"];
            Parameters.Add(computersParam);
        }

        public void UpdateParameters()
        {
            RectificationComputer = IAlgorithmParameter.FindValue<IRectificationAlgorithm>("RectificationComputer", Parameters);
        }

        public string Name
        {
            get
            {
                return "ImageRectification";
            }
        }
    }
}
