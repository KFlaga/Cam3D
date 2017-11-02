using System;
using System.Collections.Generic;
using Cam3dWrapper;
using CamCore;

namespace CamAlgorithms.ImageMatching
{
    public class CppSgmMatchingAlgorithm : ImageMatchingAlgorithm
    {
        public int CensusMaskRadius { get; set; }
        public double LowPenaltyCoeff { get; set; }
        public double HighPenaltyCoeff { get; set; }
        public double GradientCoeff { get; set; }
        public DisparityCostMethod CostMethod { get; set; }
        public DisparityMeanMethod MeanMethod { get; set; }
        public double DiparityPathLengthThreshold { get; set; }

        private SgmMatchingAlgorithm _alg = null;

        public override void MatchImages()
        {
            if(!Rectified)
            {
                throw new Exception("Images for CppSgm must be rectified");
            }

            ConvertImagesToGray();
            SgmParameters p = CreateSgmParameters();

            _alg = new SgmMatchingAlgorithm();
            _alg.Process(p);

            MapLeft = CreateMapFromWrapper(_alg.GetMapLeft());
            MapRight = CreateMapFromWrapper(_alg.GetMapRight());

            _alg = null;
        }

        private SgmParameters CreateSgmParameters()
        {
            SgmParameters p = new SgmParameters();
            p.rows = ImageLeft.RowCount;
            p.cols = ImageLeft.ColumnCount;
            p.imageType = ImageLeft is GrayScaleImage ? ImageType.Grey : ImageType.MaskedGrey;
            p.leftImageWrapper = CreateImageWrapper(ImageLeft);
            p.rightImageWrapper = CreateImageWrapper(ImageRight);

            p.maskRadius = CensusMaskRadius;
            p.lowPenaltyCoeff = LowPenaltyCoeff;
            p.highPenaltyCoeff = HighPenaltyCoeff;
            p.gradientCoeff = GradientCoeff;
            p.disparityCostMethod = CostMethod;
            p.disparityMeanMethod = MeanMethod;
            p.diparityPathLengthThreshold = DiparityPathLengthThreshold;
            return p;
        }

        private IWrapper CreateImageWrapper(IImage img)
        {
            GreyScaleImageWrapper imgGrey = new GreyScaleImageWrapper(img.RowCount, img.ColumnCount);
            imgGrey.SetMatrix(ImageToArray(img));
            if(img is GrayScaleImage)
            {
                return imgGrey;
            }
            else
            {
                GreyMaskedImageWrapper imgMasked = new GreyMaskedImageWrapper(img.RowCount, img.ColumnCount, imgGrey);
                imgMasked.SetMask(MaskToArray(img as MaskedImage));
                return imgMasked;
            }
        }

        private double[,] ImageToArray(IImage img)
        {
            double[,] mat = new double[img.RowCount, img.ColumnCount];
            for(int r = 0; r < img.RowCount; ++r)
            {
                for(int c = 0; c < img.ColumnCount; ++c)
                {
                    mat[r, c] = img[r, c];
                }
            }
            return mat;
        }

        private bool[,] MaskToArray(MaskedImage img)
        {
            bool[,] mask = new bool[img.RowCount, img.ColumnCount];
            for(int r = 0; r < img.RowCount; ++r)
            {
                for(int c = 0; c < img.ColumnCount; ++c)
                {
                    mask[r, c] = img.HaveValueAt(r, c);
                }
            }
            return mask;
        }

        private DisparityMap CreateMapFromWrapper(DisparityMapWrapper wrapper)
        {
            var disps = wrapper.GetDisparities();
            DisparityMap map = new DisparityMap(wrapper.GetRows(), wrapper.GetCols());
            for(int r = 0; r < map.RowCount; ++r)
            {
                for(int c = 0; c < map.ColumnCount; ++c)
                {
                    DisparityWrapper cppDisp = (DisparityWrapper)disps[r, c];
                    map[r, c] = new Disparity()
                    {
                        DX = cppDisp.dx,
                        SubDX = cppDisp.subDx,
                        Cost = cppDisp.cost,
                        Confidence = cppDisp.confidence,
                        Flags = cppDisp.flags
                    };
                }
            }
            return map;
        }

