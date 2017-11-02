using System.Collections.Generic;
using CamCore;
using CamAlgorithms;

namespace CalibrationModule.PointsExtraction
{
    public abstract class RefernceShapeChecker : IParameterizable
    {
        public abstract string Name { get; }
        public IImage Image { get; set; }

        public abstract bool CheckShape(CalibrationShape shape);

        private List<AlgorithmParameter> _parameters;
        public List<AlgorithmParameter> Parameters
        {
            get { return _parameters; }
        }

        public virtual void InitParameters()
        {
            _parameters = new List<AlgorithmParameter>();
        }

        public virtual void UpdateParameters()
        {

        }
    }

    public class ColorShapeChecker : RefernceShapeChecker
    {   
        public override string Name { get { return "ColorShapeChecker"; } }

        public Vector3 TargetColor { get; set; }
        public double BrightnessThreshold { get; set; } = 0.5;
        public double ColorValueThreshold { get; set; } = 0.1;
        public double ColorRatioThreshold { get; set; } = 0.2;
        public int NeighbourhoodRadius
        {
            get { return _r; }
            set { _r = value; _winSize = (2 * _r + 1) * (2 * _r + 1); }
        }

        int _r = 3;
        int _winSize = 7;
        double _minColorValue;
        double _maxRatioDiff;
        double _tBrightness;

        public ColorShapeChecker() { }
        public ColorShapeChecker(Vector3 color)
        {
            TargetColor = color;
        }

        void SetParams()
        {
            _tBrightness = BrightnessThreshold;
            _minColorValue = ColorValueThreshold * _tBrightness;
            _maxRatioDiff = ColorRatioThreshold * _tBrightness;
        }

        public override bool CheckShape(CalibrationShape shape)
        {
            SetParams();
            int cx = shape.GravityCenter.X.Round();
            int cy = shape.GravityCenter.Y.Round();
            int matches = 0;
            
            for(int dx = -_r; dx <= _r; ++dx)
            {
                for(int dy= -_r; dy <= _r; ++dy)
                {
                    matches += CheckColorOfPixel(cy + dy, cx + dx) ? 1 : 0; 
                }
            }

            return matches > _winSize * 0.75;
        }

        bool CheckColorOfPixel(int y, int x)
        {
            if(Image[y, x] > _tBrightness) { return false; }
            // Check if color is similar to target one, but make more it light condition resistant : check ratios
            Vector3 pixelColor = new Vector3(Image[y, x, (int)RGBChannel.Red],
                Image[y, x, (int)RGBChannel.Green], Image[y, x, (int)RGBChannel.Blue]);

            return pixelColor.X > (TargetColor.X - 0.01) && 
                pixelColor.Y > (TargetColor.Y - 0.01) && 
                pixelColor.Z > (TargetColor.Z - 0.01) &&
                GetColorRatio(TargetColor).DistanceTo(GetColorRatio(pixelColor)) < _maxRatioDiff;
        }

        Vector3 GetColorRatio(Vector3 color)
        {
            return new Vector3(
                TargetColor.Y > _minColorValue ? TargetColor.X / TargetColor.Y : 0.0,
                TargetColor.Z > _minColorValue ? TargetColor.Y / TargetColor.Z : 0.0,
                TargetColor.X > _minColorValue ? TargetColor.Z / TargetColor.X : 0.0).Normalised();
        }
        
        public override void InitParameters()
        {
            base.InitParameters();

            Parameters.Add(new DoubleParameter("Target Red", "TargetRed", 0.0, 0.0, 1.0));
            Parameters.Add(new DoubleParameter("Target Green", "TargetGreen", 0.0, 0.0, 1.0));
            Parameters.Add(new DoubleParameter("Target Blue", "TargetBlue", 0.0, 0.0, 1.0));

            Parameters.Add(new IntParameter("Check Neighbourhood Radius", "NeighbourhoodRadius", 3, 1, 99));
            Parameters.Add(new DoubleParameter("Brightness Threshold", "BrightnessThreshold", 0.5, 0.0, 1.0));
            Parameters.Add(new DoubleParameter("Brightness Threshold", "ColorValueThreshold", 0.1, 0.0, 10.0));
            Parameters.Add(new DoubleParameter("Brightness Threshold", "ColorRatioThreshold", 0.2, 0.0, 10.0));
        }

        public override void UpdateParameters()
        {
            base.UpdateParameters();
            NeighbourhoodRadius = AlgorithmParameter.FindValue<int>("NeighbourhoodRadius", Parameters);
            BrightnessThreshold = AlgorithmParameter.FindValue<double>("BrightnessThreshold", Parameters);
            ColorValueThreshold = AlgorithmParameter.FindValue<double>("ColorValueThreshold", Parameters);
            ColorRatioThreshold = AlgorithmParameter.FindValue<double>("ColorRatioThreshold", Parameters);

            TargetColor = new Vector3()
            {
                X = AlgorithmParameter.FindValue<double>("TargetRed", Parameters),
                Y = AlgorithmParameter.FindValue<double>("TargetGreen", Parameters),
                Z = AlgorithmParameter.FindValue<double>("TargetBlue", Parameters)
            };
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
