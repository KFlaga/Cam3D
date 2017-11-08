using System.Collections.Generic;
using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace CamAlgorithms.Calibration
{
    public class CalibrationAlgorithm : IParameterizable
    {
        public List<CalibrationPoint> Points { set; get; } // May change during calibration
        public List<RealGridData> Grids { get; set; }
        public List<RealGridData> GridsNormalized { get; private set; }
        public List<RealGridData> GridsEstimated { get; private set; }
        public Camera Camera { get; set; } = new Camera();

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

        public bool OverwriteGridsWithEstimated { get; set; }

        public int MaxIterations { get; set; }

        public Matrix<double> NormReal { get; private set; }
        public Matrix<double> NormImage { get; private set; }
        public Matrix<double> RealPoints { get; private set; }
        public Matrix<double> ImagePoints { get; private set; }

        public Vector<double> NormalisedVariances { get; private set; }

        public LMCameraMatrixGridMinimalisation Minimalisation { get; private set; } = new LMCameraMatrixGridMinimalisation();
        public LMCameraMatrixZeroSkewMinimalisation ZeroSkewMinimalisation { get; private set; } = new LMCameraMatrixZeroSkewMinimalisation();

        public bool IsLinearEstimationDone { get; private set; } = false;
        public bool IsPointsNormalized { get; private set; } = false;
        public bool IsInMinimzation { get; private set; } = false;

        public void Calibrate()
        {
            IsLinearEstimationDone = false;
            IsInMinimzation = false;
            IsPointsNormalized = false;

            ConvertPointsToHomonogeus();
            PrepareNormalization();
            Camera.Matrix = FindLinearEstimationOfCameraMatrix();
            IsLinearEstimationDone = true;

            if(MinimalizeSkew) { PerformSkewMinimalization(); }
            if(LinearOnly == false) { PerformNonlinearMinimalization(); }
            if(EliminateOuliers)
            {
                EliminateOuliers = false;
                PerformOutliersElimination();
                Calibrate();
            }
            if(OverwriteGridsWithEstimated) { StoreEstimatedGrids(); }
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

        protected void StoreEstimatedGrids()
        {
            if(IsPointsNormalized)
            {
                // Denormalize grids
                var denorm = NormReal.Inverse();
                for(int i = 0; i < Grids.Count; ++i)
                {
                    Grids[i].TopLeft = new Vector3(denorm * GridsEstimated[i].TopLeft.ToMathNetVector4());
                    Grids[i].TopRight = new Vector3(denorm * GridsEstimated[i].TopRight.ToMathNetVector4());
                    Grids[i].BotLeft = new Vector3(denorm * GridsEstimated[i].BotLeft.ToMathNetVector4());
                    Grids[i].BotRight = new Vector3(denorm * GridsEstimated[i].BotRight.ToMathNetVector4());
                }
            }
            else
            {
                Grids = GridsEstimated;
            }
        }

        protected void PerformNonlinearMinimalization()
        {
            if(!NormalizeIterative && IsPointsNormalized)
            {
                RemoveNormalization();
            }

            GridsNormalized = NormalizeCalibrationGrids(Grids);
            FindNormalizedVariances();

            IsInMinimzation = true;
            MinimizeError();
            IsInMinimzation = false;

            if(MinimalizeSkew)
            {
                PerformSkewMinimalization();
            }
        }

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

        protected List<RealGridData> NormalizeCalibrationGrids(List<RealGridData> grids)
        {
            var gridsNormalized = new List<RealGridData>();
            for(int i = 0; i < Grids.Count; ++i)
            {
                RealGridData grid = grids[i];
                RealGridData gridNorm = new RealGridData();
                gridNorm.Rows = grid.Rows;
                gridNorm.Columns = grid.Columns;

                Vector<double> corner = new DenseVector(new double[] { grid.TopLeft.X, grid.TopLeft.Y, grid.TopLeft.Z, 1.0 });
                corner = NormReal * corner;
                corner.DivideThis(corner.At(3));
                gridNorm.TopLeft = new Vector3(corner.At(0), corner.At(1), corner.At(2));

                corner = new DenseVector(new double[] { grid.TopRight.X, grid.TopRight.Y, grid.TopRight.Z, 1.0 });
                corner = NormReal * corner;
                corner.DivideThis(corner.At(3));
                gridNorm.TopRight = new Vector3(corner.At(0), corner.At(1), corner.At(2));

                corner = new DenseVector(new double[] { grid.BotLeft.X, grid.BotLeft.Y, grid.BotLeft.Z, 1.0 });
                corner = NormReal * corner;
                corner.DivideThis(corner.At(3));
                gridNorm.BotLeft = new Vector3(corner.At(0), corner.At(1), corner.At(2));

                corner = new DenseVector(new double[] { grid.BotRight.X, grid.BotRight.Y, grid.BotRight.Z, 1.0 });
                corner = NormReal * corner;
                corner.DivideThis(corner.At(3));
                gridNorm.BotRight = new Vector3(corner.At(0), corner.At(1), corner.At(2));

                gridsNormalized.Add(gridNorm);
            }
            return gridsNormalized;
        }

        public void FindNormalizedVariances()
        {
            // Scale variances with same factor as points
            NormalisedVariances = new DenseVector(Points.Count * 2 + Grids.Count * 12);
            double scaleReal = IsPointsNormalized ? NormReal[0, 0] * NormReal[0, 0] : 1.0;
            double scaleImage = IsPointsNormalized ? NormImage[0, 0] * NormImage[0, 0] : 1.0;
            //double scaleReal = 1;
            //double scaleImage = 1;
            for(int i = 0; i < Points.Count; ++i)
            {
                NormalisedVariances[2 * i] = 1.0f / (ImageMeasurementVariance_X * scaleImage);
                NormalisedVariances[2 * i + 1] = 1.0f / (ImageMeasurementVariance_Y * scaleImage);
            }

            int N2 = Points.Count * 2;
            for(int i = 0; i < Grids.Count * 4; ++i)
            {
                NormalisedVariances[N2 + i * 3] = 1.0f / (RealMeasurementVariance_X * scaleReal);
                NormalisedVariances[N2 + i * 3 + 1] = 1.0f / (RealMeasurementVariance_Y * scaleReal);
                NormalisedVariances[N2 + i * 3 + 2] = 1.0f / (RealMeasurementVariance_Z * scaleReal);
            }

        }

        public void PrepareMinimalisationAlg()
        {
            Minimalisation.MaximumIterations = MaxIterations;
            // M = [Xr | xi]
            Minimalisation.MeasurementsVector = new DenseVector(Points.Count * 2 + Grids.Count * 12);
            for(int i = 0; i < Points.Count; ++i)
            {
                Minimalisation.MeasurementsVector.At(2 * i, ImagePoints.At(0, i));
                Minimalisation.MeasurementsVector.At(2 * i + 1, ImagePoints.At(1, i));
            }

            int N2 = Points.Count * 2;
            for(int i = 0; i < Grids.Count; ++i)
            {
                Minimalisation.MeasurementsVector.At(N2 + i * 12, GridsNormalized[i].TopLeft.X);
                Minimalisation.MeasurementsVector.At(N2 + i * 12 + 1, GridsNormalized[i].TopLeft.Y);
                Minimalisation.MeasurementsVector.At(N2 + i * 12 + 2, GridsNormalized[i].TopLeft.Z);
                Minimalisation.MeasurementsVector.At(N2 + i * 12 + 3, GridsNormalized[i].TopRight.X);
                Minimalisation.MeasurementsVector.At(N2 + i * 12 + 4, GridsNormalized[i].TopRight.Y);
                Minimalisation.MeasurementsVector.At(N2 + i * 12 + 5, GridsNormalized[i].TopRight.Z);
                Minimalisation.MeasurementsVector.At(N2 + i * 12 + 6, GridsNormalized[i].BotLeft.X);
                Minimalisation.MeasurementsVector.At(N2 + i * 12 + 7, GridsNormalized[i].BotLeft.Y);
                Minimalisation.MeasurementsVector.At(N2 + i * 12 + 8, GridsNormalized[i].BotLeft.Z);
                Minimalisation.MeasurementsVector.At(N2 + i * 12 + 9, GridsNormalized[i].BotRight.X);
                Minimalisation.MeasurementsVector.At(N2 + i * 12 + 10, GridsNormalized[i].BotRight.Y);
                Minimalisation.MeasurementsVector.At(N2 + i * 12 + 11, GridsNormalized[i].BotRight.Z);
            }

            // For each real point allow like 1mm displacement and for img 0.25px
            // So e_max = N*normScaleReal^2 + N * (0.25 * normScaleImg)^2
            double scaleReal = IsPointsNormalized ? NormReal[0, 0] * NormReal[0, 0] : 1.0;
            double scaleImage = IsPointsNormalized ? NormImage[0, 0] * NormImage[0, 0] : 1.0;
            Minimalisation.MaximumResidiual =
                Points.Count * (0.1 * scaleReal + 0.0625f * scaleImage);

            Minimalisation.ParametersVector = new DenseVector(12);
            Minimalisation.ParametersVector.CopyFromMatrix(Camera.Matrix);

            Minimalisation.UseCovarianceMatrix = UseCovarianceMatrix;
            Minimalisation.InverseVariancesVector = NormalisedVariances;

            Minimalisation.CalibrationPoints = Points;
            Minimalisation.CalibrationGrids = GridsNormalized;
        }

        protected void MinimizeError()
        {
            PrepareMinimalisationAlg();
            Minimalisation.Process();

            // P = [pi | eXr]
            var estimatedParams = Minimalisation.BestResultVector;
            Camera.Matrix.CopyFromVector(estimatedParams);

            GridsEstimated = new List<RealGridData>(Grids.Count);
            for(int i = 0; i < Grids.Count; ++i)
            {
                RealGridData eg = new RealGridData();
                eg.TopLeft.X = estimatedParams.At(12 + i * 12);
                eg.TopLeft.Y = estimatedParams.At(12 + i * 12 + 1);
                eg.TopLeft.Z = estimatedParams.At(12 + i * 12 + 2);
                eg.TopRight.X = estimatedParams.At(12 + i * 12 + 3);
                eg.TopRight.Y = estimatedParams.At(12 + i * 12 + 4);
                eg.TopRight.Z = estimatedParams.At(12 + i * 12 + 5);
                eg.BotLeft.X = estimatedParams.At(12 + i * 12 + 6);
                eg.BotLeft.Y = estimatedParams.At(12 + i * 12 + 7);
                eg.BotLeft.Z = estimatedParams.At(12 + i * 12 + 8);
                eg.BotRight.X = estimatedParams.At(12 + i * 12 + 9);
                eg.BotRight.Y = estimatedParams.At(12 + i * 12 + 10);
                eg.BotRight.Z = estimatedParams.At(12 + i * 12 + 11);
                GridsEstimated.Add(eg);
            }
        }

        protected void PrepareSkewMiniAlg()
        {
            ZeroSkewMinimalisation.MaximumIterations = MaxIterations;
            // M = [Xr | xi]
            ZeroSkewMinimalisation.MeasurementsVector = new DenseVector(Points.Count * 5);
            for(int i = 0; i < Points.Count; ++i)
            {
                ZeroSkewMinimalisation.MeasurementsVector[3 * i] = RealPoints[0, i];
                ZeroSkewMinimalisation.MeasurementsVector[3 * i + 1] = RealPoints[1, i];
                ZeroSkewMinimalisation.MeasurementsVector[3 * i + 2] = RealPoints[2, i];
                ZeroSkewMinimalisation.MeasurementsVector[Points.Count * 3 + 2 * i] = ImagePoints[0, i];
                ZeroSkewMinimalisation.MeasurementsVector[Points.Count * 3 + 2 * i + 1] = ImagePoints[1, i];
            }

            // For each real point allow like 1mm displacement and for img 0.25px
            // So e_max = N*normScaleReal^2 + N * (0.25 * normScaleImg)^2
            double scaleReal = IsPointsNormalized ? NormReal[0, 0] * NormReal[0, 0] : 1.0;
            double scaleImage = IsPointsNormalized ? NormImage[0, 0] * NormImage[0, 0] : 1.0;
            ZeroSkewMinimalisation.MaximumResidiual =
                Points.Count * (0.1 * scaleReal + 0.0625f * scaleImage);

            ZeroSkewMinimalisation.ParametersVector = new DenseVector(12);
            ZeroSkewMinimalisation.ParametersVector.CopyFromMatrix(Camera.Matrix);

            ZeroSkewMinimalisation.UseCovarianceMatrix = false;
        }

        protected void PerformSkewMinimalization()
        {
            PrepareSkewMiniAlg();
            ZeroSkewMinimalisation.Process();
            Camera.Matrix.CopyFromVector(ZeroSkewMinimalisation.BestResultVector);
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
        private List<AlgorithmParameter> _parameters;
        public List<AlgorithmParameter> Parameters
        {
            get { return _parameters; }
            protected set { _parameters = value; }
        }

        public void InitParameters()
        {
            Parameters = new List<AlgorithmParameter>();

            Parameters.Add(new BooleanParameter(
                "Perform only linear estimation", "LinearOnly", false));
            Parameters.Add(new BooleanParameter(
                "Normalize points for linear estimation", "NormalizeLinear", true));
            Parameters.Add(new BooleanParameter(
                "Normalize points for iterative estimation", "NormalizeIterative", true));
            Parameters.Add(new BooleanParameter(
                "Use covariance matrix", "UseCovarianceMatrix", true));
            Parameters.Add(new BooleanParameter(
                "Minimalise skew", "MinimalizeSkew", true));
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
            Parameters.Add(new IntParameter(
                "Max Iterations", "MaximumIterations", 100, 1, 10000));
            Parameters.Add(new BooleanParameter(
                "Outliers elimination after linear estimation", "EliminateOuliers", false));
            Parameters.Add(new DoubleParameter(
                "Elimination coeff (elim if e >= m * c)", "OutliersCoeff", 1.5, 0.0, 1000.0));
            Parameters.Add(new BooleanParameter(
                "Overwrite grids with estimated", "OverwriteGridsWithEstimated", false));
        }

        public void UpdateParameters()
        {
            ImageMeasurementVariance_X = AlgorithmParameter.FindValue<double>("ImageMeasurementVariance_X", Parameters);
            ImageMeasurementVariance_Y = AlgorithmParameter.FindValue<double>("ImageMeasurementVariance_Y", Parameters);
            RealMeasurementVariance_X = AlgorithmParameter.FindValue<double>("RealMeasurementVariance_X", Parameters);
            RealMeasurementVariance_Y = AlgorithmParameter.FindValue<double>("RealMeasurementVariance_Y", Parameters);
            RealMeasurementVariance_Z = AlgorithmParameter.FindValue<double>("RealMeasurementVariance_Z", Parameters);
            MaxIterations = AlgorithmParameter.FindValue<int>("MaximumIterations", Parameters);
            LinearOnly = AlgorithmParameter.FindValue<bool>("LinearOnly", Parameters);
            NormalizeLinear = AlgorithmParameter.FindValue<bool>("NormalizeLinear", Parameters);
            NormalizeIterative = AlgorithmParameter.FindValue<bool>("NormalizeIterative", Parameters);
            UseCovarianceMatrix = AlgorithmParameter.FindValue<bool>("UseCovarianceMatrix", Parameters);
            MinimalizeSkew = AlgorithmParameter.FindValue<bool>("MinimalizeSkew", Parameters);
            EliminateOuliers = AlgorithmParameter.FindValue<bool>("EliminateOuliers", Parameters);
            OutliersCoeff = AlgorithmParameter.FindValue<double>("OutliersCoeff", Parameters);
            OverwriteGridsWithEstimated = AlgorithmParameter.FindValue<bool>("OverwriteGridsWithEstimated", Parameters);
        }

        public string Name
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
