using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CalibrationModule;
using CamCore;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using CamAlgorithms;
using CamAlgorithms.Calibration;

namespace CamUnitTest
{
    [TestClass]
    public class RaidalCorrectionMinimalisationTests
    {

        [TestMethod]
        public void Test_Compute_ABC()
        {
            LMDistortionDirectionalLineFitMinimalisation miniAlg = new LMDistortionDirectionalLineFitMinimalisation();

            // Create some lines with known A,B,C
            // Check if 'minimalisation' computes them ok
            var lines = GenerateTestLines();
            miniAlg.DistortionModel = new DummyModel();
            miniAlg.DistortionModel.InitParameters();
            miniAlg.LinePoints = lines;
            miniAlg.MeasurementsVector = new DenseVector(lines.Count);
            miniAlg.ParametersVector = new DenseVector(1);

            miniAlg.Init();

            var coeffs = miniAlg.LineCoeffs;
            Matrix<double> expectedMatrix = new DenseMatrix(3, 3);
            expectedMatrix[0, 0] = 0.0;
            expectedMatrix[0, 1] = 1.0;
            expectedMatrix[0, 2] = -0.5;
            expectedMatrix[1, 0] = -1.0;
            expectedMatrix[1, 1] = 1.0;
            expectedMatrix[1, 2] = 0.0;
            expectedMatrix[2, 0] = 1.0;
            expectedMatrix[2, 1] = 0.0;
            expectedMatrix[2, 2] = 1.0;

            Assert.IsTrue((coeffs - expectedMatrix).FrobeniusNorm() < 1e-6,
                "Ideal lines coefficients approximation failed");
        }

        [TestMethod]
        public void Test_Compute_ABC_Noised()
        {
            // Distort points, such that best fit using LSM is ~(A,B,C)
            // Check if 'minimalisation' computes them ok :            
            // sum of squared distances is equal/less than distance to (A,B,C)

            LMDistortionBasicLineFitMinimalisation miniAlg = new LMDistortionBasicLineFitMinimalisation();

            var lines = GenerateTestLinesNoised();
            miniAlg.DistortionModel = new DummyModel();
            miniAlg.DistortionModel.InitParameters();
            miniAlg.LinePoints = lines;
            miniAlg.ParametersVector = new DenseVector(1);

            miniAlg.Init();
            miniAlg.ComputeSums();
            miniAlg.ComputeLineCoeffs();

            var coeffs = miniAlg.LineCoeffs;
            double distanceToABC = 20 * 0.0025 + 10 * 0.005;

            double error = 0.0;
            for(int i = 0; i < 3; ++i)
            {
                for(int p = 0; p < 10; ++p)
                {
                    error += miniAlg.ComputeErrorForPoint(i, p);
                }
            }

            Assert.IsTrue(error - distanceToABC <= 2e-4,
                "Noised lines coefficients approximation failed");
        }

        LMDistortionBasicLineFitMinimalisation _miniAlg;
        RadialDistortionModel _model;
        List<List<Vector2>> _distLines;
        List<List<Vector2>> _realLines;
        int _pointsCount = 120;

        public void PrepareMinimalizationAlgorithm_JacobianTests()
        {
            _model = new Rational3RDModel();
            _model.InitialAspectEstimation = 1.0;
            _model.InitialCenterEstimation = new Vector2(0.25, 0.5);
            _model.InitParameters();

            _model.Coeffs[0] = 1e-1; // k1
            _model.Coeffs[1] = -2e-2; // k2
            _model.Coeffs[2] = 5e-3; // k3

            _distLines = DistortLines(_model, GenerateTestLines());

            // Change parameters a little
            _model.Coeffs[0] = 2e-1; // k1
            _model.Coeffs[1] = -1e-3; // k2
            _model.Coeffs[2] = 1e-3; // k3
            _model.Coeffs[3] = 0.25; // cx
            _model.Coeffs[4] = 0.5; // cy
            //_model.Parameters[5] = 1.0; // sx

            // Just test if analitic jacobian is close to numeric one
            // 2) Compute jacobian for lines

            _miniAlg = new LMDistortionBasicLineFitMinimalisation();

            _miniAlg.DistortionModel = _model;
            _miniAlg.LinePoints = _distLines;
            _miniAlg.ParametersVector = _model.Coeffs;

            _miniAlg.Init();
            _miniAlg.UpdateAll();
        }

