using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CamCore;
using CamAlgorithms;
using MathNet.Numerics.LinearAlgebra;

namespace TriangulationModule
{
    public class TriangulationAlgorithm : IControllableAlgorithm
    {
        public List<Vector<double>> PointsLeft { set; get; }
        public List<Vector<double>> PointsRight { set; get; }
        public List<Vector<double>> Points3D { get; private set; }
        
        public string Name { get; } = "Triangulation from points";

        TwoPointsTriangulation _triangulation = new TwoPointsTriangulation();

        public bool SupportsFinalResults { get; } = true;
        public bool SupportsPartialResults { get; } = true;

        public bool SupportsProgress { get; } = false;
        public bool SupportsSuspension { get; } = false;
        public bool SupportsTermination { get; } = false;

        public bool SupportsParameters { get; } = false;
        public event EventHandler<EventArgs> ParamtersAccepted;

        private AlgorithmStatus _status = AlgorithmStatus.Idle;
        public AlgorithmStatus Status
        {
            get { return _status; }
            set
            {
                AlgorithmStatus old = _status;
                _status = value;
                StatusChanged?.Invoke(this, new AlgorithmEventArgs()
                { CurrentStatus = _status, OldStatus = old });
            }
        }
        public event EventHandler<AlgorithmEventArgs> StatusChanged;
        
        public void Process()
        {
            Status = AlgorithmStatus.Running;

            _triangulation.CalibData = CalibrationData.Data;
            _triangulation.UseLinearEstimationOnly = true;
            _triangulation.PointsLeft = PointsLeft;
            _triangulation.PointsRight = PointsRight;

            _triangulation.Estimate3DPoints();

            Points3D = _triangulation.Points3D;

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
            //return "Iteration " + _minimalisation.CurrentIteration.ToString() +
            //    " of " + _minimalisation.MaximumIterations.ToString();
            return "";
        }

        public void Suspend() { }

        public void Resume() { }

        public void Terminate() { }

        public void ShowParametersWindow()
        {
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


