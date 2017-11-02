using CamCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CamAlgorithms;
using CamControls;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace CalibrationModule
{
    class CameraCalibrationAlgorithmUi : IControllableAlgorithm
    {
        public CalibrationAlgorithm Algorithm { get; set; }
        public Matrix<double> CameraMatrix { set { Algorithm.CameraMatrix = value; } get { return Algorithm.CameraMatrix; } }
        public List<CalibrationPoint> Points { set { Algorithm.Points = value; } get { return Algorithm.Points; } }
        public List<RealGridData> Grids { set { Algorithm.Grids = value; } get { return Algorithm.Grids; } }

        public string Name
        {
            get
            {
                return Algorithm.Name;
            }
        }

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
            Algorithm._miniAlg.Terminate = false;
            Algorithm.Calibrate();
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
            if(Algorithm._linearEstimationDone)
            {
                return "Iteration " + Algorithm._miniAlg.CurrentIteration.ToString() +
                    " of " + Algorithm._miniAlg.MaximumIterations.ToString();
            }
            else
            {
                return "Computing linear estimation of camera matrix";
            }
        }

        public void Suspend() { }

        public void Resume() { }

        public void Terminate()
        {
            Algorithm._miniAlg.Terminate = true;
        }

        public void ShowParametersWindow()
        {
            var paramsWindow = new ParametersSelectionWindow();
            paramsWindow.Processor = Algorithm;
            paramsWindow.Width = 350;
            paramsWindow.ShowDialog();
            if(paramsWindow.Accepted)
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

            Matrix<double> cameraMatrix = Algorithm.CameraMatrix.Clone();

            if(Algorithm._linearEstimationDone)
            {
                if(Algorithm._inMinimsation)
                {
                    var estimatedParams = Algorithm._miniAlg.BestResultVector;
                    cameraMatrix[0, 0] = estimatedParams[0];
                    cameraMatrix[0, 1] = estimatedParams[1];
                    cameraMatrix[0, 2] = estimatedParams[2];
                    cameraMatrix[0, 3] = estimatedParams[3];
                    cameraMatrix[1, 0] = estimatedParams[4];
                    cameraMatrix[1, 1] = estimatedParams[5];
                    cameraMatrix[1, 2] = estimatedParams[6];
                    cameraMatrix[1, 3] = estimatedParams[7];
                    cameraMatrix[2, 0] = estimatedParams[8];
                    cameraMatrix[2, 1] = estimatedParams[9];
                    cameraMatrix[2, 2] = estimatedParams[10];
                    cameraMatrix[2, 3] = estimatedParams[11];
                }

                if(Algorithm._pointsNormalised)
                {
                    cameraMatrix = Algorithm.DenormaliseCameraMatrix(cameraMatrix, Algorithm.NormImage, Algorithm.NormReal);
                }

                result.AppendLine("Camera Matrix: ");

                // TODO: add matrix to string in matrix extensions or somewhere
                result.Append("|" + cameraMatrix[0, 0].ToString("F3"));
                result.Append("; " + cameraMatrix[0, 1].ToString("F3"));
                result.Append("; " + cameraMatrix[0, 2].ToString("F3"));
                result.Append("; " + cameraMatrix[0, 3].ToString("F3"));
                result.AppendLine("|");

                result.Append("|" + cameraMatrix[1, 0].ToString("F3"));
                result.Append("; " + cameraMatrix[1, 1].ToString("F3"));
                result.Append("; " + cameraMatrix[1, 2].ToString("F3"));
                result.Append("; " + cameraMatrix[1, 3].ToString("F3"));
                result.AppendLine("|");

                result.Append("|" + cameraMatrix[2, 0].ToString("F3"));
                result.Append("; " + cameraMatrix[2, 1].ToString("F3"));
                result.Append("; " + cameraMatrix[2, 2].ToString("F3"));
                result.Append("; " + cameraMatrix[2, 3].ToString("F3"));
                result.AppendLine("|");

                Algorithm.DecomposeCameraMatrix();

                result.AppendLine();
                result.AppendLine("Calibration Matrix: ");

                result.Append("|" + Algorithm.CameraInternalMatrix[0, 0].ToString("F3"));
                result.Append("; " + Algorithm.CameraInternalMatrix[0, 1].ToString("F3"));
                result.Append("; " + Algorithm.CameraInternalMatrix[0, 2].ToString("F3"));
                result.AppendLine("|");

                result.Append("|" + Algorithm.CameraInternalMatrix[1, 0].ToString("F3"));
                result.Append("; " + Algorithm.CameraInternalMatrix[1, 1].ToString("F3"));
                result.Append("; " + Algorithm.CameraInternalMatrix[1, 2].ToString("F3"));
                result.AppendLine("|");

                result.Append("|" + Algorithm.CameraInternalMatrix[2, 0].ToString("F3"));
                result.Append("; " + Algorithm.CameraInternalMatrix[2, 1].ToString("F3"));
                result.Append("; " + Algorithm.CameraInternalMatrix[2, 2].ToString("F3"));
                result.AppendLine("|");

                result.AppendLine();
                result.AppendLine("Rotation Matrix: ");

                result.Append("|" + Algorithm.CameraRotationMatrix[0, 0].ToString("F3"));
                result.Append("; " + Algorithm.CameraRotationMatrix[0, 1].ToString("F3"));
                result.Append("; " + Algorithm.CameraRotationMatrix[0, 2].ToString("F3"));
                result.AppendLine("|");

                result.Append("|" + Algorithm.CameraRotationMatrix[1, 0].ToString("F3"));
                result.Append("; " + Algorithm.CameraRotationMatrix[1, 1].ToString("F3"));
                result.Append("; " + Algorithm.CameraRotationMatrix[1, 2].ToString("F3"));
                result.AppendLine("|");

                result.Append("|" + Algorithm.CameraRotationMatrix[2, 0].ToString("F3"));
                result.Append("; " + Algorithm.CameraRotationMatrix[2, 1].ToString("F3"));
                result.Append("; " + Algorithm.CameraRotationMatrix[2, 2].ToString("F3"));
                result.AppendLine("|");

                result.AppendLine();
                result.AppendLine("Translation Vector: ");

                result.Append("|" + Algorithm.CameraTranslation[0].ToString("F3"));
                result.AppendLine("|");
                result.Append("|" + Algorithm.CameraTranslation[1].ToString("F3"));
                result.AppendLine("|");
                result.Append("|" + Algorithm.CameraTranslation[2].ToString("F3"));
                result.AppendLine("|");

                double error = 0.0;
                double relerror = 0.0;
                double rerrx = 0.0;
                double rerry = 0.0;
                for(int p = 0; p < Algorithm.Points.Count; ++p)
                {
                    var cp = Algorithm.Points[p];
                    Vector<double> ip = new DenseVector(new double[] { cp.ImgX, cp.ImgY, 1.0 });
                    Vector<double> rp = new DenseVector(new double[] { cp.RealX, cp.RealY, cp.RealZ, 1.0 });

                    Vector<double> eip = cameraMatrix * rp;
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

                if(Algorithm.MinimaliseSkew == true)
                {
                    result.AppendLine("SkewMini - base residual: " + Algorithm._zeroSkewMini.BaseResidiual.ToString("F4"));
                    result.AppendLine("Skewini - best residual: " + Algorithm._zeroSkewMini.MinimumResidiual.ToString("F4"));
                }

                if(Algorithm.LinearOnly == false)
                {
                    result.AppendLine("GeoMini - base residual: " + Algorithm._miniAlg.BaseResidiual.ToString("F4"));
                    result.AppendLine("GeoMini - best residual: " + Algorithm._miniAlg.MinimumResidiual.ToString("F4"));
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
