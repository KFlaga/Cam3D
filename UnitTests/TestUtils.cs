using CamAlgorithms.Calibration;
using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace CamUnitTest
{
    public class TestUtils
    {
        public static string PrepareDiff(Matrix<double> m1, Matrix<double> m2, string m1Name = "", string m2Name = "")
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("Matrices " + m1Name + " and " + m2Name + " differs:");
            result.AppendLine(m1Name + ":");
            result.AppendLine(m1.CustomToString("E2"));
            result.AppendLine(m2Name + ":");
            result.AppendLine(m2.CustomToString("E2"));
            result.AppendLine("diff (m1-m2):");
            result.AppendLine((m1 - m2).CustomToString("E2"));
            result.AppendLine("rel diff (m1-m2):");
            result.AppendLine(m1.PointwiseDivide_NoNaN(m2).CustomToString("F4"));
            return result.ToString();
        }

        public static void AssertEquals(Matrix<double> m1, Matrix<double> m2, string m1Name = "Actual", string m2Name = "Expected", 
            bool scaleInvariant = false, double maxDiffError = 1e-6, double maxRelativeError = 1e-6)
        {
            if(m1.RowCount != m2.RowCount || m1.ColumnCount != m2.ColumnCount)
            {
                Assert.Fail("Matrices " + m1Name + " and " + m2Name + " dimensions does not agree");
            }

            double diffError = (m1 - m2).FrobeniusNorm();
            double relError = (m1.PointwiseDivide_NoNaN(m2) - DenseMatrix.Create(m1.RowCount, m1.ColumnCount, 1.0)).FrobeniusNorm();
            if(diffError > maxDiffError && relError > maxRelativeError)
            {
                if(scaleInvariant)
                {
                    double sc = 1.0;
                    for(int r = 0; r < m1.RowCount; ++r)
                    {
                        for(int c = 0; c < m1.ColumnCount; ++c)
                        {
                            if(m1[r, c] != 0.0 && m2[r, c] != 0.0)
                            {
                                sc = m2[r, c] / m1[r, c];
                            }
                        }
                    }
                    var m3 = m1 * sc;
                    diffError = (m3 - m2).FrobeniusNorm();
                    relError = (m1.PointwiseDivide_NoNaN(m2) - DenseMatrix.Create(m1.RowCount, m1.ColumnCount, 1.0)).FrobeniusNorm();
                    if(diffError < maxDiffError || relError < maxRelativeError) { return; }
                }
                Assert.Fail(PrepareDiff(m1, m2, m1Name, m2Name));
            }
        }

        public static string PrepareDiff(Vector<double> m1, Vector<double> m2, string m1Name = "", string m2Name = "")
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("Vectors " + m1Name + " and " + m2Name + " differs:");
            result.Append(m1Name + ": ");
            result.AppendLine(m1.ToRowVectorString("E2"));
            result.Append(m2Name + ": ");
            result.AppendLine(m2.ToRowVectorString("E2"));
            result.Append("diff (m1-m2): ");
            result.AppendLine((m1 - m2).ToRowVectorString("E2"));
            result.Append("rel diff (m1/m2): ");
            result.AppendLine(m1.PointwiseDivide_NoNaN(m2).ToRowVectorString("F4"));
            return result.ToString();
        }

        public static void AssertEquals(Vector<double> m1, Vector<double> m2, string m1Name = "Actual", string m2Name = "Expected", bool scaleInvariant = false, 
            double maxDiffError = 1e-6, double maxRelativeError = 1e-6)
        {
            double diffError = (m1 - m2).L2Norm();
            double relError = (m1.PointwiseDivide_NoNaN(m2) - DenseVector.Create(m1.Count, 1.0)).L2Norm();
            if(diffError > maxDiffError && relError > maxRelativeError)
            {
                if(scaleInvariant)
                {
                    double sc = 1.0;
                    for(int i = 0; i < m1.Count; ++i)
                    {
                        if(m1[i] != 0.0 && m2[i] != 0.0)
                        {
                            sc = m2[i] / m1[i];
                        }
                    }
                    var m3 = m1 * sc;
                    diffError = (m3 - m2).L2Norm();
                    relError = (m3.PointwiseDivide_NoNaN(m2) - DenseVector.Create(m1.Count, 1.0)).L2Norm();
                    if(diffError < maxDiffError || relError < maxRelativeError) { return; }
                }
                Assert.Fail(PrepareDiff(m1, m2, m1Name, m2Name));
            }
        }

        public static void AssertEquals(List<Vector<double>> estimated, List<Vector<double>> expected, string m1Name = "Actual", string m2Name = "Expected",
            double maxDiffError = 0.01, double maxRelativeError = 0.01)
        {
            for(int i = 0; i < estimated.Count; ++i)
            {
                double len = estimated[i].L2Norm();
                AssertEquals(estimated[i], expected[i], m1Name + "[" + i + "]", m2Name + "[" + i + "]",
                    false, len * maxDiffError, maxRelativeError);
            }
        }

        public static CameraPair CreateTestCamerasFromMatrices(
            Matrix<double> K_l, Matrix<double> K_r,
            Matrix<double> R_l, Matrix<double> R_r,
            Vector<double> T_l, Vector<double> T_r,
            int imageWidth = 640, int imageHeight = 480)
        {
            Matrix<double> Ext_l = new DenseMatrix(3, 4);
            Ext_l.SetSubMatrix(0, 0, R_l);
            Ext_l.SetColumn(3, -R_l * T_l);

            Matrix<double> Ext_r = new DenseMatrix(3, 4);
            Ext_r.SetSubMatrix(0, 0, R_r);
            Ext_r.SetColumn(3, -R_r * T_r);

            var cameras = new CameraPair();
            cameras.Left.ImageHeight = imageHeight;
            cameras.Left.ImageWidth = imageWidth;
            cameras.Right.ImageHeight = imageHeight;
            cameras.Right.ImageWidth = imageWidth;
            cameras.Left.Matrix = K_l * Ext_l;
            cameras.Right.Matrix = K_r * Ext_r;
            cameras.Update();
            return cameras;
        }

        public static Camera CreateTestCameraFromMatrices(
            Matrix<double> K, Matrix<double> R, Vector<double> T,
            int imageWidth = 640, int imageHeight = 480)
        {
            Matrix<double> Ext = new DenseMatrix(3, 4);
            Ext.SetSubMatrix(0, 0, R);
            Ext.SetColumn(3, -R * T);

            var camera = new Camera();
            camera.ImageHeight = imageHeight;
            camera.ImageWidth = imageWidth;
            camera.Matrix = K * Ext;
            camera.Decompose();
            return camera;
        }

        public static List<Vector<double>> AddNoise(List<Vector<double>> points, double variance, int seed = 0)
        {
            List<Vector<double>> noisedPoints = new List<Vector<double>>(points.Count);
            int pointSize = points[0].Count;

            GaussianNoiseGenerator noise = new GaussianNoiseGenerator();
            noise.Variance = variance;
            noise.Mean = 0.0;
            noise.RandomSeed = seed != 0;
            noise.Seed = seed;
            noise.UpdateDistribution();

            for(int i = 0; i < points.Count; ++i)
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
        
        public static Matrix<double> AddNoise(Matrix<double> matrix, double relativeVariance, int seed)
        {
            Matrix<double> noisedMat = matrix.Clone();

            GaussianNoiseGenerator noise = new GaussianNoiseGenerator();
            noise.Mean = 0.0;
            noise.RandomSeed = false;
            Random rand = new Random(seed);

            for(int r = 0; r < noisedMat.RowCount; ++r)
            {
                for(int c = 0; c < noisedMat.ColumnCount; ++c)
                {
                    noise.Seed = rand.Next();
                    noise.Variance = Math.Abs(noisedMat[r, c]) < 1e-12 ?
                        1e-12 : noisedMat[r, c] * noisedMat[r, c] * relativeVariance;
                    noise.UpdateDistribution();

                    noisedMat[r, c] = noisedMat[r, c] + noise.GetSample();
                }
            }

            return noisedMat;
        }

        public static List<Vector2> AddNoise(List<Vector2> points, double variance, int seed = 0)
        {
            List<Vector2> noisedPoints = new List<Vector2>(points.Count);

            GaussianNoiseGenerator noise = new GaussianNoiseGenerator();
            noise.Variance = variance;
            noise.Mean = 0.0;
            noise.RandomSeed = seed == 0;
            noise.Seed = seed;
            noise.UpdateDistribution();

            for(int i = 0; i < points.Count; ++i)
            {
                Vector2 d = new Vector2(noise.GetSample(), noise.GetSample());
                noisedPoints.Add(points[i] + d);
            }

            return noisedPoints;
        }


        public static List<Vector3> AddNoise(List<Vector3> points, double variance, int seed = 0)
        {
            List<Vector3> noisedPoints = new List<Vector3>(points.Count);

            GaussianNoiseGenerator noise = new GaussianNoiseGenerator();
            noise.Variance = variance;
            noise.Mean = 0.0;
            noise.RandomSeed = seed == 0;
            noise.Seed = seed;
            noise.UpdateDistribution();

            for(int i = 0; i < points.Count; ++i)
            {
                Vector3 d = new Vector3(noise.GetSample(), noise.GetSample(), noise.GetSample());
                noisedPoints.Add(points[i] + d);
            }

            return noisedPoints;
        }
    }
}
