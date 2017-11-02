using System.Collections.Generic;
using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;

namespace CamAlgorithms
{
    // Uses distortion direction to determine better line fit ( closer to real, undistorted one )
    // 1) Finds distortion direction
    // 2) Fitted line moves through point closest/furthest of distorted points
    // 3) Fitted line is tangent to quadratic fitted to line points in point from 2)
    // 4) Jacobian minimise error e = sum(di) or e = sum(di*R) where R = ru/rd or rd/ru
    // 
    // Quadric eq : Ax^2 + Bx + Cxy + Dy + Ey^2 + F = 0
    // As we seek tangent in (x0,y0), quadric should contain this point, so:
    // F = -(Ax0^2 + Bx0 + Cx0y0 + Dy0 + Ey0^2)
    // So we have:
    // | 0 |   |x1^2-x0^2  x1-x0  x1y1-x0y0  y1-y0  y1^2-y0^2| | A |   | e1 |
    // | 0 |   |                                             | | B |   |    |
    // | 0 | = |                                             | | C | + |    |
    // |   |   |                                             | | D |   |    |
    // | 0 |   |xn^2-x0^2  xn-x0  xnyn-x0y0  yn-y0  yn^2-y0^2| | E |   | en |
    //
    // To minimise e => A = (Xt*X)^-1*Xt * (-F) (or solve Svd for A)
    // At least 6 points ( excluding x0 ) should be in each line
    // If we have quadric 0 = f(x,y), then tangent in point x0 : 
    // L : (x-x0)df(x0,y0)/dx + (y-y0)df(x0,y0) = 0
    // 
    //  
    public class LMDistortionDirectionalLineFitMinimalisation : LMDistortionBasicLineFitMinimalisation
    {
        public List<DistortionDirection> LineDistortionDirections { get; private set; } // Direction of distortion on lines ( after correction points )
        public List<DistortionDirection> BaseDistortionDirections { get; private set; } // Direction of distortion on inital lines
        public List<int> FitPoints { get; set; }

        public List<Quadric> FitQuadrics;

        public bool FindInitialModelParameters { get; set; } = true;

        public class DistortionPoint_Directional : DistortionPoint
        {
            public Vector<double> Diff_RCoeff { set; get; }
            public double RCoeff { get; set; }

            public DistortionPoint_Directional(int modelParametersCount) : base(modelParametersCount)
            {
                Diff_RCoeff = new DenseVector(modelParametersCount);
            }
        }

        public override void Init()
        {
            LineDistortionDirections = new List<DistortionDirection>();
            BaseDistortionDirections = new List<DistortionDirection>();
            FitPoints = new List<int>();
            FitQuadrics = new List<Quadric>();
            for(int i = 0; i < LinePoints.Count; ++i)
            {
                LineDistortionDirections.Add(DistortionDirection.None);
                BaseDistortionDirections.Add(DistortionDirection.None);
                FitPoints.Add(0);
                FitQuadrics.Add(null);
            }

            if(FindInitialModelParameters)
            {
                DistortionModel.InitParameters();
            }

            base.Init();
            UpdateAll();

            for(int line = 0; line < LinePoints.Count; ++line)
            {
                UpdateLinePoints(line);
                base.ComputeSums(line);
                base.UpdateLineOrientations(line);
                base.ComputeLineCoeffs(line);

                BaseDistortionDirections[line] = LineDistortionDirections[line];
            }

            if(FindInitialModelParameters)
            {
                DistortionModel.SetInitialParametersFromQuadrics(
                    FitQuadrics, LinePoints, FitPoints);
            }
        }

        public override DistortionPoint CreateDistortionPoint()
        {
            return new DistortionPoint_Directional(DistortionModel.ParametersCount);
        }

        public override void UpdateAll(int line)
        {
            UpdateLinePoints(line);
            base.ComputeSums(line);
            base.UpdateLineOrientations(line);
            base.ComputeLineCoeffs(line);
            FindFitPoint(line);
            FitQuadricThroughPoint(line);
            UpdateLineDistortionDirection(line);
            ComputeLineCoeffs(line);
        }

        public void UpdateLineDistortionDirection()
        {
            for(int line = 0; line < LinePoints.Count; ++line)
            {
                UpdateLineDistortionDirection(line);
            }
        }

        public void UpdateLineDistortionDirection(int line)
        {
            var corrPoints = CorrectedPoints[line];
            //LineDistortionDirections[line] = DistortionModel.DirectionFromLine(
            //    corrPoints, LineCoeffs[line, 0], LineCoeffs[line, 1], LineCoeffs[line, 2]);
            LineDistortionDirections[line] = DistortionModel.DirectionFromFitQuadric(
                corrPoints, FitQuadrics[line], DistortionModel.DistortionCenter, corrPoints[FitPoints[line]].Pf);
        }

        public void FindFitPoint(int line)
        {
            FitPoints[line] = GetPointClosestToCenter(line);
        }

