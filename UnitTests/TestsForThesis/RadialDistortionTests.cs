using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CamAlgorithms.Calibration;
using CamCore;
using CamAlgorithms;

namespace CamUnitTest.TestsForThesis
{
    [TestClass]
    public class RadialDistortionTests
    {
        public TestContext TestContext { get; set; }
        public static Context MyContext { get; set; }

        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            MyContext = new Context("RadialDistortionTests");
        }

        [TestInitialize()]
        public void MyTestInitialize()
        {
            MyContext.InitTestSet(TestContext.TestName);
            model = new Rational3RDModel();
        }

        [TestCleanup()]
        public void MyTestCleanup()
        {
            MyContext.StoreTestOutput();
        }

        private RadialDistortionModel model;
        private double k1 { get { return model.Coeffs[0]; } set { model.Coeffs[0] = value; } }
        private double k2 { get { return model.Coeffs[1]; } set { model.Coeffs[1] = value; } }
        private double k3 { get { return model.Coeffs[2]; } set { model.Coeffs[2] = value; } }
        private double cx { get { return model.Coeffs[3]; } set { model.Coeffs[3] = value; } }
        private double cy { get { return model.Coeffs[4]; } set { model.Coeffs[4] = value; } }

        bool TestPointUndistortion(RadialDistortionModel model, Vector2 point)
        {
            this.model = model;
            double ru = Math.Sqrt((point.X - cx) * (point.X - cx) + (point.Y - cy) * (point.Y - cy));
            double xu = (point.X - cx) * (1 + ru * k1) / (1 + ru * k2 + ru * ru * k3);
            double yu = (point.Y - cy) * (1 + ru * k1) / (1 + ru * k2 + ru * ru * k3);
            Vector2 distortedPoint = new Vector2(xu + cx, yu + cy);

            Vector2 correctedPoint = model.Undistort(distortedPoint);

            return correctedPoint.DistanceTo(point) < 1e-8;
        }

        [TestMethod]
        public void VerifyDistortion()
        {
            MyContext.RunTest(() =>
            {
                var model = new Rational3RDModel(0.1, -0.02, 0.1, 0.5, 0.5);
                Vector2 realPoint = new Vector2(1, 1);

                Vector2 distortedPoint = model.Distort(realPoint);

                double ru = 0.5 * Math.Sqrt(2);
                double xu = 0.5 * (1 + ru * k1) / (1 + ru * k2 + ru * ru * k3);
                Vector2 expectedPoint = new Vector2(xu + cx, xu + cy);

                return distortedPoint.DistanceTo(expectedPoint) < 1e-12;
            }, "Basic");
        }

        [TestMethod]
        public void VerifyUndistortion()
        {
            MyContext.RunTest(() =>
            {
                return TestPointUndistortion(new Rational3RDModel(0.1, 0.02, 0.1, 0.5, 0.5), new Vector2(1, 1));
            }, "Basic");

            MyContext.RunTest(() =>
            {
                return TestPointUndistortion(new Rational3RDModel(0.1, 0.02, 0.1, -0.2, 0.6), new Vector2(1.2, -0.4));
            }, "x != y, y < 0, cx < 0");

            MyContext.RunTest(() =>
            {
                return TestPointUndistortion(new Rational3RDModel(1.0, 0.0, 0.5 * Math.Sqrt(2), 0.0, 0.0), new Vector2(1, 1));
            }, "Case k1 - rd * k3 = 0");
        }

