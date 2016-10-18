using CamCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CamImageProcessing.ImageMatching
{
    public enum DisparityFlags : int
    {
        None = 0,
        Valid = 1,
        Invalid = 2,
        Occluded = 4,
    }

    [DebuggerDisplay("X = {DX}, Y = {DY}, c = {Cost}, t = {Confidence}")]
    public class Disparity : ICloneable
    {
        public int DX;
        public int DY;
        public double SubDX;
        public double SubDY;
        public double Cost;
        public double Confidence;
        public int Flags;

        public Disparity()
        {
        }

        public Disparity(IntVector2 pixelBase, IntVector2 pixelMacthed, double cost = 0.0, double confidence = 0.0, int flags = 0)
        {
            DX = pixelBase.X - pixelMacthed.X;
            DY = pixelBase.Y - pixelMacthed.Y;
            SubDX = DX;
            SubDY = DY;
            Cost = cost;
            Confidence = confidence;
            Flags = flags;
        }

        public IntVector2 GetMatchedPixel(IntVector2 pixelBase)
        {
            return new IntVector2(pixelBase.X - DX, pixelBase.Y - DY);
        }

        public void GetMatchedPixel(IntVector2 pixelBase, IntVector2 pixelMatched)
        {
            pixelMatched.X = pixelBase.X - DX;
            pixelMatched.Y = pixelBase.Y - DY;
        }

        public IntVector2 GetBasePixel(IntVector2 pixelMatched)
        {
            return new IntVector2(pixelMatched.X + DX, pixelMatched.Y + DY);
        }

        public void GetBasePixel(IntVector2 pixelMatched, IntVector2 pixelBase)
        {
            pixelBase.X = pixelMatched.X + DX;
            pixelBase.Y = pixelMatched.Y + DY;
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
                DY = DY,
                Cost = Cost,
                Confidence = Confidence,
                Flags = Flags,
                SubDX = SubDX,
                SubDY = SubDY
            };
        }

        public override string ToString()
        {
            return "X = " + DX + ", Y = " + DY + ", c = " + Cost + ", t = " + Confidence;
        }

        public XmlNode CreateDisparityNode(XmlDocument xmlDoc)
        {
            XmlNode node = xmlDoc.CreateElement("Disparity");

            XmlAttribute dxAtt = xmlDoc.CreateAttribute("dx");
            dxAtt.Value = DX.ToString();
            XmlAttribute dyAtt = xmlDoc.CreateAttribute("dy");
            dyAtt.Value = DY.ToString();
            XmlAttribute sdxAtt = xmlDoc.CreateAttribute("subdx");
            sdxAtt.Value = SubDX.ToString();
            XmlAttribute sdyAtt = xmlDoc.CreateAttribute("subdy");
            sdyAtt.Value = SubDY.ToString();
            XmlAttribute costAtt = xmlDoc.CreateAttribute("cost");
            costAtt.Value = Cost.ToString();
            XmlAttribute confAtt = xmlDoc.CreateAttribute("confidence");
            confAtt.Value = Confidence.ToString();
            XmlAttribute flagsAtt = xmlDoc.CreateAttribute("flags");
            flagsAtt.Value = DisparityFlagsToString(Flags);

            node.Attributes.Append(dxAtt);
            node.Attributes.Append(dyAtt);
            node.Attributes.Append(sdxAtt);
            node.Attributes.Append(sdyAtt);
            node.Attributes.Append(costAtt);
            node.Attributes.Append(confAtt);
            node.Attributes.Append(flagsAtt);

            return node;
        }

        public void ReadFromNode(XmlNode node)
        {
            DX = int.Parse(node.Attributes["dx"].Value);
            DY = int.Parse(node.Attributes["dy"].Value);
            SubDX = int.Parse(node.Attributes["subdx"].Value);
            SubDY = int.Parse(node.Attributes["subdy"].Value);
            Cost = double.Parse(node.Attributes["cost"].Value);
            Confidence = double.Parse(node.Attributes["confidence"].Value);
            Flags = ParseDisparityFlags(node.Attributes["flags"].Value);
        }

        public static Disparity CreateFromNode(XmlNode node)
        {
            Disparity disp = new Disparity()
            {
                DX = int.Parse(node.Attributes["dx"].Value),
                DY = int.Parse(node.Attributes["dy"].Value),
                SubDX = int.Parse(node.Attributes["subdx"].Value),
                SubDY = int.Parse(node.Attributes["subdy"].Value),
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
