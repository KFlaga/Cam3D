using CamCore;

namespace CamAlgorithms.ImageMatching
{
    public class InvalidateLowConfidenceRefiner : DisparityRefinement
    {
        public double ConfidenceTreshold;

        public override void RefineMaps()
        {
            if(MapLeft != null)
            {
                for(int r = 0; r < MapLeft.RowCount; ++r)
                {
                    for(int c = 0; c < MapLeft.ColumnCount; ++c)
                    {
                        if(MapLeft[r, c].Confidence < ConfidenceTreshold)
                        {
                            MapLeft[r, c].Flags = (int)DisparityFlags.Invalid;
                        }
                    }
                }
            }

            if(MapRight != null)
            {
                for(int r = 0; r < MapRight.RowCount; ++r)
                {
                    for(int c = 0; c < MapRight.ColumnCount; ++c)
                    {
                        if(MapRight[r, c].Confidence < ConfidenceTreshold)
                        {
                            MapRight[r, c].Flags = (int)DisparityFlags.Invalid;
                        }
                    }
                }
            }
        }

        public override void InitParameters()
        {
            base.InitParameters();

            DoubleParameter ctreshParam = new DoubleParameter(
                "Minimum Confidence", "CONF_TRESH", 0.25, 0.0, 1.0);
            Parameters.Add(ctreshParam);
        }

        public override void UpdateParameters()
        {
            base.UpdateParameters();

            ConfidenceTreshold = AlgorithmParameter.FindValue<double>("CONF_TRESH", Parameters);
        }

        public override string Name
        {
            get
            {
                return "Invalidate Low Confidence";
            }
        }
    }
}