        [TestMethod]
        public void Test_ComputeDerivatives()
        {
            PrepareMinimalizationAlgorithm_JacobianTests();

            Vector<double> diff_a_T, diff_b_T, diff_D_T, diff_A_T, diff_C_T, diff_dist_T;
            diff_a_T = _miniAlg.GetDiff_a(1);
            diff_b_T = _miniAlg.GetDiff_b(1);
            diff_D_T = _miniAlg.GetDiff_D(1);
            diff_A_T = _miniAlg.GetDiff_A(1);
            diff_C_T = _miniAlg.GetDiff_C(1);
            diff_dist_T = _miniAlg.GetDiff_d1(1, 5);

            Vector<double> diff_a = new DenseVector(_model.ParametersCount);
            Vector<double> diff_b = new DenseVector(_model.ParametersCount);
            Vector<double> diff_D = new DenseVector(_model.ParametersCount);
            Vector<double> diff_A = new DenseVector(_model.ParametersCount);
            Vector<double> diff_C = new DenseVector(_model.ParametersCount);
            Vector<double> diff_dist = new DenseVector(_model.ParametersCount);
            for(int k = 0; k < _model.ParametersCount; ++k)
            {
                double oldK = _model.Coeffs[k];
                double k_n = oldK > float.Epsilon ? oldK * (1 - 1e-6) : -1e-8;
                double k_p = oldK > float.Epsilon ? oldK * (1 + 1e-6) : 1e-8;

                _model.Coeffs[k] = k_n;
                _model.Coeffs.CopyTo(_miniAlg.ResultsVector);
                _miniAlg.UpdateAll();

                double a_n = _miniAlg.Get_a(1);
                double b_n = _miniAlg.Get_b(1);
                double D_n = _miniAlg.Get_D(1);
                double A_n = _miniAlg.Get_A(1);
                double C_n = _miniAlg.Get_C(1);
                double d1_n = _miniAlg.Get_d1(1, 5);

                _model.Coeffs[k] = k_p;
                _model.Coeffs.CopyTo(_miniAlg.ResultsVector);
                _miniAlg.UpdateAll();

                double a_p = _miniAlg.Get_a(1);
                double b_p = _miniAlg.Get_b(1);
                double D_p = _miniAlg.Get_D(1);
                double A_p = _miniAlg.Get_A(1);
                double C_p = _miniAlg.Get_C(1);
                double d1_p = _miniAlg.Get_d1(1, 5);

                diff_a[k] = (1.0 / (k_p - k_n)) * (a_p - a_n);
                diff_b[k] = (1.0 / (k_p - k_n)) * (b_p - b_n);
                diff_D[k] = (1.0 / (k_p - k_n)) * (D_p - D_n);
                diff_A[k] = (1.0 / (k_p - k_n)) * (A_p - A_n);
                diff_C[k] = (1.0 / (k_p - k_n)) * (C_p - C_n);
                diff_dist[k] = (1.0 / (k_p - k_n)) * (d1_p - d1_n);

                _model.Coeffs[k] = oldK;
            }

            double diff_a_diff = diff_a.PointwiseDivide(diff_a_T).L2Norm();
            Assert.IsTrue(Math.Abs(diff_a_diff - Math.Sqrt(6)) < Math.Sqrt(6) / 100.0 || // 1% diffrence max
                (diff_a - diff_a_T).L2Norm() < diff_a.L2Norm() / 100.0,
                "Analitical and numeric d(a)/d(P) differ");

            double diff_b_diff = diff_b.PointwiseDivide_NoNaN(diff_b_T).L2Norm();
            Assert.IsTrue(Math.Abs(diff_b_diff - Math.Sqrt(6)) < Math.Sqrt(6) / 100.0 || // 1% diffrence max
                (diff_b - diff_b_T).L2Norm() < diff_b.L2Norm() / 100.0,
                "Analitical and numeric d(b)/d(P) differ");

            double diff_D_diff = diff_D.PointwiseDivide_NoNaN(diff_D_T).L2Norm();
            Assert.IsTrue(Math.Abs(diff_D_diff - Math.Sqrt(6)) < Math.Sqrt(6) / 100.0 || // 1% diffrence max
                (diff_D - diff_D_T).L2Norm() < diff_D.L2Norm() / 100.0,
                "Analitical and numeric d(D)/d(P) differ");

            double diff_A_diff = diff_A.PointwiseDivide_NoNaN(diff_A_T).L2Norm();
            Assert.IsTrue(Math.Abs(diff_A_diff - Math.Sqrt(6)) < Math.Sqrt(6) / 100.0 || // 1% diffrence max
                (diff_A - diff_A_T).L2Norm() < diff_A.L2Norm() / 100.0,
                "Analitical and numeric d(A)/d(P) differ");

            double diff_C_diff = diff_C.PointwiseDivide_NoNaN(diff_C_T).L2Norm();
            Assert.IsTrue(Math.Abs(diff_C_diff - Math.Sqrt(6)) < Math.Sqrt(6) / 100.0 || // 1% diffrence max
                (diff_C - diff_C_T).L2Norm() < diff_C.L2Norm() / 100.0,
                "Analitical and numeric d(C)/d(P) differ");

            double diff_d1_diff = diff_dist.PointwiseDivide_NoNaN(diff_dist_T).L2Norm();
            Assert.IsTrue(Math.Abs(diff_d1_diff - Math.Sqrt(6)) < Math.Sqrt(6) / 100.0 || // 1% diffrence max
                (diff_dist - diff_dist_T).L2Norm() < diff_dist.L2Norm() / 100.0,
                "Analitical and numeric d(d1)/d(P) differ");
        }

