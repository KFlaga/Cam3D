using System;
using System.Diagnostics;
using System.Xml;

namespace CamCore
{
    public enum DisparityFlags : int
    {
        None = 0,
        Valid = 1,
        Invalid = 2,
        Occluded = 4,
    }

    // Mached = Base + Disparity
    [DebuggerDisplay("X = {DX}, Y = {DY}, c = {Cost}, t = {Confidence}")]
    public class Disparity : ICloneable
    {
        public int DX;
        public double SubDX;
        public double Cost;
        public double Confidence;
        public int Flags;

        public Disparity()
        {
            Flags = (int)DisparityFlags.Invalid;
        }

        public Disparity(IntVector2 pixelBase, IntVector2 pixelMacthed, double cost = 0.0, double confidence = 0.0, int flags = (int)DisparityFlags.Invalid)
        {
            DX = pixelMacthed.X - pixelBase.X;
            SubDX = DX;
            Cost = cost;
            Confidence = confidence;
            Flags = flags;
        }

        public IntVector2 GetMatchedPixel(IntVector2 pixelBase)
        {
            return new IntVector2(pixelBase.X + DX, pixelBase.Y);
        }

        public void GetMatchedPixel(IntVector2 pixelBase, IntVector2 pixelMatched)
        {
            pixelMatched.X = pixelBase.X + DX;
            pixelMatched.Y = pixelBase.Y;
        }

        public IntVector2 GetBasePixel(IntVector2 pixelMatched)
        {
            return new IntVector2(pixelMatched.X - DX, pixelMatched.Y);
        }

        public void GetBasePixel(IntVector2 pixelMatched, IntVector2 pixelBase)
        {
            pixelBase.X = pixelMatched.X - DX;
            pixelBase.Y = pixelMatched.Y;
        }

        public bool IsValid()
        {
            return (Flags & (int)DisparityFlags.Valid) != 0;
        }

        public bool IsInvalid()
        {
            return (Flags & (int)DisparityFlags.Invalid) != 0;
        }

        public object Clone()
        {
            return new Disparity()
            {
                DX = DX,
                Cost = Cost,
                Confidence = Confidence,
                Flags = Flags,
                SubDX = SubDX
            };
        }

        public override string ToString()
        {
            return "X = " + DX + " c = " + Cost + ", t = " + Confidence;
        }

        public XmlNode CreateDisparityNode(XmlDocument xmlDoc)
        {
            XmlNode node = xmlDoc.CreateElement("Disparity");

            XmlAttribute dxAtt = xmlDoc.CreateAttribute("dx");
            dxAtt.Value = DX.ToString();
            XmlAttribute sdxAtt = xmlDoc.CreateAttribute("subdx");
            sdxAtt.Value = SubDX.ToString();
            XmlAttribute costAtt = xmlDoc.CreateAttribute("cost");
            costAtt.Value = Cost.ToString();
            XmlAttribute confAtt = xmlDoc.CreateAttribute("confidence");
            confAtt.Value = Confidence.ToString();
            XmlAttribute flagsAtt = xmlDoc.CreateAttribute("flags");
            flagsAtt.Value = DisparityFlagsToString(Flags);

            node.Attributes.Append(dxAtt);
            node.Attributes.Append(sdxAtt);
            node.Attributes.Append(costAtt);
            node.Attributes.Append(confAtt);
            node.Attributes.Append(flagsAtt);

            return node;
        }

        public void ReadFromNode(XmlNode node)
        {
            DX = int.Parse(node.Attributes["dx"].Value);
            SubDX = int.Parse(node.Attributes["subdx"].Value);
            Cost = double.Parse(node.Attributes["cost"].Value);
            Confidence = double.Parse(node.Attributes["confidence"].Value);
            Flags = ParseDisparityFlags(node.Attributes["flags"].Value);
        }

        public static Disparity CreateFromNode(XmlNode node)
        {
            Disparity disp = new Disparity()
            {
                DX = int.Parse(node.Attributes["dx"].Value),
                SubDX = double.Parse(node.Attributes["subdx"].Value),
                Cost = double.Parse(node.Attributes["cost"].Value),
                Confidence = double.Parse(node.Attributes["confidence"].Value),
                Flags = ParseDisparityFlags(node.Attributes["flags"].Value)
            };
            return disp;
        }

        public static int ParseDisparityFlags(string txt)
        {
            var flagsList = txt.Split('|');
            int flags = 0;
            foreach(var flag in flagsList)
            {
                if(flag.Equals("Valid", StringComparison.OrdinalIgnoreCase))
                {
                    flags |= (int)DisparityFlags.Valid;
                }
                else if(flag.Equals("Invalid", StringComparison.OrdinalIgnoreCase))
                {
                    flags |= (int)DisparityFlags.Invalid;
                }
                else if(flag.Equals("Occluded", StringComparison.OrdinalIgnoreCase))
                {
                    flags |= (int)DisparityFlags.Occluded;
                }
            }
            return flags;
        }

        public static string DisparityFlagsToString(int flags)
        {
            string res = "";
            if((flags & (int)DisparityFlags.Valid) != 0)
            {
                res += "Valid|";
            }
            if((flags & (int)DisparityFlags.Invalid) != 0)
            {
                res += "Invalid|";
            }
            if((flags & (int)DisparityFlags.Occluded) != 0)
            {
                res += "Occluded|";
            }
            return res;
        }
    }
}
