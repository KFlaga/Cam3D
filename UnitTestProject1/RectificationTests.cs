using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CamCore;
using CamAlgorithms;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace CamUnitTest
{
    [TestClass]
    public class RectificationTests
    {
        private Matrix<double> Fi;
        CalibrationData cData = new CalibrationData();

        List<Vector2Pair> matchedPairs;

        void PrepareCalibrationData()
        {
            Fi = new DenseMatrix(3); // Target F
            Fi[1, 2] = -1.0;
            Fi[2, 1] = 1.0;

            var K_l = new DenseMatrix(3, 3);
            K_l[0, 0] = 10.0; // fx
            K_l[1, 1] = 10.0; // fy
            K_l[0, 1] = 0.0; // s
            K_l[0, 2] = 300.0; // x0
            K_l[1, 2] = 250.0; // y0
            K_l[2, 2] = 1.0; // 1

            var K_r = new DenseMatrix(3, 3);
            K_r[0, 0] = 12.0; // fx
            K_r[1, 1] = 12.5; // fy
            K_r[0, 1] = 0.0; // s
            K_r[0, 2] = 300.0; // x0
            K_r[1, 2] = 200.0; // y0
            K_r[2, 2] = 1.0; // 1

            var R_l = DenseMatrix.CreateIdentity(3);
            var R_r = DenseMatrix.CreateIdentity(3);

            var C_l = new DenseVector(3);
            C_l[0] = 50.0;
            C_l[1] = 50.0;
            C_l[2] = 0.0;

            var C_r = new DenseVector(3);
            C_r[0] = 40.0;
            C_r[1] = 40.0;
            C_r[2] = 10.0;

            var Ext_l = new DenseMatrix(3, 4);
            Ext_l.SetSubMatrix(0, 0, R_l);
            Ext_l.SetColumn(3, -R_l * C_l);

            var Ext_r = new DenseMatrix(3, 4);
            Ext_r.SetSubMatrix(0, 0, R_r);
            Ext_r.SetColumn(3, -R_r * C_r);

            var CM_l = K_l * Ext_l;
            var CM_r = K_r * Ext_r;
            cData.CameraLeft = CM_l;
            cData.CameraRight = CM_r;

            // Find e_R = P_R*C_L, e_L = P_L*C_R
            var epi_r = CM_r * new DenseVector(new double[] { C_l[0], C_l[1], C_l[2], 1.0 });
            var epi_l = CM_l * new DenseVector(new double[] { C_r[0], C_r[1], C_r[2], 1.0 });

            var ex_l = new DenseMatrix(3, 3);
            ex_l[0, 0] = 0.0;
            ex_l[1, 0] = epi_l[2];
            ex_l[2, 0] = -epi_l[1];
            ex_l[0, 1] = -epi_l[2];
            ex_l[1, 1] = 0.0;
            ex_l[2, 1] = epi_l[0];
            ex_l[0, 2] = epi_l[1];
            ex_l[1, 2] = -epi_l[0];
            ex_l[2, 2] = 0.0;

            var ex_r = new DenseMatrix(3, 3);
            ex_r[0, 0] = 0.0;
            ex_r[1, 0] = epi_r[2];
            ex_r[2, 0] = -epi_r[1];
            ex_r[0, 1] = -epi_r[2];
            ex_r[1, 1] = 0.0;
            ex_r[2, 1] = epi_r[0];
            ex_r[0, 2] = epi_r[1];
            ex_r[1, 2] = -epi_r[0];
            ex_r[2, 2] = 0.0;

            // F = [er]x * Pr * pseudoinv(Pl)
            var F = ex_r * (CM_r * CM_l.PseudoInverse());
            int rank = F.Rank();
            if(rank == 3)
            {
                // Need to ensure rank 2, so set smallest singular value to 0
                var svd = F.Svd();
                var E = svd.W;
                E[2, 2] = 0;
                var oldF = F;
                F = svd.U * E * svd.VT;
                var diff = F - oldF; // Difference should be very small if all is correct
            }

            // Scale F, so that F33 = 1
            F = F.Divide(F[2, 2]);
        }

        [TestMethod]
        public void Test_Rectification_ZhangLoop()
        {
            PrepareCalibrationData();

            ImageRectification rect = new ImageRectification(new ImageRectification_ZhangLoop());
            // Assume image of size 640x480
            rect.ImageHeight = 480;
            rect.ImageWidth = 640;
            rect.CalibData = cData;

            rect.ComputeRectificationMatrices();

            // Test H'^T * Fi * H should be very close to F
            var H_r = rect.RectificationRight;
            var H_l = rect.RectificationLeft;
            var eF = H_r.Transpose() * Fi * H_l;
            eF = eF.Divide(eF[2, 2]);

            double err = (eF - cData.Fundamental).FrobeniusNorm();
            Assert.IsTrue(err < 1e-6);
        }

        [TestMethod]
        public void Test_Rectification_FussieloUncalibrated()
        {
            PrepareCalibrationData();
            PrepareMatchedPoints();

            ImageRectification rect = new ImageRectification(new ImageRectification_FussieloUncalibrated()
            {
                UseInitialCalibration = false
            });
            // Assume image of size 640x480
            rect.ImageHeight = 480;
            rect.ImageWidth = 640;
            rect.CalibData = cData;
            rect.MatchedPairs = matchedPairs;

            rect.ComputeRectificationMatrices();

            // Test H'^T * Fi * H should be very close to F
            var H_r = rect.RectificationRight;
            var H_l = rect.RectificationLeft;
            var eF = H_r.Transpose() * Fi * H_l;
            eF = eF.Divide(eF[2, 2]);

            double err = (eF - cData.Fundamental).FrobeniusNorm();
            Assert.IsTrue(err < 1e-5);
        }

        [TestMethod]
        public void Test_Rectification_FussieloCalibrated()
        {
            PrepareCalibrationData();

            ImageRectification_FusielloCalibrated rect = new ImageRectification_FusielloCalibrated();
            // Assume image of size 640x480
            rect.ImageHeight = 480;
            rect.ImageWidth = 640;
            rect.CalibData = cData;

            rect.ComputeRectificationMatrices();

            // Test H'^T * Fi * H should be very close to F
            var H_r = rect.RectificationRight;
            var H_l = rect.RectificationLeft;
            var eF = H_r.Transpose() * Fi * H_l;
            eF = eF.Divide(eF[2, 2]);

            double err = (eF - cData.Fundamental).FrobeniusNorm();
            Assert.IsTrue(err < 1e-6);
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

                var img1 = cData.CameraLeft * real;
                var img2 = cData.CameraRight * real;
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
