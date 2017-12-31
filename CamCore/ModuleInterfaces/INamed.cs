using System.Xml.Serialization;

namespace CamCore
{
    public interface INamed
    {
        [XmlIgnore]
        string Name { get; }
    }
}
