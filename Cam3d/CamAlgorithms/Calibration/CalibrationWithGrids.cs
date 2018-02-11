using System.Collections.Generic;
using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace CamAlgorithms.Calibration
{
    public class CalibrationWithGrids : CalibrationAlgorithm
    {
        public bool OverwriteGridsWithEstimated { get; set; }
        public List<RealGridData> GridsNormalized { get; protected set; }
        public List<RealGridData> GridsEstimated { get; protected set; }
        public bool UseExplicitParametrization { get; set; } = false;

        private CameraMatrixGridMinimalisation minimalization
        {
            get
            {
                return (CameraMatrixGridMinimalisation)NonlinearMinimalization;
            }
            set
            {
                NonlinearMinimalization = value;
            }
        }

        public CalibrationWithGrids()
        {
            NonlinearMinimalization = new CameraMatrixGridSimpleMinimalisation();
        }

        public override void Calibrate()
        {
            base.Calibrate();
            if(OverwriteGridsWithEstimated)
            {
                StoreEstimatedGrids();
            }
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

        protected override void PerformNonlinearMinimalization()
        {
            if(!NormalizeIterative && IsPointsNormalized)
            {
                RemoveNormalization();
            }

            GridsNormalized = NormalizeCalibrationGrids(Grids);

            IsInNonlinearMinimzation = true;
            MinimalizeNonlinearError();
            IsInNonlinearMinimzation = false;
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
        
        public void PrepareNonlinearMinimalisation()
        {
            minimalization = UseExplicitParametrization ? 
                (CameraMatrixGridMinimalisation) new CameraMatrixGridExplicitMinimalisation() :
                (CameraMatrixGridMinimalisation) new CameraMatrixGridSimpleMinimalisation() ;

            minimalization.MaximumIterations = MaxIterations;
            minimalization.MinimalizeSkew = MinimalizeSkew;
            minimalization.DoComputeJacobianNumerically = true;
            minimalization.NumericalDerivativeStep = 1e-4;
            // M = [Xr | xi]
            minimalization.MeasurementsVector = new DenseVector(Points.Count * 2 + Grids.Count * 12);
            for(int i = 0; i < Points.Count; ++i)
            {
                minimalization.MeasurementsVector.At(2 * i, ImagePoints.At(0, i));
                minimalization.MeasurementsVector.At(2 * i + 1, ImagePoints.At(1, i));
            }

            int N2 = Points.Count * 2;
            for(int i = 0; i < Grids.Count; ++i)
            {
                minimalization.MeasurementsVector.At(N2 + i * 12, GridsNormalized[i].TopLeft.X);
                minimalization.MeasurementsVector.At(N2 + i * 12 + 1, GridsNormalized[i].TopLeft.Y);
                minimalization.MeasurementsVector.At(N2 + i * 12 + 2, GridsNormalized[i].TopLeft.Z);
                minimalization.MeasurementsVector.At(N2 + i * 12 + 3, GridsNormalized[i].TopRight.X);
                minimalization.MeasurementsVector.At(N2 + i * 12 + 4, GridsNormalized[i].TopRight.Y);
                minimalization.MeasurementsVector.At(N2 + i * 12 + 5, GridsNormalized[i].TopRight.Z);
                minimalization.MeasurementsVector.At(N2 + i * 12 + 6, GridsNormalized[i].BotLeft.X);
                minimalization.MeasurementsVector.At(N2 + i * 12 + 7, GridsNormalized[i].BotLeft.Y);
                minimalization.MeasurementsVector.At(N2 + i * 12 + 8, GridsNormalized[i].BotLeft.Z);
                minimalization.MeasurementsVector.At(N2 + i * 12 + 9, GridsNormalized[i].BotRight.X);
                minimalization.MeasurementsVector.At(N2 + i * 12 + 10, GridsNormalized[i].BotRight.Y);
                minimalization.MeasurementsVector.At(N2 + i * 12 + 11, GridsNormalized[i].BotRight.Z);
            }

            // For each real point allow like 1mm displacement and for img 0.25px
            // So e_max = N*normScaleReal^2 + N * (0.25 * normScaleImg)^2
            double scaleReal = IsPointsNormalized ? NormReal[0, 0] * NormReal[0, 0] : 1.0;
            double scaleImage = IsPointsNormalized ? NormImage[0, 0] * NormImage[0, 0] : 1.0;
            minimalization.MaximumResidiual = Points.Count * (0.0001 * scaleReal + 0.0001 * scaleImage);

            Vector<double> cameraParameters = GetCameraParametersVector();
            minimalization.ParametersVector = new DenseVector(cameraParameters.Count + GridsNormalized.Count * 12);
            cameraParameters.CopySubVectorTo(minimalization.ParametersVector, 0, 0, cameraParameters.Count);
            minimalization.MeasurementsVector.CopySubVectorTo(minimalization.ParametersVector, N2, cameraParameters.Count, GridsNormalized.Count * 12);

            minimalization.UseCovarianceMatrix = UseCovarianceMatrix;
            minimalization.InverseVariancesVector = NormalisedVariances;

            minimalization.CalibrationPoints = Points;
            minimalization.CalibrationGrids = GridsNormalized;
        }

        protected Vector<double> GetCameraParametersVector()
        {
            if(UseExplicitParametrization)
            {
                Camera.Decompose();
                Vector<double> p = new DenseVector(11);
                var euler = RotationConverter.MatrixToEuler(Camera.RotationMatrix);
                p[CameraMatrixGridExplicitMinimalisation.fxIdx] = Camera.InternalMatrix[0, 0];
                p[CameraMatrixGridExplicitMinimalisation.fyIdx] = Camera.InternalMatrix[1, 1];
                p[CameraMatrixGridExplicitMinimalisation.sIdx] = Camera.InternalMatrix[0, 1];
                p[CameraMatrixGridExplicitMinimalisation.pxIdx] = Camera.InternalMatrix[0, 2];
                p[CameraMatrixGridExplicitMinimalisation.pyIdx] = Camera.InternalMatrix[1, 2];
                p[CameraMatrixGridExplicitMinimalisation.rxIdx] = euler[0];
                p[CameraMatrixGridExplicitMinimalisation.ryIdx] = euler[1];
                p[CameraMatrixGridExplicitMinimalisation.rzIdx] = euler[2];
                p[CameraMatrixGridExplicitMinimalisation.cxIdx] = Camera.Center[0];
                p[CameraMatrixGridExplicitMinimalisation.cyIdx] = Camera.Center[1];
                p[CameraMatrixGridExplicitMinimalisation.czIdx] = Camera.Center[2];               
                return p;
            }
            else
            {
                Vector<double> p = new DenseVector(12);
                p.CopyFromMatrix(Camera.Matrix);
                return p;
            }
        }

        protected void MinimalizeNonlinearError()
        {
            PrepareNonlinearMinimalisation();
            minimalization.Process();

            // P = [pi | eXr]
            var estimatedParams = minimalization.BestResultVector;
            SetCameraMatrixFromParameterVector(estimatedParams);

            GridsEstimated = new List<RealGridData>(Grids.Count);
            for(int i = 0; i < Grids.Count; ++i)
            {
                RealGridData eg = new RealGridData();
                eg.TopLeft.X = estimatedParams.At(minimalization.CameraParametersCount + i * 12);
                eg.TopLeft.Y = estimatedParams.At(minimalization.CameraParametersCount + i * 12 + 1);
                eg.TopLeft.Z = estimatedParams.At(minimalization.CameraParametersCount + i * 12 + 2);
                eg.TopRight.X = estimatedParams.At(minimalization.CameraParametersCount + i * 12 + 3);
                eg.TopRight.Y = estimatedParams.At(minimalization.CameraParametersCount + i * 12 + 4);
                eg.TopRight.Z = estimatedParams.At(minimalization.CameraParametersCount + i * 12 + 5);
                eg.BotLeft.X = estimatedParams.At(minimalization.CameraParametersCount + i * 12 + 6);
                eg.BotLeft.Y = estimatedParams.At(minimalization.CameraParametersCount + i * 12 + 7);
                eg.BotLeft.Z = estimatedParams.At(minimalization.CameraParametersCount + i * 12 + 8);
                eg.BotRight.X = estimatedParams.At(minimalization.CameraParametersCount + i * 12 + 9);
                eg.BotRight.Y = estimatedParams.At(minimalization.CameraParametersCount + i * 12 + 10);
                eg.BotRight.Z = estimatedParams.At(minimalization.CameraParametersCount + i * 12 + 11);
                GridsEstimated.Add(eg);
            }
        }

        protected void SetCameraMatrixFromParameterVector(Vector<double> p)
        {
            if(UseExplicitParametrization)
            {
                var K = DenseMatrix.OfRowArrays(new double[][]
                {
                    new double[] { p[CameraMatrixGridExplicitMinimalisation.fxIdx], p[CameraMatrixGridExplicitMinimalisation.sIdx], p[CameraMatrixGridExplicitMinimalisation.pxIdx] },
                    new double[] { 0.0, p[CameraMatrixGridExplicitMinimalisation.fyIdx], p[CameraMatrixGridExplicitMinimalisation.pyIdx] },
                    new double[] { 0.0,  0.0,   1.0 }
                });
                var R = RotationConverter.EulerToMatrix(new double[3] { p[CameraMatrixGridExplicitMinimalisation.rxIdx], p[CameraMatrixGridExplicitMinimalisation.ryIdx], p[CameraMatrixGridExplicitMinimalisation.rzIdx] });
                var C = new DenseVector(new double[] { p[CameraMatrixGridExplicitMinimalisation.cxIdx], p[CameraMatrixGridExplicitMinimalisation.cyIdx], p[CameraMatrixGridExplicitMinimalisation.czIdx] });

                Camera.Matrix = Camera.FromDecomposition(K, R, C).Matrix;
            }
            else
            {
                Camera.Matrix.CopyFromVector(p);
            }
        }

        protected override void FindNormalizedVariances()
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

        #region IParameterizable
        public override void InitParameters()
        {
            base.InitParameters();
            
            Parameters.Add(new BooleanParameter(
                "Overwrite grids with estimated", "OverwriteGridsWithEstimated", false));
            Parameters.Add(new BooleanParameter(
                "Use Explicit Parametrization", "UseExplicitParametrization", false));
        }

        public override void UpdateParameters()
        {
            base.UpdateParameters();
            
            OverwriteGridsWithEstimated = IAlgorithmParameter.FindValue<bool>("OverwriteGridsWithEstimated", Parameters);
            UseExplicitParametrization = IAlgorithmParameter.FindValue<bool>("UseExplicitParametrization", Parameters);
        }

        public override string Name
        {
            get
            {
                return "Calibration With Grids";
            }
        }
        #endregion
    }
}
