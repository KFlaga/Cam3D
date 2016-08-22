using CamCore;

namespace CalibrationModule
{
    public class CalibrationPoint
    {
        private Vector2 _img;
        public Vector2 Img { get { return _img; } set { _img.X = value.X; _img.Y = value.Y; } }

        private Point3D _real;
        public Point3D Real { get { return _real; } set { _real.X = value.X; _real.Y = value.Y; _real.Z = value.Z; } }

        private TPoint2D<int> _realGridPos;
        public TPoint2D<int> RealGridPos { get { return _realGridPos; } set { _realGridPos.X = value.X; _realGridPos.Y = value.Y; } }

        public double ImgX { get { return Img.X; } set { Img.X = value; } }
        public double ImgY { get { return Img.Y; } set { Img.Y = value; } }

        public int RealRow { get { return _realGridPos.Y; } set { _realGridPos.Y = value; } }
        public int RealCol { get { return _realGridPos.X; } set { _realGridPos.X = value; } }

        public int GridNum { get; set; }

        public double RealX { get { return Real.X; } set { Real.X = value; } }
        public double RealY { get { return Real.Y; } set { Real.Y = value; } }
        public double RealZ { get { return Real.Z; } set { Real.Z = value; } }

        public CalibrationPoint()
        {
            _img = new Vector2();
            _real = new Point3D();
            _realGridPos = new TPoint2D<int>(-1, -1);
        }

        public override string ToString()
        {
            return "( X = " + ImgX.ToString("F1") + " Y = " + ImgY.ToString("F1") + " )";
        }
    }
}
