using CamCore;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Text;
using CamControls;

namespace CalibrationModule
{
    // Radial distortion :
    // Let p=(x,y) be measured image point pd=(xd,yd) be distored image point in coord frame
    // with distortion center as (0,0) and pu=(xu,yu) be undistored image point in same coord frame
    //
    // We have then:
    // xd = (x - cx)/sx, yd = y - cy, where (cx,cy) is distortion center (DC) and sx is aspect ratio (AR)
    // Both DC and AR may differ from camera principal point and aspect, so this transform is general
    // and with AR included also takes into consideration some of tangential distortion effect
    //  
    // Let P be parameter vector, that is distortion model cofficients and cx,cy,sx
    // Let rd = sqrt(xd^2 + yd^2) be radius of pd from DC and ru = sqrt(xu^2 + yu^2) -> of pu
    // 
    // As radial distortion changes radius of point pu equal to ru to rd, then below relations hold:
    // xd = xu * rd/ru, yd = yu * rd/ru
    // Let R(r) be distortion function, so that rd = R(rd) and inverse undistortion function
    // be R^-1(r), so that ru = R^-1(rd), then above relations may be rewritten as
    // xu = xd * R^-1(rd)/rd, yu = yd * R^-1(rd)/rd
    // At last let D(ru) = R(ru)/ru and U(rd) = R^-1(rd)/rd, so that:
    // xu = xd * U(rd), yu = yd * U(rd)
    //
    // Ultimate task is to find function which maps p to pu
    // In order to do so we need to find cx,cy,sx and R^-1(r)
    // For now no assumptions about R(r) or R^-1(r) will be made except obvious invertibility
    // and that d(R)/d(ru)(0) = 1 and that it is parametrized by some vector K, so P = (K, cx, cy, sx)
    //
    // To find best P we need some cost function of P to minimise:
    // becouse radial distortion makes straight lines in real curved on image (unless it goes through DC)
    // then natural cost funtion is based on points non-linearity
    // So having set of points Ld = {(x,y)} on image, line can be fitted to them using
    // least-squares method and residiual e = sum(di^2) may be used as cost
    // If line is desribed by equation Ax + By + C = 0, then di^2 = (Axi + Byi + C)^2/(A^2+B^2)
    // 
    // For minimalisation of e Levenberg-Marquardt will be used
    // As LevenbergMarquardtBaseAlgorithm minimises function e = f(P) - X, then f(P) will be just
    // minimised error and MeasurementVector will be equal to 0
    //
    // Both minimalisation algorithm and model computation will be defined and desribed in other files

    // Minimises radial distortion in points
    // Uses RadialDistortionModel subclass to represent R^-1(r) and MinimalisationAlgorithm subclass
    // to minimise e and find parameter vector
    public class RadialDistortionCorrector : IControllableAlgorithm
    {
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }

        public List<Vector2> MeasuredPoints { set; get; } // Image measurments

        List<Vector2> _pCorr;
        public List<Vector2> CorrectedPoints { get { return _pCorr; } } // Result of correction

        List<List<Vector2>> _lines = new List<List<Vector2>>();
        public List<List<Vector2>> CorrectionLines // List of point sets used for correction
        {
            get { return _lines; }
            set { _lines = value; }
        }

        public RadialDistortionModel DistortionModel { get; set; }

        public string Name { get; } = "Radial Distortion Model - Parameters Estimation";

        public bool SupportsFinalResults { get; } = true;
        public bool SupportsPartialResults { get; } = true;

        public bool SupportsProgress { get; } = true;
        public bool SupportsSuspension { get; } = false;
        public bool SupportsTermination { get; } = false;

        public bool SupportsParameters { get; } = true;

        public AlgorithmStatus Status { get; set; } = AlgorithmStatus.Idle;

        private LMDistortionDirectionalLineFitMinimalisation _minimalisation;

        public event EventHandler<EventArgs> ParamtersAccepted;

        public RadialDistortionCorrector()
        {
            _minimalisation = new LMDistortionDirectionalLineFitMinimalisation();
            _minimalisation.UseCovarianceMatrix = false;
            _minimalisation.MaximumIterations = 100;
        }

