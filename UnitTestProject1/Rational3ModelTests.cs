using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CamCore;
using CalibrationModule;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using CamAlgorithms;
using CamAlgorithms.Calibration;

namespace CamUnitTest
{
    [TestClass]
    public class Rational3ModelTests
    {
        [TestMethod]
        public void Test_Distort()
        {
            Rational3RDModel model = new Rational3RDModel();
            model.InitialAspectEstimation = 1;
            model.InitialCenterEstimation = new Vector2(0.5, 0.5);
            model.InitParameters();

            model.Parameters[0] = 1e-1; // k1
            model.Parameters[1] = -2e-2; // k2
            model.Parameters[2] = 5e-3; // k3

            Vector2 realPoint = new Vector2(1, 1);

            model.P = realPoint;
            model.Distort();
            Vector2 distPoint = model.Pf;
            double expectedXd = 0.5 * (1 + 0.5 * Math.Sqrt(2) * model.Parameters[0]) /
                (1 + 0.5 * Math.Sqrt(2) * model.Parameters[1] + 0.5 * model.Parameters[2]);
            Vector2 expectedPoint = new Vector2(expectedXd + 0.5, expectedXd + 0.5);

            Assert.IsTrue(distPoint.DistanceTo(expectedPoint) < 1e-12,
                "Rational3 model (sx=1) distortion test failed");
            /* 
             * ============================================================== 
             */
            //model.InitialAspectEstimation = 2;
            //model.InitialCenterEstimation = new Vector2(0.5, 0.5);
            //model.InitParameters();

            //model.Parameters[0] = 1e-2; // k1
            //model.Parameters[1] = -1e-3; // k2
            //model.Parameters[2] = 1e-4; // k3

            //model.P = realPoint;
            //model.Distort();
            //distPoint = model.Pf;

            //double ru = Math.Sqrt(5.0 / 16.0);
            //expectedXd = 0.25 * (1 + ru * model.Parameters[0]) /
            //    (1 + ru * model.Parameters[1] + ru * ru * model.Parameters[2]);

            //double expectedYd = 0.5 * (1 + ru * model.Parameters[0]) /
            //    (1 + ru * model.Parameters[1] + ru * ru * model.Parameters[2]);

            //expectedPoint = new Vector2(expectedXd * 2.0 + 0.5, expectedYd + 0.5);

            //Assert.IsTrue(distPoint.DistanceTo(expectedPoint) < 1e-12,
            //    "Rational3 model (sx=2) distortion test failed");
        }

        [TestMethod]
        public void Test_Undistort()
        {
            Rational3RDModel model = new Rational3RDModel();
            model.InitialAspectEstimation = 1;
            model.InitialCenterEstimation = new Vector2(0.5, 0.5);
            model.InitParameters();

            model.Parameters[0] = 0.1; // k1
            model.Parameters[1] = 0.02; // k2
            model.Parameters[2] = (0.78 * Math.Sqrt(2.0) - 2.0) / 11.0; // k3

            Vector2 realPoint = new Vector2(1, 1);

            double ru = 0.5 * Math.Sqrt(2.0);
            double distortedX = 0.5 * (1.0 + ru * model.Parameters[0]) /
                (1.0 + ru * model.Parameters[1] + ru * ru * model.Parameters[2]);

            Vector2 distortedPoint = new Vector2(distortedX + 0.5, distortedX + 0.5);

            model.P = distortedPoint;
            model.Undistort();
            Vector2 correctedPoint = model.Pf;

            Vector2 pd = new Vector2(0.55, 0.55);
            // Test Pd
            Assert.IsTrue(model.Pd.DistanceTo(pd) < 1e-8,
                "Rational3 model Pd test failed");

            double rd = 0.55 * Math.Sqrt(2.0);
            // Test Rd
            Assert.IsTrue(Math.Abs(model.Rd - rd) < 1e-8,
                "Rational3 model Rd test failed");

            double a = 0.1 - (0.858 - 1.1 * Math.Sqrt(2.0)) / 11.0;
            double b = 1.0 - 0.011 * Math.Sqrt(2.0);
            double c = -rd;
            ru = -(b - Math.Sqrt(b * b - 4 * a * c)) / (2 * a);
            // Test Ru
            Assert.IsTrue(Math.Abs(model.Ru - ru) < 1e-8,
                "Rational3 model Ru test failed");

            Vector2 pu = new Vector2(pd.X * ru / rd, pd.Y * ru / rd);
            // Test Pu
            Assert.IsTrue(model.Pu.DistanceTo(pu) < 1e-8,
                "Rational3 model Pu test failed");

            // Test Pf
            Assert.IsTrue(correctedPoint.DistanceTo(realPoint) < 1e-8,
                "Rational3 model undistort final test failed");
        }

