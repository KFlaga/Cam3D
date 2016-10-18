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

        public override string ToString()
        {
            return "X: " + X + ", Y: " + Y;
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

        public override string ToString()
        {
            return "X: " + X + ", Y: " + Y + ", W:" + W;
        }
    }
}