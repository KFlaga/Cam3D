using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;

namespace CamCore
{
    [DebuggerDisplay("X = {X}, Y = {Y}")]
    public class Vector2
    {
        [XmlAttribute("X")]
        public double X { get; set; }
        [XmlAttribute("Y")]
        public double Y { get; set; }

        public Vector2()
        {
            X = 0.0;
            Y = 0.0;
        }

        public Vector2(double x = 0.0f, double y = 0.0f)
        {
            X = x; Y = y;
        }

        public Vector2(Vector2 other)
        {
            X = other.X;
            Y = other.Y;
        }
        
        public Vector2(IntVector2 other)
        {
            X = (double)other.X;
            Y = (double)other.Y;
        }

        public Vector2(Vector<double> other)
        {
            if(other.Count == 3)
            {
                // Treat input vector as homogeneous 2d vector
                X = other.At(0) / other.At(2);
                Y = other.At(1) / other.At(2);
            }
            else
            {
                X = other.At(0);
                Y = other.At(1);
            }
        }

        public void Set(double x, double y)
        {
            X = x;
            Y = y;
        }

        public Vector<double> ToMathNetVector2()
        {
            return new DenseVector(new double[2] { X, Y });
        }

        public Vector<double> ToMathNetVector3()
        {
            return new DenseVector(new double[3] { X, Y, 1.0 });
        }

        public static Vector2 operator +(Point2D<double> p1, Vector2 p2)
        {
            return new Vector2(p1.X + p2.X, p1.Y + p2.Y);
        }

        public static Vector2 operator +(Vector2 p1, Point2D<double> p2)
        {
            return new Vector2(p1.X + p2.X, p1.Y + p2.Y);
        }

        public static Vector2 operator -(Point2D<double> p1, Vector2 p2)
        {
            return new Vector2(p1.X - p2.X, p1.Y - p2.Y);
        }

        public static Vector2 operator -(Vector2 p1, Point2D<double> p2)
        {
            return new Vector2(p1.X - p2.X, p1.Y - p2.Y);
        }

        public static Vector2 operator +(Vector2 p1, Vector2 p2)
        {
            return new Vector2(p1.X + p2.X, p1.Y + p2.Y);
        }

        public static Vector2 operator -(Vector2 p1, Vector2 p2)
        {
            return new Vector2(p1.X - p2.X, p1.Y - p2.Y);
        }

        public static Vector2 operator *(Vector2 p1, Vector2 p2)
        {
            return new Vector2(p1.X * p2.X, p1.Y * p2.Y);
        }

        public static Vector2 operator /(Vector2 p1, Vector2 p2)
        {
            return new Vector2(p1.X / p2.X, p1.Y / p2.Y);
        }

        public static Vector2 operator +(double scalar, Vector2 p2)
        {
            return new Vector2(scalar + p2.X, scalar + p2.Y);
        }

        public static Vector2 operator +(Vector2 p2, double scalar)
        {
            return new Vector2(scalar + p2.X, scalar + p2.Y);
        }

        public static Vector2 operator -(double scalar, Vector2 p2)
        {
            return new Vector2(scalar - p2.X, scalar - p2.Y);
        }

        public static Vector2 operator -(Vector2 p1, double scalar)
        {
            return new Vector2(p1.X - scalar, p1.Y - scalar);
        }

        public static Vector2 operator *(double scalar, Vector2 p2)
        {
            return new Vector2(scalar * p2.X, scalar * p2.Y);
        }

        public static Vector2 operator *(Vector2 p2, double scalar)
        {
            return new Vector2(scalar * p2.X, scalar * p2.Y);
        }

        public static Vector2 operator /(double scalar, Vector2 p2)
        {
            return new Vector2(scalar / p2.X, scalar / p2.Y);
        }

        public static Vector2 operator /(Vector2 p2, double scalar)
        {
            return new Vector2(p2.X / scalar, p2.Y / scalar);
        }

        public double DotProduct(Vector2 v)
        {
            return X * v.X + Y * v.Y;
        }

        public double CrossProduct(Vector2 v)
        {
            return X * v.Y - Y * v.X;
        }

        public double LengthSquared()
        {
            return X * X + Y * Y;
        }

        public double Length()
        {
            return (double)Math.Sqrt(X * X + Y * Y);
        }

        public void Normalise()
        {
            double len = Length();
            if(len != 0.0f)
            {
                X = X / len;
                Y = Y / len;
            }
        }

        public Vector2 Normalised()
        {
            var v = new Vector2(this);
            v.Normalise();
            return v;
        }

