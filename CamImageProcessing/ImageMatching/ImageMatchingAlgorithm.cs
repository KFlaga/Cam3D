using System;
using System.Collections.Generic;
using System.Text;
using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace CamImageProcessing.ImageMatching
{
    public class ImageMatchingAlgorithm : IParameterizable
    {
        public CostAggregator Aggregator { get; protected set; }

        public DisparityMap MapLeft { get; set; }
        public DisparityMap MapRight { get; set; }

        public IImage ImageLeft { get; set; }
        public IImage ImageRight { get; set; }

        public bool Rectified { get; set; }
        
        public void MatchImages()
        {
            if(Rectified)
            {
                Aggregator.Fundamental = new DenseMatrix(3, 3);
                Aggregator.Fundamental[1, 2] = -1;
                Aggregator.Fundamental[2, 1] = 1;
            }
            else
            {
                Aggregator.Fundamental = CalibrationData.Data.Fundamental;
            }

            MapLeft = new DisparityMap(ImageLeft.RowCount, ImageLeft.ColumnCount);
            Aggregator.DisparityMap = MapLeft;
            Aggregator.ImageBase = ImageLeft;
            Aggregator.ImageMatched = ImageRight;
            Aggregator.IsLeftImageBase = true;

            Aggregator.Init();
            if(Rectified)
            {
                Aggregator.ComputeMatchingCosts_Rectified();
            }
            else
            {
                Aggregator.ComputeMatchingCosts();
            }

            MapRight = new DisparityMap(ImageRight.RowCount, ImageRight.ColumnCount);
            Aggregator.DisparityMap = MapRight;
            Aggregator.ImageBase = ImageRight;
            Aggregator.ImageMatched = ImageLeft;
            Aggregator.IsLeftImageBase = false;

            Aggregator.Init();
            if(Rectified)
            {
                Aggregator.ComputeMatchingCosts_Rectified();
            }
            else
            {
                Aggregator.ComputeMatchingCosts();
            }

        }

        public string Name { get { return "Image Matching Algorithm"; } }

        List<AlgorithmParameter> _params = new List<AlgorithmParameter>();
        public List<AlgorithmParameter> Parameters
        {
            get { return _params; }
        }

        public void InitParameters()
        {
            _params = new List<AlgorithmParameter>();

            ParametrizedObjectParameter aggregatorParam = new ParametrizedObjectParameter(
                "Cost Aggregator", "CAGG");

            aggregatorParam.Parameterizables = new List<IParameterizable>();
            var epi = new EpilineScanAggregator();
            epi.InitParameters();
            aggregatorParam.Parameterizables.Add(epi);

            var img = new WholeImageScan();
            img.InitParameters();
            aggregatorParam.Parameterizables.Add(img);

            var sgm = new SGMAggregator();
            sgm.InitParameters();
            aggregatorParam.Parameterizables.Add(sgm);

            _params.Add(aggregatorParam);
        
            BooleanParameter rectParam = new BooleanParameter(
                "Images Rectified", "RECT", true);
            _params.Add(rectParam);
        }

        public void UpdateParameters()
        {
            Aggregator = AlgorithmParameter.FindValue<CostAggregator>("CAGG", _params);
            Aggregator.UpdateParameters();

            Rectified = AlgorithmParameter.FindValue<bool>("RECT", _params);
        }
    }
}

