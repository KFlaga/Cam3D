using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CamCore;
using CamAlgorithms;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using CalibrationModule;
using CamAlgorithms.Calibration;
using CamAlgorithms.Triangulation;
using System.Text;

namespace CamUnitTest
{
    [TestClass]
    public class TriangulationUnitTests
    {
        CameraPair _cameras = new CameraPair();
        List<Vector<double>> _imagePointsLeft;
        List<Vector<double>> _imagePointsRight;
        List<Vector<double>> _realPoints;
        
        Matrix<double> _normReal;
        Matrix<double> _normImageLeft;
        Matrix<double> _normImageRight;
        List<Vector<double>> _realPointsNormalized;
        List<Vector<double>> _imgPointsLeftNormalized;
        List<Vector<double>> _imgPointsRightNormalized;

        int _seed = 100;
        int _pointsCount = 100;

        double _rangeReal_MinX = 1.0;
        double _rangeReal_MaxX = 100.0;
        double _rangeReal_MinY = 1.0;
        double _rangeReal_MaxY = 100.0;
        double _rangeReal_MinZ = 50.0;
        double _rangeReal_MaxZ = 100.0;

        List<Vector<double>> EstimateRealPoints(TwoPointsTriangulation triangulation, 
            List<Vector<double>> imgLeft, List<Vector<double>> imgRight)
        {
            triangulation.Cameras = _cameras;
            triangulation.PointsLeft = imgLeft;
            triangulation.PointsRight = imgRight;
            triangulation.Estimate3DPoints();
            return triangulation.Points3D;
        }

        [TestInitialize()]
        public void Initialize()
        {
            CreateCameras();
            GenerateRandomPoints(_seed);
        }

        [TestMethod]
        public void Test_Triangulation_LinearNoNoise()
        {
            var points = EstimateRealPoints(new TwoPointsTriangulation()
            {
                UseLinearEstimationOnly = true
            }, _imagePointsLeft, _imagePointsRight);
            TestUtils.AssertEquals(points, _realPoints, "Estimated");
        }

        [TestMethod]
        public void Test_Triangulation_LinearNoNoiseNormalized()
        {
            NormalizePointsAndCameras();
            var points = EstimateRealPoints(new TwoPointsTriangulation()
            {
                UseLinearEstimationOnly = true
            }, _imgPointsLeftNormalized, _imgPointsRightNormalized);
            TestUtils.AssertEquals(points, _realPointsNormalized, "Estimated");
        }

        [TestMethod]
        public void Test_Triangulation_NoNoise()
        {
            var points = EstimateRealPoints(new TwoPointsTriangulation()
            {
                UseLinearEstimationOnly = false
            }, _imagePointsLeft, _imagePointsRight);
            TestUtils.AssertEquals(points, _realPoints, "Estimated");
        }

        [TestMethod]
        public void Test_Triangulation_NoNoiseNormalized()
        {
            NormalizePointsAndCameras();
            var points = EstimateRealPoints(new TwoPointsTriangulation()
            {
                UseLinearEstimationOnly = false
            }, _imgPointsLeftNormalized, _imgPointsRightNormalized);
            TestUtils.AssertEquals(points, _realPointsNormalized, "Estimated");
        }

        [TestMethod]
        public void Test_Triangulation_LinearFullNoise()
        {
            NormalizePointsAndCameras();

            _seed = new Random().Next();
            var noisedLeft = TestUtils.AddNoise(_imgPointsLeftNormalized, 1e-12, _seed);
            var noisedRight = TestUtils.AddNoise(_imgPointsRightNormalized, 1e-12, _seed);

            _cameras.Left.Matrix = TestUtils.AddNoise(_cameras.Left.Matrix, 0.001, _seed);
            _cameras.Right.Matrix = TestUtils.AddNoise(_cameras.Right.Matrix, 0.001, _seed);
            _cameras.Update();

            var points = EstimateRealPoints(new TwoPointsTriangulation()
            {
                UseLinearEstimationOnly = true
            }, noisedLeft, noisedRight);
            TestUtils.AssertEquals(points, _realPointsNormalized, "Estimated", maxRelativeError: 0.2);
        }

        [TestMethod]
        public void Test_Triangulation_NoiseImageOnly()
        {
            NormalizePointsAndCameras();

            var noisedLeft = TestUtils.AddNoise(_imgPointsLeftNormalized, 1e-12, _seed);
            var noisedRight = TestUtils.AddNoise(_imgPointsRightNormalized, 1e-12, _seed);

            var points = EstimateRealPoints(new TwoPointsTriangulation()
            {
                UseLinearEstimationOnly = false
            }, noisedLeft, noisedRight);
            TestUtils.AssertEquals(points, _realPointsNormalized, "Estimated", maxRelativeError: 0.01);
        }