        Vector<double> _realParameters;

        public void PrepareMinimalizationAlgorithm_MinimalisationTests()
        {
            _model = new Rational3RDModel();
            _model.InitialAspectEstimation = 1.0;
            _model.InitialCenterEstimation = new Vector2(0.45, 0.55);
            _model.InitParameters();

            _model.Coeffs[0] = 1e-1; // k1
            _model.Coeffs[1] = -2e-2; // k2
            _model.Coeffs[2] = 5e-3; // k3

            _model.UseNumericDerivative = true;
            _model.NumericDerivativeStep = 1e-4;

            _realParameters = new DenseVector(_model.ParametersCount);
            _model.Coeffs.CopyTo(_realParameters);

            _distLines = DistortLines(_model, GenerateTestLines_Many());

            _miniAlg = new LMDistortionDirectionalLineFitMinimalisation();

            _miniAlg.DistortionModel = _model;
            _miniAlg.LinePoints = _distLines;
            _miniAlg.MaximumResidiual = 0.001; // 
            _miniAlg.MaximumIterations = 50;
            _miniAlg.UseCovarianceMatrix = false;
            _miniAlg.DoComputeJacobianNumerically = true;
            _miniAlg.NumericalDerivativeStep = 1e-4;
            _miniAlg.DumpingMethodUsed = LevenbergMarquardtBaseAlgorithm.DumpingMethod.None;
        }

        [TestMethod]
        public void Test_MinimiseParameters_ClosestInitialGuess()
        {
            PrepareMinimalizationAlgorithm_MinimalisationTests();

            // Set close initial values
            _model.Coeffs[0] = 0.11; // k1 = 0.1
            _model.Coeffs[1] = -0.022; // k2 = -0.02
            _model.Coeffs[2] = 0.0055; // k3 = 0.005
            _model.Coeffs[3] = 0.46; // cx = 0.45
            _model.Coeffs[4] = 0.54; // cy = 0.55
           // _model.Parameters[5] = 1.0; // sx = 1.0

            _miniAlg.ParametersVector = _model.Coeffs;
            _miniAlg.MaximumResidiual = 1e-8;
            _miniAlg.MaximumIterations = 100;
            ((LMDistortionDirectionalLineFitMinimalisation)_miniAlg).FindInitialModelParameters = false;
            _miniAlg.Process();

            double residiual = _miniAlg.ComputeResidiual();
            double result_ratio = _miniAlg.ResultsVector.PointwiseDivide(_realParameters).L2Norm();
            Assert.IsTrue(Math.Abs(result_ratio - Math.Sqrt(6)) < Math.Sqrt(6) / 100.0 || // Max 1% difference
                _miniAlg.ComputeResidiual() < 1e-6, // or very small residiual reached -> in general it is how it works
                "Parameters approximation failed: residiual = " + residiual);
        }
        
        [TestMethod]
        public void Test_MinimiseParameters_ZeroInitialGuess()
        {
            PrepareMinimalizationAlgorithm_MinimalisationTests();

            // Set close initial values
            _model.InitialCenterEstimation = new Vector2(0.5, 0.5);
            _model.InitParameters();

            _miniAlg.ParametersVector = _model.Coeffs;
            _miniAlg.MaximumResidiual = 1e-10;
            _miniAlg.MaximumIterations = 100;
            ((LMDistortionDirectionalLineFitMinimalisation)_miniAlg).FindInitialModelParameters = true;
            _miniAlg.DumpingMethodUsed = LevenbergMarquardtBaseAlgorithm.DumpingMethod.Multiplicative;
            _miniAlg.Process();

            double residiual = _miniAlg.ComputeResidiual();
            var result_ratio_v = _miniAlg.ResultsVector.PointwiseDivide(_realParameters);
            double result_ratio = _miniAlg.ResultsVector.PointwiseDivide(_realParameters).L2Norm();
            Assert.IsTrue(Math.Abs(_miniAlg.BestResultVector[3] - 0.45) < 0.02, "Estimated center X too far");
            Assert.IsTrue(Math.Abs(_miniAlg.BestResultVector[4] - 0.55) < 0.02, "Estimated center Y too far");
            Assert.IsTrue(Math.Abs(result_ratio - Math.Sqrt(6)) < Math.Sqrt(6) / 100.0 || // Max 1% difference
                _miniAlg.ComputeResidiual() < 1e-10, // or very small residiual reached -> in general it is how it works
                "Parameters approximation failed: residiual = " + residiual);
        }

