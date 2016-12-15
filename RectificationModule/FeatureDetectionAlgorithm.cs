using CamControls;
using CamCore;
using CamImageProcessing;
using CamImageProcessing.ImageMatching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RectificationModule
{
    public class FeatureDetectionAlgorithmController : IControllableAlgorithm, IParameterizable
    {
        private FeaturesDetector _detector;

        public IImage ImageLeft { get; set; }
        public IImage ImageRight { get; set; }

        public GrayScaleImage FeatureImageLeft { get; protected set; }
        public GrayScaleImage FeatureImageRight { get; protected set; }
        public List<IntVector2> FeatureListLeft { get; protected set; }
        public List<IntVector2> FeatureListRight { get; protected set; }

        public string Name { get; } = "Features Dectection";

        public bool SupportsFinalResults { get; } = true;
        public bool SupportsPartialResults { get; } = true;

        public bool SupportsProgress { get; } = true;
        public bool SupportsSuspension { get; } = false;
        public bool SupportsTermination { get; } = true;

        public bool SupportsParameters { get; } = true;
        public event EventHandler<EventArgs> ParamtersAccepted;

        private AlgorithmStatus _status = AlgorithmStatus.Idle;
        public AlgorithmStatus Status
        {
            get { return _status; }
            set
            {
                AlgorithmStatus old = _status;
                _status = value;
                StatusChanged?.Invoke(this, new CamCore.AlgorithmEventArgs()
                { CurrentStatus = _status, OldStatus = old });
            }
        }
        public event EventHandler<CamCore.AlgorithmEventArgs> StatusChanged;

        bool _detectingLeft;

        public void Process()
        {
            Status = AlgorithmStatus.Running;
            if(ImageLeft != null)
            {
                _detectingLeft = true;
                _detector.Image = ImageLeft;
                _detector.Detect();

                FeatureImageLeft = _detector.FeatureMap;
                FeatureListLeft = _detector.FeaturePoints;
            }

            if(ImageRight != null)
            {
                _detectingLeft = false;
                _detector.Image = ImageRight;
                _detector.Detect();

                FeatureImageRight = _detector.FeatureMap;
                FeatureListRight = _detector.FeaturePoints;
            }
            Status = AlgorithmStatus.Finished;
        }
        
        protected List<AlgorithmParameter> _parameters;
        public List<AlgorithmParameter> Parameters
        {
            get { return _parameters; }
        }

        public void InitParameters()
        {
            _parameters = new List<AlgorithmParameter>();

            ParametrizedObjectParameter detectorParam = new ParametrizedObjectParameter(
               "Feature Detector", "DETECTOR");

            detectorParam.Parameterizables = new List<IParameterizable>();

            var susan = new FeatureSUSANDetector();
            susan.InitParameters();
            detectorParam.Parameterizables.Add(susan);

            var harrisStephens = new FeatureHarrisStephensDetector();
            harrisStephens.InitParameters();
            detectorParam.Parameterizables.Add(harrisStephens);

            var opencv = new FeatureDetector_OpenCV();
            opencv.InitParameters();
            detectorParam.Parameterizables.Add(opencv);

            _parameters.Add(detectorParam);
        }

        public void UpdateParameters()
        {
            _detector = AlgorithmParameter.FindValue<FeaturesDetector>("DETECTOR", _parameters);
            _detector.UpdateParameters();
        }

        public string GetFinalResults()
        {
            return PrepareResults();
        }

        public string GetPartialResults()
        {
            return PrepareResults();
        }

        public string GetProgress()
        {
            return "Image: " + (_detectingLeft ? "Left" : "Right") + ". Pixel: (" +
                _detector.CurrentPixel.X + ", " + _detector.CurrentPixel.Y +
                ") of [" + ImageLeft.ColumnCount + ", " + ImageLeft.RowCount + "].";
        }

        public void Suspend() { }

        public void Resume() { }

        public void Terminate()
        {
            _detector.Terminate = true;
            Status = AlgorithmStatus.Terminated;
        }

        public void ShowParametersWindow()
        {
            var algChooserWindow = new ParametersSelectionWindow();
            algChooserWindow.Processor = this;
            algChooserWindow.Width = 380;
            algChooserWindow.ShowDialog();
            if(algChooserWindow.Accepted)
            {
                ParamtersAccepted?.Invoke(this, new EventArgs());
            }
        }

        private string PrepareResults()
        {
            StringBuilder result = new StringBuilder();
            result.Append("State: ");

            if(Status == AlgorithmStatus.Finished)
                result.Append("Finished");
            else if(Status != AlgorithmStatus.Error)
                result.Append("Not Finished");
            else
                result.Append("Error");

            result.AppendLine();
            result.AppendLine();

            return result.ToString();
        }
    }
}
