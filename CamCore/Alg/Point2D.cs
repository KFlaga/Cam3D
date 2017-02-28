using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CamCore
{
    [DebuggerDisplay("X = {X}, Y = {Y}")]
    public struct Point2D<T> where T : struct
    {
        [XmlAttribute("X")]
        public T X { get; set; }
        [XmlAttribute("Y")]
        public T Y { get; set; }

        public Point2D(T x = default(T), T y = default(T))
        {
            X = x;
            Y = y;
        }

        public Point2D(Point2D<T> other)
        {
            X = other.X;
            Y = other.Y;
        }

        public override string ToString()
        {
            return "X: " + X + ", Y: " + Y;
        }
    }
}