using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CamCore;
using CalibrationModule;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace UnitTestProject1
{
    [TestClass]
    public class CameraMatrixComputationTests
    {
        Matrix<double> _CM;
        Matrix<double> _eCM;
        CamCalibrator _calib;

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
            _calib = new CamCalibrator();
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

            _calib.HomoPoints();

            _calib.NormalizeImagePoints();
            _calib.NormalizeRealPoints();

            _calib.CameraMatrix = _calib.FindLinearEstimationOfCameraMatrix();

            _calib.DenormaliseCameraMatrix();
            _calib.DecomposeCameraMatrix();
            _eCM = _calib.CameraMatrix;

            double scaleK = -1.0 / _calib.CameraInternalMatrix[2, 2];
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

        [TestMethod]
        public void Test_EstimateCameraMatrix_NoisedLinear()
        {
            PrepareCameraMatrix();
            var points = GenerateCalibrationPoints_Random();
            PrepareCalibrator(
                AddNoise(points, _varianceReal, _varianceImage));

            _calib.HomoPoints();

            _calib.NormalizeImagePoints();
            _calib.NormalizeRealPoints();

            _calib.CameraMatrix = _calib.FindLinearEstimationOfCameraMatrix();

            _calib.DenormaliseCameraMatrix();
            _calib.DecomposeCameraMatrix();
            _eCM = _calib.CameraMatrix;

            double scaleK = 1.0 / _calib.CameraInternalMatrix[2, 2];
            _eCM.MultiplyThis(-scaleK);

            var eK = _calib.CameraInternalMatrix.Multiply(scaleK);
            var eR = -_calib.CameraRotationMatrix;
            var eC = -(_eCM.SubMatrix(0, 3, 0, 3).Inverse() * _eCM.Column(3));

            Matrix<double> eExt = new DenseMatrix(3, 4);
            eExt.SetSubMatrix(0, 0, eR);
            eExt.SetColumn(3, -eR * eC);

            var eCM = eK * eExt;

            var errVec = _CM.PointwiseDivide_NoNaN(_eCM);
            double err = errVec.L2Norm();
            Assert.IsTrue(
                Math.Abs(err - Math.Sqrt(12)) < Math.Sqrt(12) / 50.0 || // max 2% diffrence
                (_eCM - _CM).FrobeniusNorm() < 1e-3);

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
                Assert.IsTrue((ip - cp.Img).Length() < cp.Img.Length() / 10.0);
            }
        }

        [TestMethod]
        public void Test_EstimateCameraMatrix_Minimised()
        {
            PrepareCameraMatrix();
            var points = GenerateCalibrationPoints_Random(100);
            var noisedPoints = AddNoise(points, _varianceReal, _varianceImage, 200);
            PrepareCalibrator(noisedPoints);

            _calib.HomoPoints();
            _calib.NormalizeImagePoints();
            _calib.NormalizeRealPoints();
            _calib.CameraMatrix = _calib.FindLinearEstimationOfCameraMatrix();
            //    _calib.FindNormalisedVariances();
            _calib.DenormaliseCameraMatrix();

            _eCM = _calib.CameraMatrix;
            double totalDiff = 0.0;
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
                totalDiff += (ip - cp.Img).Length();
                Assert.IsTrue((ip - cp.Img).Length() < 1.0,
                    "Point after linear estimation too far : " + (ip - cp.Img).Length());
            }

            _calib.HomoPoints();
            // _calib.NormalizeImagePoints();
            // _calib.NormalizeRealPoints();
            _calib.CameraMatrix = _calib.FindLinearEstimationOfCameraMatrix();
            _calib.FindNormalisedVariances();
            var lecm = _eCM.Clone();

            _calib._geometricErrorMinimalisationAlg.DoComputeJacobianNumerically = true;
            _calib._geometricErrorMinimalisationAlg.NumericalDerivativeStep = 1e-4;
            _calib.MinimizeError();

            // _calib.DenormaliseCameraMatrix();
            _calib.DecomposeCameraMatrix();
            _eCM = _calib.CameraMatrix;

            var errVec = _eCM.PointwiseDivide_NoNaN(lecm);
            double err = errVec.L2Norm();

            double scaleK = 1.0 / _calib.CameraInternalMatrix[2, 2];
            _eCM.MultiplyThis(-scaleK);

            var eK = _calib.CameraInternalMatrix.Multiply(scaleK);
            var eR = -_calib.CameraRotationMatrix;
            var eC = -(_eCM.SubMatrix(0, 3, 0, 3).Inverse() * _eCM.Column(3));

            Matrix<double> eExt = new DenseMatrix(3, 4);
            eExt.SetSubMatrix(0, 0, eR);
            eExt.SetColumn(3, -eR * eC);

            var eCM = eK * eExt;

            // var errVec = _CM.PointwiseDivide_NoNaN(_eCM);
            // double err = errVec.L2Norm();
            // Assert.IsTrue(
            //    Math.Abs(err - Math.Sqrt(12)) < Math.Sqrt(12) / 1000.0 || // max 0.1% diffrence
            //    (_eCM - _CM).FrobeniusNorm() < 1e-3);

            double estDiff = 0;
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
                estDiff += (ip - cp.Img).Length();
                Assert.IsTrue((ip - cp.Img).Length() < 1.5,
                        "Point after error minimalisation too far : " + (ip - cp.Img).Length());
            }

            var minialg = _calib._geometricErrorMinimalisationAlg;
            // Test conovergence :
            // ||mX-rX|| = ||mX-eX|| + ||rX-eX|| (or squared??)
            // rX - real point from 'points'
            // mX - measured point, noised
            // eX = estimated X from result vector for 3d points and ePeX for image point
            double len2_mr = 0;
            double len2_me = 0;
            double len2_re = 0;
            for(int i = 0; i < points.Count; ++i)
            {
                double rX = points[i].RealX;
                double rY = points[i].RealY;
                double rZ = points[i].RealZ;
                double rx = points[i].ImgX;
                double ry = points[i].ImgY;

                double mX = noisedPoints[i].RealX;
                double mY = noisedPoints[i].RealY;
                double mZ = noisedPoints[i].RealZ;
                double mx = noisedPoints[i].ImgX;
                double my = noisedPoints[i].ImgY;

                double eX = minialg.BestResultVector[3 * i + 12];
                double eY = minialg.BestResultVector[3 * i + 13];
                double eZ = minialg.BestResultVector[3 * i + 14];

                Vector<double> rp = new DenseVector(4);
                rp[0] = eX;
                rp[1] = eY;
                rp[2] = eZ;
                rp[3] = 1.0;
                var imagePoint = _eCM * rp;
                double ex = imagePoint[0] / imagePoint[2];
                double ey = imagePoint[1] / imagePoint[2];

                len2_re += (rX - eX) * (rX - eX);
                len2_re += (rY - eY) * (rY - eY);
                len2_re += (rZ - eZ) * (rZ - eZ);
                len2_re += (rx - ex) * (rx - ex);
                len2_re += (ry - ey) * (ry - ey);

                len2_me += (mX - eX) * (mX - eX);
                len2_me += (mY - eY) * (mY - eY);
                len2_me += (mZ - eZ) * (mZ - eZ);
                len2_me += (mx - ex) * (mx - ex);
                len2_me += (my - ey) * (my - ey);

                len2_mr += (rX - mX) * (rX - mX);
                len2_mr += (rY - mY) * (rY - mY);
                len2_mr += (rZ - mZ) * (rZ - mZ);
                len2_mr += (rx - mx) * (rx - mx);
                len2_mr += (ry - my) * (ry - my);
            }

            Assert.IsTrue( Math.Abs(len2_mr - len2_re - len2_me) < len2_mr/100.0 ||
                 Math.Abs(Math.Sqrt(len2_mr) - Math.Sqrt(len2_re) - Math.Sqrt(len2_me)) < Math.Sqrt(len2_mr) / 100.0,
                "Triangle test failed");

            Assert.IsTrue(estDiff < totalDiff,
                 "Points after minimalisation are too far. LinearDiff = " + totalDiff +
                 ". MiniDiff = " + estDiff);
        }


        [TestMethod]
        public void Test_CameraMatrix_Jacobian()
        {
            PrepareCameraMatrix();
            var points = GenerateCalibrationPoints_Random();
            PrepareCalibrator(
                AddNoise(points, _varianceReal, _varianceImage));

            _calib.HomoPoints();
            //_calib.NormalizeImagePoints();
            // _calib.NormalizeRealPoints();
            _calib.CameraMatrix = _calib.FindLinearEstimationOfCameraMatrix();
            // _calib.FindNormalisedVariances();

            _eCM = _calib.CameraMatrix;
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
                Assert.IsTrue((ip - cp.Img).Length() < 0.4);
            }

            _calib.DecomposeCameraMatrix();

            var miniAlg = _calib._geometricErrorMinimalisationAlg;
            _calib.PrepareMinimalisationAlg();
            miniAlg.Init();

            miniAlg.DoComputeJacobianNumerically = false;
            Matrix<double> testedJacobian = new DenseMatrix(miniAlg.MeasurementsVector.Count, miniAlg.ParametersVector.Count);
            miniAlg.ComputeJacobian(testedJacobian);

            miniAlg.DoComputeJacobianNumerically = true;
            Matrix<double> numericJacobian = new DenseMatrix(miniAlg.MeasurementsVector.Count, miniAlg.ParametersVector.Count);
            miniAlg.ComputeJacobian(numericJacobian);

            int size = testedJacobian.RowCount * testedJacobian.ColumnCount;
            double jacobian_diff = numericJacobian.PointwiseDivide_NoNaN(testedJacobian).FrobeniusNorm();
            Assert.IsTrue(Math.Abs(jacobian_diff - Math.Sqrt(size)) < Math.Sqrt(size) / 100.0 || // 1% diffrence max
                (numericJacobian - testedJacobian).FrobeniusNorm() < 1e-6,
                "Analitical and numeric jacobians differ");
        }

        int _pointsCount = 100;

        double _rangeReal_MinX = 0.0;
        double _rangeReal_MaxX = 100.0;
        double _rangeReal_MinY = 0.0;
        double _rangeReal_MaxY = 100.0;
        double _rangeReal_MinZ = 50.0;
        double _rangeReal_MaxZ = 100.0;

        double _rangeImage_MinY = -5.0;
        double _rangeImage_MaxY = 5.0;
        double _rangeImage_MinX = -5.0;
        double _rangeImage_MaxX = 5.0;

        double _varianceReal = 1.5;
        double _varianceImage = 0.1;

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
                cpoint.Real = new Point3D(rx, ry, rz);

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
    }
}
