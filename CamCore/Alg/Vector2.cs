using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamCore
{
    [DebuggerDisplay("X = {_x}, Y = {_y}")]
    public struct TPoint2D<T> where T : struct
    {
        private T _x;
        private T _y;

        public T X { get { return _x; } set { _x = value; } }
        public T Y { get { return _y; } set { _y = value; } }

        public TPoint2D(T x = default(T), T y = default(T))
        {
            _x = x;
            _y = y;
        }

        public TPoint2D(TPoint2D<T> other)
        {
            _x = other.X;
            _y = other.Y;
        }
    }

    [DebuggerDisplay("X = {_x}, Y = {_y}, W = {_w}")]
    public struct Point2DH
    {
        private double _x;
        private double _y;
        private double _w;

        public double X { get { return _x; } set { _x = value; } }
        public double Y { get { return _y; } set { _y = value; } }
        public double W { get { return _w; } set { _w = value; } }

        public Point2DH(TPoint2D<double> p)
        {
            _x = p.X; _y = p.Y; _w = 1.0f;
        }

        public Point2DH(double x = 0.0f, double y = 0.0f, double w = 1.0f)
        {
            _x = x; _y = y; _w = w;
        }

        public Point2DH(Point2DH other)
        {
            _x = other.X;
            _y = other.Y;
            _w = other.W;
        }

        public static Point2DH operator +(Point2DH p1, Point2DH p2)
        {
            return new Point2DH(p1.X / p1.W + p2.X / p2.W, p1.Y / p1.W + p2.Y / p2.W, 1.0f);
        }

        public static Point2DH operator -(Point2DH p1, Point2DH p2)
        {
            return new Point2DH(p1.X / p1.W - p2.X / p2.W, p1.Y / p1.W - p2.Y / p2.W, 1.0f);
        }

        public static Point2DH operator +(Point2DH p1, TPoint2D<double> p2)
        {
            return new Point2DH(p1.X / p1.W + p2.X, p1.Y / p1.W + p2.Y, 1.0f);
        }

        public static Point2DH operator -(Point2DH p1, TPoint2D<double> p2)
        {
            return new Point2DH(p1.X / p1.W - p2.X, p1.Y / p1.W - p2.Y, 1.0f);
        }

        public static Point2DH operator +(TPoint2D<double> p1, Point2DH p2)
        {
            return new Point2DH(p1.X + p2.X / p2.W, p1.Y + p2.Y / p2.W, 1.0f);
        }

        public static Point2DH operator -(TPoint2D<double> p1, Point2DH p2)
        {
            return new Point2DH(p1.X - p2.X / p2.W, p1.Y - p2.Y / p2.W, 1.0f);
        }
    }

    [DebuggerDisplay("X = {_x}, Y = {_y}")]
    public class Vector2
    {
        private double _x;
        private double _y;

        public double X { get { return _x; } set { _x = value; } }
        public double Y { get { return _y; } set { _y = value; } }

        public Vector2(double x = 0.0f, double y = 0.0f)
        {
            X = x; Y = y;
        }

        public Vector2(Vector2 other)
        {
            _x = other.X;
            _y = other.Y;
        }

        public static Vector2 operator +(TPoint2D<double> p1, Vector2 p2)
        {
            return new Vector2(p1.X + p2.X, p1.Y + p2.Y);
        }

        public static Vector2 operator +(Vector2 p1, TPoint2D<double> p2)
        {
            return new Vector2(p1.X + p2.X, p1.Y + p2.Y);
        }

        public static Vector2 operator -(TPoint2D<double> p1, Vector2 p2)
        {
            return new Vector2(p1.X - p2.X, p1.Y - p2.Y);
        }

        public static Vector2 operator -(Vector2 p1, TPoint2D<double> p2)
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

        // Returns value in radians. Assumes both vector are normalised
        public double AngleToNormalized(Vector2 v)
        {
            return (double)Math.Asin(X * v.Y - Y * v.X);
        }

        // Returns sinus of angle to v, value in radians
        public double SinusTo(Vector2 v)
        {
            return (X * v.Y - Y * v.X) / (Length() * v.Length());
        }

        // Returns sinus of angle to v, value in radians. Assumes both vector are normalised
        public double SinusToNormalized(Vector2 v)
        {
            return (X * v.Y - Y * v.X);
        }
        // Returns cosinus of angle to v, value in radians
        public double CosinusTo(Vector2 v)
        {
            return (X * v.X + Y * v.Y) / (Length() * v.Length());
        }

        // Returns cosinus of angle to v, value in radians. Assumes both vector are normalised
        public double CosinusToNormalized(Vector2 v)
        {
            return (X * v.X + Y * v.Y);
        }
    }

    [DebuggerDisplay("X = {_x}, Y = {_y}")]
    public class IntVector2
    {
        private int _x;
        private int _y;
        public int X { get { return _x; } set { _x = value; } }
        public int Y { get { return _y; } set { _y = value; } }

        public IntVector2(int x = 0, int y = 0)
        {
            X = x; Y = y;
        }

        public IntVector2(IntVector2 other)
        {
            _x = other.X;
            _y = other.Y;
        }

        public static IntVector2 operator +(TPoint2D<int> p1, IntVector2 p2)
        {
            return new IntVector2(p1.X + p2.X, p1.Y + p2.Y);
        }

        public static IntVector2 operator +(IntVector2 p1, TPoint2D<int> p2)
        {
            return new IntVector2(p1.X + p2.X, p1.Y + p2.Y);
        }

        public static IntVector2 operator -(TPoint2D<int> p1, IntVector2 p2)
        {
            return new IntVector2(p1.X - p2.X, p1.Y - p2.Y);
        }

        public static IntVector2 operator -(IntVector2 p1, TPoint2D<int> p2)
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
    }
}
