using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CamCore
{
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
        
        public Vector2(IntVector2 other)
        {
            _x = (double)other.X;
            _y = (double)other.Y;
        }

        public Vector2(Vector<double> other)
        {
            if(other.Count == 3)
            {
                // Treat input vector as homogenous 2d vector
                _x = other.At(0) / other.At(2);
                _y = other.At(1) / other.At(2);
            }
            else
            {
                _x = other.At(0);
                _y = other.At(1);
            }
        }

        public void Set(double x, double y)
        {
            _x = x;
            _y = y;
        }

        public Vector<double> ToMathNetVector2()
        {
            return new DenseVector(new double[2] { _x, _y });
        }

        public Vector<double> ToMathNetVector3()
        {
            return new DenseVector(new double[3] { _x, _y, 1.0 });
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

        public override string ToString()
        {
            return "X: " + X + ", Y: " + Y;
        }
        
        public XmlNode CreateXmlNode(XmlDocument xmlDoc, string nodeName = "Vector2")
        {
            XmlNode node = xmlDoc.CreateElement(nodeName);

            var attX = xmlDoc.CreateAttribute("X");
            attX.Value = X.ToString();
            var attY = xmlDoc.CreateAttribute("Y");
            attY.Value = Y.ToString();
            node.Attributes.Append(attX);
            node.Attributes.Append(attY);

            return node;
        }

        public void ReadFromXmlNode(XmlNode node)
        {
            X = double.Parse(node.Attributes["X"]?.Value);
            Y = double.Parse(node.Attributes["Y"]?.Value);
        }

        public static Vector2 CreateFromXmlNode(XmlNode node)
        {
            return new Vector2(
                double.Parse(node.Attributes["X"]?.Value),
                double.Parse(node.Attributes["Y"]?.Value));
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

        public IntVector2(Vector2 other)
        {
            _x = (int)other.X;
            _y = (int)other.Y;
        }

        public void Set(int x, int y)
        {
            _x = x;
            _y = y;
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

        public override string ToString()
        {
            return "X: " + X + ", Y: " + Y;
        }

        public XmlNode CreateXmlNode(XmlDocument xmlDoc, string nodeName = "IntVector2")
        {
            XmlNode node = xmlDoc.CreateElement(nodeName);

            var attX = xmlDoc.CreateAttribute("X");
            attX.Value = X.ToString();
            var attY = xmlDoc.CreateAttribute("Y");
            attY.Value = Y.ToString();
            node.Attributes.Append(attX);
            node.Attributes.Append(attY);

            return node;
        }

        public void ReadFromXmlNode(XmlNode node)
        {
            X = int.Parse(node.Attributes["X"]?.Value);
            Y = int.Parse(node.Attributes["Y"]?.Value);
        }

        public static IntVector2 CreateFromXmlNode(XmlNode node)
        {
            return new IntVector2(
                int.Parse(node.Attributes["X"]?.Value),
                int.Parse(node.Attributes["Y"]?.Value));
        }
    }
}