        [TestMethod]
        public void Test_DistortionDirection()
        {
            _model = new Rational3RDModel();
            _model.InitialAspectEstimation = 1.0;
            _model.InitialCenterEstimation = new Vector2(0.5, 0.5);
            _model.InitParameters();

            _model.Coeffs[0] = 1e-1; // k1
            _model.Coeffs[1] = -2e-2; // k2
            _model.Coeffs[2] = 5e-3; // k3
            
            _distLines = DistortLines(_model, GenerateTestLines_Many());
            
            _miniAlg = new LMDistortionDirectionalLineFitMinimalisation();
            
            _miniAlg.DistortionModel = new DummyModel();
            _miniAlg.DistortionModel.InitParameters();
            _miniAlg.LinePoints = _distLines;
            _miniAlg.MeasurementsVector = new DenseVector(_distLines.Count);
            _miniAlg.ParametersVector = new DenseVector(1);

            _miniAlg.Init();
            var fitCoeffs = _miniAlg.LineCoeffs;

            // For each line find direction -> all should be cushion ?

            for(int l = 0; l < _distLines.Count; ++l)
            {
                DistortionDirection dir =
                    RadialDistortion.DirectionFromLine(_distLines[l],
                    fitCoeffs[l, 0], fitCoeffs[l, 1], fitCoeffs[l, 2], _model.DistortionCenter);

                Assert.IsTrue(dir == DistortionDirection.Cushion);
            }

            _model.Coeffs[0] = -1e-1; // k1
            _model.Coeffs[1] = 2e-2; // k2
            _model.Coeffs[2] = -5e-3; // k3

            _distLines = DistortLines(_model, GenerateTestLines_Many());

            _miniAlg.LinePoints = _distLines;
            _miniAlg.Init();
            fitCoeffs = _miniAlg.LineCoeffs;

            // For each line find direction -> all should be barrel ?

            for(int l = 0; l < _distLines.Count; ++l)
            {
                DistortionDirection dir =
                    RadialDistortion.DirectionFromLine(_distLines[l],
                    fitCoeffs[l, 0], fitCoeffs[l, 1], fitCoeffs[l, 2], _model.DistortionCenter);

                Assert.IsTrue(dir == DistortionDirection.Barrel);
            }
        }
        [TestMethod]
        public void Test_InitalModelParameters()
        {
            _model = new Rational3RDModel();
            _model.InitialAspectEstimation = 1.0;
            _model.InitialCenterEstimation = new Vector2(0.55, 0.45);
            _model.InitParameters();

            _model.Coeffs[0] = -1e-1; // k1
            _model.Coeffs[1] = 2e-2; // k2
            _model.Coeffs[2] = 5e-3; // k3

            _distLines = DistortLines(_model, GenerateTestLines_Many());

            _miniAlg = new LMDistortionDirectionalLineFitMinimalisation();

            _miniAlg.DistortionModel = new Rational3RDModel();
            _miniAlg.DistortionModel.InitialCenterEstimation = new Vector2(0.5, 0.5);
            _miniAlg.DistortionModel.InitParameters();
            _miniAlg.LinePoints = _distLines;
            _miniAlg.MeasurementsVector = new DenseVector(_distLines.Count);
            _miniAlg.ParametersVector = new DenseVector(1);

            _miniAlg.Init();
        }

