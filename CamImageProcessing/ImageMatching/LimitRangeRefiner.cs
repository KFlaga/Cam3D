using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CamCore;

namespace CamImageProcessing.ImageMatching
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
                        if(MapLeft[r, c].DX > MaxLeftDisparity_X || 
                           MapLeft[r, c].DX < MinLeftDisparity_X ||
                           MapLeft[r, c].DY > MaxLeftDisparity_Y ||
                           MapLeft[r, c].DY < MinLeftDisparity_Y)
                        {
                            MapLeft.Set(r, c, new Disparity()
                            {
                                Flags = (int)DisparityFlags.Invalid
                            });
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
                        if(MapRight[r, c].DX > MaxRightDisparity_X ||
                           MapRight[r, c].DX < MinRightDisparity_X ||
                           MapRight[r, c].DY > MaxRightDisparity_Y ||
                           MapRight[r, c].DY < MinRightDisparity_Y)
                        {
                            MapRight.Set(r, c, new Disparity()
                            {
                                Flags = (int)DisparityFlags.Invalid
                            });
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

        public override string ToString()
        {
            return "Limit Disparity Range";
        }
    }
}
