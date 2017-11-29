using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CamAlgorithms.Calibration;
using CamCore;
using CamAlgorithms;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MatrixInfo = CamUnitTest.TestsForThesis.RectificationTestUtils.MatrixInfo;

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
            MyContext.InitTestSet(TestContext.TestName);
        }

        [TestCleanup()]
        public void MyTestCleanup()
        {
            MyContext.StoreTestResults();
        }

        [TestMethod]
        public void VerifyParameterEstimation()
        {
            //TestCase.AddTestCase(CaseType.IdealVerification, "Zhang-Loop",
            //   new ImageRectification_ZhangLoop());
            //TestCase.AddTestCase(CaseType.IdealVerification, "Fusiello Uncalib",
            //   new ImageRectification_FussieloUncalibrated() { UseInitialCalibration = false } );
            //TestCase.AddTestCase(CaseType.IdealVerification, "Fusiello Uncalib With Initial",
            //   new ImageRectification_FussieloUncalibrated() { UseInitialCalibration = true });
            TestCase.AddTestCase(CaseType.IdealVerification, "Fusiello Calib",
               new ImageRectification_FusielloCalibrated());
        }

        public enum CaseType
        {
            IdealVerification,
        }

        class TestCase
        {
            public static void AddTestCase(CaseType caseType, string info,
                ImageRectificationComputer rectComp)
            {
                if(caseType == CaseType.IdealVerification)
                {
                    MyContext.RunTest(() =>
                    {
                        return TestIdealRectification(new ImageRectification(rectComp));
                    }, info);
                }
            }

            public static bool TestIdealRectification(ImageRectification rect)
            {
                CameraPair cameras = RectificationTestUtils.PrepareCalibrationData();
                List<Vector2Pair> matchedPairs = RectificationTestUtils.PrepareMatchedPoints(cameras);
                
                rect.ImageHeight = cameras.Left.ImageHeight;
                rect.ImageWidth = cameras.Right.ImageWidth;
                rect.Cameras = cameras;
                rect.MatchedPairs = matchedPairs;

                rect.ComputeRectificationMatrices();
                
                var H_r = rect.RectificationRight;
                var H_l = rect.RectificationLeft;
                var estimatedFundamental = H_r.Transpose() * Fi * H_l;
                estimatedFundamental = estimatedFundamental.Divide(estimatedFundamental[2, 2]);

                RectificationTestUtils.StoreRectificationMatrices(MyContext, H_l, H_r);
                RectificationTestUtils.StoreCamerasInfo(MyContext, cameras);

                Deviation.Store(MyContext, new NonhorizontalityError(matchedPairs, DenseMatrix.CreateIdentity(3), DenseMatrix.CreateIdentity(3)), "Initial");
                Deviation.Store(MyContext, new NonhorizontalityError(matchedPairs, H_l, H_r), "Final");

                return true;
            }
        }
    }
}
