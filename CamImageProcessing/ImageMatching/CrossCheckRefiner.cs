using CamCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamImageProcessing.ImageMatching
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
                            IntVector2 rightPixel = dispLeft.GetMatchedPixel(new IntVector2(c, r));
                            Disparity dispRight = MapRight[rightPixel.Y, rightPixel.X];

                            if(dispRight.IsValid())
                            {
                                // Check if both disparities are close
                                double pixDistance = (dispLeft.DX + dispRight.DX) * (dispLeft.DX + dispRight.DX) +
                                    (dispLeft.DY + dispRight.DY) * (dispLeft.DY + dispRight.DY);
                                double subDistance = (dispLeft.SubDX + dispRight.SubDX) * (dispLeft.SubDX + dispRight.SubDX) +
                                    (dispLeft.SubDY + dispRight.SubDY) * (dispLeft.SubDY + dispRight.SubDY);

                                if(pixDistance > MaxDisparityDiff && subDistance > MaxDisparityDiff)
                                {
                                    // Disparities are far -> invalidate
                                    MapLeft[r, c].Flags = (int)DisparityFlags.Invalid;
                                    MapRight[rightPixel.Y, rightPixel.X].Flags = (int)DisparityFlags.Invalid;
                                }
                                else
                                {
                                    // Set both disparities (sub ones) to be on middle
                                    double subDx = (MapLeft[r, c].SubDX - MapRight[rightPixel.Y, rightPixel.X].SubDX) * 0.5;
                                    double subDy = (MapLeft[r, c].SubDY - MapRight[rightPixel.Y, rightPixel.X].SubDY) * 0.5;
                                    MapLeft[r, c].SubDX = subDx;
                                    MapLeft[r, c].SubDY = subDy;
                                    MapRight[rightPixel.Y, rightPixel.X].SubDX = -subDx;
                                    MapRight[rightPixel.Y, rightPixel.X].SubDY = -subDy;
                                    MapLeft[r, c].DX = subDx.Round();
                                    MapLeft[r, c].DY = subDy.Round();
                                    MapRight[rightPixel.Y, rightPixel.X].DX = -MapLeft[r, c].DX;
                                    MapRight[rightPixel.Y, rightPixel.X].DY = -MapLeft[r, c].DY;
                                }
                            }
                            else
                            {
                                MapLeft[r, c].Flags = (int)DisparityFlags.Invalid;
                            }
                        }
                    }
                }
            }
        }

        public override void InitParameters()
        {
            base.InitParameters();

            DoubleParameter maxDiffParam = new DoubleParameter(
                "Max Disparity Difference (Squared)", "DIFF", 2.0, 0.0, 10000.0);
            Parameters.Add(maxDiffParam);
        }

        public override void UpdateParameters()
        {
            base.UpdateParameters();
            MaxDisparityDiff = AlgorithmParameter.FindValue<double>("DIFF", Parameters);
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
