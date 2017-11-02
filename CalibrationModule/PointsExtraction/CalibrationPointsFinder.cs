using CamCore;
using CamAlgorithms;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace CalibrationModule.PointsExtraction
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
