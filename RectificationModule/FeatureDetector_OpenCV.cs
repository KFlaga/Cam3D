using System;
using CamImageProcessing;
using System.Runtime.InteropServices;
using Emgu.CV.CvEnum;
using Emgu.CV;
using CamCore;

namespace RectificationModule
{
    public class FeatureDetector_OpenCV : FeaturesDetector
    {
        public int NumberOfFeatures { get; set; }
        public float ImagePyramidScaleFactor { get; set; }
        public int ImagePyramidLevels { get; set; }
        public int ImagePyramidFirstLevel { get; set; } = 0;
        public int EdgeTreshold { get; set; }
        public int PatchSize { get; set; }
        public int FastTreshold { get; set; }

        public override void Detect()
        {
            Mat img2 = EmguCVUtils.ImageToMat_Gray(Image);

            Mat imgMask = null;
            if(Image is MaskedImage)
            {
                imgMask = EmguCVUtils.ImageToMat_Mask(Image as MaskedImage);
            }

            Emgu.CV.Features2D.ORBDetector det2 = new Emgu.CV.Features2D.ORBDetector(
                NumberOfFeatures, ImagePyramidScaleFactor, ImagePyramidLevels,
                EdgeTreshold, ImagePyramidFirstLevel, 2,
                Emgu.CV.Features2D.ORBDetector.ScoreType.Harris, PatchSize, FastTreshold);

            var kp2 = det2.Detect(img2, imgMask);

            MathNet.Numerics.LinearAlgebra.Double.DenseMatrix fimg = new MathNet.Numerics.LinearAlgebra.Double.DenseMatrix(
                Image.RowCount, Image.ColumnCount);

            FeaturePoints = new System.Collections.Generic.List<IntVector2>();
            for(int i = 0; i < kp2.Length; ++i)
            {
                var point = kp2[i];
                IntVector2 pixel = new IntVector2((int)point.Point.X, (int)point.Point.Y);
                FeaturePoints.Add(pixel);
                fimg[pixel.Y, pixel.X] = 1.0;
            }

            FeatureMap = new GrayScaleImage() { ImageMatrix = fimg };
        }

        public override string Name
        {
            get
            {
                return "OpenCV ORB Keypoints";
            }
        }

        public override void InitParameters()
        {
            base.InitParameters();

            IntParameter nfeaturesParam = new IntParameter(
                "Number of Features", "NFET", 500, 1, 5000);
            Parameters.Add(nfeaturesParam);

            IntParameter fastTreshParam = new IntParameter(
                "Fast Treshold", "FAST_TR", 10, 1, 100);
            Parameters.Add(fastTreshParam);

            IntParameter edgeTreshParam = new IntParameter(
                "Edge Treshold", "EDGE_TR", 15, 1, 300);
            Parameters.Add(edgeTreshParam);

            IntParameter patchParam = new IntParameter(
                "Patch Size", "PATCH", 15, 1, 300);
            Parameters.Add(patchParam);

            FloatParameter scaleParam = new FloatParameter(
                "Image Pyramid Scale Factor", "IP_SCALE", 1.2f, 1.1f, 2.0f);
            Parameters.Add(scaleParam);

            IntParameter plevelsParam = new IntParameter(
                "Image Pyramid Levels", "IP_LEVEL", 8, 1, 100);
            Parameters.Add(plevelsParam);
        }

        public override void UpdateParameters()
        {
            base.UpdateParameters();

            NumberOfFeatures = AlgorithmParameter.FindValue<int>("NFET", Parameters);
            FastTreshold = AlgorithmParameter.FindValue<int>("FAST_TR", Parameters);
            EdgeTreshold = (AlgorithmParameter.FindValue<int>("EDGE_TR", Parameters) * 2) + 1;
            PatchSize = AlgorithmParameter.FindValue<int>("PATCH", Parameters) * 2 + 1;
            ImagePyramidScaleFactor = AlgorithmParameter.FindValue<float>("IP_SCALE", Parameters);
            ImagePyramidLevels = AlgorithmParameter.FindValue<int>("IP_LEVEL", Parameters);
        }
    }

}
