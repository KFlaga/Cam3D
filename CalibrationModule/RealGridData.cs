
namespace CalibrationModule
{
    public class RealGridData
    {
        public int Num { get; set; }
        public string Label { get; set; }
        public double WidthX { get; set; } // Lenght of cell along local X axis in global frame
        public double WidthY { get; set; }
        public double WidthZ { get; set; }
        public double HeightX { get; set; } // Lenght of cell along local Y axis in global frame
        public double HeightY { get; set; }
        public double HeightZ { get; set; }
        public double ZeroX { get; set; } // Position of (0,0,0) point on real grid in reference to global reference point
        public double ZeroY { get; set; }
        public double ZeroZ { get; set; }

        public override string ToString()
        {
            return Num.ToString() + " " + Label;
        }

        public RealGridData()
        {
            Label = "";
        }
    }
}
