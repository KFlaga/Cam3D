using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;

namespace CamCore
{
    [DebuggerDisplay("X = {X}, Y = {Y}, Z = {Z}")]
    public class Vector3
    {
        [XmlAttribute("X")]
        public double X { get; set; }
        [XmlAttribute("Y")]
        public double Y { get; set; }
        [XmlAttribute("Z")]
        public double Z { get; set; }

        public Vector3()
        {
            X = 0;
            Y = 0;
            Z = 0;
        }

        public Vector3(double x = 0.0f, double y = 0.0f, double z = 0.0f)
        {
            X = x; Y = y; Z = z;
        }

        public Vector3(Vector3 other)
        {
            X = other.X;
            Y = other.Y;
            Z = other.Z;
        }

        public Vector3(Vector<double> other)
        {
            if(other.Count == 4)
            {
                // Treat input vector as homogenous 3d vector
                X = other.At(0) / other.At(3);
                Y = other.At(1) / other.At(3);
                Z = other.At(2) / other.At(3);
            }
            else
            {
                X = other.At(0);
                Y = other.At(1);
                Z = other.At(2);
            }
        }

        public void Set(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector<double> ToMathNetVector3()
        {
            return new DenseVector(new double[3] { X, Y, Z });
        }

        public Vector<double> ToMathNetVector4()
        {
            return new DenseVector(new double[4] { X, Y, Z, 1.0 });
        }

        public static Vector3 operator +(Vector3 p1, Vector3 p2)
        {
            return new Vector3(p1.X + p2.X, p1.Y + p2.Y, p1.Z + p2.Z);
        }

        public static Vector3 operator -(Vector3 p1, Vector3 p2)
        {
            return new Vector3(p1.X - p2.X, p1.Y - p2.Y, p1.Z - p2.Z);
        }

        public static Vector3 operator *(Vector3 p1, Vector3 p2)
        {
            return new Vector3(p1.X * p2.X, p1.Y * p2.Y, p1.Z * p2.Z);
        }

        public static Vector3 operator /(Vector3 p1, Vector3 p2)
        {
            return new Vector3(p1.X / p2.X, p1.Y / p2.Y, p1.Z / p2.Z);
        }

        public static Vector3 operator +(double scalar, Vector3 p2)
        {
            return new Vector3(scalar + p2.X, scalar + p2.Y, scalar + p2.Z);
        }

        public static Vector3 operator +(Vector3 p2, double scalar)
        {
            return new Vector3(scalar + p2.X, scalar + p2.Y, scalar + p2.Z);
        }

        public static Vector3 operator -(double scalar, Vector3 p2)
        {
            return new Vector3(scalar - p2.X, scalar - p2.Y, scalar - p2.Z);
        }

        public static Vector3 operator -(Vector3 p1, double scalar)
        {
            return new Vector3(p1.X - scalar, p1.Y - scalar, p1.Z - scalar);
        }

        public static Vector3 operator *(double scalar, Vector3 p2)
        {
            return new Vector3(scalar * p2.X, scalar * p2.Y, scalar * p2.Z);
        }

        public static Vector3 operator *(Vector3 p2, double scalar)
        {
            return new Vector3(scalar * p2.X, scalar * p2.Y, scalar * p2.Z);
        }

        public static Vector3 operator /(double scalar, Vector3 p2)
        {
            return new Vector3(scalar / p2.X, scalar / p2.Y, scalar / p2.Z);
        }

        public static Vector3 operator /(Vector3 p2, double scalar)
        {
            return new Vector3(p2.X / scalar, p2.Y / scalar, p2.Z / scalar);
        }

        public double DotProduct(Vector3 v)
        {
            return X * v.X + Y * v.Y + Z * v.Z;
        }

        //public double CrossProduct(Vector3 v)
        //{
        //    return X * v.Y - Y * v.X;
        //}

        public double LengthSquared()
        {
            return X * X + Y * Y + Z * Z;
        }

        public double Length()
        {
            return (double)Math.Sqrt(X * X + Y * Y + Z * Z);
        }

        public void Normalise()
        {
            double len = Length();
            if(len != 0.0f)
            {
                X = X / len;
                Y = Y / len;
                Z = Z / len;
            }
        }

        public Vector3 Normalised()
        {
            double len = Length();
            if(len > 0.0f)
            {
                return new Vector3(X / len, Y / len, Z / len);
            }
            return new Vector3();
        }

        public double DistanceToSquared(Vector3 v)
        {
            return (X - v.X) * (X - v.X) + (Y - v.Y) * (Y - v.Y) + (Z - v.Z) * (Z - v.Z);
        }

        public double DistanceTo(Vector3 v)
        {
            return (double)Math.Sqrt(DistanceToSquared(v));
        }

        //// Returns value in radians
        //public double AngleTo(Vector3 v)
        //{
        //    return (double)Math.Asin((X * v.Y - Y * v.X) / (Length() * v.Length()));
        //}

        //// Returns value in radians. Assumes both vector are normalized
        //public double AngleToNormalized(Vector3 v)
        //{
        //    return (double)Math.Asin(X * v.Y - Y * v.X);
        //}

        //// Returns sinus of angle to v, value in radians
        //public double SinusTo(Vector3 v)
        //{
        //    return (X * v.Y - Y * v.X) / (Length() * v.Length());
        //}

        //// Returns sinus of angle to v, value in radians. Assumes both vector are normalised
        //public double SinusToNormalized(Vector3 v)
        //{
        //    return (X * v.Y - Y * v.X);
        //}
        //// Returns cosinus of angle to v, value in radians
        //public double CosinusTo(Vector3 v)
        //{
        //    return (X * v.X + Y * v.Y) / (Length() * v.Length());
        //}

        //// Returns cosinus of angle to v, value in radians. Assumes both vector are normalised
        //public double CosinusToNormalized(Vector3 v)
        //{
        //    return (X * v.X + Y * v.Y);
        //}

        public override string ToString()
        {
            return "X: " + X + ",Y: " + Y + ",Z: " + Z;
        }

        public string ToString(string format)
        {
            return "X: " + X.ToString(format) + ",Y: " + Y.ToString(format) + ",Z: " + Z.ToString(format);
        }

        public XmlNode CreateXmlNode(XmlDocument xmlDoc, string nodeName = "Vector3")
        {
            XmlNode node = xmlDoc.CreateElement(nodeName);

            var attX = xmlDoc.CreateAttribute("X");
            attX.Value = X.ToString();
            var attY = xmlDoc.CreateAttribute("Y");
            attY.Value = Y.ToString();
            var attZ = xmlDoc.CreateAttribute("Z");
            attZ.Value = Z.ToString();
            node.Attributes.Append(attX);
            node.Attributes.Append(attY);
            node.Attributes.Append(attZ);

            return node;
        }

        public void ReadFromXmlNode(XmlNode node)
        {
            X = double.Parse(node.Attributes["X"]?.Value);
            Y = double.Parse(node.Attributes["Y"]?.Value);
            Z = double.Parse(node.Attributes["Z"]?.Value);
        }

        public static Vector3 CreateFromXmlNode(XmlNode node)
        {
            return new Vector3(
                double.Parse(node.Attributes["X"]?.Value),
                double.Parse(node.Attributes["Y"]?.Value),
                double.Parse(node.Attributes["Z"]?.Value));
        }
    }

}
