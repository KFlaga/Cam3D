using System;
using System.Diagnostics;

namespace CamCore
{
    [DebuggerDisplay("{DebuggerDisplay, nq}")]
    public class Line2D
    {
        public enum LineDirection
        {
            None = 0,
            Horizontal,
            Vertical,
            Other
        }

        public double[] Coeffs { get; set; } = new double[3];
        public double A { get { return Coeffs[0]; } set { Coeffs[0] = value; } }
        public double B { get { return Coeffs[1]; } set { Coeffs[1] = value; } }
        public double C { get { return Coeffs[2]; } set { Coeffs[2] = value; } }
        public LineDirection Direction { get; set; } = LineDirection.None;

        public bool IsHorizontal()
        {
            return Math.Abs(A) < Math.Abs(B) * 1e-6;
        }

        public bool IsVertical()
        {
            return Math.Abs(B) < Math.Abs(A) * 1e-6;
        }

        public Line2D() { }

        public Line2D(Vector2 p1, Vector2 p2)
        {
            Vector2 intPoint = new Vector2();
            if(Math.Abs(p1.X - p2.X) < 1e-6)
            {
                this.Direction = LineDirection.Vertical;
                this.A = 1.0;
                this.B = 0.0;
            }
            else if(Math.Abs(p1.Y - p2.Y) < 1e-6)
            {
                this.Direction = LineDirection.Horizontal;
                this.A = 0.0;
                this.B = 1.0;
            }
            else
            {
                this.A = -(p1.Y - p2.Y) / (p1.X - p2.X);
                this.B = 1.0;
                this.Direction = LineDirection.Other;
            }
            this.C = -this.A * p1.X - this.B * p1.Y;
        }

        public Line2D(double a, double b, double c)
        {
            A = a;
            B = b;
            C = c;
            if(IsHorizontal())
                Direction = LineDirection.Horizontal;
            else if(IsVertical())
                Direction = LineDirection.Vertical;
            else
                Direction = LineDirection.Other;
        }

        // Returns point of intersetion of 2 lines or null if they are parallel
        public static Vector2 IntersectionPoint(Line2D l1, Line2D l2)
        {
            Vector2 intPoint = new Vector2();
            if(l1.Direction == LineDirection.Vertical)
            {
                if(l2.Direction == LineDirection.Vertical)
                {
                    return null;
                }
                intPoint.X = -l1.C / l1.A;
                intPoint.Y = -(l2.A * intPoint.X + l2.C) / l2.B;
            }
            else if(l1.Direction == LineDirection.Horizontal)
            {
                if(l2.Direction == LineDirection.Horizontal)
                {
                    return null;
                }
                intPoint.Y = -l1.C / l1.B;
                intPoint.X = -(l2.B * intPoint.Y + l2.C) / l2.A;
            }
            else
            {
                if(l2.Direction == LineDirection.Vertical)
                {
                    intPoint.X = -l2.C / l2.A;
                    intPoint.Y = -(l1.A * intPoint.X + l1.C) / l1.B;
                }
                else if(l2.Direction == LineDirection.Horizontal)
                {
                    intPoint.Y = -l2.C / l2.B;
                    intPoint.X = -(l1.B * intPoint.Y + l1.C) / l1.A;
                }
                else
                {
                    intPoint.Y = -(l1.C * l2.A / l1.A - l2.C) / (l1.B * l2.A / l1.A - l2.B);
                    intPoint.X = -(l1.B * intPoint.Y + l1.C) / l1.A;
                }
            }
            return intPoint;
        }

        private string DebuggerDisplay
        {
            get
            {
                double a, b, c;
                if(IsVertical())
                {
                    a = 1.0;
                    b = 0.0;
                    c = C / A;
                }
                else
                {
                    b = 1.0;
                    a = A / B;
                    c = C / B;
                }

                return a.ToString("F4") + "x + " + b.ToString("F4") + "y + " + c.ToString("F4");
            }
        }
    }
}
