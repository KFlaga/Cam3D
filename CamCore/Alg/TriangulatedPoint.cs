using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }

}