        [TestMethod]
        public void Test_Compute_ABC_Ver3()
        {
            LMDistortionDirectionalLineFitMinimalisation miniAlg = 
                new LMDistortionDirectionalLineFitMinimalisation();
            
            _model = new Rational3RDModel();
            _model.InitialAspectEstimation = 1.0;
            _model.InitialCenterEstimation = new Vector2(0.5, 0.5);
            _model.InitParameters();

            _model.Coeffs[0] = 1e-2; // k1
            _model.Coeffs[1] = -2e-3; // k2
            _model.Coeffs[2] = 1e-4; // k3

            _distLines = DistortLines(_model, GenerateTestLines_Many());

            _model.Coeffs[0] = 0; // k1
            _model.Coeffs[1] = 0; // k2
            _model.Coeffs[2] = 0; // k3

            miniAlg.DistortionModel = _model;
            miniAlg.LinePoints = _distLines;
            miniAlg.MeasurementsVector = new DenseVector(_distLines.Count);
            miniAlg.ParametersVector = new DenseVector(_model.ParametersCount);

            miniAlg.Init();
            miniAlg.UpdateAll();

            var coeffs = miniAlg.LineCoeffs;
            double distanceToABC = 20 * 0.0025 + 10 * 0.005;

            double error = 0.0;
            for(int i = 0; i < _distLines.Count; ++i)
            {
                for(int p = 0; p < _distLines[i].Count; ++p)
                {
                    error += miniAlg.ComputeErrorForPoint(i, p);
                }
            }

            Assert.IsTrue(error - distanceToABC <= 2e-4,
                "Noised lines coefficients approximation failed");
        }

        [TestMethod]
        public void Test_ComputeJacobian_Ver3()
        {
            _model = new Rational3RDModel();
            _model.InitialAspectEstimation = 1.0;
            _model.InitialCenterEstimation = new Vector2(0.25, 0.5);
            _model.InitParameters();

            _model.Coeffs[0] = 1e-1; // k1
            _model.Coeffs[1] = -2e-2; // k2
            _model.Coeffs[2] = 5e-3; // k3

            _distLines = DistortLines(_model, GenerateTestLines());

            // Change parameters a little
            _model.Coeffs[0] = 2e-1; // k1
            _model.Coeffs[1] = -1e-3; // k2
            _model.Coeffs[2] = 1e-3; // k3
            _model.Coeffs[3] = 0.25; // cx
            _model.Coeffs[4] = 0.5; // cy
           // _model.Parameters[5] = 1.0; // sx

            _miniAlg = new LMDistortionDirectionalLineFitMinimalisation();

            _miniAlg.DistortionModel = _model;
            _miniAlg.LinePoints = _distLines;
            _miniAlg.ParametersVector = _model.Coeffs;

            _miniAlg.Init();
            _miniAlg.UpdateAll();

            int size = 10 * _model.ParametersCount;
            _miniAlg.DoComputeJacobianNumerically = false;
            Matrix<double> testedJacobian = new DenseMatrix(_pointsCount, _model.ParametersCount);
            _miniAlg.ComputeJacobian(testedJacobian);

            _miniAlg.NumericalDerivativeStep = 1e-6;
            _miniAlg.DoComputeJacobianNumerically = true;
            Matrix<double> numericJacobian = new DenseMatrix(_pointsCount, _model.ParametersCount);
            _miniAlg.ComputeJacobian(numericJacobian);
            
            var line2NJ = numericJacobian.SubMatrix(10, 10, 0, _model.ParametersCount);
            var line2TJ = testedJacobian.SubMatrix(10, 10, 0, _model.ParametersCount);

            var jacobian_diff = line2NJ.PointwiseDivide_NoNaN(line2TJ);
            double err = jacobian_diff.FrobeniusNorm();
            Assert.IsTrue(Math.Abs(err - Math.Sqrt(size)) < Math.Sqrt(size) / 100.0 || // 1% diffrence max
                (line2NJ - line2TJ).FrobeniusNorm() < line2NJ.FrobeniusNorm() / 100.0,
                "Analitical and numeric jacobians differ");
        }

        RadialDistortionModel _realModel;

        public void PrepareMinimalizationAlgorithm_MinimalisationTests_Ver3()
        {
            _realModel = new Rational3RDModel();
            _realModel.InitialAspectEstimation = 1.0;
            _realModel.InitialCenterEstimation = new Vector2(0.5, 0.5);
            _realModel.InitParameters();

            _realModel.Coeffs[0] = 0.2; // k1
            _realModel.Coeffs[1] = -0.2; // k2
            _realModel.Coeffs[2] = 0; // k3

            _realModel.UseNumericDerivative = true;
            _realModel.NumericDerivativeStep = 1e-4;

            _realParameters = new DenseVector(_realModel.ParametersCount);
            _realModel.Coeffs.CopyTo(_realParameters);

            _realLines = GenerateTestLines_Many();
            _distLines = DistortLines(_realModel, _realLines);

            _model = new Rational3RDModel();
            _model.InitialAspectEstimation = 1.0;
            _model.InitialCenterEstimation = new Vector2(0.5, 0.5);
            _model.InitParameters();

            _model.UseNumericDerivative = true;
            _model.NumericDerivativeStep = 1e-4;

            _miniAlg = new LMDistortionDirectionalLineFitMinimalisation();

            _miniAlg.DistortionModel = _model;
            _miniAlg.LinePoints = _distLines;
            _miniAlg.MaximumIterations = 100;
            _miniAlg.UseCovarianceMatrix = false;
            _miniAlg.DoComputeJacobianNumerically = true;
            _miniAlg.NumericalDerivativeStep = 1e-4;
            _miniAlg.DumpingMethodUsed = LevenbergMarquardtBaseAlgorithm.DumpingMethod.Multiplicative;
        }

