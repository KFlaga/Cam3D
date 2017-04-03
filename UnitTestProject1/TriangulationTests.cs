
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CamCore;
using CamImageProcessing;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using CalibrationModule;

namespace UnitTestProject1
{
    [TestClass]
    public class TriangulationTests
    {
        CalibrationData cData = new CalibrationData();
        List<Vector<double>> _imagePointsLeft;
        List<Vector<double>> _imagePointsRight;
        List<Vector<double>> _realPoints;

        int _seed = 100;

        [TestMethod]
        public void Test_Triangulation_LinearNoNoise()
        {
            // 1) We have P, P', F, e, e'
            // 2) Create set of 3d points in sensible range
            // 3) Project them onto 2 images using P and P'
            CreateCameraMatrices();
            GeneratePoints_Random(_seed);
            //NormalisePoints();
            // 4) Using pairs of corresponding points find their 3D back-projection with Triangulation
            TwoPointsTriangulation trangulation = new TwoPointsTriangulation();
            trangulation.CalibData = cData;

            trangulation.PointsLeft = _imagePointsLeft;
            trangulation.PointsRight = _imagePointsRight;
            trangulation.UseLinearEstimationOnly = true;
            trangulation.Estimate3DPoints();
            var point3d = trangulation.Points3D[0];

            Assert.IsTrue((point3d - _realPoints[0]).L2Norm() < 1e-6);
        }

        [TestMethod]
        public void Test_Triangulation_LinearNoNoiseNormalised()
        {
            // 1) We have P, P', F, e, e'
            // 2) Create set of 3d points in sensible range
            // 3) Project them onto 2 images using P and P'
            CreateCameraMatrices();
            GeneratePoints_Random(_seed);
            NormalisePoints();
            // 4) Using pairs of corresponding points find their 3D back-projection with Triangulation
            TwoPointsTriangulation trangulation = new TwoPointsTriangulation();
            trangulation.CalibData = cData;

            trangulation.PointsLeft = _imgPointsNormLeft;
            trangulation.PointsRight = _imgPointsNormRight;
            trangulation.UseLinearEstimationOnly = true;
            trangulation.Estimate3DPoints();
            var point3d = trangulation.Points3D[0];

            Assert.IsTrue((point3d - _realPointsNorm[0]).L2Norm() < 1e-6);
        }

        [TestMethod]
        public void Test_Triangulation_NoNoise()
        {
            // 1) We have P, P', F, e, e'
            CreateCameraMatrices();
            // 2) Create set of 3d points in sensible range
            // 3) Project them onto 2 images using P and P'
            GeneratePoints_Random(_seed);
            // 4) Using pairs of corresponding points find their 3D back-projection with Triangulation
            TwoPointsTriangulation trangulation = new TwoPointsTriangulation();
            trangulation.CalibData = cData;
            trangulation.PointsLeft = _imagePointsLeft;
            trangulation.PointsRight = _imagePointsRight;

            trangulation.Estimate3DPoints();
            var ePoints3D = trangulation.Points3D;

            for(int i = 0; i < _pointsCount; ++i)
            {
                var errVec = ePoints3D[i].PointwiseDivide_NoNaN(_realPoints[i]);
                double err = (errVec - DenseVector.Create(4, 1.0)).L2Norm();
                Assert.IsTrue(
                    err < 0.2 || // max 2% diffrence
                    (ePoints3D[i] - _realPoints[i]).L2Norm() < 10);
            }

            // 5) Repeat adding some noise to image coords (matching error)
            // 6) Repeat adding some noise to camera matrix (calibration error)
            // 7) Repeat adding some noise to camera matrix and matched image coords (theoreticaly this is case close to real one)
        }

        int _pointsCount = 100;

        double _rangeReal_MinX = 1.0;
        double _rangeReal_MaxX = 100.0;
        double _rangeReal_MinY = 1.0;
        double _rangeReal_MaxY = 100.0;
        double _rangeReal_MinZ = 50.0;
        double _rangeReal_MaxZ = 100.0;

        double _varianceReal = 1.5;
        double _varianceImage = 1.0;