        public int GetPointClosestToCenter(int line)
        {
            var points = LinePoints[line];
            var center = DistortionModel.DistortionCenter;
            double minDist = center.DistanceToSquared(points[0]);
            int minPoint = 0;
            for(int p = 1; p < points.Count; ++p)
            {
                double dist = center.DistanceToSquared(points[p]);
                if(dist < minDist)
                {
                    minDist = dist;
                    minPoint = p;
                }
            }
            return minPoint;
        }

        public int GetPointFurthestFromCenter(int line)
        {
            var points = LinePoints[line];
            var center = DistortionModel.DistortionCenter;
            double maxDist = center.DistanceToSquared(points[0]);
            int maxPoint = 0;
            for(int p = 1; p < points.Count; ++p)
            {
                double dist = center.DistanceToSquared(points[p]);
                if(dist > maxDist)
                {
                    maxDist = dist;
                    maxPoint = p;
                }
            }
            return maxPoint;
        }

        public void FitQuadric(int line)
        {
            FitQuadrics[line] = Quadric.FitQuadricToPoints(_correctedPf[line]);
        }

        public void FitQuadricThroughPoint(int line)
        {
            FitQuadrics[line] = Quadric.FitQuadricThroughPoint(_correctedPf[line], FitPoints[line]);
        }

        public void FindFitPointOnQuadric(int line)
        {
            // Stationary point of quadric -> both partial derivatives are zero
            // df/dx = 2Ax0 + B + Cy0, df/dy = 2Ey0 + D + Cx0
            // 2Ax0 + B + Cy0 = 0 => x0 = -(B+Cy0)/2A
            // 2Ey0 + D + Cx0 = 0 -> 2Ey0 + D - C((B+Cy0)/2A) = 2Ey0 + D - CB/2A - C^2y0/2A
            // y0(2E - C^2/2A) = D - CB/2A => y0 = (D - CB/2A)/(2E - C^2/2A)
            // 
            // If A == 0, then y0 = -B/C
            //  2Ey0 + D + Cx0 = 0 -> -2EB/C + D + Cx0 = 0 => x0 = (EB - CD)/C^2
            // If E == 0, then x0 = -D/C
            //  2Ax0 + B + Cy0 = 0 -> -2AD/C + B + Cx0 = 0 => x0 = (AD - CB)/C^2
            // If C == 0, then x0 = -B/2A, y0 = -D/2E 
            // ( if additionally A == 0 or E == 0, then one of points is determined, second 
            //
            // If A == 0 && E == 0
        }

        public override void ComputeLineCoeffs(int line)
        {
            // If we have fit point and fit quadric, we can find tangent -> fit line
            // L : (x-x0)df(x0,y0)/dx + (y-y0)df(x0,y0) = 0
            // df/dx = 2Ax0 + B + Cy0, df/dy = 2Ey0 + D + Cx0
            // AL = df/dx, BL = df/dy, CL = -x0df/dx -y0df/dy = x0AL + y0BL
            var pf = _correctedPf[line][FitPoints[line]];
            double A = FitQuadrics[line][0];
            double B = FitQuadrics[line][1];
            double C = FitQuadrics[line][2];
            double D = FitQuadrics[line][3];
            double E = FitQuadrics[line][4];
            LineCoeffs[line, 0] = 2 * A * pf.X + B + C * pf.Y;
            LineCoeffs[line, 1] = 2 * E * pf.Y + D + C * pf.X;
            LineCoeffs[line, 2] = -(pf.X * LineCoeffs[line, 0] + pf.Y * LineCoeffs[line, 1]);
        }

        public double Get_RadiusErrorCoeff(int line, int point)
        {
            //if(BaseDistortionDirections[line] == DistortionDirection.None)
            //    return 1;
            //if(BaseDistortionDirections[line] == DistortionDirection.Barrel)
            //    return CorrectedPoints[line][point].Rd / CorrectedPoints[line][point].Ru;
            //if(BaseDistortionDirections[line] == DistortionDirection.Cushion)
            //    return CorrectedPoints[line][point].Ru / CorrectedPoints[line][point].Rd;

            double R = CorrectedPoints[line][point].Ru / CorrectedPoints[line][point].Rd;
            return Math.Max(R, 1.0 / R);
        }

        public override double ComputeErrorForPoint(int l, int p)
        {
            // e = dL_i'^2 = (Axi+Byi+C)^2 / A^2 + B^2 * (rd/ru)^2
            var point = CorrectedPoints[l][p];
            double R = Get_RadiusErrorCoeff(l, p);
            double d = LineCoeffs[l, 0] * point.Pf.X + LineCoeffs[l, 1] * point.Pf.Y + LineCoeffs[l, 2];
            double error = d * R / Math.Sqrt(LineCoeffs[l, 0] * LineCoeffs[l, 0] + LineCoeffs[l, 1] * LineCoeffs[l, 1]);
            //double error = d * d * R * R / (LineCoeffs[l, 0] * LineCoeffs[l, 0] + LineCoeffs[l, 1] * LineCoeffs[l, 1]);
            return error;
        }

        // Error here is equal to mapping function
        public override void ComputeErrorVector(Vector<double> error)
        {
            ComputeMappingFucntion(error);
        }
    }
}
