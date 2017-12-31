using CamCore;
using System;

namespace CamAlgorithms.ImageMatching
{
    public class LimitRangeRefiner : DisparityRefinement
    {
        public int MaxDisparity { get; set; }
        public int MinDisparity { get; set; }
        
        public override void RefineMaps()
        {
            if(MapLeft != null)
            {
                LimitMap(MapLeft);
            }

            if(MapRight != null)
            {
                LimitMap(MapRight);
            }
        }

        private void LimitMap(DisparityMap map)
        {
            for(int r = 0; r < map.RowCount; ++r)
            {
                for(int c = 0; c < map.ColumnCount; ++c)
                {
                    Disparity d = map[r, c];
                    if(Math.Abs(d.DX) > MaxDisparity || Math.Abs(d.DX) < MinDisparity)
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
                "Max Disparity X", "MaxDisparity", 100, 0, 100000));
            Parameters.Add(new IntParameter(
                "Min Disparity X", "MinDisparity", 1, 0, 100000));
        }

        public override void UpdateParameters()
        {
            base.UpdateParameters();

            MaxDisparity = IAlgorithmParameter.FindValue<int>("MaxDisparity", Parameters);
            MinDisparity = IAlgorithmParameter.FindValue<int>("MinDisparity", Parameters);
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
