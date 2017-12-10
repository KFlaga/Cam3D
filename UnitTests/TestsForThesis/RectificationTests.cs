using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CamAlgorithms.Calibration;
using CamCore;
using CamAlgorithms;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MatrixInfo = CamUnitTest.TestsForThesis.RectificationTestUtils.MatrixInfo;
using System.Linq;

namespace CamUnitTest.TestsForThesis
{
    [TestClass]
    public class RectificationTests
    {
        public TestContext TestContext { get; set; }
        public static Context MyContext { get; set; }

        static Matrix<double> Fi = DenseMatrix.Build.SparseOfRowArrays(new double[][]
        {
            new double[] { 0,  0,  0 },
            new double[] { 0,  0, -1 },
            new double[] { 0,  1,  0 }
        });

        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            MyContext = new Context("RectificationTests");
        }

        [TestInitialize()]
        public void MyTestInitialize()
        {
            MyContext = new Context("RectificationTests");
            MyContext.InitTestSet(TestContext.TestName);
        }

        [TestCleanup()]
        public void MyTestCleanup()
        {
            MyContext.StoreTestResults();
        }

        //[TestMethod]
        public void VerifyParameterEstimation()
        {
            TestCase.AddTestCase(CaseType.IdealVerification, "Zhang-Loop",
               new Rectification_ZhangLoop());
            TestCase.AddTestCase(CaseType.IdealVerification, "Fusiello Uncalib",
               new Rectification_FussieloIrsara() { UseInitialCalibration = false });
            TestCase.AddTestCase(CaseType.IdealVerification, "Fusiello Uncalib With Initial",
               new Rectification_FussieloIrsara() { UseInitialCalibration = true });
            TestCase.AddTestCase(CaseType.IdealVerification, "Fusiello Calib",
               new Rectification_FusielloTruccoVerri());
        }

        [TestMethod]
        public void SynteticNoisedEstimation_ZhangLoop()
        {
            double[] variances = { 0, 0.002, 0.005, 0.01, 0.02, 0.05, 0.1 };

            foreach(var v in variances)
            {
                TestCase.AddTestCase(CaseType.NoisedSyntetic, "Zhang-Loop Almost Rectified",
                   new Rectification_ZhangLoop(), noiseVariance: v, cameras: RectificationTestUtils.PrepareCalibrationData_AlmostRectified());
            }
            //foreach(var v in variances)
            //{
            //    TestCase.AddTestCase(CaseType.NoisedSyntetic, "Zhang-Loop Close To Rectified",
            //       new Rectification_ZhangLoop(), noiseVariance: v, cameras: RectificationTestUtils.PrepareCalibrationData_CloseToBeRectified());
            //}
            foreach(var v in variances)
            {
                TestCase.AddTestCase(CaseType.NoisedSyntetic, "Zhang-Loop Far To Rectified",
                   new Rectification_ZhangLoop(), noiseVariance: v, cameras: RectificationTestUtils.PrepareCalibrationData_FarToRectified());
            }
        }

        [TestMethod]
        public void SynteticNoisedEstimation_FusielloUncalib()
        {
            double[] variances = { 0, 0.002, 0.005, 0.01, 0.02, 0.05, 0.1 };

            foreach(var v in variances)
            {
                TestCase.AddTestCase(CaseType.NoisedSyntetic, "Fusiello Uncalib Almost Rectified",
                   new Rectification_FussieloIrsara() { UseInitialCalibration = false }, noiseVariance: v, cameras: RectificationTestUtils.PrepareCalibrationData_AlmostRectified());
            }
            //foreach(var v in variances)
            //{
            //    TestCase.AddTestCase(CaseType.NoisedSyntetic, "Fusiello Uncalib Close To Rectified",
            //       new Rectification_FussieloIrsara() { UseInitialCalibration = false }, noiseVariance: v, cameras: RectificationTestUtils.PrepareCalibrationData_CloseToBeRectified());
            //}
            foreach(var v in variances)
            {
                TestCase.AddTestCase(CaseType.NoisedSyntetic, "Fusiello Uncalib Far To Rectified",
                   new Rectification_FussieloIrsara() { UseInitialCalibration = false }, noiseVariance: v, cameras: RectificationTestUtils.PrepareCalibrationData_FarToRectified());
            }
        }
        
