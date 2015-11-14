
namespace CalibrationModule
{
    public class CalibrationPoint
    {
        public float ImgX { get ;set; }
        public float ImgY { get; set; }
        public int RealRow { get; set; }
        public int RealCol { get; set; }
        public int GridNum { get; set; }

        public float RealX { get; set; }
        public float RealY { get; set; }
        public float RealZ { get; set; }

        public override string ToString()
        {
            return "( X = " + ImgX.ToString("F1") + " Y = " + ImgY.ToString("F1") + " )";
        }
    }
}
