using CamCore;
using CamImageProcessing;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace CalibrationModule
{
    public abstract class CalibrationPointsFinder : IParametrizedProcessor
    {
        private List<ProcessorParameter> _parameters;
        public List<ProcessorParameter> Parameters
        {
            get { return _parameters; }
            protected set { _parameters = value; }
        }

        public abstract void InitParameters();
        public abstract void UpdateParameters();

        public ShapeChecker PrimaryShapeChecker { get; set; }
        public ICalibrationLinesExtractor LinesExtractor { get; protected set; }

        public List<CalibrationPoint> Points { get; protected set; }
        public abstract void SetBitmapSource(BitmapSource source);

        public abstract void FindCalibrationPoints();
    }
}
