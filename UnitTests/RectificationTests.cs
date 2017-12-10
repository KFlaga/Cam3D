using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CamCore;
using CamAlgorithms;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using CamAlgorithms.Calibration;

namespace CamUnitTest
{
    [TestClass]
    public class RectificationVerificationTests
    {
        private Matrix<double> Fi;
        CameraPair _cameras = new CameraPair();

        List<Vector2Pair> matchedPairs;

        void PrepareCalibrationData()
        {
            Fi = new DenseMatrix(3); // Target F
            Fi[1, 2] = -1.0;
            Fi[2, 1] = 1.0;

            var K_L = DenseMatrix.OfRowArrays(new double[][]
            {
                new double[] { 500, 0.0, 300.0 },
                new double[] { 0.0, 500, 250.0 },
                new double[] { 0.0,  0.0,   1.0 }
            });
            var K_R = DenseMatrix.OfRowArrays(new double[][]
            {
                new double[] { 520, 0.0, 300.0 },
                new double[] { 0.0, 520, 200.0 },
                new double[] { 0.0,  0.0,   1.0 }
            });
            var R_L = DenseMatrix.CreateIdentity(3);
            var R_R = DenseMatrix.CreateIdentity(3);
            var C_L = new DenseVector(new double[] { 10.0, 50.0, 0.0 });
            var C_R = new DenseVector(new double[] { 50.0, 40.0, 10.0 });

            _cameras = TestUtils.CreateTestCamerasFromMatrices(K_L, K_R, R_L, R_R, C_L, C_R);
        }

        void TestRectification(ImageRectification rect)
        {
            PrepareCalibrationData();
            PrepareMatchedPoints();

            // Assume image of size 640x480
            rect.ImageHeight = 480;
            rect.ImageWidth = 640;
            rect.Cameras = _cameras;
            rect.MatchedPairs = matchedPairs;

            rect.ComputeRectificationMatrices();

            // Test H'^T * Fi * H should be very close to F
            var H_r = rect.RectificationRight;
            var H_l = rect.RectificationLeft;
            var estimatedFundamental = H_r.Transpose() * Fi * H_l;
            estimatedFundamental = estimatedFundamental.Divide(estimatedFundamental[2, 2]);
            TestUtils.AssertEquals(estimatedFundamental, _cameras.Fundamental, "estimatedFundamental", maxDiffError:1e-3);
        }

        [TestMethod]
        public void Verify_ZhangLoop()
        {
            ImageRectification rect = new ImageRectification(new Rectification_ZhangLoop());
            TestRectification(rect);
        }

        [TestMethod]
        public void Verify__FussieloUncalibrated()
        {
            ImageRectification rect = new ImageRectification(new Rectification_FussieloIrsara()
            {
                UseInitialCalibration = false
            });
            TestRectification(rect);
        }


        [TestMethod]
        public void Verify_FussieloUncalibrated_WithInitial()
        {
            ImageRectification rect = new ImageRectification(new Rectification_FussieloIrsara()
            {
                UseInitialCalibration = true
            });
            TestRectification(rect);
        }

        [TestMethod]
        public void Verify_FussieloCalibrated()
        {
            ImageRectification rect = new ImageRectification(new Rectification_FusielloTruccoVerri());
            TestRectification(rect);
        }

        int seed = 100;
        double _rangeReal_MaxX = 100;
        double _rangeReal_MaxY = 100;
        double _rangeReal_MaxZ = 100;
        double _rangeReal_MinX = -100;
        double _rangeReal_MinY = -100;
        double _rangeReal_MinZ = 50;
        void PrepareMatchedPoints()
        {
            matchedPairs = new List<Vector2Pair>();

            Random rand;
            if(seed == 0)
                rand = new Random();
            else
                rand = new Random(seed);

            // Create about 100 3d points
            for(int i = 0; i < 100; ++i)
            {
                Vector<double> real = new DenseVector(4);
                real[0] = rand.NextDouble() * (_rangeReal_MaxX - _rangeReal_MinX) + _rangeReal_MinX;
                real[1] = rand.NextDouble() * (_rangeReal_MaxY - _rangeReal_MinY) + _rangeReal_MinY;
                real[2] = rand.NextDouble() * (_rangeReal_MaxZ - _rangeReal_MinZ) + _rangeReal_MinZ;
                real[3] = 1.0;

                var img1 = _cameras.Left.Matrix * real;
                var img2 = _cameras.Right.Matrix * real;
                Vector2Pair pair = new Vector2Pair()
                {
                    V1 = new Vector2(img1),
                    V2 = new Vector2(img2)
                };
                matchedPairs.Add(pair);
            }
        }
    }
}
