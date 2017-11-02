using System;
using System.Text;
using System.Threading.Tasks;
using CamCore;
using CamAlgorithms;
using MathNet.Numerics.LinearAlgebra;
using CamAlgorithms.ImageMatching;
using CamControls;

namespace ImageMatchingModule
{
    public class ImageMatchingAlgorithmController : IControllableAlgorithm
    {
        //private ImageMatchingAlgorithm _matcher = new GenericImageMatchingAlgorithm();
        private ImageMatchingAlgorithm _matcher = new CppSgmMatchingAlgorithm();

        public IImage ImageLeft { get; set; }
        public IImage ImageRight { get; set; }

        public DisparityMap MapLeft { get { return _matcher.MapLeft; } }
        public DisparityMap MapRight { get { return _matcher.MapRight; } }

        public string Name { get; } = "Dense Image Matching";

        public bool SupportsFinalResults { get; } = true;
        public bool SupportsPartialResults { get; } = true;

        public bool SupportsProgress { get; } = true;
        public bool SupportsSuspension { get; } = false;
        public bool SupportsTermination { get; } = false;

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
            _matcher.ImageLeft = ImageLeft;
            _matcher.ImageRight = ImageRight;
            _matcher.MatchImages();
            Status = AlgorithmStatus.Finished;
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
            return _matcher.GetProgress();
        }

        public void Suspend() { }

        public void Resume() { }

        public void Terminate() { }

        public void ShowParametersWindow()
        {
            var algChooserWindow = new ParametersSelectionWindow();
            algChooserWindow.Processor = _matcher;
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

            result.AppendLine("Current results:");
            result.Append(_matcher.GetStatus());

            return result.ToString();
        }
    }
}