        [TestMethod]
        public void VerifyParameterEstimation()
        {
            TestCase.AddTestCase(CaseType.IdealVerification, "About 10% dispacement for r = 1. Close initial guess.",
               new Rational3RDModel(0.1, -0.02, 0.005, 0.5, 0.5),
               new Rational3RDModel(0.11, -0.022, 0.0055, 0.52, 0.48), autoInit: false);

            TestCase.AddTestCase(CaseType.IdealVerification,"About 10% dispacement for r = 1. Too strong initial guess of k, close center.",
               new Rational3RDModel(0.1, -0.02, 0.005, 0.5, 0.5),
               new Rational3RDModel(0.5, -0.22, -0.1, 0.52, 0.48), autoInit: false);

            TestCase.AddTestCase(CaseType.IdealVerification,"About 10% dispacement for r = 1. Wrong direction of k, close center.",
               new Rational3RDModel(0.1, -0.02, 0.005, 0.5, 0.5),
               new Rational3RDModel(0.1, 0.8, 0, 0.52, 0.48), autoInit: false);

            TestCase.AddTestCase(CaseType.IdealVerification,"About 30% dispacement for r = 1 and scales strongly with r^2. k not far, close center.",
               new Rational3RDModel(0.05, 0.01, -0.05, 0.5, 0.5),
               new Rational3RDModel(0.03, 0.05, -0.02, 0.52, 0.48), autoInit: false);

            TestCase.AddTestCase(CaseType.IdealVerification, "About 30% dispacement for r = 1 and scales strongly with r^2. Wrong direction of k, close center.",
               new Rational3RDModel(0.05, 0.01, -0.05, 0.5, 0.5),
               new Rational3RDModel(0.1, 0.8, 0, 0.52, 0.48), autoInit: false);

            TestCase.AddTestCase(CaseType.IdealVerification,"About 10% dispacement for r = 1. Close initial guess, far center.",
               new Rational3RDModel(0.1, -0.02, 0.005, 0.5, 0.5),
               new Rational3RDModel(0.11, -0.022, 0.0055, 0.7, 0.3), autoInit: false);

            TestCase.AddTestCase(CaseType.IdealVerification, "About 10% dispacement for r = 1. Too strong initial guess of k, far center.",
               new Rational3RDModel(0.1, -0.02, 0.005, 0.5, 0.5),
               new Rational3RDModel(0.5, -0.22, -0.1, 0.7, 0.3), autoInit: false);

            TestCase.AddTestCase(CaseType.IdealVerification, "About 10% dispacement for r = 1. Start with Auto-Initial",
                new Rational3RDModel(0.1, -0.02, 0.005, 0.55, 0.45),
                new Rational3RDModel(0, 0, 0, 0.5, 0.5), autoInit: true);

            TestCase.AddTestCase(CaseType.IdealVerification,"About 30% dispacement for r = 1. Start with Auto-Initial",
                new Rational3RDModel(0.04, 0.01, -0.04, 0.55, 0.45),
                new Rational3RDModel(0, 0, 0, 0.5, 0.5), autoInit: true);
        }

        [TestMethod]
        public void TestParameterEstimationWithPolynomialDistortion()
        {
            TestCase.AddTestCase(CaseType.IdealWithPolynomial, "About 15% dispacement for r = 1. Scales slowly. Start with Auto-Initial",
                new PolynomialModel(0.17, -0.02, 0.55, 0.45),
                new Rational3RDModel(0, 0, 0, 0.5, 0.5));

            TestCase.AddTestCase(CaseType.IdealWithPolynomial, "About 10% dispacement for r = 1. Scales fast. Start with Auto-Initial",
                new PolynomialModel(0.08, 0.02, 0.55, 0.45),
                new Rational3RDModel(0, 0, 0, 0.5, 0.5));

            TestCase.AddTestCase(CaseType.IdealWithPolynomial, "Inverse direction on r = 1.5. Start with Auto-Initial",
                new PolynomialModel(-0.05, 0.022, 0.55, 0.45),
                new Rational3RDModel(0, 0, 0, 0.5, 0.5));
        }

