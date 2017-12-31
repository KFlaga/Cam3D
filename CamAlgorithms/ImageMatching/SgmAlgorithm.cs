using System.Collections.Generic;
using CamCore;
using MathNet.Numerics.LinearAlgebra.Double;
using CamAlgorithms.Calibration;

namespace CamAlgorithms.ImageMatching
{
    public class SgmAlgorithm : DenseMatchingAlgorithm
    {
        public CostAggregator Aggregator { get; set; }
        
        public override void MatchImages()
        {
            ConvertImagesToGray();
            
            MapLeft = MatchImages(true);
            MapRight = MatchImages(false);
        }

        private DisparityMap MatchImages(bool isLeftBase)
        {
            Aggregator.DisparityMap = new DisparityMap(ImageRight.RowCount, ImageRight.ColumnCount);
            Aggregator.ImageBase = isLeftBase ? ImageLeft : ImageRight;
            Aggregator.ImageMatched = isLeftBase ? ImageRight : ImageLeft;
            Aggregator.IsLeftImageBase = isLeftBase;

            Aggregator.Init();
            Aggregator.ComputeMatchingCosts();
            return Aggregator.DisparityMap;
        }

        public override string GetProgress()
        {
            return "Run: " + (Aggregator.IsLeftImageBase ? "Left" : "Right") + ". Pixel: (" +
                Aggregator.CurrentPixel.X + ", " + Aggregator.CurrentPixel.Y +
                ") of [" + ImageLeft.ColumnCount + ", " + ImageLeft.RowCount + "].";
        }

        public override void Terminate()
        {

        }

        public override string Name { get { return "Semi-Global Matching Algorithm"; } }
        
        public override void InitParameters()
        {
            base.InitParameters();

            ParametrizedObjectParameter aggregatorParam = new ParametrizedObjectParameter(
                "Cost Aggregator", "Aggregator");

            aggregatorParam.Parameterizables = new List<IParameterizable>();
            var sgm = new SgmAggregator();
            sgm.InitParameters();
            aggregatorParam.Parameterizables.Add(sgm);
            
            Parameters.Add(aggregatorParam);
        }

        public override void UpdateParameters()
        {
            base.UpdateParameters();

            Aggregator = IAlgorithmParameter.FindValue<CostAggregator>("Aggregator", Parameters);
            Aggregator.UpdateParameters();
        }
    }
}