        [TestMethod]
        public void Test_Triangulation_FullNoise()
        {
            NormalizePointsAndCameras();

            _seed = new Random().Next();
            var noisedLeft = TestUtils.AddNoise(_imgPointsLeftNormalized, 1e-12, _seed);
            var noisedRight = TestUtils.AddNoise(_imgPointsRightNormalized, 1e-12, _seed);

            _cameras.Left.Matrix = TestUtils.AddNoise(_cameras.Left.Matrix, 0.01, _seed);
            _cameras.Right.Matrix = TestUtils.AddNoise(_cameras.Right.Matrix, 0.01, _seed);
            _cameras.Update();

            var points = EstimateRealPoints(new TwoPointsTriangulation()
            {
                UseLinearEstimationOnly = false
            }, noisedLeft, noisedRight);
            TestUtils.AssertEquals(points, _realPointsNormalized, "Estimated", maxRelativeError: 0.02);
        }

        public void CreateCameras()
        {
            var K_L = DenseMatrix.OfRowArrays(new double[][]
            {
                new double[] { 500.0, 0.0, 300.0 },
                new double[] { 0.0, 500.0, 250.0 },
                new double[] { 0.0,  0.0,   1.0 }
            });
            var K_R = DenseMatrix.OfRowArrays(new double[][]
            {
                new double[] { 500.0, 0.0, 300.0 },
                new double[] { 0.0, 520.0, 200.0 },
                new double[] { 0.0,  0.0,   1.0 }
            });
            var R_L = DenseMatrix.CreateIdentity(3);
            var R_R = DenseMatrix.CreateIdentity(3);
            var C_L = new DenseVector(new double[] { 50.0, 50.0,  0.0 });
            var C_R = new DenseVector(new double[] {  0.0, 40.0, 10.0 });

            _cameras = TestUtils.CreateTestCamerasFromMatrices(K_L, K_R, R_L, R_R, C_L, C_R);
        }

        public void GenerateRandomPoints(int seed = 0)
        {
            _imagePointsLeft = new List<Vector<double>>();
            _imagePointsRight = new List<Vector<double>>();
            _realPoints = new List<Vector<double>>();

            Random rand;
            if(seed == 0) { rand = new Random(); }
            else { rand = new Random(seed); }        

            for(int i = 0; i < _pointsCount; ++i)
            {
                double rx = rand.NextDouble() * (_rangeReal_MaxX - _rangeReal_MinX) + _rangeReal_MinX;
                double ry = rand.NextDouble() * (_rangeReal_MaxY - _rangeReal_MinY) + _rangeReal_MinY;
                double rz = rand.NextDouble() * (_rangeReal_MaxZ - _rangeReal_MinZ) + _rangeReal_MinZ;

                Vector<double> rp = new DenseVector(new double[] { rx, ry, rx, 1.0 });
                var ip_L = _cameras.Left.Matrix * rp;
                var ip_R = _cameras.Right.Matrix * rp;

                ip_L.DivideThis(ip_L[2]);
                ip_R.DivideThis(ip_R[2]);

                _imagePointsLeft.Add(ip_L);
                _imagePointsRight.Add(ip_R);
                _realPoints.Add(rp);
            }
        }

        public void NormalizePointsAndCameras()
        {
            _normReal = PointNormalization.FindNormalizationMatrix3d(_realPoints);
            _realPointsNormalized = PointNormalization.NormalizePoints(_realPoints, _normReal);
            _normImageLeft = PointNormalization.FindNormalizationMatrix2d(_realPoints);
            _imgPointsLeftNormalized = PointNormalization.NormalizePoints(_imagePointsLeft, _normImageLeft);
            _normImageRight = PointNormalization.FindNormalizationMatrix2d(_realPoints);
            _imgPointsRightNormalized = PointNormalization.NormalizePoints(_imagePointsRight, _normImageRight);
            
            // Pn = Ni * P * Nr^-1
            _cameras.Left.Matrix = _normImageLeft * _cameras.Left.Matrix * _normReal.Inverse();
            _cameras.Right.Matrix = _normImageRight * _cameras.Right.Matrix * _normReal.Inverse();
            _cameras.Update();
        }
    }
}