        [TestMethod]
        public void Test_Minimise_J3_Zero_R3_R3()
        {
            PrepareMinimalizationAlgorithm_MinimalisationTests_Ver3();

            // Set close initial values
            _model.Coeffs[0] = 0.0;
            _model.Coeffs[1] = 0.0;
            _model.Coeffs[2] = 0.0;
            _model.Coeffs[3] = 0.4;
            _model.Coeffs[4] = 0.6;

            _miniAlg.MaximumResidiual = 1e-16;
            _miniAlg.ParametersVector = _model.Coeffs;
            _miniAlg.Process(); 

            double residiual = _miniAlg.ComputeResidiual();
            double result_ratio = _miniAlg.ResultsVector.PointwiseDivide(_realParameters).L2Norm();

            _miniAlg.BestResultVector.CopyTo(_model.Coeffs);
            double realResidiual = 0.0;
            double baseResidiual = 0.0;
            for(int l = 0; l < _realLines.Count; ++l)
            {
                for(int p = 0; p < _realLines[l].Count; ++p)
                {
                    _model.P = _distLines[l][p];
                    _model.Undistort();
                    var pf = _model.Pf;
                    var pd = _distLines[l][p];
                    var pr = _realLines[l][p];
                    realResidiual += pf.DistanceToSquared(pr);
                    baseResidiual += pd.DistanceToSquared(pr);
                }
            }

            Assert.IsTrue(Math.Abs(result_ratio - Math.Sqrt(6)) < Math.Sqrt(6) / 100.0 || // Max 1% difference
                _miniAlg.MinimumResidiual / _miniAlg.BaseResidiual < 1e-4 ||// or very small residiual reached -> in general it is how it works
                (realResidiual / baseResidiual < 0.05 && realResidiual < 0.1), // or points are much closer to original ones -> it is the ultimate aim
                "Parameters approximation failed: \r\n" + 
                "Minimum alg-residiual: " + _miniAlg.MinimumResidiual + ", alg-base: " + _miniAlg.BaseResidiual + "\r\n" +
                "Distorted residiual: " + baseResidiual + ", final: " + realResidiual);
        }

        [TestMethod]
        public void Test_Minimise_J3_Zero_R3_T4()
        {
            PrepareMinimalizationAlgorithm_MinimalisationTests_Ver3();

            _model = new Taylor4Model();
            _model.InitialAspectEstimation = 1.0;
            _model.InitialCenterEstimation = new Vector2(0.5, 0.5);
            _model.InitParameters();

            _model.UseNumericDerivative = true;
            _model.NumericDerivativeStep = 1e-4;

            // Set close initial values
            _model.Coeffs[0] = 0.0;
            _model.Coeffs[1] = 0.0;
            _model.Coeffs[2] = 0.0;
            _model.Coeffs[3] = 0.0;
            _model.Coeffs[4] = 0.4;
            _model.Coeffs[5] = 0.6;

            _miniAlg.DistortionModel = _model;
            _miniAlg.MaximumResidiual = 1e-16;
            _miniAlg.ParametersVector = _model.Coeffs;
            _miniAlg.Process();

            double residiual = _miniAlg.ComputeResidiual();
            _miniAlg.BestResultVector.CopyTo(_model.Coeffs);
            double realResidiual = 0.0;
            double baseResidiual = 0.0;
            for(int l = 0; l < _realLines.Count; ++l)
            {
                for(int p = 0; p < _realLines[l].Count; ++p)
                {
                    _model.P = _distLines[l][p];
                    _model.Undistort();
                    var pf = _model.Pf;
                    var pd = _distLines[l][p];
                    var pr = _realLines[l][p];
                    realResidiual += pf.DistanceToSquared(pr);
                    baseResidiual += pd.DistanceToSquared(pr);
                }
            }

            Assert.IsTrue(_miniAlg.MinimumResidiual / _miniAlg.BaseResidiual < 1e-4 ||// or very small residiual reached -> in general it is how it works
                (realResidiual / baseResidiual < 0.05 && realResidiual < 0.1), // or points are much closer to original ones -> it is the ultimate aim
                "Parameters approximation failed: \r\n" +
                "Minimum alg-residiual: " + _miniAlg.MinimumResidiual + ", alg-base: " + _miniAlg.BaseResidiual + "\r\n" +
                "Distorted residiual: " + baseResidiual + ", final: " + realResidiual);
        }
        
