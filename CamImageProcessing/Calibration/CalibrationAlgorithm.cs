﻿using System;
using System.Collections.Generic;
using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Text;

namespace CamAlgorithms
{
    public class CalibrationAlgorithm : IParameterizable
    {
        public List<CalibrationPoint> Points { set; get; } // May change during calibration
        public List<RealGridData> Grids { get; set; }
        public List<RealGridData> GridsNormalised { get; private set; }
        public List<RealGridData> GridsEstimated { get; private set; }
        public Matrix<double> CameraMatrix { set; get; }
        public Matrix<double> CameraInternalMatrix { set; get; }
        public Matrix<double> CameraRotationMatrix { set; get; }
        public Vector<double> CameraTranslation { set; get; }

        public double ImageMeasurementVariance_X { get; set; }
        public double ImageMeasurementVariance_Y { get; set; }
        public double RealMeasurementVariance_X { get; set; }
        public double RealMeasurementVariance_Y { get; set; }
        public double RealMeasurementVariance_Z { get; set; }

        public bool LinearOnly { get; set; }
        public bool NormalizeLinear { get; set; }
        public bool NormalizeIterative { get; set; }
        public bool UseCovarianceMatrix { get; set; }
        public bool MinimaliseSkew { get; set; }

        public bool EliminateOuliers { get; set; }
        public double OutliersCoeff { get; set; }

        public bool OverwriteGridsWithEstimated { get; set; }

        public int MaxIterations { get; set; }

        public Matrix<double> NormReal { get; private set; }
        public Matrix<double> NormImage { get; private set; }
        public Matrix<double> RealPoints { get; private set; }
        public Matrix<double> ImagePoints { get; private set; }

        public Vector<double> NormalisedVariances { get; private set; }
        
        public LMCameraMatrixGridMinimalisation _miniAlg =
            new LMCameraMatrixGridMinimalisation();
        public LMCameraMatrixZeroSkewMinimalisation _zeroSkewMini = new LMCameraMatrixZeroSkewMinimalisation();

        public bool _linearEstimationDone = false;
        public bool _pointsNormalised = false;
        public bool _inMinimsation = false;

