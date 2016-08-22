using System.Collections.Generic;
using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;

namespace CalibrationModule
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

        public List<Vector<double>> FitQuadrics; // Each vector is [A,B,C,D,E,F]
        public List<Vector2> StationaryPoints { get; set; }

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
            FitQuadrics = new List<Vector<double>>();
            for(int i = 0; i < LinePoints.Count; ++i)
            {
                LineDistortionDirections.Add(DistortionDirection.None);
                BaseDistortionDirections.Add(DistortionDirection.None);
                FitPoints.Add(0);
                FitQuadrics.Add(null);
            }

            base.Init();

            for(int line = 0; line < LinePoints.Count; ++line)
            {
                UpdateLinePoints(line);
                base.ComputeSums(line);
                base.UpdateLineOrientations(line);
                base.ComputeLineCoeffs(line);

                BaseDistortionDirections[line] = DistortionModel.DirectionFromLine(
                    LinePoints[line], LineCoeffs[line, 0], LineCoeffs[line, 1], LineCoeffs[line, 2]);
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
            UpdateLineDistortionDirection(line);
            FindFitPoint(line);
            FitQuadricThroughPoint(line);
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
            LineDistortionDirections[line] = DistortionModel.DirectionFromLine(
                corrPoints, LineCoeffs[line, 0], LineCoeffs[line, 1], LineCoeffs[line, 2]);
        }

        public void FindFitPoint(int line)
        {
            if(LineDistortionDirections[line] == DistortionDirection.Barrel)
            {
                FitPoints[line] = GetPointFurthestFromCenter(line);
            }
            else
            {
                FitPoints[line] = GetPointClosestToCenter(line);
            }
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
            // | 0 |   |x1^2  x1  x1y1  y1  y1^2  1| | A |
            // |   |   |                           | | B |
            // |   |   |                           | | C |
            // |   | = |                           | | D |
            // |   |   |                           | | E |
            // | 0 |   |xn^2  xn  xnyn  yn  yn^2  1| | F |

            var points = _correctedPf[line];
            Matrix<double> X = new DenseMatrix(LinePoints[line].Count, 6);
            for(int p = 0; p < LinePoints[line].Count; ++p)
            {
                var point = points[p];
                X[p, 0] = point.X * point.X;
                X[p, 1] = point.X;
                X[p, 2] = point.X * point.Y;
                X[p, 3] = point.Y;
                X[p, 4] = point.Y * point.Y ;
                X[p, 5] = 1.0;
            }

            var coeffs = SvdZeroFullrankSolver.Solve(X);
            FitQuadrics[line] = coeffs;
        }

        public void FitQuadricThroughPoint(int line)
        {
            // | 0 |   |x1^2-x0^2  x1-x0  x1y1-x0y0  y1-y0  y1^2-y0^2| | A |
            // |   |   |                                             | | B |
            // |   | = |                                             | | C |
            // |   |   |                                             | | D |
            // | 0 |   |xn^2-x0^2  xn-x0  xnyn-x0y0  yn-y0  yn^2-y0^2| | E |

            var points = _correctedPf[line];
            Vector2 fp = points[FitPoints[line]];
            Matrix<double> X = new DenseMatrix(LinePoints[line].Count - 1, 5);
            for(int p = 0; p < FitPoints[line]; ++p)
            {
                var point = points[p];
                X[p, 0] = point.X * point.X - fp.X * fp.X;
                X[p, 1] = point.X - fp.X;
                X[p, 2] = point.X * point.Y - fp.X * fp.Y;
                X[p, 3] = point.Y - fp.Y;
                X[p, 4] = point.Y * point.Y - fp.Y * fp.Y;
            }

            for(int p = FitPoints[line] + 1; p < LinePoints[line].Count; ++p)
            {
                var point = points[p];
                X[p - 1, 0] = point.X * point.X - fp.X * fp.X;
                X[p - 1, 1] = point.X - fp.X;
                X[p - 1, 2] = point.X * point.Y - fp.X * fp.Y;
                X[p - 1, 3] = point.Y - fp.Y;
                X[p - 1, 4] = point.Y * point.Y - fp.Y * fp.Y;
            }

            var coeffs = SvdZeroFullrankSolver.Solve(X);

            double F = -(coeffs[0] * fp.X * fp.X + coeffs[1] * fp.X +
                coeffs[2] * fp.X * fp.Y + coeffs[3] * fp.Y + coeffs[4] * fp.Y * fp.Y);

            var coeffs_f = new DenseVector(6);
            coeffs_f.SetSubVector(0, 5, coeffs);
            coeffs_f[5] = F;

            FitQuadrics[line] = coeffs_f;
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
            return Math.Max(R, 1.0/R);
        }

        public override double ComputeErrorForPoint(int l, int p)
        {
            // e = dL_i'^2 = (Axi+Byi+C)^2 / A^2 + B^2 * (rd/ru)^2
            var point = CorrectedPoints[l][p];
            double R = Get_RadiusErrorCoeff(l, p);
            double d = LineCoeffs[l, 0] * point.Pf.X + LineCoeffs[l, 1] * point.Pf.Y + LineCoeffs[l, 2];
           // double error = d * R / Math.Sqrt(LineCoeffs[l, 0] * LineCoeffs[l, 0] + LineCoeffs[l, 1] * LineCoeffs[l, 1]);
            double error = d *d * R * R / (LineCoeffs[l, 0] * LineCoeffs[l, 0] + LineCoeffs[l, 1] * LineCoeffs[l, 1]);
            return error;
        }

        // Error here is equal to mapping function
        public override void ComputeErrorVector(Vector<double> error)
        {
            ComputeMappingFucntion(error);
        }

        // Computes d(ei)/d(P) for ith line
        public override void ComputeJacobianForLine(Matrix<double> J, int l, int p0)
        {
            throw new NotImplementedException("Analitical jacobian for directional line fit not implemented");

            double A = LineCoeffs[l, 0];
            double B = LineCoeffs[l, 1];
            double C = LineCoeffs[l, 2];
            double Ex = _sumX[l];
            double Ey = _sumY[l];
            double Exy = _sumXY[l];
            double Ex2 = _sumX2[l];
            double Ey2 = _sumY2[l];
            Vector<double> dEx = _dEx[l];
            Vector<double> dEy = _dEy[l];
            Vector<double> dExy = _dExy[l];
            Vector<double> dEx2 = _dEx2[l];
            Vector<double> dEy2 = _dEy2[l];
            double N = _n[l];

            //   A = (-b - sqrt(D)) / 2a
            //   D = b^2 + 4a^2
            //   a = Exy - Ex*Ey/N
            //   b = E(y^2) - (Ey)^2/N - (E(x^2) - (Ex)^2/N)
            //double a = Exy - Ex * Ey / N;
            //double b = Ey2 - Ex2 + (Ex * Ex - Ey * Ey) / N;
            //double D = b * b + 4 * a * a;

            double a = Get_a(l);
            double b = Get_b(l);
            double D = Get_D(l);

            // d(a) = d(Exy) - 1/N * (Ey*d(Ex) + *Ex*d(Ey))
            Vector<double> diff_a = dExy - (1.0 / N) * (dEx * Ey + dEy * Ex);

            // d(b) = d(Ey2) - 2/N * Ey * d(Ey) - d(Ex2) + 2/N * Ex * d(Ex)
            Vector<double> diff_b = dEy2 - dEx2 + (2.0 / N) * (dEx * Ex - dEy * Ey);

            // d(D) = 2b*d(b) + 8a*d(a)
            Vector<double> diff_D = (2.0 * b) * diff_b + (8.0 * a) * diff_a;

            // d(A) = -a(d(b) + 1/2sqrt(D) * d(D)) + d(a)(b+sqrt(D)) / 2a^2
            Vector<double> diff_A = (0.5 / (a * a)) *
                ((b + Math.Sqrt(D)) * diff_a - a * (diff_b + (0.5 / Math.Sqrt(D)) * diff_D));

            // d(C)/d(P) = -(1/N)(Ex*d(A) + A*d(Ex) + d(Ey))
            Vector<double> diff_C = (-1.0 / N) * (Ex * diff_A + A * dEx + dEy);

            double A212 = 1.0 / ((A * A + 1.0) * (A * A + 1.0));
            var line = CorrectedPoints[l];
            for(int p = 0; p < line.Count; ++p)
            {
                double R = Get_RadiusErrorCoeff(l, p);
                var point = line[p] as DistortionPoint_Directional;
                double dist = (A * point.Pf.X + point.Pf.Y + C) * R;
                Vector<double> diff_dist =
                    (diff_A * point.Pf.X + A * point.Diff_Xf + point.Diff_Yf + diff_C) * R +
                    point.Diff_RCoeff * dist;
                // J[point, k] = d(e)/d(P[k]) =
                //  (2 * (Ax+y+C)R/(A^2+1)^2) * (d((Ax+y+C)R) - A*d(A)*(Ax+y+C)R)
                //  d((Ax+y+C)) = a(A)*x + A*d(x) + d(y) + d(C)
                Vector<double> diff_e = (2.0 * dist * A212) * (
                    diff_dist * (A * A + 1.0) - (A * dist) * diff_A);

                // Throw if NaN found in jacobian
                bool nanInfFound = diff_e.Exists((e) => { return double.IsNaN(e) || double.IsInfinity(e); });
                if(nanInfFound)
                {
                    throw new DivideByZeroException("NaN or Infinity found on jacobian");
                }

                J.SetRow(p0 + p, diff_e);
            }
        }

        //public override void ComputeDelta(Vector<double> delta)
        //{
        //    ComputeJacobian(_J);
        //    _J.TransposeToOther(_Jt);
        //    _Jt.MultiplyToOther(_currentErrorVector, _Jte);
        //    _Jte.Negate().CopyTo(delta);

        //   // delta = _J.ColumnSums().Negate();

        //    double bestLam = 1.0;
        //    double lam = 1.0;
        //    double lamMult = 2.0;
        //    double lastRes = ComputeResidiual();
        //    Vector<double> newResults = ResultsVector + lam * delta;
        //    newResults.CopyTo(DistortionModel.Parameters);
        //    DistortionModel.UpdateParamters();
        //    UpdateAll();
        //    ComputeErrorVector(_currentErrorVector);
        //    double res = ComputeResidiual();

        //    if(res > lastRes)
        //    {
        //        lamMult = 0.5;
        //    }

        //    do
        //    {
        //        bestLam = lam;
        //        lam *= lamMult;
        //        newResults = ResultsVector + lam * delta;
        //        newResults.CopyTo(DistortionModel.Parameters);
        //        DistortionModel.UpdateParamters();
        //        UpdateAll();
        //        ComputeErrorVector(_currentErrorVector);
        //        lastRes = res;
        //        res = ComputeResidiual();
        //    }
        //    while(res < lastRes && lam > 1e-3);

        //    delta.MultiplyThis(bestLam);
        //    ResultsVector.CopyTo(DistortionModel.Parameters);
        //    UpdateAll();
        //}
    }
}
