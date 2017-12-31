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
                            CrossCheckLeftPixel(r, c, dispLeft);
                        }
                        Disparity dispRight = MapRight[r, c];
                        if(dispRight.IsValid())
                        {
                            CrossCheckRightPixel(r, c, dispRight);
                        }
                    }
                }
            }
        }

        private void CrossCheckLeftPixel(int r, int c, Disparity dispLeft)
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
                    SetAverageDisparityForBothMaps(new IntVector2(c, r), rightPixel);
                }
            }
            else
            {
                MapRight[rightPixel.Y, rightPixel.X].Flags = (int)DisparityFlags.Valid;
                MapRight[rightPixel.Y, rightPixel.X].DX = -dispLeft.DX;
                MapRight[rightPixel.Y, rightPixel.X].SubDX = -dispLeft.SubDX;
            }
        }

        private void CrossCheckRightPixel(int r, int c, Disparity dispRight)
        {
            IntVector2 leftPixel = dispRight.GetMatchedPixel(new IntVector2(c, r));
            Disparity dispLeft = MapLeft[leftPixel.Y, leftPixel.X];

            if(dispLeft.IsValid())
            {
                if(CheckDisparitiesAreFar(dispLeft, dispRight))
                {
                    MapLeft[r, c].Flags = (int)DisparityFlags.Invalid;
                    MapRight[leftPixel.Y, leftPixel.X].Flags = (int)DisparityFlags.Invalid;
                }
                else
                {
                    SetAverageDisparityForBothMaps(leftPixel, new IntVector2(c, r));
                }
            }
            else
            {
                MapLeft[leftPixel.Y, leftPixel.X].Flags = (int)DisparityFlags.Valid;
                MapLeft[leftPixel.Y, leftPixel.X].DX = -dispRight.DX;
                MapLeft[leftPixel.Y, leftPixel.X].SubDX = -dispRight.SubDX;
            }
        }

        private void SetAverageDisparityForBothMaps(IntVector2 leftPixel, IntVector2 rightPixel)
        {
            double subDx = (MapLeft[leftPixel.Y, leftPixel.X].SubDX - MapRight[rightPixel.Y, rightPixel.X].SubDX) * 0.5;
            MapLeft[leftPixel.Y, leftPixel.X].SubDX = subDx;
            MapRight[rightPixel.Y, rightPixel.X].SubDX = -subDx;
            MapLeft[leftPixel.Y, leftPixel.X].DX = subDx.Round();
            MapRight[rightPixel.Y, rightPixel.X].DX = -MapLeft[leftPixel.Y, leftPixel.X].DX;
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
            MaxDisparityDiff = IAlgorithmParameter.FindValue<double>("MaxDisparityDiff", Parameters);
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
