using System.Diagnostics;
using System.Xml.Serialization;

namespace CamCore
{
    [DebuggerDisplay("X = {X}, Y = {Y}")]
    public struct IntPoint2
    {
        [XmlAttribute("X")]
        public int X { get; set; }
        [XmlAttribute("Y")]
        public int Y { get; set; }

        public IntPoint2(int x = 0, int y = 0)
        {
            X = x;
            Y = y;
        }

        public IntPoint2(IntPoint2 other)
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