        [TestMethod]
        public void Test_IntersectionPoints()
        {
            Test_IntersectionPoint(
                new Vector2(0,0), new Vector2(4,4), 
                new Vector2(3,0), new Vector2(2,5), 
                new Vector2(2.5,2.5));

            Test_IntersectionPoint(
                new Vector2(5, 5), new Vector2(5, -5),
                new Vector2(3, 3), new Vector2(6, -3),
                new Vector2(5, -1));

            Test_IntersectionPoint(
                new Vector2(3, 3), new Vector2(6, -3),
                new Vector2(5, 5), new Vector2(5, -5),
                new Vector2(5, -1));
        }

        void Test_IntersectionPoint(Vector2 p11, Vector2 p12, Vector2 p21, Vector2 p22, Vector2 expectedPoint)
        {
            Line2D line1 = new Line2D(p11, p12);
            Line2D line2 = new Line2D(p21, p22);
            Vector2 intPoint = Line2D.IntersectionPoint(line1, line2);

            Assert.IsTrue(intPoint.DistanceToSquared(expectedPoint) < 1e-6);
        }

        [TestMethod]
        public void Test_DistortionDirection_Quadric()
        {
            _model = new Rational3RDModel();
            _model.InitialAspectEstimation = 1.0;
            _model.InitialCenterEstimation = new Vector2(0.5, 0.5);
            _model.InitParameters();

            _model.Coeffs[0] = 0.2; // k1
            _model.Coeffs[1] = -0.2; // k2
            _model.Coeffs[2] = 0.01; // k3

            _distLines = DistortLines(_model, GenerateTestLines_Many());

            _miniAlg = new LMDistortionDirectionalLineFitMinimalisation();
            var miniAlg = (LMDistortionDirectionalLineFitMinimalisation)_miniAlg;

            miniAlg.DistortionModel = _model;
            _model.Coeffs[0] = 0.0;
            _model.Coeffs[1] = 0.0;
            _model.Coeffs[2] = 0.0;
            miniAlg.LinePoints = _distLines;
            miniAlg.MeasurementsVector = new DenseVector(_distLines.Count);
            miniAlg.ParametersVector = new DenseVector(1);

            miniAlg.Init();
            miniAlg.UpdateAll();

            // For each line find direction -> all should be cushion ?

            for(int l = 0; l < _distLines.Count; ++l)
            {
                DistortionDirection dir = miniAlg.LineDistortionDirections[l];

                Assert.IsTrue(dir == DistortionDirection.Cushion);
            }

            _model.Coeffs[0] = -1e-1; // k1
            _model.Coeffs[1] = 2e-2; // k2
            _model.Coeffs[2] = -5e-3; // k3

            _distLines = DistortLines(_model, GenerateTestLines_Many());

            _model.Coeffs[0] = 0.0;
            _model.Coeffs[1] = 0.0;
            _model.Coeffs[2] = 0.0;
            _miniAlg.LinePoints = _distLines;
            miniAlg.Init();
            miniAlg.UpdateAll();

            // For each line find direction -> all should be barrel ?

            for(int l = 0; l < _distLines.Count; ++l)
            {
                DistortionDirection dir = miniAlg.LineDistortionDirections[l];

                Assert.IsTrue(dir == DistortionDirection.Barrel);
            }
        }

        // Returns 3 lines, each 10 points, coeffs:
        // L1 : A = 0, B = 1, C = -0.5 (horizontal)
        // L2 : A = -1, B = 1, C = 0
        // L3 : A = 1, B = 0, C = 1 (vertical)
        public List<List<Vector2>> GenerateTestLines()
        {
            List<List<Vector2>> lines = new List<List<Vector2>>();
            _pointsCount = 30;

            var line1 = new List<Vector2>(10);
            var line2 = new List<Vector2>(10);
            var line3 = new List<Vector2>(10);
            for(int p = 0; p < 10; ++p)
            {
                line1.Add(new Vector2(p * 0.1, 0.5));
                line2.Add(new Vector2(p * 0.1, p * 0.1));
                line3.Add(new Vector2(-1.0, p * 0.1));
            }
            lines.Add(line1);
            lines.Add(line2);
            lines.Add(line3);

            return lines;
        }

