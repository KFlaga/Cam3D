using CamCore;
using System.Collections.Generic;
using System;
using System.Text;
using CamAlgorithms.Calibration;

namespace CalibrationModule
{
    class RadialDistrotionCorrectionAlgorithmUi : IControllableAlgorithm
    {
        public RadialDistrotionCorrectionAlgorithm Algorithm { get; } = new RadialDistrotionCorrectionAlgorithm();
        public RadialDistortion Distortion { get { return Algorithm.Distortion; } set { Algorithm.Distortion = value; } }

        public int ImageWidth { get { return Algorithm.ImageWidth; } set { Algorithm.ImageWidth = value; } }
        public int ImageHeight { get { return Algorithm.ImageHeight; } set { Algorithm.ImageHeight = value; } }
        public double Scale { get { return Algorithm.Scale; } set { Algorithm.Scale = value; } }
        public List<List<Vector2>> CorrectionLines { get { return Algorithm.CorrectionLines; } set { Algorithm.CorrectionLines = value; } }

        public bool IsTerminable { get; } = true;
        public bool IsParametrizable { get; } = true;
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
            Algorithm.FindModelParameters();
            Status = AlgorithmStatus.Finished;
        }

        public string GetResults()
        {
            return PrepareResults();
        }

        public string GetProgress()
        {
            return "Iteration " + Algorithm.CurrentIteration.ToString() +
                " of " + Algorithm.MaxIterations.ToString();
        }

        public void Terminate()
        {
            Algorithm.Terminate();
        }

        public void ShowParametersWindow()
        {
            // TODO
            //var algChooserWindow = new ParametrizedProcessorsSelectionWindow();
            //algChooserWindow.AddProcessorFamily("Radial Distortion Model");
            //algChooserWindow.AddToFamily("Radial Distortion Model", new Rational3RDModel());
            //algChooserWindow.AddToFamily("Radial Distortion Model", new Taylor4Model());

            //algChooserWindow.ShowDialog();
            //if(algChooserWindow.Accepted)
            //{
            //    DistortionModel = algChooserWindow.GetSelectedProcessor("Radial Distortion Model") as RadialDistortionModel;
            //    _minimalisation.DistortionModel = DistortionModel;
            //    _modelParams = DistortionModel.Parameters.Clone();
            //    ParamtersAccepted?.Invoke(this, new EventArgs());
            //}
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

            result.AppendLine("Radial Distrotion Model: " + Distortion.ToString());
            result.AppendLine("Estmated Paramters:");

            int paramsCount = Distortion.Model.ParametersCount - 2; // Center
            paramsCount = Distortion.Model.ComputesAspect ? paramsCount - 1 : paramsCount; // Aspect
            for(int k = 0; k < paramsCount; ++k)
            {
                result.AppendLine("K" + k + ": " + Distortion.Model.Coeffs[k]);
            }
            result.AppendLine("Cx: " + Distortion.Model.Coeffs[paramsCount] / Algorithm.Scale);
            result.AppendLine("Cy: " + Distortion.Model.Coeffs[paramsCount + 1] / Algorithm.Scale);
            if(Distortion.Model.ComputesAspect)
            {
                result.AppendLine("Sx: " + Distortion.Model.Coeffs[paramsCount + 2]);
            }

            result.AppendLine();

            result.AppendLine("Minimal residiual: " + Algorithm.BestResidiual);
            result.AppendLine("Base residiual: " + Algorithm.InitialResidiual);

            return result.ToString();
        }

        public string Name { get; } = "Radial Distortion Model - Parameters Estimation";
        public override string ToString()
        {
            return Name;
        }
    }
}