        public override string GetProgress()
        {
            if(_alg != null)
            {
                return _alg.GetStatus();
            }
            return "";
        }

        public override void Terminate()
        {
            _alg.Terminate();
        }

        public override string Name { get { return "Cpp Sgm Image Matching Algorithm"; } }

        public override void InitParameters()
        {
            base.InitParameters();

            IntParameter censusMaskRadiusParam = new IntParameter(
                "Census Mask Radius", "CENSUS_MASK_RADIUS", 6, 1, 7);
            Parameters.Add(censusMaskRadiusParam);

            DoubleParameter lowPenaltyCoeffParam = new DoubleParameter(
                "Sgm Low Penalty Coeff", "LOW_COEFF", 0.02, 0.0, 1.0);
            Parameters.Add(lowPenaltyCoeffParam);

            DoubleParameter highPenaltyCoeffParam = new DoubleParameter(
                "Sgm High Penalty Coeff", "HIGH_COEFF", 0.04, 0.0, 1.0);
            Parameters.Add(highPenaltyCoeffParam);

            DoubleParameter gradientCoeffParam = new DoubleParameter(
                "Sgm Gradient Coeff", "GRAIDENT_COEFF", 0.75, 0.0, 2.0);
            Parameters.Add(gradientCoeffParam);

            DictionaryParameter disparityMeanMethodParam = new DictionaryParameter(
                "Sgm Disparity Cost Computing Method", "MEAN_METHOD");
            disparityMeanMethodParam.ValuesMap = new Dictionary<string, object>()
            {
                { "Simple Average", DisparityMeanMethod.SimpleAverage },
                { "Weighted Average", DisparityMeanMethod.WeightedAverage },
                { "Weighted Average With Path Length", DisparityMeanMethod.WeightedAverageWithPathLength }
            };
            Parameters.Add(disparityMeanMethodParam);

            DictionaryParameter disparityCostMethodParam = new DictionaryParameter(
                "Sgm Disparity Cost Computing Method", "COST_METHOD");
            disparityCostMethodParam.ValuesMap = new Dictionary<string, object>()
            {
                { "E(||d - m||) / n^2", DisparityCostMethod.DistanceToMean },
                { "E(||d - m||^2) / n^4", DisparityCostMethod.DistanceSquredToMean },
                { "E(||d - m||) / n*sqrt(n)", DisparityCostMethod.DistanceSquredToMeanRoot }
            };
            Parameters.Add(disparityCostMethodParam);

            DoubleParameter diparityPathLengthThresholdParam = new DoubleParameter(
                "Sgm Disparity Path Length Threshold For Mean Computing", "PATH_LENGTH_THRESHOLD", 1.0, 0.0, 2.0);
            Parameters.Add(diparityPathLengthThresholdParam);
        }
         
        public override void UpdateParameters()
        {
            base.UpdateParameters();

            CensusMaskRadius = AlgorithmParameter.FindValue<int>("CENSUS_MASK_RADIUS", Parameters);
            LowPenaltyCoeff = AlgorithmParameter.FindValue<double>("LOW_COEFF", Parameters);
            HighPenaltyCoeff = AlgorithmParameter.FindValue<double>("HIGH_COEFF", Parameters);
            GradientCoeff = AlgorithmParameter.FindValue<double>("GRAIDENT_COEFF", Parameters);
            MeanMethod = AlgorithmParameter.FindValue<DisparityMeanMethod>("MEAN_METHOD", Parameters);
            CostMethod = AlgorithmParameter.FindValue<DisparityCostMethod>("COST_METHOD", Parameters);
            DiparityPathLengthThreshold = AlgorithmParameter.FindValue<double>("PATH_LENGTH_THRESHOLD", Parameters);
        }
    }
}
