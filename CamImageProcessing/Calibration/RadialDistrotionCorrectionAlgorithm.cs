using CamCore;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;

namespace CamAlgorithms.Calibration
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
    public class RadialDistrotionCorrectionAlgorithm : IParameterizable
    {
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
        public double Scale { get; set; }
        
        public List<List<Vector2>> CorrectionLines { set; get; } = new List<List<Vector2>>(); // List of point sets used for correction

        public RadialDistortionModel DistortionModel { get; set; }
        public bool FindInitialModelParameters { get; set; }

        public int MaxIterations { get; set; } = 100;
        public int CurrentIteration { get { return _minimalization.CurrentIteration; } }
        
        public double InitialResidiual { get { return _minimalization != null ? _minimalization.BaseResidiual : -1.0; } }
        public double BestResidiual { get { return _minimalization != null ? _minimalization.MinimumResidiual : -1.0; } }

        protected LMDistortionDirectionalLineFitMinimalisation _minimalization;
        protected List<List<Vector2>> _scaledLines;
        
        public void FindModelParameters()
        {
            if(CorrectionLines.Count == 0) { throw new Exception("CorrectionLines not set"); }
            
            // Find scale so that max radius is sqrt(w^2+h^2) is equal to 1
            Scale = 1.0 / Math.Sqrt(ImageHeight * ImageHeight + ImageWidth * ImageWidth);
            DistortionModel.ImageScale = Scale;

            ScaleCorrectionLines();
            ScaleDistortionCenter();
            PrepareMinimalizationAlgorithm();
            FindTargetErrorForMinimalization();

            _minimalization.Process();

            _minimalization.BestResultVector.CopyTo(DistortionModel.Parameters);
        }

        // Corrects image points using previously computed model
        public List<Vector2> CorrectPoints(List<Vector2> points)
        {
            Scale = 1.0 / Math.Sqrt(ImageHeight * ImageHeight + ImageWidth * ImageWidth);
            DistortionModel.ImageScale = Scale;
            List<Vector2>  correctedPoints = new List<Vector2>(points.Count);
            foreach(var point in points)
            {
                DistortionModel.P = point * Scale;
                DistortionModel.Undistort();
                correctedPoints.Add(new Vector2(DistortionModel.Pf / Scale));
            }
            return correctedPoints;
        }

        public void Terminate()
        {
            _minimalization.Terminate = true;
        }

        private void ScaleCorrectionLines()
        {
            _scaledLines = new List<List<Vector2>>();
            for(int l = 0; l < CorrectionLines.Count; ++l)
            {
                List<Vector2> line = new List<Vector2>();
                for(int p = 0; p < CorrectionLines[l].Count; ++p)
                {
                    line.Add(CorrectionLines[l][p] * Scale);
                }
                _scaledLines.Add(line);
            }
        }

        private void ScaleDistortionCenter()
        {
            var dc = DistortionModel.DistortionCenter;
            double scaledWidth = dc.X * Scale;
            double scaledHeight = dc.Y * Scale;
            DistortionModel.InitialCenterEstimation = new Vector2(scaledWidth, scaledHeight);
            DistortionModel.DistortionCenter = new Vector2(scaledWidth, scaledHeight);
        }

        protected void PrepareMinimalizationAlgorithm()
        {
            _minimalization = new LMDistortionDirectionalLineFitMinimalisation();
            _minimalization.MaximumIterations = MaxIterations;
            _minimalization.DoComputeJacobianNumerically = true;
            _minimalization.NumericalDerivativeStep = 1e-4;
            _minimalization.UseCovarianceMatrix = false;
            _minimalization.DumpingMethodUsed = LevenbergMarquardtBaseAlgorithm.DumpingMethod.Multiplicative;
            _minimalization.FindInitialModelParameters = FindInitialModelParameters;

            _minimalization.DistortionModel = DistortionModel;
            Vector<double> parVec = new DenseVector(DistortionModel.ParametersCount);
            DistortionModel.Parameters.CopyTo(parVec);
            _minimalization.ParametersVector = parVec;
            _minimalization.LinePoints = _scaledLines;
            _minimalization.Terminate = false;
            _minimalization.FindInitialModelParameters = FindInitialModelParameters;
        }

        private void FindTargetErrorForMinimalization()
        {
            _minimalization.MaximumResidiual = 0.0;
            Vector2 imgCenter = new Vector2(ImageWidth * Scale, ImageHeight * Scale);
            foreach(var points in _scaledLines)
            {
                foreach(var point in points)
                {
                    _minimalization.MaximumResidiual += point.DistanceToSquared(imgCenter) * 0.01 * Scale * Scale;
                }
            }
        }

        private List<AlgorithmParameter> _parameters;
        public List<AlgorithmParameter> Parameters
        {
            get { return _parameters; }
            protected set { _parameters = value; }
        }

        public void InitParameters()
        {
            Parameters = new List<AlgorithmParameter>();

            ParametrizedObjectParameter modelParam = new ParametrizedObjectParameter(
                "Radial Distortion Model", "DistortionModel");

            modelParam.Parameterizables = new List<IParameterizable>();
            var rationalModel = new Rational3RDModel();
            rationalModel.InitParameters();
            modelParam.Parameterizables.Add(rationalModel);
            var taylorModel = new Taylor4Model();
            taylorModel.InitParameters();
            modelParam.Parameterizables.Add(taylorModel);

            Parameters.Add(new IntParameter("Max Iterations", "MaxIterations", 100, 1, 10000));
            Parameters.Add(new BooleanParameter("Find Initial Model Parameters", "FindInitialModelParameters", true));
        }

        public void UpdateParameters()
        {
            DistortionModel = AlgorithmParameter.FindValue<RadialDistortionModel>("DistortionModel", Parameters);
            MaxIterations = AlgorithmParameter.FindValue<int>("MaxIterations", Parameters);
            FindInitialModelParameters = AlgorithmParameter.FindValue<bool>("FindInitialModelParameters", Parameters);
        }

        public string Name { get; } = "RadialDistrotionCorrectionAlgorithm";
        public override string ToString()
        {
            return Name;
        }
    }
}
