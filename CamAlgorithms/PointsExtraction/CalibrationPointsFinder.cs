using CamCore;
using System.Collections.Generic;
using CamAlgorithms.Calibration;

namespace CamAlgorithms.PointsExtraction
{
    public abstract class CalibrationPointsFinder : IParameterizable
    {
        private List<IAlgorithmParameter> _parameters;
        public List<IAlgorithmParameter> Parameters
        {
            get { return _parameters; }
        }

        public virtual void InitParameters()
        {
            _parameters = new List<IAlgorithmParameter>();
        }

        public virtual void UpdateParameters()
        {

        }

        public abstract string Name { get; }
        public IImage Image { get; set; }
        
        public ICalibrationLinesExtractor LinesExtractor { get; set; }

        public List<CalibrationPoint> Points { get; protected set; }

        public abstract void FindCalibrationPoints();
    }
}
