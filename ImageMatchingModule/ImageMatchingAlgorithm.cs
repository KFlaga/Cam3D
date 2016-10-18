using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CamCore;
using CamImageProcessing;
using MathNet.Numerics.LinearAlgebra;
using CamImageProcessing.ImageMatching;
using CamControls;

namespace ImageMatchingModule
{
    public class ImageMatchingAlgorithmController : IControllableAlgorithm
    {
        private ImageMatchingAlgorithm _matcher = new ImageMatchingAlgorithm();

        public ColorImage ImageLeft { get; set; }
        public ColorImage ImageRight { get; set; }

        public DisparityMap MapLeft { get { return _matcher.MapLeft; } }
        public DisparityMap MapRight { get { return _matcher.MapRight; } }

        public string Name { get; } = "Radial Distortion Model - Parameters Estimation";

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
            return "Run: " + (_matcher.Aggregator.IsLeftImageBase ? "1" : "2") + ". Pixel: (" +
                _matcher.Aggregator.CurrentPixel.X + ", " + _matcher.Aggregator.CurrentPixel.Y + 
                ") of [" + ImageLeft.SizeX + ", " + ImageLeft.SizeY + "].";
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
            result.AppendLine();

            //result.AppendLine("Radial Distrotion Model: " + DistortionModel.ToString());
            //result.AppendLine("Estmated Paramters:");

            //int paramsCount = DistortionModel.ParametersCount - 2; // Center
            //paramsCount = DistortionModel.ComputesAspect ? paramsCount - 1 : paramsCount; // Aspect
            //for(int k = 0; k < paramsCount; ++k)
            //{
            //    result.AppendLine("K" + k + ": " + DistortionModel.Parameters[k]);
            //}
            //result.AppendLine("Cx: " + DistortionModel.Parameters[paramsCount] / Scale);
            //result.AppendLine("Cy: " + DistortionModel.Parameters[paramsCount + 1] / Scale);
            //if(DistortionModel.ComputesAspect)
            //{
            //    result.AppendLine("Sx: " + DistortionModel.Parameters[paramsCount + 2]);
            //}

            //result.AppendLine();

            //result.AppendLine("Minimal residiual: " + _minimalisation.MinimumResidiual);
            //result.AppendLine("Base residiual: " + _minimalisation.BaseResidiual);

            return result.ToString();
        }
    }
}


