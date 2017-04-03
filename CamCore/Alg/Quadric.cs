using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CamCore
{
    /// <summary>
    /// Quadric desribed by equation : Ax^2 + Bx + Cxy + Dy + Ey^2 + F = 0
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay, nq}")]
    public class Quadric
    {
        public double[] Coeffs { get; set; } = new double[6];
        public double A { get { return Coeffs[0]; } set { Coeffs[0] = value; } }
        public double B { get { return Coeffs[1]; } set { Coeffs[1] = value; } }
        public double C { get { return Coeffs[2]; } set { Coeffs[2] = value; } }
        public double D { get { return Coeffs[3]; } set { Coeffs[3] = value; } }
        public double E { get { return Coeffs[4]; } set { Coeffs[4] = value; } }
        public double F { get { return Coeffs[5]; } set { Coeffs[5] = value; } }

        public Line2D GetTangetThroughPoint(Vector2 point)
        {
            // If we have fit point and fit quadric, we can find tangent -> fit line
            // L : (x-x0)df(x0,y0)/dx + (y-y0)df(x0,y0) = 0
            // df/dx = 2Ax0 + B + Cy0, df/dy = 2Ey0 + D + Cx0
            // AL = df/dx, BL = df/dy, CL = -x0df/dx -y0df/dy = -(x0AL + y0BL)

            double tA = GetDiff_X(point);
            double tB = GetDiff_Y(point);
            double tC = -(point.X * tA + point.Y * tB);
            return new Line2D(tA, tB, tC);
        }

        public double this[int i]
        {
            get
            {
                return Coeffs[i];
            }
            set
            {
                Coeffs[i] = value;
            }
        }

        public static Quadric FitQuadricToPoints(List<Vector2> points)
        {
            // | 0 |   |x1^2  x1  x1y1  y1  y1^2  1| | A |
            // |   |   |                           | | B |
            // |   |   |                           | | C |
            // |   | = |                           | | D |
            // |   |   |                           | | E |
            // | 0 |   |xn^2  xn  xnyn  yn  yn^2  1| | F |

            Matrix<double> X = new DenseMatrix(points.Count, 6);
            for(int p = 0; p < points.Count; ++p)
            {
                var point = points[p];
                X[p, 0] = point.X * point.X;
                X[p, 1] = point.X;
                X[p, 2] = point.X * point.Y;
                X[p, 3] = point.Y;
                X[p, 4] = point.Y * point.Y;
                X[p, 5] = 1.0;
            }

            var coeffs = SvdZeroFullrankSolver.Solve(X);
            return new Quadric()
            {
                Coeffs = coeffs.ToArray()
            };
        }

        public static Quadric FitQuadricThroughPoint(List<Vector2> points, int fitPoint)
        {
            // | 0 |   |x1^2-x0^2  x1-x0  x1y1-x0y0  y1-y0  y1^2-y0^2| | A |
            // |   |   |                                             | | B |
            // |   | = |                                             | | C |
            // |   |   |                                             | | D |
            // | 0 |   |xn^2-x0^2  xn-x0  xnyn-x0y0  yn-y0  yn^2-y0^2| | E |

            Vector2 fp = points[fitPoint];
            Matrix<double> X = new DenseMatrix(points.Count - 1, 5);
            for(int p = 0; p < fitPoint; ++p)
            {
                var point = points[p];
                X[p, 0] = point.X * point.X - fp.X * fp.X;
                X[p, 1] = point.X - fp.X;
                X[p, 2] = point.X * point.Y - fp.X * fp.Y;
                X[p, 3] = point.Y - fp.Y;
                X[p, 4] = point.Y * point.Y - fp.Y * fp.Y;
            }

            for(int p = fitPoint + 1; p < points.Count; ++p)
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

            return new Quadric()
            {
                Coeffs = coeffs_f.ToArray()
            };
        }

        public double GetCurvature(Vector2 point)
        {
            // k = -Fy^2 Fxx + 2Fx Fy Fxy - Fx^2 Fyy / (Fx^2+Fy^2)^(3/2)
            double Fx = GetDiff_X(point);
            double Fy = GetDiff_Y(point);
            double Fxx = GetDiff_XX(point);
            double Fyy = GetDiff_YY(point);
            double Fxy = GetDiff_XY(point);
            double k = (-Fy * Fy * Fxx + 2.0 * Fx * Fy * Fxy - Fx * Fx * Fyy) /
                Math.Pow(Fx * Fx + Fy * Fy, 1.5);
            return k;
        }

        public double GetDiff_X(Vector2 point)
        {
            return 2.0 * A * point.X + B + C * point.Y;
        }

        public double GetDiff_Y(Vector2 point)
        {
            return 2.0 * E * point.Y + D + C * point.X;
        }

        public double GetDiff_XY(Vector2 point)
        {
            return C;
        }

        public double GetDiff_XX(Vector2 point)
        {
            return 2.0 * A;
        }

        public double GetDiff_YY(Vector2 point)
        {
            return 2.0 * E;
        }
        
        public Vector2 GetMaxCurvaturePoint()
        {
            return new Vector2();
        }

        private string DebuggerDisplay
        {
            get
            {
                double scale = Math.Max(Math.Abs(MathExtensions.MaxInRange(Coeffs)), Math.Abs(MathExtensions.MinInRange(Coeffs)));
                double a = A / scale;
                double b = B / scale;
                double c = C / scale;
                double d = D / scale;
                double e = E / scale;
                double f = F / scale;

                return a.ToString("F4") +
                    (b > 0 ? "x^2 + " + b.ToString("F4") : "x^2 - " + (-b).ToString("F4")) +
                    (c > 0 ? "x + " + c.ToString("F4") : "x - " + (-c).ToString("F4")) +
                    (d > 0 ? "xy + " + d.ToString("F4") : "xy - " + (-d).ToString("F4")) +
                    (e > 0 ? "y + " + e.ToString("F4") : "y - " + (-e).ToString("F4")) +
                    (f > 0 ? "y^2 + " + f.ToString("F4") : "y^2 - " + (-f).ToString("F4"));
            }
        }
    }
}
