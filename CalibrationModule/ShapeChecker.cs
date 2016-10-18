using System.Collections.Generic;
using CamCore;
using CamImageProcessing;

namespace CalibrationModule
{
    // Returns true if shape fills arbitrary condition ( intended for primary shape test )
    public abstract class ShapeChecker : IParameterizable
    {
        public GrayScaleImage ImageGray { get; set; }
        public ColorImage ImageRGB { get; set; }
        public HSIImage ImageHSI { get; set; }

        public abstract bool CheckShape(CalibrationShape shape);

        private List<AlgorithmParameter> _parameters;
        public List<AlgorithmParameter> Parameters
        {
            get { return _parameters; }
            protected set { _parameters = value; }
        }

        public abstract void InitParameters();
        public abstract void UpdateParameters();
    }

    // Qualifies shape as primary one if most (75%) of centers' neighbourhood (def r=3) 
    // have at least 0.25 red (treshold adjustable -> may be greater for bright images to avoid false-positives)
    public class RedNeighbourhoodChecker : ShapeChecker
    {
        int _r;
        int _winSize;
        public int NeighbourhoodRadius
        {
            get { return _r; }
            set { _r = value; _winSize = (2 * _r + 1) * (2 * _r + 1); }
        }

        double _minRed;
        public double RedTreshold
        {
            get { return _minRed; }
            set { _minRed = value; }
        }
        
        public RedNeighbourhoodChecker(int radius = 3, double redTreshold = 0.25f)
        {
            NeighbourhoodRadius = radius;
            _minRed = redTreshold;
        }

        public override bool CheckShape(CalibrationShape shape)
        {
            int cx = shape.GravityCenter.X.Round();
            int cy = shape.GravityCenter.Y.Round();
            int redCount = 0;

            for(int dx = -_r; dx <= _r; ++dx)
            {
                for(int dy= -_r; dy <= _r; ++dy)
                {
                    redCount = ImageRGB[RGBChannel.Red][cy + dy, cx + dx] > _minRed ? 
                        redCount + 1 : redCount; 
                }
            }

            return redCount > _winSize * 0.75;
        }
        
        public override void InitParameters()
        {
            Parameters = new List<AlgorithmParameter>();

            AlgorithmParameter radius = new IntParameter(
                "Check Neighbourhood Radius", "CNR",  3, 1, 99);
            Parameters.Add(radius);

            AlgorithmParameter redTresh = new DoubleParameter(
               "Red Value Treshold", "RVT", 0.25, 0.0, 1.0);
            Parameters.Add(redTresh);
        }

        public override void UpdateParameters()
        {
            NeighbourhoodRadius = AlgorithmParameter.FindValue<int>("CNR", Parameters);
            RedTreshold = AlgorithmParameter.FindValue<double>("RVT", Parameters);
        }

        public override string ToString()
        {
            return "RedNeighbourhoodChecker";
        }
    }
}
