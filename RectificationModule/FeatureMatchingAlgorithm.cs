using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CamCore;
using CamAlgorithms;
using CamControls;

namespace RectificationModule
{
    public class FeatureMatchingAlgorithm : IControllableAlgorithm, IParameterizable
    {
        private FeaturesMatcher _matcher;

        public IImage ImageLeft { get; set; }
        public IImage ImageRight { get; set; }
        
        public List<IntVector2> FeatureListLeft { get; set; }
        public List<IntVector2> FeatureListRight { get; set; }
        public List<MatchedPair> Matches { get; protected set; }

        public string Name { get; } = "Features Matching";

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
        
        public void Process()
        {
            Status = AlgorithmStatus.Running;

            _matcher.LeftImage = ImageLeft;
            _matcher.RightImage = ImageRight;
            _matcher.LeftFeaturePoints = FeatureListLeft;
            _matcher.RightFeaturePoints = FeatureListRight;
            _matcher.Match();
            
            Matches = _matcher.Matches;

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

            ParametrizedObjectParameter matcherParam = new ParametrizedObjectParameter(
               "Feature Matcher", "MATCHER");

            matcherParam.Parameterizables = new List<IParameterizable>();

            var moments = new MomentsFeatureMatcher();
            moments.InitParameters();
            matcherParam.Parameterizables.Add(moments);

            var census = new CensusFeatureMatcher();
            census.InitParameters();
            matcherParam.Parameterizables.Add(census);

            var opencv = new FeatureMatcher_OpenCV();
            opencv.InitParameters();
            matcherParam.Parameterizables.Add(opencv);

            _parameters.Add(matcherParam);
        }

        public void UpdateParameters()
        {
            _matcher = AlgorithmParameter.FindValue<FeaturesMatcher>("MATCHER", _parameters);
            _matcher.UpdateParameters();
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
            //return "Image: " + (_detectingLeft ? "Left" : "Right") + ". Pixel: (" +
            //    _detector.CurrentPixel.X + ", " + _detector.CurrentPixel.Y +
            //    ") of [" + ImageLeft.ColumnCount + ", " + ImageLeft.RowCount + "].";
            return "";
        }

        public void Suspend() { }

        public void Resume() { }

        public void Terminate() { }

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