        [TestMethod]
        public void ParameterEstimationFullSynteticTest()
        {
            List<double> deviations = new List<double>()
            {
                0.0, 0.002, 0.005, 0.01, 0.02, 0.05
            };

            foreach(var v in deviations)
            {
                TestCase.AddTestCase(CaseType.NoisedRadial, "Cushion. [0.2, 1.0, 2.0] -> [0.2%, 2.0%, 6.0%]",
                    new Rational3RDModel(0.1755, 0.1491, 0.00337, 0, 0),
                    new Rational3RDModel(0, 0, 0, -0.01, -0.01) { InitialMethod = Rational3RDModel.InitialMethods.SymmertricK1 }, deviation: v);

                TestCase.AddTestCase(CaseType.NoisedRadial, "Barrel. [0.2, 1.0, 2.0] -> [0.3%, 5.0%, 20%]",
                   new Rational3RDModel(1, 0.9, 0.2, 0, 0),
                   new Rational3RDModel(0, 0, 0, -0.01, -0.01) { InitialMethod = Rational3RDModel.InitialMethods.SymmertricK1 }, deviation: v);

                TestCase.AddTestCase(CaseType.NoisedPolynomial, "Cushion Polynomial. [0.2, 1.0, 2.0] -> [0.2%, 4.5%, 11%]",
                   new PolynomialModel(0.05035, -0.009, 0, 0),
                   new Rational3RDModel(0, 0, 0, -0.01, -0.01) { InitialMethod = Rational3RDModel.InitialMethods.SymmertricK1 }, deviation: v);

                TestCase.AddTestCase(CaseType.NoisedPolynomial, "Barrel Polynomial. [0.2, 1.0, 2.0] -> [0.4%, 5%, 7%]",
                   new PolynomialModel(-0.08, 0.018, 0, 0),
                   new Rational3RDModel(0, 0, 0, -0.01, -0.01) { InitialMethod = Rational3RDModel.InitialMethods.SymmertricK1 }, deviation: v);
            }
        }

        [TestMethod]
        public void BenchmarkUndostortion()
        {
            k1 = 0.1;
            k2 = 0.02;
            k3 = 0.1;
            cx = 0.5;
            cy = 0.5;
            Vector2 point = new Vector2(1, 1);

            MyContext.RunBenchmark(() =>
            {
                model.Undistort(point);
            }, description: "Undistortion of point with Radial3 Model");
        }

        public enum CaseType
        {
            IdealVerification,
            IdealWithPolynomial,
            NoisedRadial,
            NoisedPolynomial,
        }

        class TestCase
        {
            static int seed = 0;
            public static int Seed
            {
                get
                {
                    if(seed == 0)
                    {
                        seed = DateTime.Now.Second * 1000 + DateTime.Now.Millisecond;
                    }
                    return seed;
                }
            }


            static List<List<Vector2>> synteticTestLines = null;
            public static List<List<Vector2>> SynteticTestLines
            {
                get
                {
                    if(synteticTestLines == null)
                    {
                        synteticTestLines = new List<List<Vector2>>();
                        List<Line2D> linesCoeffs = new List<Line2D>()
                        {
                            new Line2D(new Vector2(0.1, -1), new Vector2(0.1, 1)),
                            new Line2D(new Vector2(0, 2), new Vector2(1, 0)),
                            new Line2D(new Vector2(0, -1), new Vector2(-1, 0)),
                            new Line2D(new Vector2(-1, 0.5), new Vector2(1, 0.5)),
                            new Line2D(new Vector2(-1.2, 0), new Vector2(0, 1.2)),
                        };
                        synteticTestLines.AddRange(RadialDistortionTestUtils.GeneratePointLines(10, new List<Line2D>() { linesCoeffs[0] }, -1, 1));
                        synteticTestLines.AddRange(RadialDistortionTestUtils.GeneratePointLines(10, new List<Line2D>() { linesCoeffs[1] }, 0, 2));
                        synteticTestLines.AddRange(RadialDistortionTestUtils.GeneratePointLines(10, new List<Line2D>() { linesCoeffs[2] }, -1, 0));
                        synteticTestLines.AddRange(RadialDistortionTestUtils.GeneratePointLines(10, new List<Line2D>() { linesCoeffs[3] }, -1, 1));
                        synteticTestLines.AddRange(RadialDistortionTestUtils.GeneratePointLines(10, new List<Line2D>() { linesCoeffs[4] }, -1.2, 0));
                    }
                    return synteticTestLines;
                }
            }
            
