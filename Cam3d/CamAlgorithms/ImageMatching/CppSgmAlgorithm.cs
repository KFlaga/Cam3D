using System;
using System.Collections.Generic;
using Cam3dWrapper;
using CamCore;

namespace CamAlgorithms.ImageMatching
{
    public class CppSgmAlgorithm : DenseMatchingAlgorithm
    {
        public int MaxParallelTasks { get; set; }
        public int MaxDisparity { get; set; }
        public int CensusMaskRadius { get; set; }
        public double LowPenaltyCoeff { get; set; }
        public double HighPenaltyCoeff { get; set; }
        public double IntensityThreshold { get; set; }
        public DisparityCostMethod CostMethod { get; set; }
        public DisparityMeanMethod MeanMethod { get; set; }
        public int DiparityPathLengthThreshold { get; set; }
        public double CostMethodPower { get; set; }

        private Cam3dWrapper.SgmMatchingAlgorithm _cppSgm = null;

        public override void MatchImages()
        {
            ConvertImagesToGray();
            SgmParameters p = CreateSgmParameters();

            _cppSgm = new Cam3dWrapper.SgmMatchingAlgorithm();
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

            p.maxParallelTasks = MaxParallelTasks;
            p.maxDisparity = MaxDisparity < 0 ? ImageLeft.ColumnCount : MaxDisparity;
            p.censusMaskRadius = CensusMaskRadius;
            p.lowPenaltyCoeff = LowPenaltyCoeff;
            p.highPenaltyCoeff = HighPenaltyCoeff;
            p.intensityThreshold = IntensityThreshold;
            p.disparityCostMethod = CostMethod;
            p.disparityMeanMethod = MeanMethod;
            p.diparityPathLengthThreshold = DiparityPathLengthThreshold;
            p.costMethodPower = CostMethodPower;
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
                "High Disparity Intensity Threshold", "InstenistyThreshold", 0.1, 0.0, 1.0));

            DictionaryParameter disparityMeanMethodParam = new DictionaryParameter(
                "Sgm Disparity Cost Computing Method", "MeanMethod");
            disparityMeanMethodParam.ValuesMap = new Dictionary<string, object>()
            {
                { "Simple Average", DisparityMeanMethod.SimpleAverage },
                { "Weighted Average With Path Length", DisparityMeanMethod.WeightedAverageWithPathLength }
            };
            Parameters.Add(disparityMeanMethodParam);

            DictionaryParameter disparityCostMethodParam = new DictionaryParameter(
                "Sgm Disparity Cost Computing Method", "CostMethod");
            disparityCostMethodParam.ValuesMap = new Dictionary<string, object>()
            {
                { "E(||d - m||) / n^x", DisparityCostMethod.DistanceToMean },
                { "E(||d - m||^2) / n^x", DisparityCostMethod.DistanceSquredToMean },
            };
            Parameters.Add(disparityCostMethodParam);
            
            Parameters.Add(new IntParameter(
                "Sgm Disparity Path Length Threshold For Mean Computing", "DiparityPathLengthThreshold", 3, 1, 1000));
            Parameters.Add(new DoubleParameter(
                "Cost Method Coefficient", "CostMethodPower", 2.0, 0.1, 10.0));
            Parameters.Add(new IntParameter(
                "Max Disparity", "MaxDisparity", -1, -1, 10000));
            Parameters.Add(new IntParameter(
                "Max Parallel Tasks", "MaxParallelTasks", 2, 1, 100));
        }
         
        public override void UpdateParameters()
        {
            base.UpdateParameters();

            MaxParallelTasks = IAlgorithmParameter.FindValue<int>("MaxParallelTasks", Parameters);
            MaxDisparity = IAlgorithmParameter.FindValue<int>("MaxDisparity", Parameters);
            CensusMaskRadius = IAlgorithmParameter.FindValue<int>("CensusMaskRadius", Parameters);
            LowPenaltyCoeff = IAlgorithmParameter.FindValue<double>("LowPenaltyCoeff", Parameters);
            HighPenaltyCoeff = IAlgorithmParameter.FindValue<double>("HighPenaltyCoeff", Parameters);
            IntensityThreshold = IAlgorithmParameter.FindValue<double>("InstenistyThreshold", Parameters);
            MeanMethod = IAlgorithmParameter.FindValue<DisparityMeanMethod>("MeanMethod", Parameters);
            CostMethod = IAlgorithmParameter.FindValue<DisparityCostMethod>("CostMethod", Parameters);
            DiparityPathLengthThreshold = IAlgorithmParameter.FindValue<int>("DiparityPathLengthThreshold", Parameters);
            CostMethodPower = IAlgorithmParameter.FindValue<double>("CostMethodPower", Parameters);
        }
    }
}
