using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamImageProcessing.ImageMatching
{
    public class EpilineScanAggregator : CostAggregator
    {
        public override void ComputeMatchingCosts()
        {
            // For each pixel pb in ImageBase:
            // 1) Find corresponding epiline on matched image
            // == 2) Find y0 = e(x=0) (point on epiline with x = 0) == NOPE
            // 3) Find xmax = min(Im.cols, xd=e(y=Im.rows)) (point where epilines corsses border)
            // 4) For each xd = [0, 1, ... , xmax-1]
            // 4.1) find yd0 = e(x=xd) and yd1 = e(x=xd+1)
            // === 4b) if (int)yd0 == (int)yd1 : skip ( it will be later or was checked already) === NOPE
            // 4.2) For each yd = [(int)yd0, (int)yd1] (may also use range [(int)yd0-1, (int)yd1+1] - or [(int)yd0+1, (int)yd1-1] if yd0 > yd1)
            //  - Set disparity D = (px-xd,px-yd) (or (xd,yd))
            //  - Compute cost for disparity

            int xmax;
            int x0;
            for(int c = 0; c < ImageBase.ColumnCount; ++c)
            {
                for(int r = 0; r < ImageBase.RowCount; ++r)
                {
                    Vector2 pb_d = new Vector2(x: c, y: r);
                    CurrentPixel = new IntVector2(x: c, y: r);
                    Vector<double> epiLine = IsLeftImageBase ?
                        FindCorrespondingEpiline_OnRightImage(pb_d) : FindCorrespondingEpiline_OnLeftImage(pb_d);

                    if(Math.Abs(epiLine[0]) < Math.Abs(epiLine[1]) * 1e-12)
                    {
                        // Horizotal
                        FindDisparitiesCosts_HorizontalEpiline(CurrentPixel, r);
                    }
                    else if(Math.Abs(epiLine[1]) < Math.Abs(epiLine[0]) * 1e-12)
                    {
                        // Vertical
                        FindDisparitiesCosts_VerticalEpiline(CurrentPixel, c);
                    }
                    else
                    {
                        // FindDisparitiesCosts(CurrentPixel, epiLine);
                        epiLine.DivideThis(epiLine.At(1));
                        IntVector2 pm = new IntVector2();
                        xmax = FindXmax(epiLine);
                        x0 = FindX0(epiLine);
                        xmax = xmax - 1;

                        for(int xm = x0 + 1; xm < xmax; ++xm)
                        {
                            double ym0 = FindYd(xm, epiLine);
                            double ym1 = FindYd(xm + 1, epiLine);
                            for(int ym = (int)ym0; ym < (int)ym1; ++ym)
                            {
                                pm.X = xm;
                                pm.Y = ym;
                                double cost = CostComp.GetCost_Border(CurrentPixel, pm);
                                DispComp.StoreDisparity(CurrentPixel, pm, cost);
                            }
                        }
                    }

                    DispComp.FinalizeForPixel(CurrentPixel);
                }
            }
        }

        private void FindDisparitiesCosts(IntVector2 pb, Vector<double> epiLine)
        {
            IntVector2 pm = new IntVector2();
            int xmax = FindXmax(epiLine);
            xmax = xmax - 1;

            for(int xm = FindX0(epiLine) + 1; xm < xmax; ++xm)
            {
                double ym0 = FindYd(xm, epiLine);
                double ym1 = FindYd(xm + 1, epiLine);
                for(int ym = (int)ym0; ym < (int)ym1; ++ym)
                {
                    pm.X = xm;
                    pm.Y = ym;
                    double cost = CostComp.GetCost_Border(pb, pm);
                    DispComp.StoreDisparity(CurrentPixel, pm, cost);
                }
            }
        }

        private void FindDisparitiesCosts_HorizontalEpiline(IntVector2 pb, int y)
        {
            IntVector2 pm = new IntVector2();

            for(int xm = 0; xm < ImageBase.ColumnCount; ++xm)
            {
                pm.X = xm;
                pm.Y = y;
                double cost = CostComp.GetCost_Border(pb, pm);
                DispComp.StoreDisparity(CurrentPixel, pm, cost);
            }
        }

        private void FindDisparitiesCosts_VerticalEpiline(IntVector2 pb, int x)
        {
            IntVector2 pm = new IntVector2();

            for(int ym = 0; ym < ImageBase.RowCount; ++ym)
            {
                pm.X = x;
                pm.Y = ym;
                double cost = CostComp.GetCost_Border(pb, pm);
                DispComp.StoreDisparity(CurrentPixel, pm, cost);
            }
        }

        public override void ComputeMatchingCosts_Rectified()
        {
            // For each pixel pb in ImageBase:
            // 1) Find corresponding epiline on matched image (as its recified its just x' = px)
            // 2) For each xd = [0, 1, ... , xmax] (xmax = Im.cols)
            //  - Set disparity D = (px-xd, 0) (or (xd,yd))
            //  - Compute cost for disparity
            //  - Save only best one, but delegate each disparity to DisparityComputer
            // 3) 2 may be repeated for yd = 1/-1
            for(int c = 0; c < ImageBase.ColumnCount; ++c)
            {
                for(int r = 0; r < ImageBase.RowCount; ++r)
                {
                    Vector2 pb_d = new Vector2(x: c, y: r);
                    CurrentPixel = new IntVector2(x: c, y: r);
                    FindDisparitiesCosts_HorizontalEpiline(CurrentPixel, r);
                    DispComp.FinalizeForPixel(CurrentPixel);
                }
            }
        }

        public double FindYd(double xd, Vector<double> epiLine)
        {
            // Ax + By + C = 0 => y = -(Ax+C)/B
            return -(epiLine[0] * xd + epiLine[2]) / epiLine[1];
        }

        public double FindXd(double yd, Vector<double> epiLine)
        {
            // Ax + By + C = 0 => x = -(By+C)/A
            return -(epiLine[1] * yd + epiLine[2]) / epiLine[0];
        }

        public int FindXmax(Vector<double> epiLine)
        {
            // 3) Find xmax = min(Im.cols, max(xd=e(y=Im.rows),xd=e(y=0.0))) (point where epilines corsses border 2nd time)
            return (int)(Math.Min((double)ImageMatched.ColumnCount,
               Math.Max(FindXd((double)ImageMatched.RowCount, epiLine), FindXd(0.0, epiLine))));
        }

        public int FindX0(Vector<double> epiLine)
        {
            // 3) Find x0 = max(0, xd=e(y=Im.rows)xd=e(y=0.0)) (point where epilines corsses border 1st time)
            return (int)(Math.Max(0.0, Math.Min(FindXd((double)ImageMatched.RowCount, epiLine), FindXd(0.0, epiLine))));
        }

        public Vector<double> FindCorrespondingEpiline_OnRightImage(Vector2 point)
        {
            // Epiline on right image is given by l' = F*x
            return new DenseVector(new double[3]
            {
                Fundamental[0,0] * point.X + Fundamental[0,1] * point.Y + Fundamental[0,2],
                Fundamental[1,0] * point.X + Fundamental[1,1] * point.Y + Fundamental[1,2],
                Fundamental[2,0] * point.X + Fundamental[2,1] * point.Y + Fundamental[2,2]
            });
        }

        public Vector<double> FindCorrespondingEpiline_OnLeftImage(Vector2 point)
        {
            // Epiline on left image is given by l = F^T*x'
            return new DenseVector(new double[3]
            {
                Fundamental[0,0] * point.X + Fundamental[1,0] * point.Y + Fundamental[2,0],
                Fundamental[0,1] * point.X + Fundamental[1,1] * point.Y + Fundamental[2,1],
                Fundamental[0,2] * point.X + Fundamental[1,2] * point.Y + Fundamental[2,2]
            });
        }

        public override string ToString()
        {
            return "Epiline Scan Cost Aggregator";
        }
    }
}