        public double DistanceToSquared(Vector2 v)
        {
            return (X - v.X) * (X - v.X) + (Y - v.Y) * (Y - v.Y);
        }

        public double DistanceTo(Vector2 v)
        {
            return (double)Math.Sqrt(DistanceToSquared(v));
        }

        // Returns value in radians
        public double AngleTo(Vector2 v)
        {
            return (double)Math.Asin((X * v.Y - Y * v.X) / (Length() * v.Length()));
        }

        // Returns value in radians. Assumes both vector are normalized
        public double AngleToNormalized(Vector2 v)
        {
            return (double)Math.Asin(X * v.Y - Y * v.X);
        }

        // Returns sinus of angle to v, value in radians
        public double SinusTo(Vector2 v)
        {
            return (X * v.Y - Y * v.X) / (Length() * v.Length());
        }

        // Returns sinus of angle to v, value in radians. Assumes both vector are normalized
        public double SinusToNormalized(Vector2 v)
        {
            return (X * v.Y - Y * v.X);
        }
        // Returns cosinus of angle to v, value in radians
        public double CosinusTo(Vector2 v)
        {
            return (X * v.X + Y * v.Y) / (Length() * v.Length());
        }

        // Returns cosinus of angle to v, value in radians. Assumes both vector are normalized
        public double CosinusToNormalized(Vector2 v)
        {
            return (X * v.X + Y * v.Y);
        }

        public override string ToString()
        {
            return "X: " + X + ",Y: " + Y;
        }

        public string ToString(string format)
        {
            return "X: " + X.ToString(format) + ",Y: " + Y.ToString(format);
        }
    }

    [DebuggerDisplay("X = {X}, Y = {Y}")]
    public class IntVector2
    {
        [XmlAttribute("X")]
        public int X { get; set; }
        [XmlAttribute("Y")]
        public int Y { get; set; }

        public IntVector2()
        {
            X = 0;
            Y = 0;
        }

        public IntVector2(int x = 0, int y = 0)
        {
            X = x; Y = y;
        }

        public IntVector2(IntVector2 other)
        {
            X = other.X;
            Y = other.Y;
        }

        public IntVector2(Vector2 other)
        {
            X = (int)other.X;
            Y = (int)other.Y;
        }

        public void Set(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static IntVector2 operator +(Point2D<int> p1, IntVector2 p2)
        {
            return new IntVector2(p1.X + p2.X, p1.Y + p2.Y);
        }

        public static IntVector2 operator +(IntVector2 p1, Point2D<int> p2)
        {
            return new IntVector2(p1.X + p2.X, p1.Y + p2.Y);
        }

        public static IntVector2 operator -(Point2D<int> p1, IntVector2 p2)
        {
            return new IntVector2(p1.X - p2.X, p1.Y - p2.Y);
        }

        public static IntVector2 operator -(IntVector2 p1, Point2D<int> p2)
        {
            return new IntVector2(p1.X - p2.X, p1.Y - p2.Y);
        }

        public static IntVector2 operator +(IntVector2 p1, IntVector2 p2)
        {
            return new IntVector2(p1.X + p2.X, p1.Y + p2.Y);
        }

        public static IntVector2 operator -(IntVector2 p1, IntVector2 p2)
        {
            return new IntVector2(p1.X - p2.X, p1.Y - p2.Y);
        }

        public static IntVector2 operator *(IntVector2 p1, IntVector2 p2)
        {
            return new IntVector2(p1.X * p2.X, p1.Y * p2.Y);
        }

        public static IntVector2 operator /(IntVector2 p1, IntVector2 p2)
        {
            return new IntVector2(p1.X / p2.X, p1.Y / p2.Y);
        }

        public static bool operator ==(IntVector2 p1, IntVector2 p2)
        {
            return p1.X == p2.X && p1.Y == p2.Y;
        }

        public static bool operator !=(IntVector2 p1, IntVector2 p2)
        {
            return p1.X != p2.X || p1.Y != p2.Y;
        }

        public int DotProduct(IntVector2 v)
        {
            return X * v.X + Y * v.Y;
        }

        public int LengthSquared()
        {
            return X * X + Y * Y;
        }

        public double Length()
        {
            return (double)Math.Sqrt(X * X + Y * Y);
        }

        public int DistanceToSquared(IntVector2 v)
        {
            return (X - v.X) * X - v.X + (Y - v.Y) * Y - v.Y;
        }

        public double DistanceTo(IntVector2 v)
        {
            return (double)Math.Sqrt(DistanceToSquared(v));
        }

        public override string ToString()
        {
            return "X: " + X + ", Y: " + Y;
        }
    }
}
