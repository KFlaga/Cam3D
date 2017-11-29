using SharpDX;
using System.Runtime.InteropServices;
using System.Xml;

namespace CamDX
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct MaterialIlluminationData
    {
        public Color4 AmbientColor;
        public Color4 DiffuseColor;
        public Color4 SpecularColor;
        public Color4 EmmisiveColor;
        public float Shineness;
        public Vector3 Padding;

        public const int SizeInBytes = 80;

        public static MaterialIlluminationData Default = new MaterialIlluminationData()
        {
            AmbientColor = new Color4(1.0f),
            DiffuseColor = new Color4(1.0f),
            SpecularColor = new Color4(1.0f),
            EmmisiveColor = new Color4(1.0f),
            Shineness = 1.0f
        };

        public static MaterialIlluminationData ParserFromXmlNode(XmlNode illNode)
        {
            //<Ambient a="1.0" r="1.0" g="1.0" b="1.0"/>
            //<Diffuse a="1.0" r="1.0" g="1.0" b="1.0"/>
            //<Emmisive a="0.0" r="1.0" g="1.0" b="1.0"/>
            XmlNode ambNode = illNode.SelectSingleNode("Ambient");
            XmlNode diffNode = illNode.SelectSingleNode("Diffuse");
            XmlNode speNode = illNode.SelectSingleNode("Specular");
            XmlNode emiNode = illNode.SelectSingleNode("Emmisive");
            MaterialIlluminationData data = new MaterialIlluminationData()
            {
                AmbientColor = new Color4()
                {
                    Alpha = float.Parse(ambNode.Attributes["a"].Value),
                    Red = float.Parse(ambNode.Attributes["r"].Value),
                    Green = float.Parse(ambNode.Attributes["g"].Value),
                    Blue = float.Parse(ambNode.Attributes["b"].Value)
                },
                DiffuseColor = new Color4()
                {
                    Alpha = float.Parse(diffNode.Attributes["a"].Value),
                    Red = float.Parse(diffNode.Attributes["r"].Value),
                    Green = float.Parse(diffNode.Attributes["g"].Value),
                    Blue = float.Parse(diffNode.Attributes["b"].Value)
                },
                SpecularColor = new Color4()
                {
                    Alpha = float.Parse(speNode.Attributes["a"].Value),
                    Red = float.Parse(speNode.Attributes["r"].Value),
                    Green = float.Parse(speNode.Attributes["g"].Value),
                    Blue = float.Parse(speNode.Attributes["b"].Value)
                },
                EmmisiveColor = new Color4()
                {
                    Alpha = float.Parse(emiNode.Attributes["a"].Value),
                    Red = float.Parse(emiNode.Attributes["r"].Value),
                    Green = float.Parse(emiNode.Attributes["g"].Value),
                    Blue = float.Parse(emiNode.Attributes["b"].Value)
                },
                Shineness = float.Parse(speNode.Attributes["shine"].Value)
            };
            return data;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct GlobalLightsData
    {
        public Color4 Ambient;
        public Color4 Directional;
        public Vector3 Direction;
        public float Padding;
    }
    
    public class DXLight
    {
        public Color4 Color { get; set; }
        public Vector3 Direction { get; set; }
        public Vector3 Position { get; set; }

        public enum LightType
        {
            Ambient = 0,
            Directional,
            Spot,
            Cone
        }
    }
}
