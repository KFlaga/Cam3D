using System;
using System.Collections.Generic;
using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace CalibrationModule
{
    public class CamCalibrator : IParametrizedProcessor
    {
        private List<ProcessorParameter> _parameters;
        public List<ProcessorParameter> Parameters
        {
            get { return _parameters; }
            protected set { _parameters = value; }
        }

        public List<CalibrationPoint> Points { set; get; } // May change during calibration
        public Matrix<double> CameraMatrix { set; get; }
        public Matrix<double> CameraInternalMatrix { set; get; }
        public Matrix<double> CameraRotationMatrix { set; get; }
        public Vector<double> CameraTranslation { set; get; }

        private Matrix<double> _normReal;
        private Matrix<double> _normImage;
        private Matrix<double> _realPoints;
        private Matrix<double> _imagePoints;

        public double ImageMeasurementVariance_X { get; set; }
        public double ImageMeasurementVariance_Y { get; set; }
        public double RealMeasurementVariance_X { get; set; }
        public double RealMeasurementVariance_Y { get; set; }
        public double RealMeasurementVariance_Z { get; set; }
        private Vector<double> _normalisedVariances;

        public ILinearEquationsSolver _linearSolver = new SvdZeroFullrankSolver();
        public MinimalisationAlgorithm _geometricErrorMinimalisationAlg =
            new LMCameraMatrixSimpleMinimalisation();

        public void Calibrate()
        {
            // Action plan:
            // 1) Make points homonogeus                      
            // 2) Normalize points                            
            // 3) Get linear estimation of camera matrix
            // 4) Compute normalised variances/inverses
            // 5) Minimise geometric error of camera matrix
            // 6) Denormalise camera matrix

            HomoPoints();

            NormalizeImagePoints();
            NormalizeRealPoints();

            CameraMatrix = FindLinearEstimationOfCameraMatrix();
            FindNormalisedVariances();

            MinimizeError();

            DenormaliseCameraMatrix();
            DecomposeCameraMatrix();
        }

        public void HomoPoints() // Create homonogeus points matrices form CalibrationPoint list
        {
            _realPoints = new DenseMatrix(4, Points.Count);
            _imagePoints = new DenseMatrix(3, Points.Count);
            for(int point = 0; point < Points.Count; point++)
            {
                _realPoints[0, point] = Points[point].RealX;
                _realPoints[1, point] = Points[point].RealY;
                _realPoints[2, point] = Points[point].RealZ;
                _realPoints[3, point] = 1;
                _imagePoints[0, point] = Points[point].ImgX;
                _imagePoints[1, point] = Points[point].ImgY;
                _imagePoints[2, point] = 1;
            }
        }

        public void NormalizeRealPoints()
        {
            _normReal = new DenseMatrix(4, 4);
            // Compute center of real grid
            double xc = 0, yc = 0, zc = 0;
            foreach(var point in Points)
            {
                xc += point.RealX;
                yc += point.RealY;
                zc += point.RealZ;
            }
            xc /= Points.Count;
            yc /= Points.Count;
            zc /= Points.Count;
            // Get mean distance of points from center
            double dist = 0;
            foreach(var point in Points)
            {
                dist += (double)Math.Sqrt((point.RealX - xc) * (point.RealX - xc) +
                    (point.RealY - yc) * (point.RealY - yc) + (point.RealZ - zc) * (point.RealZ - zc));
            }
            dist /= Points.Count;
            // Normalize in a way that mean dist = sqrt(3)
            double ratio = (double)Math.Sqrt(3) / dist;
            // Noramlize matrix - homonogeus point must be multiplied by it
            _normReal[0, 0] = ratio;
            _normReal[1, 1] = ratio;
            _normReal[2, 2] = ratio;
            _normReal[0, 3] = -ratio * xc;
            _normReal[1, 3] = -ratio * yc;
            _normReal[2, 3] = -ratio * zc;
            _normReal[3, 3] = 1;
            // Normalize points
            _realPoints = _normReal.Multiply(_realPoints);
            for(int p = 0; p < _realPoints.ColumnCount; p++)
            {
                _realPoints[0, p] = _realPoints[0, p] / _realPoints[3, p];
                _realPoints[1, p] = _realPoints[1, p] / _realPoints[3, p];
                _realPoints[2, p] = _realPoints[2, p] / _realPoints[3, p];
                _realPoints[3, p] = 1;
            }
        }

        public void NormalizeImagePoints()
        {
            _normImage = new DenseMatrix(3, 3);
            // Compute center of image points
            double xc = 0, yc = 0;
            foreach(var point in Points)
            {
                xc += point.ImgX;
                yc += point.ImgY;
            }
            xc /= Points.Count;
            yc /= Points.Count;
            // Get mean distance of points from center
            double dist = 0;
            foreach(var point in Points)
            {
                dist += (double)Math.Sqrt((point.ImgX - xc) * (point.ImgX - xc) +
                    (point.ImgY - yc) * (point.ImgY - yc));
            }
            dist /= Points.Count;
            // Normalize in a way that mean dist = sqrt(2)
            double ratio = (double)Math.Sqrt(2) / dist;
            // Noramlize matrix - homonogeus point must be multiplied by it
            _normImage[0, 0] = ratio;
            _normImage[1, 1] = ratio;
            _normImage[0, 2] = -ratio * xc;
            _normImage[1, 2] = -ratio * yc;
            _normImage[2, 2] = 1;

            _imagePoints = _normImage.Multiply(_imagePoints);
            for(int p = 0; p < _realPoints.ColumnCount; p++)
            {
                _imagePoints[0, p] = _imagePoints[0, p] / _imagePoints[2, p];
                _imagePoints[1, p] = _imagePoints[1, p] / _imagePoints[2, p];
                _imagePoints[2, p] = 1;
            }
        }

        public void FindNormalisedVariances()
        {
            // Scale variances with same factor as points
            //_normalisedVariances = new DenseVector(Points.Count * 5);
            //double scaleReal = _normReal[0, 0];
            //double scaleImage = _normImage[0, 0];
            //for(int i = 0; i < Points.Count; ++i)
            //{
            //    _normalisedVariances[3 * i] = 1.0f / (RealMeasurementVariance_X * scaleReal);
            //    _normalisedVariances[3 * i + 1] = 1.0f / (RealMeasurementVariance_Y * scaleReal);
            //    _normalisedVariances[3 * i + 2] = 1.0f / (RealMeasurementVariance_Z * scaleReal);
            //}

            //for(int i = 0; i < Points.Count; ++i)
            //{
            //    _normalisedVariances[Points.Count * 3 + 2 * i] = 1.0f / (ImageMeasurementVariance_X * scaleImage);
            //    _normalisedVariances[Points.Count * 3 + 2 * i + 1] = 1.0f / (ImageMeasurementVariance_Y * scaleImage);
            //}
            _normalisedVariances = new DenseVector(Points.Count * 5);
            double scaleReal = 1;
            double scaleImage = 1;
            for(int i = 0; i < Points.Count; ++i)
            {
                _normalisedVariances[3 * i] = 1.0 / (RealMeasurementVariance_X * scaleReal);
                _normalisedVariances[3 * i + 1] = 1.0 / (RealMeasurementVariance_Y * scaleReal);
                _normalisedVariances[3 * i + 2] = 1.0 / (RealMeasurementVariance_Z * scaleReal);
            }

            for(int i = 0; i < Points.Count; ++i)
            {
                _normalisedVariances[Points.Count * 3 + 2 * i] = 1.0 / (ImageMeasurementVariance_X * scaleImage);
                _normalisedVariances[Points.Count * 3 + 2 * i + 1] = 1.0 / (ImageMeasurementVariance_Y * scaleImage);
            }
        }

        public Matrix<double> FindLinearEstimationOfCameraMatrix()
        {
            Matrix<double> equationsMat = new DenseMatrix(2 * Points.Count, 12);

            for(int p = 0; p < Points.Count; p++)
            {
                // Fill matrix A with point info
                equationsMat.SetRow(2 * p, new double[12] {
                     0, 0, 0, 0,
                     -_realPoints[0, p], -_realPoints[1, p], -_realPoints[2, p], -1.0f,
                     _imagePoints[1, p] * _realPoints[0, p], _imagePoints[1, p] * _realPoints[1, p],
                     _imagePoints[1, p] * _realPoints[2, p], _imagePoints[1, p] });
                equationsMat.SetRow(2 * p + 1, new double[12] {
                    _realPoints[0, p], _realPoints[1, p], _realPoints[2, p], 1.0f,
                    0, 0, 0, 0,
                    -_imagePoints[0, p] * _realPoints[0, p], -_imagePoints[0, p] * _realPoints[1, p],
                    -_imagePoints[0, p] * _realPoints[2, p], -_imagePoints[0, p]});
            }

            _linearSolver.EquationsMatrix = equationsMat;
            _linearSolver.Solve();
            Vector<double> p_vec = _linearSolver.ResultVector;

            Matrix<double> cameraMatrix = new DenseMatrix(3, 4);
            cameraMatrix.SetRow(0, p_vec.SubVector(0, 4));
            cameraMatrix.SetRow(1, p_vec.SubVector(4, 4));
            cameraMatrix.SetRow(2, p_vec.SubVector(8, 4));

            return cameraMatrix;
        }

        public void DenormaliseCameraMatrix()
        {
            // Denormalises and scales camera matrix, so that CM[2,3] = 1
            CameraMatrix = _normImage.Inverse().Multiply(CameraMatrix.Multiply(_normReal));
        }

        public void DecomposeCameraMatrix()
        {
            var RQ = CameraMatrix.SubMatrix(0, 3, 0, 3).QR();
            CameraInternalMatrix = RQ.R;
            CameraRotationMatrix = RQ.Q;
            CameraTranslation = CameraMatrix.SubMatrix(0, 3, 0, 3).Inverse().
                Multiply(CameraMatrix.SubMatrix(0, 3, 3, 1)).Column(0);
        }

        public void PrepareMinimalisationAlg()
        {
            // M = [Xr | xi]
            _geometricErrorMinimalisationAlg.MeasurementsVector = new DenseVector(Points.Count * 5);
            for(int i = 0; i < Points.Count; ++i)
            {
                _geometricErrorMinimalisationAlg.MeasurementsVector[3 * i] = _realPoints[0, i];
                _geometricErrorMinimalisationAlg.MeasurementsVector[3 * i + 1] = _realPoints[1, i];
                _geometricErrorMinimalisationAlg.MeasurementsVector[3 * i + 2] = _realPoints[2, i];
                _geometricErrorMinimalisationAlg.MeasurementsVector[Points.Count * 3 + 2 * i] = _imagePoints[0, i];
                _geometricErrorMinimalisationAlg.MeasurementsVector[Points.Count * 3 + 2 * i + 1] = _imagePoints[1, i];
            }

            //_geometricErrorMinimalisationAlg.MeasurementsVector = new DenseVector(Points.Count * 2);
            //var RealPoints = new DenseVector(Points.Count * 3);
            //for(int i = 0; i < Points.Count; ++i)
            //{
            //    RealPoints[3 * i] = _realPoints[0, i];
            //    RealPoints[3 * i + 1] = _realPoints[1, i];
            //    RealPoints[3 * i + 2] = _realPoints[2, i];
            //    _geometricErrorMinimalisationAlg.MeasurementsVector[2 * i] = _imagePoints[0, i];
            //    _geometricErrorMinimalisationAlg.MeasurementsVector[2 * i + 1] = _imagePoints[1, i];
            //}
            //((LMCameraMatrixSimpleMinimalisation)_geometricErrorMinimalisationAlg).RealPoints = RealPoints;

            _geometricErrorMinimalisationAlg.MaximumIterations = 100;

            // For each real point allow like 1mm displacement and for img 0.25px
            // So e_max = (N * normScaleReal)^2 + (0.25 * N * normScaleImg)^2
            double scaleReal = 0.001;// _normReal[0, 0] * _normReal[0, 0];
            double scaleImage = 0.01;// _normImage[0, 0] * _normImage[0, 0];
            _geometricErrorMinimalisationAlg.MaximumResidiual = 0.1;
            //Points.Count * Points.Count * (scaleReal + 0.0625f * scaleImage);

            // P = [pi | Xr]
            _geometricErrorMinimalisationAlg.ParametersVector = new DenseVector(Points.Count * 3 + 12);
            //_geometricErrorMinimalisationAlg.ParametersVector = new DenseVector(12);
            _geometricErrorMinimalisationAlg.ParametersVector[0] = CameraMatrix[0, 0];
            _geometricErrorMinimalisationAlg.ParametersVector[1] = CameraMatrix[0, 1];
            _geometricErrorMinimalisationAlg.ParametersVector[2] = CameraMatrix[0, 2];
            _geometricErrorMinimalisationAlg.ParametersVector[3] = CameraMatrix[0, 3];
            _geometricErrorMinimalisationAlg.ParametersVector[4] = CameraMatrix[1, 0];
            _geometricErrorMinimalisationAlg.ParametersVector[5] = CameraMatrix[1, 1];
            _geometricErrorMinimalisationAlg.ParametersVector[6] = CameraMatrix[1, 2];
            _geometricErrorMinimalisationAlg.ParametersVector[7] = CameraMatrix[1, 3];
            _geometricErrorMinimalisationAlg.ParametersVector[8] = CameraMatrix[2, 0];
            _geometricErrorMinimalisationAlg.ParametersVector[9] = CameraMatrix[2, 1];
            _geometricErrorMinimalisationAlg.ParametersVector[10] = CameraMatrix[2, 2];
            _geometricErrorMinimalisationAlg.ParametersVector[11] = CameraMatrix[2, 3];
            for(int i = 0; i < Points.Count; ++i)
            {
                _geometricErrorMinimalisationAlg.ParametersVector[3 * i + 12] = _realPoints[0, i];
                _geometricErrorMinimalisationAlg.ParametersVector[3 * i + 13] = _realPoints[1, i];
                _geometricErrorMinimalisationAlg.ParametersVector[3 * i + 14] = _realPoints[2, i];
            }

            _geometricErrorMinimalisationAlg.UseCovarianceMatrix = true;
            _geometricErrorMinimalisationAlg.InverseVariancesVector = _normalisedVariances;
        }

        public void MinimizeError()
        {
            PrepareMinimalisationAlg();
            _geometricErrorMinimalisationAlg.Process();

            // P = [pi | eXr]
            var estimatedParams = _geometricErrorMinimalisationAlg.BestResultVector;
            CameraMatrix[0, 0] = estimatedParams[0];
            CameraMatrix[0, 1] = estimatedParams[1];
            CameraMatrix[0, 2] = estimatedParams[2];
            CameraMatrix[0, 3] = estimatedParams[3];
            CameraMatrix[1, 0] = estimatedParams[4];
            CameraMatrix[1, 1] = estimatedParams[5];
            CameraMatrix[1, 2] = estimatedParams[6];
            CameraMatrix[1, 3] = estimatedParams[7];
            CameraMatrix[2, 0] = estimatedParams[8];
            CameraMatrix[2, 1] = estimatedParams[9];
            CameraMatrix[2, 2] = estimatedParams[10];
            CameraMatrix[2, 3] = estimatedParams[11];
        }

        public void InitParameters()
        {
            Parameters = new List<ProcessorParameter>();

            ProcessorParameter varImgX = new ProcessorParameter(
                    "Image Measurements Variance X", "VIX",
                    "System.Single", 0.25f, 0.0f, 100.0f);
            Parameters.Add(varImgX);

            ProcessorParameter varImgY = new ProcessorParameter(
               "Image Measurements Variance Y", "VIY",
               "System.Single", 0.25f, 0.0f, 100.0f);
            Parameters.Add(varImgY);

            ProcessorParameter varRealX = new ProcessorParameter(
               "Real Measurements Variance X", "VRX",
               "System.Boolean", 6.25f, 0.0f, 1000.0f);
            Parameters.Add(varRealX);

            ProcessorParameter varRealY = new ProcessorParameter(
               "Real Measurements Variance Y", "VRY",
               "System.Int32", 6.25f, 0.0f, 1000.0f);
            Parameters.Add(varRealY);

            ProcessorParameter varRealZ = new ProcessorParameter(
               "Real Measurements Variance Z", "VRZ",
               "System.Boolean", 6.25f, 0.0f, 1000.0f);
            Parameters.Add(varRealZ);
        }

        public void UpdateParameters()
        {
            ImageMeasurementVariance_X = (double)ProcessorParameter.FindValue("VIX", Parameters);
            ImageMeasurementVariance_Y = (double)ProcessorParameter.FindValue("VIY", Parameters);
            RealMeasurementVariance_X = (double)ProcessorParameter.FindValue("VRX", Parameters);
            RealMeasurementVariance_Y = (double)ProcessorParameter.FindValue("VRY", Parameters);
            RealMeasurementVariance_Z = (double)ProcessorParameter.FindValue("VRZ", Parameters);
        }

        public override string ToString()
        {
            return "Camera Calibrator";
        }
    }
}