        public void CreateCameraMatrices()
        {
            var _K_L = new DenseMatrix(3, 3);
            _K_L[0, 0] = 10.0; // fx
            _K_L[1, 1] = 10.0; // fy
            _K_L[0, 1] = 0.0; // s
            _K_L[0, 2] = 300.0; // x0
            _K_L[1, 2] = 250.0; // y0
            _K_L[2, 2] = 1.0; // 1

            var _K_R = new DenseMatrix(3, 3);
            _K_R[0, 0] = 10.0; // fx
            _K_R[1, 1] = 10.5; // fy
            _K_R[0, 1] = 0.0; // s
            _K_R[0, 2] = 300.0; // x0
            _K_R[1, 2] = 200.0; // y0
            _K_R[2, 2] = 1.0; // 1

            var _R_L = DenseMatrix.CreateIdentity(3);
            var _R_R = DenseMatrix.CreateIdentity(3);

            var _C_L = new DenseVector(3);
            _C_L[0] = 50.0;
            _C_L[1] = 50.0;
            _C_L[2] = 0.0;

            var _C_R = new DenseVector(3);
            _C_R[0] = 0.0;
            _C_R[1] = 40.0;
            _C_R[2] = 10.0;

            Matrix<double> Ext_l = new DenseMatrix(3, 4);
            Ext_l.SetSubMatrix(0, 0, _R_L);
            Ext_l.SetColumn(3, -_R_L * _C_L);

            Matrix<double> Ext_r = new DenseMatrix(3, 4);
            Ext_r.SetSubMatrix(0, 0, _R_R);
            Ext_r.SetColumn(3, -_R_R * _C_R);

            cData.CameraLeft = _K_L * Ext_l;
            cData.CameraRight = _K_R * Ext_r;
        }

        public void GeneratePoints_Random(int seed = 0)
        {
            _imagePointsLeft = new List<Vector<double>>();
            _imagePointsRight = new List<Vector<double>>();
            _realPoints = new List<Vector<double>>();

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

                Vector<double> rp = new DenseVector(4);
                rp[0] = rx;
                rp[1] = ry;
                rp[2] = rz;
                rp[3] = 1.0;
                var ip_L = cData.CameraLeft * rp;
                var ip_R = cData.CameraRight * rp;

                ip_L.DivideThis(ip_L[2]);
                ip_R.DivideThis(ip_R[2]);

                _imagePointsLeft.Add(ip_L);
                _imagePointsRight.Add(ip_R);
                _realPoints.Add(rp);
            }
        }

        public List<Vector<double>> AddNoise(List<Vector<double>> points,
            double variance, int seed = 0)
        {
            List<Vector<double>> noisedPoints = new List<Vector<double>>(points.Count);
            int pointSize = points[0].Count;

            GaussianNoiseGenerator noise = new GaussianNoiseGenerator();
            noise.Variance = variance;
            noise.Mean = 0.0;
            noise.RandomSeed = seed != 0;
            noise.Seed = seed;
            noise.UpdateDistribution();

            for(int i = 0; i < _pointsCount; ++i)
            {
                Vector<double> cpoint = new DenseVector(pointSize);
                for(int p = 0; p < pointSize - 1; ++p)
                {
                    cpoint[p] = points[i][p] + noise.GetSample();
                }
                cpoint[pointSize - 1] = 1.0f;

                noisedPoints.Add(cpoint);
            }

            return noisedPoints;
        }

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

        Matrix<double> _Nr;
        Matrix<double> _Ni_L;
        Matrix<double> _Ni_R;
        List<Vector<double>> _realPointsNorm;
        List<Vector<double>> _imgPointsNormLeft;
        List<Vector<double>> _imgPointsNormRight;
        Matrix<double> _CMN_L;
        Matrix<double> _CMN_R;

