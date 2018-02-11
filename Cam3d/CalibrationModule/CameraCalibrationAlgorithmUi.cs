using CamCore;
using System;
using System.Collections.Generic;
using System.Text;
using CamControls;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using CamAlgorithms.Calibration;

namespace CalibrationModule
{
    class CameraCalibrationAlgorithmUi : IControllableAlgorithm
    {
        public CalibrationAlgorithm Algorithm { get; set; }
        public Camera Camera { get; set; }
        public List<CalibrationPoint> Points { get; set; }
        public List<RealGridData> Grids { get; set; }

        public string Name
        {
            get
            {
                return "Calibration Algorithm";
            }
        }
        
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
            Algorithm.Camera = Camera;
            Algorithm.Points = Points;
            Algorithm.Grids = Grids;

            Algorithm.NonlinearMinimalization.Terminate = false;
            Algorithm.Calibrate();

            Grids = Algorithm.Grids;
            Status = AlgorithmStatus.Finished;
        }

        public string GetResults()
        {
            return PrepareResults();
        }

        public string GetProgress()
        {
            if(Algorithm.IsLinearEstimationDone)
            {
                return "Iteration " + Algorithm.NonlinearMinimalization.CurrentIteration.ToString() +
                    " of " + Algorithm.NonlinearMinimalization.MaximumIterations.ToString();
            }
            else
            {
                return "Computing linear estimation of camera matrix";
            }
        }

        public void Terminate()
        {
            Algorithm.NonlinearMinimalization.Terminate = true;
        }

        public void ShowParametersWindow()
        {
            var paramsWindow = new ParametrizableSelectionWindow();
            paramsWindow.AddParametrizable(new CalibrationWithGrids());
            paramsWindow.AddParametrizable(new CalibrationHartleyZisserman());

            paramsWindow.Width = 450;
            paramsWindow.ShowDialog();
            if(paramsWindow.Accepted)
            {
                Algorithm = (CalibrationAlgorithm)paramsWindow.Selected;
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

            Camera camera = Algorithm.Camera.Clone();

            if(Algorithm.IsLinearEstimationDone)
            {
                if(Algorithm.IsInNonlinearMinimzation)
                {
                    camera.Matrix.CopyFromVector(Algorithm.NonlinearMinimalization.BestResultVector);
                }

                if(Algorithm.IsPointsNormalized)
                {
                    camera.Denormalize(Algorithm.NormImage, Algorithm.NormReal);
                }

                camera.Decompose();
                result.AppendLine(camera.ToString());
                
                double error = 0.0;
                double relerror = 0.0;
                double rerrx = 0.0;
                double rerry = 0.0;
                for(int p = 0; p < Algorithm.Points.Count; ++p)
                {
                    var cp = Algorithm.Points[p];
                    Vector<double> ip = new DenseVector(new double[] { cp.ImgX, cp.ImgY, 1.0 });
                    Vector<double> rp = new DenseVector(new double[] { cp.RealX, cp.RealY, cp.RealZ, 1.0 });

                    Vector<double> eip = camera.Matrix * rp;
                    eip.DivideThis(eip[2]);

                    var d = (ip - eip);
                    error += d.L2Norm();
                    ip[2] = 0.0;
                    relerror += d.L2Norm() / ip.L2Norm();
                    rerrx += Math.Abs(d[0]) / Math.Abs(ip[0]);
                    rerry += Math.Abs(d[1]) / Math.Abs(ip[1]);
                }

                result.AppendLine();
                result.AppendLine("Projection error ( d(xi, PXr)^2 ): ");
                result.AppendLine("Points count: " + Algorithm.Points.Count.ToString());
                result.AppendLine("Total: " + error.ToString("F4"));
                result.AppendLine("Mean: " + (error / Algorithm.Points.Count).ToString("F4"));
                result.AppendLine("Realtive: " + (relerror).ToString("F4"));
                result.AppendLine("Realtive mean: " + (relerror / Algorithm.Points.Count).ToString("F4"));
                result.AppendLine("Realtive in X: " + (rerrx).ToString("F4"));
                result.AppendLine("Realtive in X mean: " + (rerrx / Algorithm.Points.Count).ToString("F4"));
                result.AppendLine("Realtive in Y: " + (rerry).ToString("F4"));
                result.AppendLine("Realtive in Y mean: " + (rerry / Algorithm.Points.Count).ToString("F4"));

                if(Algorithm.MinimalizeSkew == true)
                {
                    result.AppendLine("SkewMini - base residual: " + Algorithm.ZeroSkewMinimalisation.BaseResidiual.ToString("F4"));
                    result.AppendLine("Skewini - best residual: " + Algorithm.ZeroSkewMinimalisation.MinimumResidiual.ToString("F4"));
                }

                if(Algorithm.LinearOnly == false)
                {
                    result.AppendLine("GeoMini - base residual: " + Algorithm.NonlinearMinimalization.BaseResidiual.ToString("F4"));
                    result.AppendLine("GeoMini - best residual: " + Algorithm.NonlinearMinimalization.MinimumResidiual.ToString("F4"));
                }
            }
            else
            {
                result.AppendLine("Camera not yet computed");
                return result.ToString();
            }

            return result.ToString();
        }
    }
}
