using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CamCore;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using CamAlgorithms;
using CamAlgorithms.Calibration;

namespace CamUnitTest
{
    [TestClass]
    public class CameraMatrixComputationTests
    {
        Matrix<double> K_l;
        Matrix<double> K_r;
        Matrix<double> R_l;
        Matrix<double> R_r;
        Vector<double> C_l;
        Vector<double> C_r;
        Matrix<double> CM_l;
        Matrix<double> CM_r;

        CameraPair PrepareCamerasForDecomposition()
        {
            K_l = new DenseMatrix(3, 3);
            K_l[0, 0] = 10.0; // fx
            K_l[1, 1] = 10.0; // fy
            K_l[0, 1] = 0.0; // s
            K_l[0, 2] = 300.0; // x0
            K_l[1, 2] = 250.0; // y0
            K_l[2, 2] = 1.0; // 1

            K_r = new DenseMatrix(3, 3);
            K_r[0, 0] = 12.0; // fx
            K_r[1, 1] = 12.5; // fy
            K_r[0, 1] = 0.0; // s
            K_r[0, 2] = 300.0; // x0
            K_r[1, 2] = 200.0; // y0
            K_r[2, 2] = 1.0; // 1

            R_l = DenseMatrix.CreateIdentity(3);
            R_r = DenseMatrix.CreateIdentity(3);

            C_l = new DenseVector(3);
            C_l[0] = 50.0;
            C_l[1] = 50.0;
            C_l[2] = 0.0;

            C_r = new DenseVector(3);
            C_r[0] = 40.0;
            C_r[1] = 40.0;
            C_r[2] = 10.0;

            var Ext_l = new DenseMatrix(3, 4);
            Ext_l.SetSubMatrix(0, 0, R_l);
            Ext_l.SetColumn(3, -R_l * C_l);

            var Ext_r = new DenseMatrix(3, 4);
            Ext_r.SetSubMatrix(0, 0, R_r);
            Ext_r.SetColumn(3, -R_r * C_r);

            CM_l = K_l * Ext_l;
            CM_r = K_r * Ext_r;

            CameraPair cData = new CameraPair();
            cData.Left.Matrix = CM_l;
            cData.Right.Matrix = CM_r;
            cData.Update();

            return cData;
        }

        [TestMethod]
        public void Test_CameraMatrixDecomposition()
        {
            CameraPair cData = PrepareCamerasForDecomposition();

            TestUtils.AssertEquals(cData.Left.Matrix, CM_l, "Left.Matrix", scaleInvariant: true);
            TestUtils.AssertEquals(cData.Right.Matrix, CM_r, "Right.Matrix", scaleInvariant: true);
            TestUtils.AssertEquals(cData.Left.InternalMatrix, K_l, "Left.InternalMatrix");
            TestUtils.AssertEquals(cData.Right.InternalMatrix, K_r, "Right.InternalMatrix");
            TestUtils.AssertEquals(cData.Left.RotationMatrix, R_l, "Left.RotationMatrix");
            TestUtils.AssertEquals(cData.Right.RotationMatrix, R_r, "Right.RotationMatrix");
            TestUtils.AssertEquals(cData.Left.Center, C_l, "Left.Translation");
            TestUtils.AssertEquals(cData.Right.Center, C_r, "Right.Translation");
        }

        [TestMethod]
        public void Test_CameraMatrixFundamentalComputation()
        {
            CameraPair cData = PrepareCamerasForDecomposition();

            Vector<double> epiLeft = new DenseVector(new double[] { 2900.0, 2400.0, 10.0 });
            Vector<double> epiRight = new DenseVector(new double[] { -2880.0, -1875.0, -10 });
            Matrix<double> fundamental = new DenseMatrix(3, 3, new double[] {
                -2.477e-19, -5.673e-5, 1.064e-2, 5.910e-5, 8.398e-21, -1.702e-2, -1.418e-2, 1.645e-2, 1.0
            });

            TestUtils.AssertEquals(cData.EpiPoleLeft, epiLeft, "EpipoleLeft");
            TestUtils.AssertEquals(cData.EpiPoleRight, epiRight, "EpipoleRight");
            TestUtils.AssertEquals(cData.Fundamental, fundamental, "Fundamental", maxDiffError:1e-4);
        }

        Matrix<double> _CM;
        Matrix<double> _eCM;
        
        class TestCalibrationAlgorithm : CalibrationAlgorithm
        {
            public new void ConvertPointsToHomonogeus()
            {
                base.ConvertPointsToHomonogeus();
            }

            public new void NormalizePoints()
            {
                base.NormalizePoints();
            }

            public new Matrix<double> FindLinearEstimationOfCameraMatrix()
            {
                return base.FindLinearEstimationOfCameraMatrix();
            }
        }
        TestCalibrationAlgorithm _calib;

        public void PrepareCameraMatrix()
        {
            Matrix<double> K = new DenseMatrix(3);
            K[0, 0] = 10.0; // fx
            K[1, 1] = 10.0; // fy
            K[0, 1] = 0.0; // s
            K[0, 2] = 0.0; // x0
            K[1, 2] = 0.0; // y0
            K[2, 2] = 1.0; // 1

            Matrix<double> R = DenseMatrix.CreateIdentity(3);
            Vector<double> C = new DenseVector(3);
            C[0] = 50.0;
            C[1] = 50.0;
            C[2] = 0.0;

            Matrix<double> Ext = new DenseMatrix(3, 4);
            Ext.SetSubMatrix(0, 0, R);
            Ext.SetColumn(3, -R * C);

            _CM = K * Ext;
        }

