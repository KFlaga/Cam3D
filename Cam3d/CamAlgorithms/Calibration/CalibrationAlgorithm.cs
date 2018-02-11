using System.Collections.Generic;
using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;

namespace CamAlgorithms.Calibration
{
    public class CalibrationAlgorithm : IParameterizable
    {
        public List<CalibrationPoint> Points { set; get; } // May change during calibration
        public List<RealGridData> Grids { get; set; }
        public Camera Camera { get; set; } = new Camera();
        public Camera LinearEstimation { get; set; }

        public double ImageMeasurementVariance_X { get; set; }
        public double ImageMeasurementVariance_Y { get; set; }
        public double RealMeasurementVariance_X { get; set; }
        public double RealMeasurementVariance_Y { get; set; }
        public double RealMeasurementVariance_Z { get; set; }

        public bool LinearOnly { get; set; }
        public bool NormalizeLinear { get; set; }
        public bool NormalizeIterative { get; set; }
        public bool UseCovarianceMatrix { get; set; }
        public bool MinimalizeSkew { get; set; }

        public bool EliminateOuliers { get; set; }
        public double OutliersCoeff { get; set; }

        public int MaxIterations { get; set; }

        public Matrix<double> NormReal { get; protected set; }
        public Matrix<double> NormImage { get; protected set; }
        public Matrix<double> RealPoints { get; protected set; }
        public Matrix<double> ImagePoints { get; protected set; }

        public Vector<double> NormalisedVariances { get; protected set; }
        
        public LMCameraMatrixZeroSkewMinimalisation ZeroSkewMinimalisation { get; protected set; } = new LMCameraMatrixZeroSkewMinimalisation();
        public MinimalisationAlgorithm NonlinearMinimalization { get; protected set; }

        public bool IsLinearEstimationDone { get; protected set; } = false;
        public bool IsPointsNormalized { get; protected set; } = false;
        public bool IsInNonlinearMinimzation { get; protected set; } = false;

        public virtual void Calibrate()
        {
            IsLinearEstimationDone = false;
            IsPointsNormalized = false;
            
            ConvertPointsToHomonogeus();
            PrepareNormalization();
            Camera.Matrix = FindLinearEstimationOfCameraMatrix();
            LinearEstimation = new Camera() { Matrix = Camera.Matrix };
            if(IsPointsNormalized) { LinearEstimation.Denormalize(NormImage, NormReal); }
            IsLinearEstimationDone = true;
            
            if(EliminateOuliers)
            {
                EliminateOuliers = false;
                PerformOutliersElimination();
                Calibrate();
				return;
            }
            if(LinearOnly == false)
            {
                FindNormalizedVariances();
                PerformNonlinearMinimalization();
            }
            if(IsPointsNormalized)
            {
                Camera.Denormalize(NormImage, NormReal);
                IsPointsNormalized = false;
            }
            Camera.Decompose();
        }

        protected void ConvertPointsToHomonogeus()
        {
            RealPoints = new DenseMatrix(4, Points.Count);
            ImagePoints = new DenseMatrix(3, Points.Count);
            for(int point = 0; point < Points.Count; point++)
            {
                RealPoints[0, point] = Points[point].RealX;
                RealPoints[1, point] = Points[point].RealY;
                RealPoints[2, point] = Points[point].RealZ;
                RealPoints[3, point] = 1;
                ImagePoints[0, point] = Points[point].ImgX;
                ImagePoints[1, point] = Points[point].ImgY;
                ImagePoints[2, point] = 1;
            }
        }

        protected void PrepareNormalization()
        {
            if(NormalizeLinear)
            {
                NormalizePoints();
                IsPointsNormalized = true;
            }
            else
            {
                NormImage = DenseMatrix.CreateIdentity(3);
                NormReal = DenseMatrix.CreateIdentity(4);
            }
        }

        protected void NormalizePoints()
        {
            NormImage = PointNormalization.FindNormalizationMatrix2d(ImagePoints);
            ImagePoints = PointNormalization.NormalizePoints(ImagePoints, NormImage);
            NormReal = PointNormalization.FindNormalizationMatrix3d(RealPoints);
            RealPoints = PointNormalization.NormalizePoints(RealPoints, NormReal);
        }

        protected virtual void FindNormalizedVariances() { }

