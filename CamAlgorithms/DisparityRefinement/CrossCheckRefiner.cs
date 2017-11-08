using CamCore;

namespace CamAlgorithms.ImageMatching
{
    public class CrossCheckRefiner : DisparityRefinement
    {
        public double MaxDisparityDiff { get; set; }

        public override void RefineMaps()
        {
            if(MapLeft != null && MapRight != null)
            {
                for(int r = 0; r < MapLeft.RowCount; ++r)
                {
                    for(int c = 0; c < MapLeft.ColumnCount; ++c)
                    {
                        Disparity dispLeft = MapLeft[r, c];
                        if(dispLeft.IsValid())
                        {
                            CrossCheckPixel(r, c, dispLeft);
                        }
                    }
                }
            }
        }

        private void CrossCheckPixel(int r, int c, Disparity dispLeft)
        {
            IntVector2 rightPixel = dispLeft.GetMatchedPixel(new IntVector2(c, r));
            Disparity dispRight = MapRight[rightPixel.Y, rightPixel.X];

            if(dispRight.IsValid())
            {
                if(CheckDisparitiesAreFar(dispLeft, dispRight))
                {
                    MapLeft[r, c].Flags = (int)DisparityFlags.Invalid;
                    MapRight[rightPixel.Y, rightPixel.X].Flags = (int)DisparityFlags.Invalid;
                }
                else
                {
                    SetAverageDisparityForBothMaps(r, c, rightPixel);
                }
            }
            //else
            //{
            //    MapLeft[r, c].Flags = (int)DisparityFlags.Invalid;
            //}
        }

        private void SetAverageDisparityForBothMaps(int r, int c, IntVector2 rightPixel)
        {
            double subDx = (MapLeft[r, c].SubDX - MapRight[rightPixel.Y, rightPixel.X].SubDX) * 0.5;
            MapLeft[r, c].SubDX = subDx;
            MapRight[rightPixel.Y, rightPixel.X].SubDX = -subDx;
            MapLeft[r, c].DX = subDx.Round();
            MapRight[rightPixel.Y, rightPixel.X].DX = -MapLeft[r, c].DX;
        }

        private bool CheckDisparitiesAreFar(Disparity dispLeft, Disparity dispRight)
        {
            double pixDistance = (dispLeft.DX + dispRight.DX);
            double subDistance = (dispLeft.SubDX + dispRight.SubDX);
            return pixDistance > MaxDisparityDiff && subDistance > MaxDisparityDiff;
        }

        public override void InitParameters()
        {
            base.InitParameters();
            
            Parameters.Add(new DoubleParameter(
                "Max Disparity Difference", "MaxDisparityDiff", 1.5, 0.0, 10000.0));
        }

        public override void UpdateParameters()
        {
            base.UpdateParameters();
            MaxDisparityDiff = AlgorithmParameter.FindValue<double>("MaxDisparityDiff", Parameters);
        }

        public override string Name
        {
            get
            {
                return "Cross validation";
            }
        }
    }
}
