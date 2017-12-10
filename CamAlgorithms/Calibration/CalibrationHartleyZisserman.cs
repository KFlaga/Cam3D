using System.Collections.Generic;
using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace CamAlgorithms.Calibration
{
    public class CalibrationHartleyZisserman : CalibrationAlgorithm
    {
        private LMCameraMatrixSimpleMinimalisation minimalization
        {
            get
            {
                return (LMCameraMatrixSimpleMinimalisation)NonlinearMinimalization;
            }
        }

        public CalibrationHartleyZisserman()
        {
            NonlinearMinimalization = new LMCameraMatrixSimpleMinimalisation();
        }

        protected override void PerformNonlinearMinimalization()
        {
            if(!NormalizeIterative && IsPointsNormalized)
            {
                RemoveNormalization();
            }

            PrepareNonlinearMinimalisation();

            IsInNonlinearMinimzation = true;
            minimalization.Process();
            IsInNonlinearMinimzation = false;

            // P = [pi | eXr]
            var estimatedParams = minimalization.BestResultVector;
            Camera.Matrix.CopyFromVector(estimatedParams.SubVector(0, 12));
        }

        public void PrepareNonlinearMinimalisation()
        {
            minimalization.MaximumIterations = MaxIterations;
            // M = [Xr | xi]
            minimalization.MeasurementsVector = new DenseVector(Points.Count * 5);
            int N3 = Points.Count * 3;
            for(int i = 0; i < Points.Count; ++i)
            {
                minimalization.MeasurementsVector.At(3 * i, RealPoints.At(0, i));
                minimalization.MeasurementsVector.At(3 * i + 1, RealPoints.At(1, i));
                minimalization.MeasurementsVector.At(3 * i + 2, RealPoints.At(2, i));
                minimalization.MeasurementsVector.At(N3 + 2 * i, ImagePoints.At(0, i));
                minimalization.MeasurementsVector.At(N3 + 2 * i + 1, ImagePoints.At(1, i));
            }

            // For each real point allow like 1mm displacement and for img 0.25px
            // So e_max = N*normScaleReal^2 + N * (0.25 * normScaleImg)^2
            double scaleReal = IsPointsNormalized ? NormReal[0, 0] * NormReal[0, 0] : 1.0;
            double scaleImage = IsPointsNormalized ? NormImage[0, 0] * NormImage[0, 0] : 1.0;
            minimalization.MaximumResidiual =
                Points.Count * (1e-6 * scaleReal + 1e-6 * scaleImage);

            minimalization.ParametersVector = new DenseVector(12 + N3);
            minimalization.ParametersVector.CopyFromMatrix(Camera.Matrix);
            for(int i = 0; i < Points.Count; ++i)
            {
                minimalization.ParametersVector.At(12 + 3 * i, RealPoints.At(0, i));
                minimalization.ParametersVector.At(12 + 3 * i + 1, RealPoints.At(1, i));
                minimalization.ParametersVector.At(12 + 3 * i + 2, RealPoints.At(2, i));
            }

            minimalization.UseCovarianceMatrix = UseCovarianceMatrix;
            minimalization.InverseVariancesVector = NormalisedVariances;
        }

        protected void MinimalizeNonlinearError()
        {
            PrepareNonlinearMinimalisation();
            minimalization.Process();

            // P = [pi | eXr]
            var estimatedParams = minimalization.BestResultVector;
            Camera.Matrix.CopyFromVector(estimatedParams);
        }

        protected override void FindNormalizedVariances()
        {
            // Scale variances with same factor as points
            NormalisedVariances = new DenseVector(Points.Count * 5);
            double scaleReal = IsPointsNormalized ? NormReal[0, 0] * NormReal[0, 0] : 1.0;
            double scaleImage = IsPointsNormalized ? NormImage[0, 0] * NormImage[0, 0] : 1.0;

            int N3 = Points.Count * 3;
            for(int i = 0; i < Points.Count; ++i)
            {
                NormalisedVariances.At(3 * i, 1.0f /  (RealMeasurementVariance_X * scaleReal));
                NormalisedVariances.At(3 * i + 1, 1.0f /  (RealMeasurementVariance_Y * scaleReal));
                NormalisedVariances.At(3 * i + 2, 1.0f /  (RealMeasurementVariance_Z * scaleReal));
                NormalisedVariances.At(N3 + 2 * i, 1.0f /  (ImageMeasurementVariance_X * scaleImage));
                NormalisedVariances.At(N3 + 2 * i + 1, 1.0f /  (ImageMeasurementVariance_Y * scaleImage));
            }
        }

        #region IParameterizable
        public override void InitParameters()
        {
            base.InitParameters();
        }

        public override void UpdateParameters()
        {
            base.UpdateParameters();
        }

        public override string Name
        {
            get
            {
                return "Calibration Hartley-Zisserman";
            }
        }
        #endregion
    }
}