        public void PrepareCalibrator(List<CalibrationPoint> points)
        {
            _calib = new TestCalibrationAlgorithm();
            _calib.Points = points;

            _calib.ImageMeasurementVariance_X = _varianceImage;
            _calib.ImageMeasurementVariance_Y = _varianceImage;

            _calib.RealMeasurementVariance_X = _varianceReal;
            _calib.RealMeasurementVariance_Y = _varianceReal;
            _calib.RealMeasurementVariance_Z = _varianceReal;
        }

        [TestMethod]
        public void Test_EstimateCameraMatrix_IdealLinear()
        {
            PrepareCameraMatrix();
            var points = GenerateCalibrationPoints_Random();
            PrepareCalibrator(points);

            _calib.ConvertPointsToHomonogeus();
            _calib.NormalizePoints();

            _calib.Camera.Matrix = _calib.FindLinearEstimationOfCameraMatrix();

            _calib.Camera.Denormalize(_calib.NormImage, _calib.NormReal);
            _calib.Camera.Decompose();
            _eCM = _calib.Camera.Matrix;

            double scaleK = -1.0 / _calib.Camera.InternalMatrix[2, 2];
            _eCM.MultiplyThis(scaleK);

            Assert.IsTrue((_eCM - _CM).FrobeniusNorm() < 1e-6);

            for(int p = 0; p < _pointsCount; ++p)
            {
                var cp = points[p];
                Vector<double> rp = new DenseVector(4);
                rp[0] = cp.RealX;
                rp[1] = cp.RealY;
                rp[2] = cp.RealZ;
                rp[3] = 1.0;
                var imagePoint = _eCM * rp;

                Vector2 ip = new Vector2(imagePoint[0] / imagePoint[2], imagePoint[1] / imagePoint[2]);
                Assert.IsTrue((ip - cp.Img).Length() < cp.Img.Length() / 100.0);
            }
        }
        
        int _pointsCount = 100;

        double _rangeReal_MinX = 0.0;
        double _rangeReal_MaxX = 100.0;
        double _rangeReal_MinY = 0.0;
        double _rangeReal_MaxY = 100.0;
        double _rangeReal_MinZ = 50.0;
        double _rangeReal_MaxZ = 100.0;
        
        double _varianceReal = 1;
        double _varianceImage = 0.01;

        public List<CalibrationPoint> GenerateCalibrationPoints_Random(int seed = 0)
        {
            List<CalibrationPoint> pointList = new List<CalibrationPoint>(_pointsCount);

            Random rand;
            if(seed == 0)
                rand = new Random();
            else
                rand = new Random(seed);

            for(int i = 0; i < _pointsCount; ++i)
            {
                double rx = rand.NextDouble() * (_rangeReal_MaxX - _rangeReal_MinX) + _rangeReal_MinX;
                double ry = rand.NextDouble() * (_rangeReal_MaxY - _rangeReal_MinY) + _rangeReal_MinY;
                double rz = rand.NextDouble() * (_rangeReal_MaxZ - _rangeReal_MinZ) + _rangeReal_MinZ;

                CalibrationPoint cpoint = new CalibrationPoint();
                cpoint.Real = new Vector3(rx, ry, rz);

                Vector<double> rp = new DenseVector(4);
                rp[0] = rx;
                rp[1] = ry;
                rp[2] = rz;
                rp[3] = 1.0;
                var imagePoint = _CM * rp;

                cpoint.Img = new Vector2(imagePoint[0] / imagePoint[2], imagePoint[1] / imagePoint[2]);

                pointList.Add(cpoint);
            }

            return pointList;
        }

        public List<CalibrationPoint> AddNoise(List<CalibrationPoint> points,
            double varReal, double varImg, int seed = 0)
        {
            List<CalibrationPoint> noisedPoints = new List<CalibrationPoint>(points.Count);

            GaussianNoiseGenerator noiseReal = new GaussianNoiseGenerator();
            noiseReal.Variance = varReal;
            noiseReal.Mean = 0.0;
            noiseReal.RandomSeed = seed != 0;
            noiseReal.Seed = seed;
            noiseReal.UpdateDistribution();

            GaussianNoiseGenerator noiseImage = new GaussianNoiseGenerator();
            noiseImage.Variance = varImg;
            noiseImage.Mean = 0.0;
            noiseImage.RandomSeed = seed != 0;
            noiseImage.Seed = seed;
            noiseImage.UpdateDistribution();

            for(int i = 0; i < _pointsCount; ++i)
            {
                CalibrationPoint cpoint = new CalibrationPoint();

                cpoint.RealX = points[i].RealX + noiseReal.GetSample();
                cpoint.RealY = points[i].RealY + noiseReal.GetSample();
                cpoint.RealZ = points[i].RealZ + noiseReal.GetSample();

                cpoint.ImgX = points[i].ImgX + noiseImage.GetSample();
                cpoint.ImgY = points[i].ImgY + noiseImage.GetSample();

                noisedPoints.Add(cpoint);
            }

            return noisedPoints;
        }

        int _seed = 100;
        public Matrix<double> AddNoise(Matrix<double> matrix)
        {
            Matrix<double> noisedMat = matrix.Clone();

            GaussianNoiseGenerator noise = new GaussianNoiseGenerator();
            noise.Mean = 0.0;
            noise.RandomSeed = false;
            Random rand = new Random(_seed);

            for(int r = 0; r < noisedMat.RowCount; ++r)
            {
                for(int c = 0; c < noisedMat.ColumnCount; ++c)
                {
                    noise.Seed = rand.Next();
                    noise.Variance = Math.Abs(noisedMat[r, c]) < 1e-12 ?
                        1e-12 : Math.Abs(noisedMat[r, c]) / 100.0;
                    noise.UpdateDistribution();

                    noisedMat[r, c] = noisedMat[r, c] + noise.GetSample();
                }
            }

            return noisedMat;
        }
    }
}