        public void Calibrate()
        {
            // Action plan:
            // 1) Make points homonogeus                      
            // 2) Normalize points                            
            // 3) Get linear estimation of camera matrix
            // 4) Compute normalised variances/inverses
            // 5) Minimise geometric error of camera matrix
            // 6) Denormalise camera matrix
            _linearEstimationDone = false;
            _inMinimsation = false;
            _pointsNormalised = false;
            HomoPoints();

            if(NormalizeLinear)
            {
                NormalizePoints();
                _pointsNormalised = true;
            }
            else
            {
                NormImage = DenseMatrix.CreateIdentity(3);
                NormReal = DenseMatrix.CreateIdentity(4);
            }

            CameraMatrix = FindLinearEstimationOfCameraMatrix(ImagePoints, RealPoints);

            //if(EliminateOuliers)
            //{
            //    OutliersElimination();

            //    HomoPoints();
            //    if(NormalizeLinear)
            //    {
            //        NormalizeImagePoints();
            //        NormalizeRealPoints();
            //        _pointsNormalised = true;
            //    }
            //    CameraMatrix = FindLinearEstimationOfCameraMatrix();
            //}
            _linearEstimationDone = true;

            if(MinimaliseSkew)
            {
                MinimizeSkew();
            }

            if(LinearOnly == false)
            {
                if(NormalizeIterative == false && _pointsNormalised)
                {
                    HomoPoints();
                    CameraMatrix = DenormaliseCameraMatrix(CameraMatrix, NormImage, NormReal);
                    _pointsNormalised = false;
                    NormImage = DenseMatrix.CreateIdentity(3);
                    NormReal = DenseMatrix.CreateIdentity(4);
                }

                GridsNormalised = NormalizeCalibGrids(Grids);
                FindNormalisedVariances();

                _inMinimsation = true;
                MinimizeError();
                _inMinimsation = false;

                if(MinimaliseSkew)
                {
                    MinimizeSkew();
                }
            }

            if(EliminateOuliers)
            {
                EliminateOuliers = false;
                OutliersElimination();
                Calibrate();
            }

            if(OverwriteGridsWithEstimated)
            {
                if(_pointsNormalised)
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

            if(_pointsNormalised)
            {
                CameraMatrix = DenormaliseCameraMatrix(CameraMatrix, NormImage, NormReal);
                _pointsNormalised = false;
            }
            DecomposeCameraMatrix();

        }

        public void NormalizePoints()
        {
            NormImage = PointNormalization.FindNormalizationMatrix2d(ImagePoints);
            ImagePoints = PointNormalization.NormalizePoints(ImagePoints, NormImage);
            NormReal = PointNormalization.FindNormalizationMatrix2d(RealPoints);
            RealPoints = PointNormalization.NormalizePoints(RealPoints, NormReal);
        }
        
        public void HomoPoints() // Create homonogeus points matrices form CalibrationPoint list
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
        
        public List<RealGridData> NormalizeCalibGrids(List<RealGridData> grids)
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
        
        public Matrix<double> FindLinearEstimationOfCameraMatrix(Matrix<double> imagePoints, Matrix<double> realPoints)
        {
            Matrix<double> equationsMat = new DenseMatrix(2 * imagePoints.ColumnCount, 12);

            for(int p = 0; p < imagePoints.ColumnCount; p++)
            {
                // Fill matrix A with point info
                equationsMat.SetRow(2 * p, new double[12] {
                     0, 0, 0, 0,
                     -realPoints[0, p], -realPoints[1, p], -realPoints[2, p], -1.0f,
                     imagePoints[1, p] * realPoints[0, p], imagePoints[1, p] * realPoints[1, p],
                     imagePoints[1, p] * realPoints[2, p], imagePoints[1, p] });
                equationsMat.SetRow(2 * p + 1, new double[12] {
                    realPoints[0, p], realPoints[1, p], realPoints[2, p], 1.0f,
                    0, 0, 0, 0,
                    -imagePoints[0, p] * realPoints[0, p], -imagePoints[0, p] * realPoints[1, p],
                    -imagePoints[0, p] * realPoints[2, p], -imagePoints[0, p]});
            }

            var eqI = equationsMat.ColumnSums();
            var solver = new SvdZeroFullrankSolver();
            solver.EquationsMatrix = equationsMat;
            solver.Solve();
            Vector<double> p_vec = solver.ResultVector;

            Matrix<double> cameraMatrix = new DenseMatrix(3, 4);
            cameraMatrix.SetRow(0, p_vec.SubVector(0, 4));
            cameraMatrix.SetRow(1, p_vec.SubVector(4, 4));
            cameraMatrix.SetRow(2, p_vec.SubVector(8, 4));

            return cameraMatrix;
        }
        
        public Matrix<double> DenormaliseCameraMatrix(Matrix<double> cameraMatrix, Matrix<double> normImage, Matrix<double> normReal)
        {
            return normImage.Inverse().Multiply(cameraMatrix.Multiply(normReal));
        }

        public void DenormaliseCameraMatrix()
        {
            CameraMatrix = NormImage.Inverse().Multiply(CameraMatrix.Multiply(NormReal));
        }

        // TODO: dont set properties
        public void DecomposeCameraMatrix()
        {
            var RQ = CameraMatrix.SubMatrix(0, 3, 0, 3).QR();

            double scaleK = 1.0 / RQ.R[2, 2];
            CameraMatrix.MultiplyThis(scaleK);

            RQ = CameraMatrix.SubMatrix(0, 3, 0, 3).QR();
            CameraInternalMatrix = RQ.R;
            CameraRotationMatrix = RQ.Q;

            // If fx < 0 (which in practice happens often), then set fx = -fx and [r11,r12,r13] = -[r11,r12,r13]
            // As first row of rotation matrix is multiplied only with fx, then changing sign of both
            // fx and this row won't change matrix M = K*R, and so camera matrix
            if(CameraInternalMatrix[0, 0] < 0)
            {
                CameraInternalMatrix[0, 0] = -CameraInternalMatrix[0, 0];
                CameraRotationMatrix[0, 0] = -CameraRotationMatrix[0, 0];
                CameraRotationMatrix[0, 1] = -CameraRotationMatrix[0, 1];
                CameraRotationMatrix[0, 2] = -CameraRotationMatrix[0, 2];
            }
            if(CameraInternalMatrix[1, 1] < 0)
            {
                CameraInternalMatrix[1, 1] = -CameraInternalMatrix[1, 1];
                CameraInternalMatrix[0, 1] = -CameraInternalMatrix[0, 1];
                CameraRotationMatrix[1, 0] = -CameraRotationMatrix[1, 0];
                CameraRotationMatrix[1, 1] = -CameraRotationMatrix[1, 1];
                CameraRotationMatrix[1, 2] = -CameraRotationMatrix[1, 2];
            }

            CameraTranslation = CameraMatrix.SubMatrix(0, 3, 0, 3).Inverse().
                Multiply(CameraMatrix.SubMatrix(0, 3, 3, 1)).Column(0);
        }

        // TODO: pass and return
        public void FindNormalisedVariances()
        {
            // Scale variances with same factor as points
            NormalisedVariances = new DenseVector(Points.Count * 2 + Grids.Count * 12);
            double scaleReal = _pointsNormalised ? NormReal[0, 0] * NormReal[0, 0] : 1.0;
            double scaleImage = _pointsNormalised ? NormImage[0, 0] * NormImage[0, 0] : 1.0;
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

        // TODO: Pass camera matrix
        public void PrepareMinimalisationAlg()
        {
            _miniAlg.MaximumIterations = MaxIterations;
            // M = [Xr | xi]
            _miniAlg.MeasurementsVector = new DenseVector(Points.Count * 2 + Grids.Count * 12);
            for(int i = 0; i < Points.Count; ++i)
            {
                _miniAlg.MeasurementsVector.At(2 * i, ImagePoints.At(0, i));
                _miniAlg.MeasurementsVector.At(2 * i + 1, ImagePoints.At(1, i));
            }

            int N2 = Points.Count * 2;
            for(int i = 0; i < Grids.Count; ++i)
            {
                _miniAlg.MeasurementsVector.At(N2 + i * 12, GridsNormalised[i].TopLeft.X);
                _miniAlg.MeasurementsVector.At(N2 + i * 12 + 1, GridsNormalised[i].TopLeft.Y);
                _miniAlg.MeasurementsVector.At(N2 + i * 12 + 2, GridsNormalised[i].TopLeft.Z);
                _miniAlg.MeasurementsVector.At(N2 + i * 12 + 3, GridsNormalised[i].TopRight.X);
                _miniAlg.MeasurementsVector.At(N2 + i * 12 + 4, GridsNormalised[i].TopRight.Y);
                _miniAlg.MeasurementsVector.At(N2 + i * 12 + 5, GridsNormalised[i].TopRight.Z);
                _miniAlg.MeasurementsVector.At(N2 + i * 12 + 6, GridsNormalised[i].BotLeft.X);
                _miniAlg.MeasurementsVector.At(N2 + i * 12 + 7, GridsNormalised[i].BotLeft.Y);
                _miniAlg.MeasurementsVector.At(N2 + i * 12 + 8, GridsNormalised[i].BotLeft.Z);
                _miniAlg.MeasurementsVector.At(N2 + i * 12 + 9, GridsNormalised[i].BotRight.X);
                _miniAlg.MeasurementsVector.At(N2 + i * 12 + 10, GridsNormalised[i].BotRight.Y);
                _miniAlg.MeasurementsVector.At(N2 + i * 12 + 11, GridsNormalised[i].BotRight.Z);
            }

            // For each real point allow like 1mm displacement and for img 0.25px
            // So e_max = N*normScaleReal^2 + N * (0.25 * normScaleImg)^2
            double scaleReal = _pointsNormalised ? NormReal[0, 0] * NormReal[0, 0] : 1.0;
            double scaleImage = _pointsNormalised ? NormImage[0, 0] * NormImage[0, 0] : 1.0;
            _miniAlg.MaximumResidiual =
                Points.Count * (0.1 * scaleReal + 0.0625f * scaleImage);

            // P = [pi | Xr]
            //_geometricErrorMinimalisationAlg.ParametersVector = new DenseVector(Points.Count * 3 + 12);
            _miniAlg.ParametersVector = new DenseVector(12);
            _miniAlg.ParametersVector[0] = CameraMatrix[0, 0];
            _miniAlg.ParametersVector[1] = CameraMatrix[0, 1];
            _miniAlg.ParametersVector[2] = CameraMatrix[0, 2];
            _miniAlg.ParametersVector[3] = CameraMatrix[0, 3];
            _miniAlg.ParametersVector[4] = CameraMatrix[1, 0];
            _miniAlg.ParametersVector[5] = CameraMatrix[1, 1];
            _miniAlg.ParametersVector[6] = CameraMatrix[1, 2];
            _miniAlg.ParametersVector[7] = CameraMatrix[1, 3];
            _miniAlg.ParametersVector[8] = CameraMatrix[2, 0];
            _miniAlg.ParametersVector[9] = CameraMatrix[2, 1];
            _miniAlg.ParametersVector[10] = CameraMatrix[2, 2];
            _miniAlg.ParametersVector[11] = CameraMatrix[2, 3];
            //for(int i = 0; i < Points.Count; ++i)
            //{
            //    _geometricErrorMinimalisationAlg.ParametersVector[3 * i + 12] = _realPoints[0, i];
            //    _geometricErrorMinimalisationAlg.ParametersVector[3 * i + 13] = _realPoints[1, i];
            //    _geometricErrorMinimalisationAlg.ParametersVector[3 * i + 14] = _realPoints[2, i];
            //}

            _miniAlg.UseCovarianceMatrix = UseCovarianceMatrix;
            _miniAlg.InverseVariancesVector = NormalisedVariances;

            _miniAlg.CalibrationPoints = Points;
            _miniAlg.CalibrationGrids = GridsNormalised;
        }

        // TODO: Pass camera matrix
        public void MinimizeError()
        {
            PrepareMinimalisationAlg();
            _miniAlg.Process();

            // P = [pi | eXr]
            var estimatedParams = _miniAlg.BestResultVector;
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

        // TODO: Pass camera matrix
        public void PrepareSkewMiniAlg()
        {
            _zeroSkewMini.MaximumIterations = MaxIterations;
            // M = [Xr | xi]
            _zeroSkewMini.MeasurementsVector = new DenseVector(Points.Count * 5);
            for(int i = 0; i < Points.Count; ++i)
            {
                _zeroSkewMini.MeasurementsVector[3 * i] = RealPoints[0, i];
                _zeroSkewMini.MeasurementsVector[3 * i + 1] = RealPoints[1, i];
                _zeroSkewMini.MeasurementsVector[3 * i + 2] = RealPoints[2, i];
                _zeroSkewMini.MeasurementsVector[Points.Count * 3 + 2 * i] = ImagePoints[0, i];
                _zeroSkewMini.MeasurementsVector[Points.Count * 3 + 2 * i + 1] = ImagePoints[1, i];
            }

            // For each real point allow like 1mm displacement and for img 0.25px
            // So e_max = N*normScaleReal^2 + N * (0.25 * normScaleImg)^2
            double scaleReal = _pointsNormalised ? NormReal[0, 0] * NormReal[0, 0] : 1.0;
            double scaleImage = _pointsNormalised ? NormImage[0, 0] * NormImage[0, 0] : 1.0;
            _zeroSkewMini.MaximumResidiual =
                Points.Count * (0.1 * scaleReal + 0.0625f * scaleImage);

            _zeroSkewMini.ParametersVector = new DenseVector(12);
            _zeroSkewMini.ParametersVector[0] = CameraMatrix[0, 0];
            _zeroSkewMini.ParametersVector[1] = CameraMatrix[0, 1];
            _zeroSkewMini.ParametersVector[2] = CameraMatrix[0, 2];
            _zeroSkewMini.ParametersVector[3] = CameraMatrix[0, 3];
            _zeroSkewMini.ParametersVector[4] = CameraMatrix[1, 0];
            _zeroSkewMini.ParametersVector[5] = CameraMatrix[1, 1];
            _zeroSkewMini.ParametersVector[6] = CameraMatrix[1, 2];
            _zeroSkewMini.ParametersVector[7] = CameraMatrix[1, 3];
            _zeroSkewMini.ParametersVector[8] = CameraMatrix[2, 0];
            _zeroSkewMini.ParametersVector[9] = CameraMatrix[2, 1];
            _zeroSkewMini.ParametersVector[10] = CameraMatrix[2, 2];
            _zeroSkewMini.ParametersVector[11] = CameraMatrix[2, 3];

            _zeroSkewMini.UseCovarianceMatrix = false;
        }

        // TODO: Pass camera matrix return camera matrix
        public void MinimizeSkew()
        {
            PrepareSkewMiniAlg();
            _zeroSkewMini.Process();

            // P = [pi | eXr]
            var estimatedParams = _zeroSkewMini.BestResultVector;
            // TODO: Copy vector to matrix and vice versa in extensions
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

        private void OutliersElimination()
        {
            // Eliminate from calibration all points with reprojection error greater than _outlierCoeff * mean_error
            double error = 0.0;
            double[] errors = new double[Points.Count];
            for(int p = 0; p < Points.Count; ++p)
            {
                var cp = Points[p];
                Vector<double> ip = new DenseVector(new double[] { ImagePoints.At(0, p), ImagePoints.At(1, p), 1.0 });
                Vector<double> rp = new DenseVector(new double[] { RealPoints.At(0, p), RealPoints.At(1, p), RealPoints.At(2, p), 1.0 });

                Vector<double> eip = CameraMatrix * rp;
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
        //            bestResult = CameraMatrix;

        //            if(bestError < threshold)
        //            {
        //                break;
        //            }
        //        }
        //    }

        //    CameraMatrix = bestResult;
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

        //        Vector<double> eip = CameraMatrix * rp;
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

            AlgorithmParameter linOnly = new BooleanParameter(
                    "Perform only linear estimation", "LIN", false);
            Parameters.Add(linOnly);

            AlgorithmParameter normLin = new BooleanParameter(
                    "Normalize points for linear estimation", "NPL", true);
            Parameters.Add(normLin);

            AlgorithmParameter normIt = new BooleanParameter(
                    "Normalize points for iterative estimation", "NPI", true);
            Parameters.Add(normIt);

            AlgorithmParameter cov = new BooleanParameter(
                    "Use covariance matrix", "COV", true);
            Parameters.Add(cov);

            AlgorithmParameter skew = new BooleanParameter(
                    "Minimalise skew", "SKEW", true);
            Parameters.Add(skew);

            AlgorithmParameter varImgX = new DoubleParameter(
                    "Image Measurements Variance X", "VIX", 0.25, 0.0, 100.0);
            Parameters.Add(varImgX);

            AlgorithmParameter varImgY = new DoubleParameter(
               "Image Measurements Variance Y", "VIY", 0.25, 0.0, 100.0);
            Parameters.Add(varImgY);

            AlgorithmParameter varRealX = new DoubleParameter(
               "Real Measurements Variance X", "VRX", 1.0, 0.0, 1000.0);
            Parameters.Add(varRealX);

            AlgorithmParameter varRealY = new DoubleParameter(
               "Real Measurements Variance Y", "VRY", 1.0, 0.0, 1000.0);
            Parameters.Add(varRealY);

            AlgorithmParameter varRealZ = new DoubleParameter(
               "Real Measurements Variance Z", "VRZ", 1.0, 0.0, 1000.0);
            Parameters.Add(varRealZ);

            AlgorithmParameter iters = new IntParameter(
               "Max Iterations", "MI", 100, 1, 10000);
            Parameters.Add(iters);

            AlgorithmParameter eliminate = new BooleanParameter(
               "Outliers elimination after linear estimation", "ELIM", false);
            Parameters.Add(eliminate);

            AlgorithmParameter outCoeff = new DoubleParameter(
               "Elimination coeff (elim if e >= m * c)", "ECOEFF", 1.5, 0.0, 1000.0);
            Parameters.Add(outCoeff);

            AlgorithmParameter overGrids = new BooleanParameter(
               "Overwrite grids with estimated", "OVERG", false);
            Parameters.Add(overGrids);

            AlgorithmParameter maxIters = new IntParameter(
               "MaxIterations", "ITER", 100, 1, 1000);
            Parameters.Add(maxIters);
        }

        public void UpdateParameters()
        {
            ImageMeasurementVariance_X = AlgorithmParameter.FindValue<double>("VIX", Parameters);
            ImageMeasurementVariance_Y = AlgorithmParameter.FindValue<double>("VIY", Parameters);
            RealMeasurementVariance_X = AlgorithmParameter.FindValue<double>("VRX", Parameters);
            RealMeasurementVariance_Y = AlgorithmParameter.FindValue<double>("VRY", Parameters);
            RealMeasurementVariance_Z = AlgorithmParameter.FindValue<double>("VRZ", Parameters);
            _miniAlg.MaximumIterations = AlgorithmParameter.FindValue<int>("MI", Parameters);
            _zeroSkewMini.MaximumIterations = AlgorithmParameter.FindValue<int>("MI", Parameters);
            LinearOnly = AlgorithmParameter.FindValue<bool>("LIN", Parameters);
            NormalizeLinear = AlgorithmParameter.FindValue<bool>("NPL", Parameters);
            NormalizeIterative = AlgorithmParameter.FindValue<bool>("NPI", Parameters);
            UseCovarianceMatrix = AlgorithmParameter.FindValue<bool>("COV", Parameters);
            MinimaliseSkew = AlgorithmParameter.FindValue<bool>("SKEW", Parameters);
            EliminateOuliers = AlgorithmParameter.FindValue<bool>("ELIM", Parameters);
            OutliersCoeff = AlgorithmParameter.FindValue<double>("ECOEFF", Parameters);
            OverwriteGridsWithEstimated = AlgorithmParameter.FindValue<bool>("OVERG", Parameters);
            MaxIterations = AlgorithmParameter.FindValue<int>("ITER", Parameters);
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