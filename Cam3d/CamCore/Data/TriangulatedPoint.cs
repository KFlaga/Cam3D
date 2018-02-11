using System.Xml.Serialization;

namespace CamCore
{
    public class TriangulatedPoint
    {
        [XmlElement("ImageLeft")]
        public Vector2 ImageLeft { get; set; }
        [XmlElement("ImageRight")]
        public Vector2 ImageRight { get; set; }
        [XmlElement("Real")]
        public Vector3 Real { get; set; }

        public override string ToString()
        {
            return "Left = " + ImageLeft.ToString() + ", Right = " + ImageRight.ToString() + ", Real = " + Real.ToString();
        }
    }

}
