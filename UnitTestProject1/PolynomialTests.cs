
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MathNet.Numerics.LinearAlgebra;
using CamCore;

namespace UnitTestProject1
{
    [TestClass]
    public class PolynomialTests
    {
        double _a = 2.0;
        double[] _r = new double[]
        {
            -1.5, -0.5, 0.0, 1.0, 2.0, 2.5, 3.0
        };
        int rank = 7;

        [TestMethod]
        public void Test_EstimatePolynomial()
        {
            // Polynomial a(x-r0)(x-r1)...
            
            int n = 8;
            float[] x = new float[] { -1.2f, 1.2f, 1.4f, 1.6f, 1.8f, 2.2f, -1.6f, -1.4f };
            Matrix<float> estimationMatrix = new MathNet.Numerics.LinearAlgebra.Single.DenseMatrix(n, 2);
            for(int r = 0; r < n; ++r)
            {
                estimationMatrix.At(r, 0, x[r]);
                estimationMatrix.At(r, 1, (float)PolyValue(x[r]));
            }
            Polynomial poly = Polynomial.EstimatePolynomial(estimationMatrix, rank);

            // Check if values are same for extimated coeffs and real poly
            double[] testX = new double[] { 1.01, -10.0, 100.0 };

            double real0 = PolyValue(testX[0]);
            double est0 = poly.At((float)testX[0]);
            double real1 = PolyValue(testX[1]);
            double est1 = poly.At((float)testX[1]);
            double real2 = PolyValue(testX[2]);
            double est2 = poly.At((float)testX[2]);

            Assert.IsTrue(Math.Abs(PolyValue(testX[0]) / poly.At((float)testX[0]) - 1.0) < 1e-3);
            Assert.IsTrue(Math.Abs(PolyValue(testX[1]) / poly.At((float)testX[1]) - 1.0) < 1e-3);
            Assert.IsTrue(Math.Abs(PolyValue(testX[2]) / poly.At((float)testX[2]) - 1.0) < 1e-3);
        }

        double PolyValue(double x)
        {
            double val = _a;
            for(int i = 0; i < rank; ++i)
            {
                val *= (x - _r[i]);
            }
            return val;
        }


        [TestMethod]
        public void Test_FindRoots_EstimatedPoly()
        {
            int n = 8;
            float[] x = new float[] { -1.2f, 1.2f, 1.4f, 1.6f, 1.8f, 2.2f, -1.6f, -1.4f };
            Matrix<float> estimationMatrix = new MathNet.Numerics.LinearAlgebra.Single.DenseMatrix(n, 2);
            for(int r = 0; r < n; ++r)
            {
                estimationMatrix.At(r, 0, x[r]);
                estimationMatrix.At(r, 1, (float)PolyValue(x[r]));
            }
            Polynomial poly = Polynomial.EstimatePolynomial(estimationMatrix, rank);

            PolynomialRootFinder rootFinder = new PolynomialRootFinder();
            rootFinder.Poly = poly;
            rootFinder.Process();

            var roots = rootFinder.RealRoots;
            
            Assert.IsTrue(roots.Count == rank);

            Array.Sort(_r);
            roots.Sort();

            for(int i = 0; i < rank; ++i)
            {
                Assert.IsTrue( Math.Abs(roots[i] / _r[i] - 1.0f) < 1e-4f ||
                    Math.Abs(roots[i] - _r[i]) < 1e-4f);
            }
        }
    }
}