        // Computes parameters of model (CorrectionLines should be set)
        public void ComputeCorrectionParameters()
        {
            // 1) Scale lines so that max radius sqrt(w^2+h^2) is equal to 1
            double scale = 1.0 / Math.Sqrt(ImageHeight * ImageHeight + ImageWidth * ImageWidth);
            List<List<Vector2>> scaledLines = new List<List<Vector2>>();
            for(int l = 0; l < _lines.Count; ++l)
            {
                List<Vector2> line = new List<Vector2>();
                for(int p = 0; p < _lines[l].Count; ++p)
                {
                    line.Add(_lines[l][p] * scale);
                }
                scaledLines.Add(line);
            }

            // 2) Scale point choosen as center as well (it should be in pixels)
            DistortionModel.DistortionCenter.X = DistortionModel.DistortionCenter.X;

            int paramsCount = DistortionModel.ParametersCount - 2; // Center
            paramsCount = DistortionModel.ComputesAspect ? paramsCount - 1 : paramsCount; // Aspect

            var dc = DistortionModel.DistortionCenter;
            double scaledWidth = dc.X * scale;
            double scaledHeight = dc.Y * scale;
            DistortionModel.DistortionCenter = new Vector2(scaledWidth, scaledHeight);
            
            // 3) Init minimalisation algorithm
            Vector<double> parVec = new DenseVector(DistortionModel.ParametersCount);
            DistortionModel.Parameters.CopyTo(parVec);
            _minimalisation.ParametersVector = parVec;
            _minimalisation.LinePoints = scaledLines;

            _minimalisation.MaximumResidiual = 0.0;
            foreach(var points in CorrectionLines)
            {
                _minimalisation.MaximumResidiual += points.Count * 0.25 * scale; // max 0.5 pixel deviation for each point (with gives 0.25 squared)
            }

            _minimalisation.Process();

            _minimalisation.BestResultVector.CopyTo(DistortionModel.Parameters);
        }

        // Corrects image points using previously computed model
        public void CorrectPoints()
        {
            _pCorr = new List<Vector2>(MeasuredPoints.Count);
            foreach(var point in MeasuredPoints)
            {
                DistortionModel.P = point;
                DistortionModel.Undistort();
                _pCorr.Add(new Vector2(DistortionModel.Pf));
            }
        }

        public void Process()
        {
            Status = AlgorithmStatus.Running;
            ComputeCorrectionParameters();
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
            return "Iteration " + _minimalisation.CurrentIteration.ToString() +
                " of " + _minimalisation.MaximumIterations.ToString();
        }

        public void Suspend() { }

        public void Resume() { }

        public void Terminate() { }

        public void ShowParametersWindow()
        {
            var algChooserWindow = new ParametrizedProcessorsSelectionWindow();
            algChooserWindow.AddProcessorFamily("Radial Distortion Model");
            algChooserWindow.AddToFamily("Radial Distortion Model", new Rational3RDModel());
            algChooserWindow.AddToFamily("Radial Distortion Model", new Taylor4Model());

            algChooserWindow.ShowDialog();
            if(algChooserWindow.Accepted)
            {
                DistortionModel = algChooserWindow.GetSelectedProcessor("Radial Distortion Model") as RadialDistortionModel;
                _minimalisation.DistortionModel = DistortionModel;
            }
        }

        private string PrepareResults()
        {
            StringBuilder result = new StringBuilder();
            result.Append("State: ");

            if(Status == AlgorithmStatus.Finished)
                result.Append("Finished");
            else if(Status != AlgorithmStatus.Terminated)
                result.Append("Not Finished");
            else
                result.Append("Terminated");

            result.AppendLine();
            result.AppendLine();

            result.Append("Radial Distrotion Model: " + DistortionModel.ToString());
            result.AppendLine("Estmated Paramters:");

            int paramsCount = DistortionModel.ParametersCount - 2; // Center
            paramsCount = DistortionModel.ComputesAspect ? paramsCount - 1 : paramsCount; // Aspect
            for(int k = 0; k < paramsCount; ++k)
            {
                result.AppendLine("K" + k + ": " + DistortionModel.Parameters[k]);
            }
            result.AppendLine("Cx: " + DistortionModel.Parameters[paramsCount]);
            result.AppendLine("Cy: " + DistortionModel.Parameters[paramsCount + 1]);
            if(DistortionModel.ComputesAspect)
            {
                result.AppendLine("Sx: " + DistortionModel.Parameters[paramsCount + 2]);
            }

            result.AppendLine();
            result.AppendLine();

            result.AppendLine("Minimal residiual: " + _minimalisation.MinimumResidiual);
            result.AppendLine("Base residiual: " + _minimalisation.BaseResidiual);

            return result.ToString();
        }
    }
}