        // Adds symetric noise to lines from 'GenerateTestLines'
        public List<List<Vector2>> GenerateTestLinesNoised()
        {
            List<List<Vector2>> lines = GenerateTestLines();
            Vector2[] d = new Vector2[3];
            d[0] = new Vector2(0.0, 0.05);
            d[1] = new Vector2(0.05, -0.05);
            d[2] = new Vector2(0.05, 0.0);

            for(int i = 0; i < 3; ++i)
            {
                lines[i][0] += d[i];
                lines[i][1] -= d[i];
                lines[i][2] += d[i];
                lines[i][3] -= d[i];
                lines[i][4] += d[i];
                lines[i][5] -= d[i];
                lines[i][6] += d[i];
                lines[i][7] -= d[i];
                lines[i][8] += d[i];
                lines[i][9] -= d[i];
            }

            return lines;
        }

        public List<List<Vector2>> DistortLines(RadialDistortionModel model, List<List<Vector2>> realLines)
        {
            List<List<Vector2>> dlines;
            dlines = new List<List<Vector2>>(realLines.Count);

            for(int l = 0; l < realLines.Count; ++l)
            {
                List<Vector2> rline = realLines[l];
                List<Vector2> dline = new List<Vector2>(rline.Count);
                for(int p = 0; p < rline.Count; ++p)
                {
                    model.P = rline[p];
                    model.Distort();
                    dline.Add(new Vector2(model.Pf));
                }

                dlines.Add(dline);
            }

            return dlines;
        }

        // Generate 12 lines in range [0,1]
        public List<List<Vector2>> GenerateTestLines_Many()
        {
            int linesCount = 12;
            int pointsInLine = 10;
            _pointsCount = 120;
            Random rand = new Random();
            List<List<Vector2>> realLines = new List<List<Vector2>>();
            // Predefined starts/ends of lines
            Vector2[] pi = {
                new Vector2(0.0, 0.0),
                new Vector2(0.0, 0.3),
                new Vector2(0.0, 0.7),
                new Vector2(0.0, 1.0),

                new Vector2(0.0, 0.0),
                new Vector2(0.3, 0.0),
                new Vector2(0.7, 0.0),
                new Vector2(1.0, 0.0),

                new Vector2(0.1, 0.5),
                new Vector2(0.5, 0.9),
                new Vector2(0.9, 0.5),
                new Vector2(0.5, 0.1)
            };

            Vector2[] pf = {
                new Vector2(1.0, 0.0),
                new Vector2(1.0, 0.3),
                new Vector2(1.0, 0.7),
                new Vector2(1.0, 1.0),

                new Vector2(0.0, 1.0),
                new Vector2(0.3, 1.0),
                new Vector2(0.7, 1.0),
                new Vector2(1.0, 1.0),

                new Vector2(0.5, 0.9),
                new Vector2(0.9, 0.5),
                new Vector2(0.5, 0.1),
                new Vector2(0.1, 0.5)
            };

            for(int i = 0; i < linesCount; ++i)
            {
                List<Vector2> line = new List<Vector2>();
                // Create 10 points between pi and pf (inclusive)

                for(int p = 0; p < pointsInLine; ++p)
                {
                    Vector2 point = new Vector2();
                    point.X = pi[i].X + (double)p / (double)(pointsInLine - 1) * (pf[i].X - pi[i].X);
                    point.Y = pi[i].Y + (double)p / (double)(pointsInLine - 1) * (pf[i].Y - pi[i].Y);

                    line.Add(point);
                }

                realLines.Add(line);
            }
            return realLines;
        }

        class DummyModel : RadialDistortionModel
        {
            public override int ParametersCount => 1;

            public override void FullUpdate()
            {
                Pf = new Vector2(P);
            }

            public override void InitParameters()
            {
                Coeffs = new DenseVector(ParametersCount);
                Diff_Xf = new DenseVector(ParametersCount);
                Diff_Yf = new DenseVector(ParametersCount);
                Diff_Xu = new DenseVector(ParametersCount);
                Diff_Yu = new DenseVector(ParametersCount);
                Diff_Xd = new DenseVector(ParametersCount);
                Diff_Yd = new DenseVector(ParametersCount);
                Diff_Ru = new DenseVector(ParametersCount);
                Diff_Rd = new DenseVector(ParametersCount);

                Pu = new Vector2();
                Pd = new Vector2();
                Pf = new Vector2();

                Aspect = 1.0;
                DistortionCenter = new Vector2(0.5, 0.5);
            }

            public override void SetInitialParametersFromQuadrics(List<Quadric> quadrics, List<List<Vector2>> linePoints, List<int> fitPoints)
            {
                InitParameters();
            }

            public override void Undistort()
            {
                Pf = new Vector2(P);
            }
        }
    }
}
