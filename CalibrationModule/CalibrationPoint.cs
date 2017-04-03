using System.Xml;
using System.Xml.Serialization;
using CamCore;

namespace CalibrationModule
{
    [XmlRoot("Point")]
    public class CalibrationPoint
    {
        private readonly Vector2 _img;
        [XmlIgnore]
        public Vector2 Img { get { return _img; } set { _img.X = value.X; _img.Y = value.Y; } }

        private readonly Vector3 _real;
        [XmlIgnore]
        public Vector3 Real { get { return _real; } set { _real.X = value.X; _real.Y = value.Y; _real.Z = value.Z; } }

        private IntVector2 _realGridPos;
        [XmlIgnore]
        public IntVector2 RealGridPos { get { return _realGridPos; } set { _realGridPos.X = value.X; _realGridPos.Y = value.Y; } }

        [XmlAttribute("ImgX")]
        public double ImgX { get { return Img.X; } set { Img.X = value; } }
        [XmlAttribute("ImgY")]
        public double ImgY { get { return Img.Y; } set { Img.Y = value; } }

        [XmlAttribute("GridRow")]
        public int RealRow { get { return _realGridPos.Y; } set { _realGridPos.Y = value; } }
        [XmlAttribute("GridCol")]
        public int RealCol { get { return _realGridPos.X; } set { _realGridPos.X = value; } }

        [XmlAttribute("GridNum")]
        public int GridNum { get; set; }

        [XmlAttribute("RealX")]
        public double RealX { get { return Real.X; } set { Real.X = value; } }
        [XmlAttribute("RealY")]
        public double RealY { get { return Real.Y; } set { Real.Y = value; } }
        [XmlAttribute("RealZ")]
        public double RealZ { get { return Real.Z; } set { Real.Z = value; } }

        public CalibrationPoint()
        {
            _img = new Vector2();
            _real = new Vector3();
            _realGridPos = new IntVector2(-1, -1);
        }

        public CalibrationPoint Clone()
        {
            return new CalibrationPoint()
            {
                Img = this.Img,
                Real = this.Real,
                GridNum = this.GridNum,
                RealGridPos = this.RealGridPos
            };
        }

        public override string ToString()
        {
            return "( X = " + ImgX.ToString("F1") + " Y = " + ImgY.ToString("F1") + " )";
        }
    }
}
