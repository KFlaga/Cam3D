
using CamCore;
using System.Xml;
using System.Xml.Serialization;

namespace CalibrationModule
{
    public class RealGridData
    {
        [XmlAttribute("Num")]
        public int Num { get; set; }
        [XmlAttribute("Label")]
        public string Label { get; set; } = "";

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
        public IntVector2 OffsetLeft { get; set; } = new IntVector2(0, 0);
        public IntVector2 OffsetRight { get; set; } = new IntVector2(0, 0);

        private double WidthTop_X { get { return TopRight.X - TopLeft.X; } }
        private double WidthTop_Y { get { return TopRight.Y - TopLeft.Y; } }
        private double WidthTop_Z { get { return TopRight.Z - TopLeft.Z; } }
        
        private double WidthBot_X { get { return BotRight.X - BotLeft.X; } }
        private double WidthBot_Y { get { return BotRight.Y - BotLeft.Y; } }
        private double WidthBot_Z { get { return BotRight.Z - BotLeft.Z; } }
        
        private double HeightLeft_X { get { return BotLeft.X - TopLeft.X; } }
        private double HeightLeft_Y { get { return BotLeft.Y - TopLeft.Y; } }
        private double HeightLeft_Z { get { return BotLeft.Z - TopLeft.Z; } }

        private double HeightRight_X { get { return BotRight.X - TopRight.X; } }
        private double HeightRight_Y { get { return BotRight.Y - TopRight.Y; } }
        private double HeightRight_Z { get { return BotRight.Z - TopRight.Z; } }

        public Vector3 GetRealFromCell(int row, int col)
        {
            double ry = (double)row / (double)(Rows-1);
            double rx = (double)col / (double)(Columns-1);
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
                0.5 * ((rx1 + ry1) * (TopLeft.X + WidthTop_X * rx + HeightLeft_X * ry) +
                (rx + ry) * (BotRight.X - WidthBot_X * rx1 - HeightRight_X * ry1)),
                0.5 * ((rx1 + ry1) * (TopLeft.Y + WidthTop_Y * rx + HeightLeft_Y * ry) +
                (rx + ry) * (BotRight.Y - WidthBot_Y * rx1 - HeightRight_Y * ry1)),
                0.5 * ((rx1 + ry1) * (TopLeft.Z + WidthTop_Z * rx + HeightLeft_Z * ry) +
                (rx + ry) * (BotRight.Z - WidthBot_Z * rx1 - HeightRight_Z * ry1)));
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
        }
    }
}
