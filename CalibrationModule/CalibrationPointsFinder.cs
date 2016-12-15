using CamCore;
using CamImageProcessing;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace CalibrationModule
{
    public abstract class CalibrationPointsFinder : IParameterizable
    {
        private List<AlgorithmParameter> _parameters;
        public List<AlgorithmParameter> Parameters
        {
            get { return _parameters; }
        }

        public virtual void InitParameters()
        {
            _parameters = new List<AlgorithmParameter>();

            ParametrizedObjectParameter shapeCheckParam = new ParametrizedObjectParameter(
                "Primary Point Checker", "PPC");
            shapeCheckParam.Parameterizables = new List<IParameterizable>();
            var redNCheck = new RedNeighbourhoodChecker();
            redNCheck.InitParameters();

            shapeCheckParam.Parameterizables.Add(redNCheck);

            _parameters.Add(shapeCheckParam);
        }

        public virtual void UpdateParameters()
        {

        }

        public abstract string Name { get; }
        public IImage Image { get; set; }

        public ShapeChecker PrimaryShapeChecker { get; set; }
        public ICalibrationLinesExtractor LinesExtractor { get; protected set; }

        public List<CalibrationPoint> Points { get; protected set; }

        public abstract void FindCalibrationPoints();
    }
}