        public void NormalisePoints()
        {
            _Nr = new DenseMatrix(4, 4);
            // Compute center of real grid
            double xc = 0, yc = 0, zc = 0;
            foreach(var point in _realPoints)
            {
                xc += point[0];
                yc += point[1];
                zc += point[2];
            }
            xc /= _realPoints.Count;
            yc /= _realPoints.Count;
            zc /= _realPoints.Count;
            // Get mean distance of points from center
            double dist = 0;
            foreach(var point in _realPoints)
            {
                dist += (double)Math.Sqrt((point[0] - xc) * (point[0] - xc) +
                    (point[1] - yc) * (point[1] - yc) + (point[2] - zc) * (point[2] - zc));
            }
            dist /= _realPoints.Count;
            // Normalize in a way that mean dist = sqrt(3)
            double ratio = (double)Math.Sqrt(3) / dist;
            // Noramlize matrix - homonogeus point must be multiplied by it
            _Nr[0, 0] = ratio;
            _Nr[1, 1] = ratio;
            _Nr[2, 2] = ratio;
            _Nr[0, 3] = -ratio * xc;
            _Nr[1, 3] = -ratio * yc;
            _Nr[2, 3] = -ratio * zc;
            _Nr[3, 3] = 1;
            // Normalize points
            _realPointsNorm = new List<Vector<double>>();
            for(int i = 0; i < _realPoints.Count; ++i)
            {
                _realPointsNorm.Add(_Nr * _realPoints[i]);
            }

            _Ni_L = new DenseMatrix(3, 3);
            // Compute center of real grid
            xc = 0; yc = 0; zc = 0;
            foreach(var point in _imagePointsLeft)
            {
                xc += point[0];
                yc += point[1];
            }
            xc /= _imagePointsLeft.Count;
            yc /= _imagePointsLeft.Count;
            // Get mean distance of points from center
            dist = 0;
            foreach(var point in _imagePointsLeft)
            {
                dist += (double)Math.Sqrt((point[0] - xc) * (point[0] - xc) +
                    (point[1] - yc) * (point[1] - yc));
            }
            dist /= _imagePointsLeft.Count;
            // Normalize in a way that mean dist = sqrt(3)
            ratio = (double)Math.Sqrt(2) / dist;
            // Noramlize matrix - homonogeus point must be multiplied by it
            _Ni_L[0, 0] = ratio;
            _Ni_L[1, 1] = ratio;
            _Ni_L[0, 2] = -ratio * xc;
            _Ni_L[1, 2] = -ratio * yc;
            _Ni_L[2, 2] = 1;

            _imgPointsNormLeft = new List<Vector<double>>();
            for(int i = 0; i < _imagePointsLeft.Count; ++i)
            {
                _imgPointsNormLeft.Add(_Ni_L * _imagePointsLeft[i]);
            }

            _Ni_R = new DenseMatrix(3, 3);
            // Compute center of real grid
            xc = 0; yc = 0; zc = 0;
            foreach(var point in _imagePointsLeft)
            {
                xc += point[0];
                yc += point[1];
            }
            xc /= _imagePointsRight.Count;
            yc /= _imagePointsRight.Count;
            // Get mean distance of points from center
            dist = 0;
            foreach(var point in _imagePointsRight)
            {
                dist += (double)Math.Sqrt((point[0] - xc) * (point[0] - xc) +
                    (point[1] - yc) * (point[1] - yc));
            }
            dist /= _imagePointsRight.Count;
            // Normalize in a way that mean dist = sqrt(3)
            ratio = (double)Math.Sqrt(2) / dist;
            // Noramlize matrix - homonogeus point must be multiplied by it
            _Ni_R[0, 0] = ratio;
            _Ni_R[1, 1] = ratio;
            _Ni_R[0, 2] = -ratio * xc;
            _Ni_R[1, 2] = -ratio * yc;
            _Ni_R[2, 2] = 1;

            _imgPointsNormRight = new List<Vector<double>>();
            for(int i = 0; i < _imagePointsRight.Count; ++i)
            {
                _imgPointsNormRight.Add(_Ni_R * _imagePointsRight[i]);
            }

            // Pn = Ni * P * Nr^-1
            cData.CameraLeft = _Ni_L * cData.CameraLeft * _Nr.Inverse();
            cData.CameraRight = _Ni_R * cData.CameraRight * _Nr.Inverse();
        }

        [TestMethod]
        public void Test_Triangulation_LinearFullNoise()
        {
            CreateCameraMatrices();
            GeneratePoints_Random(_seed);
            NormalisePoints();

            var noisedLeft = AddNoise(_imgPointsNormLeft, 1e-12);
            var noisedRight = AddNoise(_imgPointsNormRight, 1e-12);

            cData.CameraLeft = AddNoise(cData.CameraLeft);
            cData.CameraRight = AddNoise(cData.CameraRight);

            TwoPointsTriangulation trangulation = new TwoPointsTriangulation();
            trangulation.CalibData = cData;

            trangulation.PointsLeft = noisedLeft;
            trangulation.PointsRight = noisedRight;
            trangulation.UseLinearEstimationOnly = false;
            trangulation.Estimate3DPoints();

            double error = 0;
            for(int i = 0; i < _pointsCount; ++i)
            {
                var point3d = trangulation.Points3D[i];
                var rpoint = _realPointsNorm[i];

                double e = (point3d - rpoint).L2Norm();
                error += e;
            }
            Assert.IsTrue(error < 1);
        }

        [TestMethod]
        public void Test_Triangulation_NoiseImageOnly()
        {
            CreateCameraMatrices();
            GeneratePoints_Random(_seed);
            NormalisePoints();

            var noisedLeft = AddNoise(_imgPointsNormLeft, 1e-12);
            var noisedRight = AddNoise(_imgPointsNormRight, 1e-12);

            TwoPointsTriangulation trangulation = new TwoPointsTriangulation();
            trangulation.CalibData = cData;

            trangulation.PointsLeft = noisedLeft;
            trangulation.PointsRight = noisedRight;
            trangulation.UseLinearEstimationOnly = false;
            trangulation.Estimate3DPoints();

            double error = 0;
            for(int i = 0; i < _pointsCount; ++i)
            {
                var point3d = trangulation.Points3D[i];
                var rpoint = _realPointsNorm[i];

                double e = (point3d - rpoint).L2Norm();
                error += e;
            }
            Assert.IsTrue(error < 1);
        }


        [TestMethod]
        public void Test_Triangulation_EpilineFitCost()
        {
            CreateCameraMatrices();
            GeneratePoints_Random(_seed);
            NormalisePoints();

            TwoPointsTriangulation trangulation = new TwoPointsTriangulation();
            trangulation.CalibData = cData;

            //  trangulation.ComputeEpilineFitCost(10.0);
        }
    }
}