            public static void AddTestCase(CaseType caseType, string info,
                RadialDistortionModel idealModel, RadialDistortionModel testedModel,
                bool autoInit = true, double deviation = 0.0)
            {
                if(caseType == CaseType.IdealVerification || caseType == CaseType.IdealWithPolynomial)
                {
                    MyContext.RunTest(() =>
                    {
                        return RunParameterEstimationVerificationTest(idealModel, testedModel, computeInitialParams: autoInit);
                    }, info);
                }
                else
                {
                    MyContext.RunTest(() =>
                    {
                        return RunParameterEstimationSynteticTest(idealModel, testedModel, deviation: deviation);
                    }, info);
                }
            }
            
            public static bool RunParameterEstimationVerificationTest(
                RadialDistortionModel idealModel, RadialDistortionModel usedModel, bool computeInitialParams = false)
            {
                var idealLines = RadialDistortionTestUtils.GeneratePointLines();
                var distortedLines = RadialDistortionTestUtils.DistortLines(idealModel, idealLines);
                var noisedLines = distortedLines;
                
                var minimalization = RadialDistortionTestUtils.PrepareMinimalizationAlgorithm(usedModel, noisedLines, 1e-8, 100, computeInitialParams);
                minimalization.Process();

                Rational3RDModel bestModel = new Rational3RDModel();
                minimalization.BestResultVector.CopyTo(bestModel.Coeffs);
                var correctedLines = RadialDistortionTestUtils.UndistortLines(bestModel, distortedLines);
                double meanRadius = RadialDistortionTestUtils.GetMeanRadius(idealLines, idealModel.DistortionCenter);

                RadialDistortionTestUtils.StoreModelInfo(MyContext, idealModel, usedModel, minimalization);
                RadialDistortionTestUtils.StoreTestInfo(MyContext, RadialDistortionTestUtils.GetPointsCount(idealLines), meanRadius, 0.0);
                new RegressionDeviation(distortedLines, meanRadius).Store(MyContext, "Inital");
                new RegressionDeviation(correctedLines, meanRadius).Store(MyContext, "Final");
                RadialDistortionTestUtils.StoreMinimalizationInfo(MyContext, minimalization);

                return minimalization.MinimumResidiual < minimalization.MaximumResidiual * 10.0;
            }

            public static bool RunParameterEstimationSynteticTest(
                RadialDistortionModel idealModel, RadialDistortionModel usedModel, double deviation = 0.0)
            {
                List<List<Vector2>> idealLines = SynteticTestLines;
                var distortedLines = RadialDistortionTestUtils.DistortLines(idealModel, idealLines);
                var noisedLines = distortedLines;

                if(deviation > 1e-16)
                {
                    noisedLines = RadialDistortionTestUtils.AddNoiseToLines(distortedLines, deviation * deviation, Seed);
                }

                var minimalization = RadialDistortionTestUtils.PrepareMinimalizationAlgorithm(usedModel, noisedLines, 1e-8, 200, true);
                minimalization.Process();

                Rational3RDModel bestModel = new Rational3RDModel();
                minimalization.BestResultVector.CopyTo(bestModel.Coeffs);
                var correctedLines = RadialDistortionTestUtils.UndistortLines(bestModel, noisedLines);
                double meanRadius = RadialDistortionTestUtils.GetMeanRadius(idealLines, idealModel.DistortionCenter);

                RadialDistortionTestUtils.StoreModelInfo(MyContext, idealModel, usedModel, minimalization);
                RadialDistortionTestUtils.StoreTestInfo(MyContext, RadialDistortionTestUtils.GetPointsCount(idealLines), meanRadius, deviation);
                new RegressionDeviation(distortedLines, meanRadius).Store(MyContext, "Inital", true);
                new RegressionDeviation(correctedLines, meanRadius).Store(MyContext, "Final", true);
               // RadialDistortionTestUtils.StoreMinimalizationInfo(MyContext, minimalization);

                return minimalization.MinimumResidiual < minimalization.MaximumResidiual * 10.0;
            }
        }
    }
}