        [TestMethod]
        public void SynteticNoisedEstimation_FusielloUncalibWithInitial()
        {
            double[] variances = { 0, 0.002, 0.005, 0.01, 0.02, 0.05, 0.1 };

            foreach(var v in variances)
            {
                TestCase.AddTestCase(CaseType.NoisedSyntetic, "Fusiello Uncalib Init Almost Rectified",
                   new Rectification_FussieloIrsara() { UseInitialCalibration = true }, noiseVariance: v, cameras: RectificationTestUtils.PrepareCalibrationData_AlmostRectified());
            }
            //foreach(var v in variances)
            //{
            //    TestCase.AddTestCase(CaseType.NoisedSyntetic, "Fusiello Uncalib Init Close To Rectified",
            //       new Rectification_FussieloIrsara() { UseInitialCalibration = true }, noiseVariance: v, cameras: RectificationTestUtils.PrepareCalibrationData_CloseToBeRectified());
            //}
            foreach(var v in variances)
            {
                TestCase.AddTestCase(CaseType.NoisedSyntetic, "Fusiello Uncalib Init Far To Rectified",
                   new Rectification_FussieloIrsara() { UseInitialCalibration = true }, noiseVariance: v, cameras: RectificationTestUtils.PrepareCalibrationData_FarToRectified());
            }
        }

        [TestMethod]
        public void SynteticNoisedEstimation_FusielloCalib()
        {
            double[] variances = { 0, 0.002, 0.005, 0.01, 0.02, 0.05, 0.1 };

            foreach(var v in variances)
            {
                TestCase.AddTestCase(CaseType.NoisedSyntetic, "Fusiello Calib Almost Rectified",
                   new Rectification_FusielloTruccoVerri(), noiseVariance: v, cameras: RectificationTestUtils.PrepareCalibrationData_AlmostRectified());
            }
            //foreach(var v in variances)
            //{
            //    TestCase.AddTestCase(CaseType.NoisedSyntetic, "Fusiello Calib Close To Rectified",
            //       new Rectification_FusielloTruccoVerri() , noiseVariance: v, cameras: RectificationTestUtils.PrepareCalibrationData_CloseToBeRectified());
            //}
            foreach(var v in variances)
            {
                TestCase.AddTestCase(CaseType.NoisedSyntetic, "Fusiello Calib Far To Rectified",
                   new Rectification_FusielloTruccoVerri(), noiseVariance: v, cameras: RectificationTestUtils.PrepareCalibrationData_FarToRectified());
            }
        }

        //[TestMethod]
        public void ImageBaseEstimation()
        { 
            
        }

        public enum CaseType
        {
            IdealVerification,
            NoisedSyntetic,
        }

        class TestCase
        {
            static int _pointsSeed = 5551;
            static int PointsSeed
            {
                get
                {
                    if(_pointsSeed == 0)
                    {
                        _pointsSeed = (int)(new Random().NextDouble() * 10000);
                    }
                    return _pointsSeed;
                }
            }

            public static void AddTestCase(CaseType caseType, string info,
                IRectificationAlgorithm rectComp, int noiseSeed = 0, double noiseVariance = 0.0, CameraPair cameras = null)
            {
                if(caseType == CaseType.IdealVerification)
                {
                    MyContext.RunTest(() =>
                    {
                        return TestIdealRectification(new ImageRectification(rectComp), RectificationTestUtils.PrepareCalibrationData_CloseToBeRectified());
                    }, info);
                }
                else if(caseType == CaseType.NoisedSyntetic)
                {
                    MyContext.RunTest(() =>
                    {
                        MyContext.Output.AppendLine("Noise: " + noiseVariance.ToString("F3"));
                        TestNoisedRectification(new ImageRectification(rectComp), cameras, noiseSeed, noiseVariance);
                        return true;
                    }, info);
                }
            }

