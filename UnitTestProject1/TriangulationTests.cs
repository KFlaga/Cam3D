
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
        Matrix<double> _F;

        Matrix<double> _K_L;
        Matrix<double> _K_R;
        Matrix<double> _R_L;
        Matrix<double> _R_R;
        Vector<double> _C_L;
        Vector<double> _C_R;
        Matrix<double> _CM_L;
        Matrix<double> _CM_R;

        Vector<double> _epi_L;
        Vector<double> _epi_R;
        Matrix<double> _epiX_L;
        Matrix<double> _epiX_R;

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
            NormalisePoints();
            CreateEpiGeometry_Normalized();
            // 4) Using pairs of corresponding points find their 3D back-projection with Triangulation
            TwoPointsTriangulation trangulation = new TwoPointsTriangulation();
            trangulation.CameraLeft = _CM_L;
            trangulation.CameraRight = _CM_R;
            trangulation.EpipoleLeft = _epi_L;
            trangulation.EpipoleRight = _epi_R;
            trangulation.Fundamental = _F;

            trangulation._pL = _imagePointsLeft[0];
            trangulation._pR = _imagePointsRight[0];
         //   trangulation.ComputeBackprojected3DPoint();
            var point3d = trangulation._p3D;

            Assert.IsTrue((point3d - _realPoints[0]).L2Norm() < 1e-6);
        }

        [TestMethod]
        public void Test_Triangulation_NoNoise()
        {
            // 1) We have P, P', F, e, e'
            CreateCameraMatrices();
            CreateEpiGeometry();
            // 2) Create set of 3d points in sensible range
            // 3) Project them onto 2 images using P and P'
            GeneratePoints_Random(_seed);
            // 4) Using pairs of corresponding points find their 3D back-projection with Triangulation
            TwoPointsTriangulation trangulation = new TwoPointsTriangulation();
            trangulation.CameraLeft = _CM_L;
            trangulation.CameraRight = _CM_R;
            trangulation.EpipoleLeft = _epi_L;
            trangulation.EpipoleRight = _epi_R;
            trangulation.Fundamental = _F;
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
            _K_L = new DenseMatrix(3, 3);
            _K_L[0, 0] = 10.0; // fx
            _K_L[1, 1] = 10.0; // fy
            _K_L[0, 1] = 0.0; // s
            _K_L[0, 2] = 300.0; // x0
            _K_L[1, 2] = 250.0; // y0
            _K_L[2, 2] = 1.0; // 1

            _K_R = new DenseMatrix(3, 3);
            _K_R[0, 0] = 10.0; // fx
            _K_R[1, 1] = 10.5; // fy
            _K_R[0, 1] = 0.0; // s
            _K_R[0, 2] = 300.0; // x0
            _K_R[1, 2] = 200.0; // y0
            _K_R[2, 2] = 1.0; // 1

            _R_L = DenseMatrix.CreateIdentity(3);
            _R_R = DenseMatrix.CreateIdentity(3);

            _C_L = new DenseVector(3);
            _C_L[0] = 50.0;
            _C_L[1] = 50.0;
            _C_L[2] = 0.0;

            _C_R = new DenseVector(3);
            _C_R[0] = 0.0;
            _C_R[1] = 40.0;
            _C_R[2] = 10.0;

            Matrix<double> Ext_l = new DenseMatrix(3, 4);
            Ext_l.SetSubMatrix(0, 0, _R_L);
            Ext_l.SetColumn(3, -_R_L * _C_L);

            Matrix<double> Ext_r = new DenseMatrix(3, 4);
            Ext_r.SetSubMatrix(0, 0, _R_R);
            Ext_r.SetColumn(3, -_R_R * _C_R);

            _CM_L = _K_L * Ext_l;
            _CM_R = _K_R * Ext_r;
        }

        public void CreateEpiGeometry()
        {
            // Find e_R = P_R*C_L, e_L = P_L*C_R
            _epi_R = _CM_R * new DenseVector(new double[] { _C_L[0], _C_L[1], _C_L[2], 1.0 });
            _epi_L = _CM_L * new DenseVector(new double[] { _C_R[0], _C_R[1], _C_R[2], 1.0 });
            _epi_R.DivideThis(_epi_R[2]);
            _epi_L.DivideThis(_epi_L[2]);

            _epiX_L = new DenseMatrix(3, 3);
            _epiX_L[0, 0] = 0.0;
            _epiX_L[1, 0] = _epi_L[2];
            _epiX_L[2, 0] = -_epi_L[1];
            _epiX_L[0, 1] = -_epi_L[2];
            _epiX_L[1, 1] = 0.0;
            _epiX_L[2, 1] = _epi_L[0];
            _epiX_L[0, 2] = _epi_L[1];
            _epiX_L[1, 2] = -_epi_L[0];
            _epiX_L[2, 2] = 0.0;

            _epiX_R = new DenseMatrix(3, 3);
            _epiX_R[0, 0] = 0.0;
            _epiX_R[1, 0] = _epi_R[2];
            _epiX_R[2, 0] = -_epi_R[1];
            _epiX_R[0, 1] = -_epi_R[2];
            _epiX_R[1, 1] = 0.0;
            _epiX_R[2, 1] = _epi_R[0];
            _epiX_R[0, 2] = _epi_R[1];
            _epiX_R[1, 2] = -_epi_R[0];
            _epiX_R[2, 2] = 0.0;

            // F = [er]x * Pr * pseudoinv(Pl)
            _F = _epiX_R * (_CM_R * _CM_L.PseudoInverse());
            int rank = _F.Rank();
            if(rank == 3)
            {
                // Need to ensure rank 2, so set smallest singular value to 0
                var svd = _F.Svd();
                var E = svd.W;
                E[2, 2] = 0;
                var oldF = _F;
                _F = svd.U * E * svd.VT;
                var diff = _F - oldF; // Difference should be very small if all is correct
            }

            // Scale F, so that F33 = 1
            _F = _F.Divide(_F[2, 2]);
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
                var ip_L = _CM_L * rp;
                var ip_R = _CM_R * rp;

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
            _CMN_L = _Ni_L * _CM_L * _Nr.Inverse();
            _CMN_R = _Ni_R * _CM_R * _Nr.Inverse();
        }

        [TestMethod]
        public void Test_Triangulation_LinearFullNoise()
        {
            // 1) We have P, P', F, e, e'
            CreateCameraMatrices();
            GeneratePoints_Random(_seed);
            NormalisePoints();
            
            // 2) Create set of 3d points in sensible range
            // 3) Project them onto 2 images using P and P'
            
            var noisedLeft = AddNoise(_imgPointsNormLeft, 1e-12);
            var noisedRight = AddNoise(_imgPointsNormRight, 1e-12);

            //_CMN_L = AddNoise(_CMN_L);
            //_CMN_R = AddNoise(_CMN_R);
            CreateEpiGeometry_Normalized();

            // 4) Using pairs of corresponding points find their 3D back-projection with Triangulation
            TwoPointsTriangulation trangulation = new TwoPointsTriangulation();
            trangulation.CameraLeft = _CMN_L;
            trangulation.CameraRight = _CMN_R;
            trangulation.EpipoleLeft = _epi_L;
            trangulation.EpipoleRight = _epi_R;
            trangulation.Fundamental = _F;
            for(int i = 0; i < _pointsCount; ++i)
            {
                trangulation._pL = noisedLeft[i];
                trangulation._pR = noisedRight[i];
              //  trangulation.ComputeBackprojected3DPoint();
                var point3d = trangulation._p3D;
                var rpoint = _realPointsNorm[i];

                double error = (point3d - rpoint).L2Norm();
            }
       //     Assert.IsTrue(error < 1);
        }

        [TestMethod]
        public void Test_Triangulation_NoiseImageOnly()
        {
            // 1) We have P, P', F, e, e'
            CreateCameraMatrices();
            GeneratePoints_Random(_seed);
            NormalisePoints();

            CreateEpiGeometry_Normalized();
            // 2) Create set of 3d points in sensible range
            // 3) Project them onto 2 images using P and P'
            var noisedLeft = AddNoise(_imgPointsNormLeft, 1e-6);
            var noisedRight = AddNoise(_imgPointsNormRight, 1e-6);
            // 4) Using pairs of corresponding points find their 3D back-projection with Triangulation
            TwoPointsTriangulation trangulation = new TwoPointsTriangulation();
            trangulation.CameraLeft = _CMN_L;
            trangulation.CameraRight = _CMN_R;
            trangulation.EpipoleLeft = _epi_L;
            trangulation.EpipoleRight = _epi_R;
            trangulation.Fundamental = _F;
            trangulation.PointsLeft = noisedLeft;
            trangulation.PointsRight = noisedRight;

            trangulation.Estimate3DPoints();
            var ePoints3D = trangulation.Points3D;

            for(int i = 0; i < _pointsCount; ++i)
            {
                trangulation._pL = noisedLeft[i];
                trangulation._pR = noisedRight[i];
               // trangulation.ComputeBackprojected3DPoint();
                var point3d = trangulation._p3D; // To compare with linear
                var realPoint = _realPointsNorm[i];
                var ep3d = ePoints3D[i];
                var elin = (point3d - realPoint);

                var errVec = ePoints3D[i].PointwiseDivide_NoNaN(_realPointsNorm[i]);
                double err = (errVec - DenseVector.Create(4, 1.0)).L2Norm();
                double err2 = (ePoints3D[i] - _realPointsNorm[i]).L2Norm();
                double errlin = (point3d - realPoint).L2Norm();
                //   Assert.IsTrue(
                //       err < 0.2 || // max 2% diffrence
                //       (ePoints3D[i] - _realPoints[i]).L2Norm() < 10);
            }
        }


        [TestMethod]
        public void Test_Triangulation_EpilineFitCost()
        {
            CreateCameraMatrices();
            GeneratePoints_Random(_seed);
            NormalisePoints();
            CreateEpiGeometry_Normalized();
            
            TwoPointsTriangulation trangulation = new TwoPointsTriangulation();
            trangulation.CameraLeft = _CMN_L;
            trangulation.CameraRight = _CMN_R;
            trangulation.EpipoleLeft = _epi_L;
            trangulation.EpipoleRight = _epi_R;
            trangulation.Fundamental = _F;

          //  trangulation.ComputeEpilineFitCost(10.0);
        }

        public void CreateEpiGeometry_Normalized()
        {
            var ChL = new DenseVector(4);
            _C_L.CopySubVectorTo(ChL, 0, 0, 3);
            ChL[3] = 1.0;
            var ChR = new DenseVector(4);
            _C_R.CopySubVectorTo(ChR, 0, 0, 3);
            ChR[3] = 1.0;

            // Find e_R = P_R*C_L, e_L = P_L*C_R
            _epi_R = _CMN_R * (_Nr * ChL);
            _epi_L = _CMN_L * (_Nr * ChR);
            _epi_R.DivideThis(_epi_R[2]);
            _epi_L.DivideThis(_epi_L[2]);

            _epiX_L = new DenseMatrix(3, 3);
            _epiX_L[0, 0] = 0.0;
            _epiX_L[1, 0] = _epi_L[2];
            _epiX_L[2, 0] = -_epi_L[1];
            _epiX_L[0, 1] = -_epi_L[2];
            _epiX_L[1, 1] = 0.0;
            _epiX_L[2, 1] = _epi_L[0];
            _epiX_L[0, 2] = _epi_L[1];
            _epiX_L[1, 2] = -_epi_L[0];
            _epiX_L[2, 2] = 0.0;

            _epiX_R = new DenseMatrix(3, 3);
            _epiX_R[0, 0] = 0.0;
            _epiX_R[1, 0] = _epi_R[2];
            _epiX_R[2, 0] = -_epi_R[1];
            _epiX_R[0, 1] = -_epi_R[2];
            _epiX_R[1, 1] = 0.0;
            _epiX_R[2, 1] = _epi_R[0];
            _epiX_R[0, 2] = _epi_R[1];
            _epiX_R[1, 2] = -_epi_R[0];
            _epiX_R[2, 2] = 0.0;

            // F = [er]x * Pr * pseudoinv(Pl)
            _F = _epiX_R * (_CMN_R * _CMN_L.PseudoInverse());
            int rank = _F.Rank();
            if(rank == 3)
            {
                // Need to ensure rank 2, so set smallest singular value to 0
                var svd = _F.Svd();
                var E = svd.W;
                E[2, 2] = 0;
                var oldF = _F;
                _F = svd.U * E * svd.VT;
                var diff = _F - oldF; // Difference should be very small if all is correct
            }

            // Scale F, so that F33 = 1
            _F = _F.Divide(_F[2, 2]);
        }
    }
}

