using CamCore;
using System.Xml.Serialization;

namespace CamAlgorithms
{
    public class TriangulatedPoint
    {
        [XmlElement("ImageLeft")]
        public Vector2 ImageLeft { get; set; }
        [XmlElement("ImageRight")]
        public Vector2 ImageRight { get; set; }
        [XmlElement("Real")]
        public Vector3 Real { get; set; }
    }

}
