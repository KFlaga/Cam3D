using CamCore;

namespace CamAlgorithms.ImageMatching
{
    public class LimitRangeRefiner : DisparityRefinement
    {
        public int MaxLeftDisparity_X { get; set; }
        public int MinLeftDisparity_X { get; set; }
        public int MaxLeftDisparity_Y { get; set; }
        public int MinLeftDisparity_Y { get; set; }
        public int MaxRightDisparity_X { get; set; }
        public int MinRightDisparity_X { get; set; }
        public int MaxRightDisparity_Y { get; set; }
        public int MinRightDisparity_Y { get; set; }

        public override void RefineMaps()
        {
            if(MapLeft != null)
            {
                for(int r = 0; r < MapLeft.RowCount; ++r)
                {
                    for(int c = 0; c < MapLeft.ColumnCount; ++c)
                    {
                        Disparity d = MapLeft[r, c];
                        if(d.DX > MaxLeftDisparity_X ||
                           d.DX < MinLeftDisparity_X ||
                           d.DY > MaxLeftDisparity_Y ||
                           d.DY < MinLeftDisparity_Y)
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
                        Disparity d = MapRight[r, c];
                        if(d.DX > MaxRightDisparity_X ||
                           d.DX < MinRightDisparity_X ||
                           d.DY > MaxRightDisparity_Y ||
                           d.DY < MinRightDisparity_Y)
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

            IntParameter leftMaxXParam = new IntParameter(
                "Left Max Disparity X", "MAX_LEFT_X", 0, -10000, 10000);
            Parameters.Add(leftMaxXParam);

            IntParameter leftMinXParam = new IntParameter(
                "Left Min Disparity X", "MIN_LEFT_X", -100, -10000, 10000);
            Parameters.Add(leftMinXParam);

            IntParameter leftMaxYParam = new IntParameter(
                "Left Max Disparity Y", "MAX_LEFT_Y", 10, -10000, 10000);
            Parameters.Add(leftMaxYParam);

            IntParameter leftMinYParam = new IntParameter(
                "Left Min Disparity Y", "MIN_LEFT_Y", -10, -10000, 10000);
            Parameters.Add(leftMinYParam);

            IntParameter rightMaxXParam = new IntParameter(
                "Right Max Disparity X", "MAX_RIGHT_X", 100, -10000, 10000);
            Parameters.Add(rightMaxXParam);

            IntParameter rightMinXParam = new IntParameter(
                "Right Min Disparity X", "MIN_RIGHT_X", 0, -10000, 10000);
            Parameters.Add(rightMinXParam);

            IntParameter rightMaxYParam = new IntParameter(
                "Right Max Disparity Y", "MAX_RIGHT_Y", 10, -10000, 10000);
            Parameters.Add(rightMaxYParam);

            IntParameter rightMinYParam = new IntParameter(
                "Right Min Disparity Y", "MIN_RIGHT_Y", -10, -10000, 10000);
            Parameters.Add(rightMinYParam);
        }

        public override void UpdateParameters()
        {
            base.UpdateParameters();

            MaxLeftDisparity_X = AlgorithmParameter.FindValue<int>("MAX_LEFT_X", Parameters);
            MinLeftDisparity_X = AlgorithmParameter.FindValue<int>("MIN_LEFT_X", Parameters);
            MaxLeftDisparity_Y = AlgorithmParameter.FindValue<int>("MAX_LEFT_Y", Parameters);
            MinLeftDisparity_Y = AlgorithmParameter.FindValue<int>("MIN_LEFT_Y", Parameters);
            MaxRightDisparity_X = AlgorithmParameter.FindValue<int>("MAX_RIGHT_X", Parameters);
            MinRightDisparity_X = AlgorithmParameter.FindValue<int>("MIN_RIGHT_X", Parameters);
            MaxRightDisparity_Y = AlgorithmParameter.FindValue<int>("MAX_RIGHT_Y", Parameters);
            MinRightDisparity_Y = AlgorithmParameter.FindValue<int>("MIN_RIGHT_Y", Parameters);
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
