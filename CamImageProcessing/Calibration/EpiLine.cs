using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;

namespace CamAlgorithms
{
    public struct EpiLine
    {
        public Vector<double> Coeffs { get; set; }

        public double FindYd(double xd)
        {
            // Ax + By + C = 0 => y = -(Ax+C)/B
            return -(Coeffs.At(0) * xd + Coeffs.At(2)) / Coeffs.At(1);
        }

        public double FindXd(double yd)
        {
            // Ax + By + C = 0 => x = -(By+C)/A
            return -(Coeffs.At(1) * yd + Coeffs.At(2)) / Coeffs.At(0);
        }

        public int FindXmax(int rows, int colums)
        {
            // 3) Find xmax = min(Im.cols, max(xd=e(y=Im.rows),xd=e(y=0.0))) (point where epilines corsses border 2nd time)
            return (int)(Math.Min((double)colums, Math.Max(FindXd(rows), FindXd(0.0))));
        }

        public int FindX0(int rows)
        {
            // 3) Find x0 = max(0, xd=e(y=Im.rows)xd=e(y=0.0)) (point where epilines corsses border 1st time)
            return (int)(Math.Max(0.0, Math.Min(FindXd(rows), FindXd(0.0))));
        }

        public bool IsHorizontal()
        {
            return Math.Abs(Coeffs.At(0)) < Math.Abs(Coeffs.At(1)) * 1e-4;
        }

        public bool IsVertical()
        {
            return Math.Abs(Coeffs.At(1)) < Math.Abs(Coeffs.At(0)) * 1e-4;
        }

        public void Normalize()
        {
            // Divide each coeff by sqrt(A^2+B^2)
            double s = Math.Sqrt(Coeffs.At(0) * Coeffs.At(0) + Coeffs.At(1) * Coeffs.At(1));
            Coeffs.DivideThis(s);
        }

        public static EpiLine FindCorrespondingEpiline_LineOnRightImage(Vector2 point, Matrix<double> fundamental)
        {
            // Epiline on right image is given by l' = F*x
            return new EpiLine()
            {
                Coeffs = new DenseVector(new double[3]
            {
                fundamental[0,0] * point.X + fundamental[0,1] * point.Y + fundamental[0,2],
                fundamental[1,0] * point.X + fundamental[1,1] * point.Y + fundamental[1,2],
                fundamental[2,0] * point.X + fundamental[2,1] * point.Y + fundamental[2,2]
            })
            };
        }

        public static EpiLine FindCorrespondingEpiline_LineOnLeftImage(Vector2 point, Matrix<double> fundamental)
        {
            // Epiline on left image is given by l = F^T*x'
            return new EpiLine()
            {
                Coeffs = new DenseVector(new double[3]
            {
                fundamental[0,0] * point.X + fundamental[1,0] * point.Y + fundamental[2,0],
                fundamental[0,1] * point.X + fundamental[1,1] * point.Y + fundamental[2,1],
                fundamental[0,2] * point.X + fundamental[1,2] * point.Y + fundamental[2,2]
            })
            };
        }
    }
}
