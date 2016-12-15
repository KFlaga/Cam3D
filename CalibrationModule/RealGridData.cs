
using CamCore;
using System.Xml;

namespace CalibrationModule
{
    public class RealGridData
    {
        public int Num { get; set; }
        public string Label { get; set; } = "";

        // Old data model
        //public double WidthX { get; set; } // Lenght of cell along local X axis in global frame
        //public double WidthY { get; set; }
        //public double WidthZ { get; set; }
        //public double HeightX { get; set; } // Lenght of cell along local Y axis in global frame
        //public double HeightY { get; set; }
        //public double HeightZ { get; set; }
        //public double ZeroX { get; set; } // Position of (0,0,0) point on real grid in reference to global reference point
        //public double ZeroY { get; set; }
        //public double ZeroZ { get; set; }

        public override string ToString()
        {
            return Num.ToString() + " " + Label;
        }

        // New data model
        public Vector3 TopLeft { get; set; } = new Vector3();
        public Vector3 BotLeft { get; set; } = new Vector3();
        public Vector3 BotRight { get; set; } = new Vector3();
        public Vector3 TopRight { get; set; } = new Vector3();

        public int Rows { get; set; }
        public int Columns { get; set; }
        public IntVector2 Offset { get; set; } = new IntVector2(0, 0);

        public Vector3 WidthTop { get; set; } = new Vector3(); // TR - TL
        public Vector3 WidthBot { get; set; } = new Vector3(); // BR - BL

        public Vector3 HeightLeft { get; set; } = new Vector3(); // BL - TL
        public Vector3 HeightRight { get; set; } = new Vector3(); // BR - TR

        public void Update()
        {
            WidthTop = TopRight - TopLeft;
            WidthBot = BotRight - BotLeft;
            HeightLeft = BotLeft - TopLeft;
            HeightRight = BotRight - TopRight;
        }

        public Vector3 GetRealFromCell(int row, int col)
        {
            double ry = (double)(row + Offset.Y) / (double)(Rows-1);
            double rx = (double)(col + Offset.X) / (double)(Columns-1);
            return GetRealFromRatio(ry, rx);
        }

        public Vector3 GetRealFromCell(IntVector2 cell)
        {
            return GetRealFromCell(cell.Y, cell.X);
        }

        public Vector3 GetRealFromRatio(double ry, double rx)
        {
            double rx1 = 1.0 - rx, ry1 = 1.0 - ry;
            return new Vector3(
                0.5 * ((rx1 + ry1) * (TopLeft.X + WidthTop.X * rx + HeightLeft.X * ry) +
                (rx + ry) * (BotRight.X - WidthBot.X * rx1 - HeightRight.X * ry1)),
                0.5 * ((rx1 + ry1) * (TopLeft.Y + WidthTop.Y * rx + HeightLeft.Y * ry) +
                (rx + ry) * (BotRight.Y - WidthBot.Y * rx1 - HeightRight.Y * ry1)),
                0.5 * ((rx1 + ry1) * (TopLeft.Z + WidthTop.Z * rx + HeightLeft.Z * ry) +
                (rx + ry) * (BotRight.Z - WidthBot.Z * rx1 - HeightRight.Z * ry1)));
            //return new Vector3((TopLeft.X + WidthTop.X * rx + HeightLeft.X * ry),
            //    (TopLeft.Y + WidthTop.Y * rx + HeightLeft.Y * ry),
            //    (TopLeft.Z + WidthTop.Z * rx + HeightLeft.Z * ry));
        }

        public Vector3 GetRealFromRatio(Vector2 ratio)
        {
            return GetRealFromRatio(ratio.Y, ratio.X);
        }

        public void FillFromP1P4(Vector3 p1, Vector3 p4, Vector3 p1p, Vector3 p4p)
        {
            Vector3 h_left_per_mm = (p1 - p4) / (3 * 27.0);
            Vector3 h_right_per_mm = (p1p - p4p) / (3 * 27.0);

            Vector3 p7 = p1 - h_left_per_mm * (6 * 27.0 + 17.0);
            Vector3 p7p = p1p - h_right_per_mm * (6 * 27.0 + 17.0);

            Vector3 w_bot_per_mm = (p1p - p1) / (10 * 17.0 + 9 * 10.0);
            Vector3 w_top_per_mm = (p7p - p7) / (10 * 17.0 + 9 * 10.0);

            TopLeft = p7 + w_top_per_mm * 8.5 + h_left_per_mm * 8.5;
            TopRight = p7p - w_top_per_mm * 8.5 + h_right_per_mm * 8.5;
            BotLeft = p1 + w_bot_per_mm * 8.5 - h_left_per_mm * 8.5;
            BotRight = p1p - w_bot_per_mm * 8.5 - h_right_per_mm * 8.5;

            Update();
        }

        public static RealGridData FromXml(XmlNode gridNode)
        {
            RealGridData grid = new RealGridData();

            grid.Num = int.Parse(gridNode.Attributes["id"].Value);
            grid.Label = gridNode.Attributes["label"].Value;      
            grid.Rows = int.Parse(gridNode.Attributes["rows"].Value);
            grid.Columns = int.Parse(gridNode.Attributes["columns"].Value);

            XmlNode topleftNode = gridNode.SelectSingleNode("child::TopLeft");
            grid.TopLeft = Vector3.CreateFromXmlNode(topleftNode);

            XmlNode toprightNode = gridNode.SelectSingleNode("child::TopRight");
            grid.TopRight = Vector3.CreateFromXmlNode(toprightNode);

            XmlNode botleftNode = gridNode.SelectSingleNode("child::BotLeft");
            grid.BotLeft = Vector3.CreateFromXmlNode(botleftNode);

            XmlNode botrightNode = gridNode.SelectSingleNode("child::BotRight");
            grid.BotRight = Vector3.CreateFromXmlNode(botrightNode);

            XmlNode offsetNode = gridNode.SelectSingleNode("child::Offset");
            grid.Offset = IntVector2.CreateFromXmlNode(botrightNode);

            grid.Update();

            return grid;
        }

        public XmlNode ToXml(XmlDocument dataDoc)
        {
            return ToXml(this, dataDoc);
        }

        public static XmlNode ToXml(RealGridData grid, XmlDocument dataDoc)
        {
            var gridNode = dataDoc.CreateElement("Grid");

            var gridAttNum = dataDoc.CreateAttribute("id");
            gridAttNum.Value = grid.Num.ToString();

            var gridAttLabel = dataDoc.CreateAttribute("label");
            gridAttLabel.Value = grid.Label;

            var gridAttRows = dataDoc.CreateAttribute("rows");
            gridAttLabel.Value = grid.Rows.ToString();

            var gridAttCols = dataDoc.CreateAttribute("columns");
            gridAttLabel.Value = grid.Columns.ToString();

            gridNode.Attributes.Append(gridAttNum);
            gridNode.Attributes.Append(gridAttLabel);
            gridNode.Attributes.Append(gridAttRows);
            gridNode.Attributes.Append(gridAttCols);

            gridNode.AppendChild(grid.TopLeft.CreateXmlNode(dataDoc, "TopLeft"));
            gridNode.AppendChild(grid.TopRight.CreateXmlNode(dataDoc, "TopRight"));
            gridNode.AppendChild(grid.BotLeft.CreateXmlNode(dataDoc, "BotLeft"));
            gridNode.AppendChild(grid.BotRight.CreateXmlNode(dataDoc, "BotRight"));
            gridNode.AppendChild(grid.Offset.CreateXmlNode(dataDoc, "Offset"));
            
            return gridNode;
        }
    }
}