        [TestMethod]
        public void Test_UndistortDerivatives()
        {
            Rational3RDModel model = new Rational3RDModel();
            model.InitialAspectEstimation = 1;
            model.InitialCenterEstimation = new Vector2(0.5, 0.5);
            model.InitParameters();

            double k1 = 0.1, k2 = 0.02, k3 = (0.78 * Math.Sqrt(2.0) - 2.0) / 11.0;
            model.Parameters[0] = k1;
            model.Parameters[1] = k2;
            model.Parameters[2] = k3;

            Vector2 realPoint = new Vector2(1, 1);

            double ru = 0.5 * Math.Sqrt(2.0);
            double distortedX = 0.5 * (1.0 + ru * model.Parameters[0]) /
                (1.0 + ru * model.Parameters[1] + ru * ru * model.Parameters[2]);

            Vector2 distortedPoint = new Vector2(distortedX + 0.5, distortedX + 0.5);

            model.P = distortedPoint;
            model.FullUpdate();
            Vector2 correctedPoint = model.Pf;

            // Test Pf
            Assert.IsTrue(correctedPoint.DistanceTo(realPoint) < 1e-8,
                "Rational3 model undistort_derivatives final point test failed");

            Vector2 pd = new Vector2(0.55, 0.55);
            double rd = 0.55 * Math.Sqrt(2.0);
            double a = 0.1 - (0.858 - 1.1 * Math.Sqrt(2.0)) / 11.0;
            double b = 1.0 - 0.011 * Math.Sqrt(2.0);
            double c = -rd;
            double delta = b * b - 4 * a * c;
            ru = -(b - Math.Sqrt(delta)) / (2 * a);
            Vector2 pu = new Vector2(pd.X * ru / rd, pd.Y * ru / rd);

            // Test Diff_Xd
            Vector<double> diff_x = new DenseVector(model.ParametersCount);
            diff_x[0] = 0.0; // k1
            diff_x[1] = 0.0; // k2
            diff_x[2] = 0.0; // k3
            diff_x[3] = -1.0; // cx
            diff_x[4] = 0.0; // cy
           // diff_x[5] = -pd.X; // sx

            Vector<double> diff_y = new DenseVector(model.ParametersCount);
            diff_y[0] = 0.0; // k1
            diff_y[1] = 0.0; // k2
            diff_y[2] = 0.0; // k3
            diff_y[3] = 0.0; // cx
            diff_y[4] = -1.0; // cy
           // diff_y[5] = 0.0; // sx

            Assert.IsTrue((diff_x - model.Diff_Xd).L2Norm() < 1e-8,
                "Rational3 model Diff_Xd test failed");

            // Test Diff_Rd
            Vector<double> diff_rd = new DenseVector(model.ParametersCount);
            diff_rd[0] = 0.0; // k1
            diff_rd[1] = 0.0; // k2
            diff_rd[2] = 0.0; // k3
            diff_rd[3] = (pd.X / rd) * diff_x[3]; // cx
            diff_rd[4] = (pd.Y / rd) * diff_y[4]; // cy
           // diff_rd[5] = (pd.X / rd) * diff_x[5]; // sx

            Assert.IsTrue((diff_rd - model.Diff_Rd).L2Norm() < 1e-8,
                "Rational3 model Diff_Rd test failed");

            // Test Diff_Ru
            Vector<double> diff_a = new DenseVector(model.ParametersCount);
            diff_a[0] = 1.0; // k1
            diff_a[1] = 0.0; // k2
            diff_a[2] = -rd; // k3
            diff_a[3] = -k3 * diff_rd[3]; // cx
            diff_a[4] = -k3 * diff_rd[4]; // cy
          //  diff_a[5] = -k3 * diff_rd[5]; // sx

            Vector<double> diff_b = new DenseVector(model.ParametersCount);
            diff_b[0] = 0.0; // k1
            diff_b[1] = -rd; // k2
            diff_b[2] = 0.0; // k3
            diff_b[3] = -k2 * diff_rd[3]; // cx
            diff_b[4] = -k2 * diff_rd[4]; // cy
          //  diff_b[5] = -k2 * diff_rd[5]; // sx

            Vector<double> diff_c = diff_rd.Negate();
            Vector<double> diff_delta = 2 * b * diff_b - 4 * (a * diff_c + c * diff_a);
            Vector<double> diff_ru =
                (a * (diff_b.Negate() + (0.5 / Math.Sqrt(delta)) * diff_delta) -
                diff_a * (-b + Math.Sqrt(delta))) / (2 * a * a);

            Assert.IsTrue((diff_ru - model.Diff_Ru).L2Norm() < 1e-8,
                "Rational3 model Diff_Ru test failed");

            // Test Diff_Xu
            Vector<double> diff_xu = diff_x * (ru / rd) +
                pd.X * (diff_ru * (1 / rd) - (ru / (rd * rd)) * diff_rd);

            Assert.IsTrue((diff_xu - model.Diff_Xu).L2Norm() < 1e-8,
                "Rational3 model Diff_Xu test failed");

            // Test Diff_Xf
            Vector<double> diff_xf = new DenseVector(model.ParametersCount);
            diff_xf[0] = diff_xu[0]; // k1
            diff_xf[1] = diff_xu[1]; // k2
            diff_xf[2] = diff_xu[2]; // k3
            diff_xf[3] = diff_xu[3] + 1.0; // cx
            diff_xf[4] = diff_xu[4]; // cy
            //diff_xf[5] = diff_xu[5] + pu.X; // sx

            Assert.IsTrue((diff_xf - model.Diff_Xf).L2Norm() < 1e-8,
                "Rational3 model Diff_Xf test failed");
            
            Rational3RDModel model2 = new Rational3RDModel();
            model2.InitialAspectEstimation = 1;
            model2.InitialCenterEstimation = new Vector2(0.5, 0.5);
            model2.InitParameters();

            model2.Parameters[0] = k1;
            model2.Parameters[1] = k2;
            model2.Parameters[2] = k3;

            model2.UseNumericDerivative = true;
            model2.NumericDerivativeStep = 1e-8;

            model2.P = distortedPoint;
            model2.FullUpdate();

            Assert.IsTrue((model2.Diff_Xd - model.Diff_Xd).L2Norm() < 1e-4,
                "Rational3 model analitical and numeric derivative for Xd differ");
            Assert.IsTrue((model2.Diff_Rd - model.Diff_Rd).L2Norm() < 1e-4,
                "Rational3 model analitical and numeric derivative for Rd  differ");
            Assert.IsTrue((model2.Diff_Ru - model.Diff_Ru).L2Norm() < 1e-4,
                "Rational3 model analitical and numeric derivative for Ru  differ");
            Assert.IsTrue((model2.Diff_Xu - model.Diff_Xu).L2Norm() < 1e-4,
                "Rational3 model analitical and numeric derivative for Xu  differ");
            Assert.IsTrue((model2.Diff_Xf - model.Diff_Xf).L2Norm() < 1e-4,
                "Rational3 model analitical and numeric derivative for Xf  differ");
        }
    }
}