        protected Matrix<double> FindLinearEstimationOfCameraMatrix()
        {
            Matrix<double> equationsMat = new DenseMatrix(2 * ImagePoints.ColumnCount, 12);

            for(int p = 0; p < ImagePoints.ColumnCount; p++)
            {
                // Fill matrix A with point info
                equationsMat.SetRow(2 * p, new double[12] {
                     0, 0, 0, 0,
                     -RealPoints[0, p], -RealPoints[1, p], -RealPoints[2, p], -1.0f,
                     ImagePoints[1, p] * RealPoints[0, p], ImagePoints[1, p] * RealPoints[1, p],
                     ImagePoints[1, p] * RealPoints[2, p], ImagePoints[1, p] });
                equationsMat.SetRow(2 * p + 1, new double[12] {
                    RealPoints[0, p], RealPoints[1, p], RealPoints[2, p], 1.0f,
                    0, 0, 0, 0,
                    -ImagePoints[0, p] * RealPoints[0, p], -ImagePoints[0, p] * RealPoints[1, p],
                    -ImagePoints[0, p] * RealPoints[2, p], -ImagePoints[0, p]});
            }

            var eqI = equationsMat.ColumnSums();
            var solver = new SvdZeroFullrankSolver();
            solver.EquationsMatrix = equationsMat;
            solver.Solve();

            Matrix<double> cameraMatrix = new DenseMatrix(3, 4);
            cameraMatrix.CopyFromVector(solver.ResultVector);

            return cameraMatrix;
        }
        
        protected virtual void PerformNonlinearMinimalization() { }

        protected void RemoveNormalization()
        {
            if(NormalizeIterative == false && IsPointsNormalized)
            {
                ConvertPointsToHomonogeus();
                Camera.Denormalize(NormImage, NormReal);
                IsPointsNormalized = false;
                NormImage = DenseMatrix.CreateIdentity(3);
                NormReal = DenseMatrix.CreateIdentity(4);
            }
        }
        
        private void PerformOutliersElimination()
        {
            // Eliminate from calibration all points with reprojection error greater than _outlierCoeff * mean_error
            double error = 0.0;
            double[] errors = new double[Points.Count];
            for(int p = 0; p < Points.Count; ++p)
            {
                var cp = Points[p];
                Vector<double> ip = new DenseVector(new double[] { ImagePoints.At(0, p), ImagePoints.At(1, p), 1.0 });
                Vector<double> rp = new DenseVector(new double[] { RealPoints.At(0, p), RealPoints.At(1, p), RealPoints.At(2, p), 1.0 });

                Vector<double> eip = Camera.Matrix * rp;
                eip.DivideThis(eip[2]);

                var d = (ip - eip);
                errors[p] = d.L2Norm();
                error += errors[p];
            }

            double meanError = error / Points.Count;

            List<CalibrationPoint> reducedPoints = new List<CalibrationPoint>(Points.Count);
            for(int p = 0; p < Points.Count; ++p)
            {
                if(errors[p] < meanError * OutliersCoeff)
                {
                    reducedPoints.Add(Points[p]);
                }
            }

            Points = reducedPoints;
        }

        //private void Ransac()
        //{
        //    int ransacPointsCount = 12; // ?
        //    int ransacInterations = 100;

        //    Matrix<double> bestResult = null;
        //    double bestError = 1e12;
        //    for(int k = 0; k < ransacInterations; ++k)
        //    {
        //        List<CalibrationPoint> randomSet = ChooseRandomlyFrom(Points, ransacPointsCount);
        //        double randomModelError = RansacIteration(randomSet);
        //        if(randomModelError < bestError)
        //        {
        //            bestError = randomModelError;
        //            bestResult = Camera.Matrix;

        //            if(bestError < threshold)
        //            {
        //                break;
        //            }
        //        }
        //    }

        //    Camera.Matrix = bestResult;
        //}

        //// TODO: Pass camera matrix return camera matrix
        //private double RansacIteration(List<CalibrationPoint> points)
        //{
        //    var save = Points;
        //    Points = points;
        //    var checkPoints = save.Without(points);
        //    Matrix<double> camera = Calibrate();
        //    foreach(var cp in checkPoints)
        //    {
        //        var cp = Points[p];
        //        Vector<double> ip = new DenseVector(new double[] { ImagePoints.At(0, p), ImagePoints.At(1, p), 1.0 });
        //        Vector<double> rp = new DenseVector(new double[] { RealPoints.At(0, p), RealPoints.At(1, p), RealPoints.At(2, p), 1.0 });

