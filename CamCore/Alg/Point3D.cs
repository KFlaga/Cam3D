using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CamCore
{
    [DebuggerDisplay("X = {X}, Y = {Y}, Z = {Z}")]
    public class Point3D
    {
        [XmlAttribute("X")]
        public double X { get; set; }
        [XmlAttribute("Y")]
        public double Y { get; set; }
        [XmlAttribute("Z")]
        public double Z { get; set; }

        public Point3D()
        {
            X = 0.0f; Y = 0.0f; Z = 0.0f;
        }

        public Point3D(Point3D p)
        {
            X = p.X; Y = p.Y; Z = p.Z;
        }

        public Point3D(Point3DH p)
        {
            X = p.X / p.W; Y = p.Y / p.W; Z = p.Z / p.W;
        }

        public Point3D(double x, double y, double z)
        {
            X = x; Y = y; Z = z;
        }
    }

    [DebuggerDisplay("X = {_x}, Y = {_y}, Z = {_z}, W = {_w}")]
    public class Point3DH
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double W { get; set; }

        public Point3DH()
        {
            X = 0.0f; Y = 0.0f; Z = 0.0f; W = 1.0f;
        }

        public Point3DH(Point3D p)
        {
            X = p.X; Y = p.Y; Z = p.Z; W = 1.0f;
        }

        public Point3DH(Point3DH p)
        {
            X = p.X; Y = p.Y; Z = p.Z; W = p.W;
        }

        public Point3DH(double x, double y, double z, double w = 1.0f)
        {
            X = x; Y = y; Z = z; W = w;
        }
    }
}
