using System.Collections.Generic;
using CamCore;
using MathNet.Numerics.LinearAlgebra.Double;

namespace CamAlgorithms.ImageMatching
{
    public class GenericImageMatchingAlgorithm : ImageMatchingAlgorithm
    {
        public CostAggregator Aggregator { get; set; }
        
        public override void MatchImages()
        {
            ConvertImagesToGray();

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
            if(Rectified)
            {
                Aggregator.ComputeMatchingCosts_Rectified();
            }
            else
            {
                Aggregator.ComputeMatchingCosts();
            }
            return Aggregator.DisparityMap;
        }

        public override string GetProgress()
        {
            return "Run: " + (Aggregator.IsLeftImageBase ? "1" : "2") + ". Pixel: (" +
                Aggregator.CurrentPixel.X + ", " + Aggregator.CurrentPixel.Y +
                ") of [" + ImageLeft.ColumnCount + ", " + ImageLeft.RowCount + "].";
        }

        public override void Terminate()
        {

        }

        public override string Name { get { return "Image Matching Algorithm"; } }
        
        public override void InitParameters()
        {
            base.InitParameters();

            ParametrizedObjectParameter aggregatorParam = new ParametrizedObjectParameter(
                "Cost Aggregator", "COST_AGGREGATOR");

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

            Parameters.Add(aggregatorParam);
        
            BooleanParameter rectParam = new BooleanParameter(
                "Images Rectified", "IS_RECTIFIED", true);
            Parameters.Add(rectParam);
        }

        public override void UpdateParameters()
        {
            base.UpdateParameters();

            Aggregator = AlgorithmParameter.FindValue<CostAggregator>("COST_AGGREGATOR", Parameters);
            Aggregator.UpdateParameters();

            Rectified = AlgorithmParameter.FindValue<bool>("IS_RECTIFIED", Parameters);
        }
    }
}