        //        Vector<double> eip = Camera.Matrix * rp;
        //        eip.DivideThis(eip[2]);

        //        var d = (ip - eip);
        //        double error = d.L2Norm();
        //        if(error < threshold)
        //        {
        //            Points.Add(cp);
        //        }
        //    }

        //    if(Points.Count > threshold)
        //    {
        //        Calibrate();
        //        return FindError();
        //    }

        //    Points = save;
        //    return 1e12;
        //}

        #region IParameterizable
        public List<IAlgorithmParameter> Parameters { get; protected set; }

        public virtual void InitParameters()
        {
            Parameters = new List<IAlgorithmParameter>();

            Parameters.Add(new BooleanParameter(
                "Perform only linear estimation", "LinearOnly", false));
            Parameters.Add(new BooleanParameter(
                "Normalize points for linear estimation", "NormalizeLinear", true));
            Parameters.Add(new BooleanParameter(
                "Normalize points for iterative estimation", "NormalizeIterative", true));
            Parameters.Add(new BooleanParameter(
                "Minimalise skew", "MinimalizeSkew", true));
            Parameters.Add(new IntParameter(
                "Max Iterations", "MaximumIterations", 100, 10, 10000));
            Parameters.Add(new BooleanParameter(
                "Outliers elimination after linear estimation", "EliminateOuliers", false));
            Parameters.Add(new DoubleParameter(
                "Elimination coeff (elim if e >= m * c)", "OutliersCoeff", 1.5, 0.0, 1000.0));
            Parameters.Add(new BooleanParameter(
                "Use covariance matrix", "UseCovarianceMatrix", true));
            Parameters.Add(new DoubleParameter(
                "Image Measurements Variance X", "ImageMeasurementVariance_X", 0.25, 0.0, 100.0));
            Parameters.Add(new DoubleParameter(
                "Image Measurements Variance Y", "ImageMeasurementVariance_Y", 0.25, 0.0, 100.0));
            Parameters.Add(new DoubleParameter(
                "Real Measurements Variance X", "RealMeasurementVariance_X", 1.0, 0.0, 1000.0));
            Parameters.Add(new DoubleParameter(
                "Real Measurements Variance Y", "RealMeasurementVariance_Y", 1.0, 0.0, 1000.0));
            Parameters.Add(new DoubleParameter(
                "Real Measurements Variance Z", "RealMeasurementVariance_Z", 1.0, 0.0, 1000.0));
        }

        public virtual void UpdateParameters()
        {
            ImageMeasurementVariance_X = IAlgorithmParameter.FindValue<double>("ImageMeasurementVariance_X", Parameters);
            ImageMeasurementVariance_Y = IAlgorithmParameter.FindValue<double>("ImageMeasurementVariance_Y", Parameters);
            RealMeasurementVariance_X = IAlgorithmParameter.FindValue<double>("RealMeasurementVariance_X", Parameters);
            RealMeasurementVariance_Y = IAlgorithmParameter.FindValue<double>("RealMeasurementVariance_Y", Parameters);
            RealMeasurementVariance_Z = IAlgorithmParameter.FindValue<double>("RealMeasurementVariance_Z", Parameters);
            MaxIterations = IAlgorithmParameter.FindValue<int>("MaximumIterations", Parameters);
            LinearOnly = IAlgorithmParameter.FindValue<bool>("LinearOnly", Parameters);
            NormalizeLinear = IAlgorithmParameter.FindValue<bool>("NormalizeLinear", Parameters);
            NormalizeIterative = IAlgorithmParameter.FindValue<bool>("NormalizeIterative", Parameters);
            UseCovarianceMatrix = IAlgorithmParameter.FindValue<bool>("UseCovarianceMatrix", Parameters);
            MinimalizeSkew = IAlgorithmParameter.FindValue<bool>("MinimalizeSkew", Parameters);
            EliminateOuliers = IAlgorithmParameter.FindValue<bool>("EliminateOuliers", Parameters);
            OutliersCoeff = IAlgorithmParameter.FindValue<double>("OutliersCoeff", Parameters);
        }

        public virtual string Name
        {
            get
            {
                return "Calibration Algorithm";
            }
        }
        #endregion

        public override string ToString()
        {
            return Name;
        }
    }
}
