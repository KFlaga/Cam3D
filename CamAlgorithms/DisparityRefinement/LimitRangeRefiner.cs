using CamCore;

namespace CamAlgorithms.ImageMatching
{
    public class LimitRangeRefiner : DisparityRefinement
    {
        public int MaxLeftDisparity { get; set; }
        public int MinLeftDisparity { get; set; }
        public int MaxRightDisparity { get; set; }
        public int MinRightDisparity { get; set; }
        
        public override void RefineMaps()
        {
            if(MapLeft != null)
            {
                LimitMap(MapLeft, MinLeftDisparity, MaxLeftDisparity);
            }

            if(MapRight != null)
            {
                LimitMap(MapRight, MinRightDisparity, MaxRightDisparity);
            }
        }

        private void LimitMap(DisparityMap map, int dmin, int dmax)
        {
            for(int r = 0; r < map.RowCount; ++r)
            {
                for(int c = 0; c < map.ColumnCount; ++c)
                {
                    Disparity d = map[r, c];
                    if(d.DX > dmax || d.DX < dmin)
                    {
                        map[r, c].Flags = (int)DisparityFlags.Invalid;
                    }
                }
            }
        }
        
        public override void InitParameters()
        {
            base.InitParameters();
            
            Parameters.Add(new IntParameter(
                "Left Max Disparity X", "MaxLeftDisparity", 0, -10000, 10000));
            Parameters.Add(new IntParameter(
                "Left Min Disparity X", "MinLeftDisparity", -100, -10000, 10000));
            Parameters.Add(new IntParameter(
                "Right Max Disparity X", "MaxRightDisparity", 100, -10000, 10000));
            Parameters.Add(new IntParameter(
                "Right Min Disparity X", "MinRightDisparity", 0, -10000, 10000));
        }

        public override void UpdateParameters()
        {
            base.UpdateParameters();

            MaxLeftDisparity = IAlgorithmParameter.FindValue<int>("MaxLeftDisparity", Parameters);
            MinLeftDisparity = IAlgorithmParameter.FindValue<int>("MinLeftDisparity", Parameters);
            MaxRightDisparity = IAlgorithmParameter.FindValue<int>("MaxRightDisparity", Parameters);
            MinRightDisparity = IAlgorithmParameter.FindValue<int>("MinRightDisparity", Parameters);
        }

        public override string Name
        {
            get
            {
                return "Limit Disparity Range";
            }
        }
    }
}