            public static bool TestIdealRectification(ImageRectification rect, CameraPair cameras)
            {
                List<Vector2Pair> matchedPairs = RectificationTestUtils.PrepareMatchedPoints(cameras, pointCount: 100, seed: PointsSeed);

                rect.ImageHeight = cameras.Left.ImageHeight;
                rect.ImageWidth = cameras.Right.ImageWidth;
                rect.Cameras = cameras;
                rect.MatchedPairs = matchedPairs;

                rect.ComputeRectificationMatrices();

                var H_r = rect.RectificationRight;
                var H_l = rect.RectificationLeft;
                var estimatedFundamental = H_r.Transpose() * Fi * H_l;
                estimatedFundamental = estimatedFundamental.Divide(estimatedFundamental[2, 2]);

                new NonhorizontalityError(matchedPairs, DenseMatrix.CreateIdentity(3), DenseMatrix.CreateIdentity(3)).Store(MyContext, "Initial");
                new NonhorizontalityError(matchedPairs, H_l, H_r).Store(MyContext, "Final");
                new NonperpendicularityError(H_l, H_r, new Vector2(rect.ImageHeight, rect.ImageWidth)).Store(MyContext);
                new AspectError(H_l, H_r, new Vector2(rect.ImageHeight, rect.ImageWidth)).Store(MyContext);

                RectificationTestUtils.StoreRectificationMatrices(MyContext, H_l, H_r);
                RectificationTestUtils.StoreCamerasInfo(MyContext, cameras, H_l, H_r);

                return true;
            }


            public static bool TestNoisedRectification(ImageRectification rect, CameraPair cameras, int noiseSeed = 0, double noiseRelative = 0)
            {
                List<Vector2Pair> matchedPairs = RectificationTestUtils.PrepareMatchedPoints(cameras, 100, seed: PointsSeed);
                List<Vector2Pair> noisedPairs = new List<Vector2Pair>();

                if(noiseRelative > 0.0)
                {
                    double matVariance = noiseRelative * noiseRelative;
                    double pointsVariance = noiseRelative * noiseRelative * 5000;

                    foreach(var p in matchedPairs)
                    {
                        var p2 = TestUtils.AddNoise(new List<Vector2>() { p.V1, p.V2 }, pointsVariance, noiseSeed);
                        noisedPairs.Add(new Vector2Pair() { V1 = p2[0], V2 = p2[1] });
                    }

                    cameras.Left.Matrix = TestUtils.AddNoise(cameras.Left.Matrix, matVariance, noiseSeed);
                    cameras.Right.Matrix = TestUtils.AddNoise(cameras.Right.Matrix, matVariance, noiseSeed);
                }
                else
                {
                    noisedPairs = matchedPairs;
                }

                cameras.Update();
                rect.ImageHeight = cameras.Left.ImageHeight;
                rect.ImageWidth = cameras.Right.ImageWidth;
                rect.Cameras = cameras;
                rect.MatchedPairs = noisedPairs;

                rect.ComputeRectificationMatrices();

                var H_r = rect.RectificationRight;
                var H_l = rect.RectificationLeft;
                var estimatedFundamental = H_r.Transpose() * Fi * H_l;
                estimatedFundamental = estimatedFundamental.Divide(estimatedFundamental[2, 2]);

                new NonhorizontalityError(matchedPairs, DenseMatrix.CreateIdentity(3), DenseMatrix.CreateIdentity(3)).Store(MyContext, shortVer: true);
                new NonhorizontalityError(matchedPairs, H_l, H_r).Store(MyContext, shortVer: true);
                new NonperpendicularityError(H_l, H_r, new Vector2(rect.ImageHeight, rect.ImageWidth)).Store(MyContext, shortVer: true);
                new AspectError(H_l, H_r, new Vector2(rect.ImageHeight, rect.ImageWidth)).Store(MyContext, shortVer: true);

                RectificationTestUtils.StoreRectificationMatrices(MyContext, H_l, H_r);
                //RectificationTestUtils.StoreCamerasInfo(MyContext, cameras, H_l, H_r);

                return true;
            }
        }
    }
}
