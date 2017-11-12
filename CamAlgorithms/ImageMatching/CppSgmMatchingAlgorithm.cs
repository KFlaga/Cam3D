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

        private SgmMatchingAlgorithm _cppSgm = null;

        public override void MatchImages()
        {
            if(!Rectified)
            {
                throw new Exception("Images for CppSgm must be rectified");
            }

            ConvertImagesToGray();
            SgmParameters p = CreateSgmParameters();

            _cppSgm = new SgmMatchingAlgorithm();
            _cppSgm.Process(p);

            MapLeft = CreateMapFromWrapper(_cppSgm.GetMapLeft());
            MapRight = CreateMapFromWrapper(_cppSgm.GetMapRight());

            _cppSgm = null;
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
            if(_cppSgm != null)
            {
                return _cppSgm.GetStatus();
            }
            return "";
        }

        public override void Terminate()
        {
            if(_cppSgm != null)
            {
                _cppSgm.Terminate();
            }
        }

        public override string Name { get { return "Cpp Sgm Image Matching Algorithm"; } }

        public override void InitParameters()
        {
            base.InitParameters();
            
            Parameters.Add(new IntParameter(
                "Census Mask Radius", "CensusMaskRadius", 6, 1, 7));
            Parameters.Add(new DoubleParameter(
                "Sgm Low Penalty Coeff", "LowPenaltyCoeff", 0.02, 0.0, 1.0));     
            Parameters.Add(new DoubleParameter(
                "Sgm High Penalty Coeff", "HighPenaltyCoeff", 0.04, 0.0, 1.0));
            Parameters.Add(new DoubleParameter(
                "Sgm Gradient Coeff", "GradientCoeff", 0.75, 0.0, 2.0));

            DictionaryParameter disparityMeanMethodParam = new DictionaryParameter(
                "Sgm Disparity Cost Computing Method", "MeanMethod");
            disparityMeanMethodParam.ValuesMap = new Dictionary<string, object>()
            {
                { "Simple Average", DisparityMeanMethod.SimpleAverage },
                { "Weighted Average", DisparityMeanMethod.WeightedAverage },
                { "Weighted Average With Path Length", DisparityMeanMethod.WeightedAverageWithPathLength }
            };
            Parameters.Add(disparityMeanMethodParam);

            DictionaryParameter disparityCostMethodParam = new DictionaryParameter(
                "Sgm Disparity Cost Computing Method", "CostMethod");
            disparityCostMethodParam.ValuesMap = new Dictionary<string, object>()
            {
                { "E(||d - m||) / n^2", DisparityCostMethod.DistanceToMean },
                { "E(||d - m||^2) / n^4", DisparityCostMethod.DistanceSquredToMean },
                { "E(||d - m||) / n*sqrt(n)", DisparityCostMethod.DistanceSquredToMeanRoot }
            };
            Parameters.Add(disparityCostMethodParam);
            
            Parameters.Add(new DoubleParameter(
                "Sgm Disparity Path Length Threshold For Mean Computing", "DiparityPathLengthThreshold", 1.0, 0.0, 2.0));
        }
         
        public override void UpdateParameters()
        {
            base.UpdateParameters();

            CensusMaskRadius = IAlgorithmParameter.FindValue<int>("CensusMaskRadius", Parameters);
            LowPenaltyCoeff = IAlgorithmParameter.FindValue<double>("LowPenaltyCoeff", Parameters);
            HighPenaltyCoeff = IAlgorithmParameter.FindValue<double>("HighPenaltyCoeff", Parameters);
            GradientCoeff = IAlgorithmParameter.FindValue<double>("GradientCoeff", Parameters);
            MeanMethod = IAlgorithmParameter.FindValue<DisparityMeanMethod>("MeanMethod", Parameters);
            CostMethod = IAlgorithmParameter.FindValue<DisparityCostMethod>("CostMethod", Parameters);
            DiparityPathLengthThreshold = IAlgorithmParameter.FindValue<double>("DiparityPathLengthThreshold", Parameters);
        }
    }
}
