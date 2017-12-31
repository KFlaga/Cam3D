using CamAlgorithms.ImageMatching;
using CamCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;


namespace CamUnitTest.TestsForThesis
{
    [TestClass]
    public class RefinementsTests
    {
        public TestContext TestContext { get; set; }
        public static Context MyContext { get; set; }

        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
        }

        [TestInitialize()]
        public void MyTestInitialize()
        {
            MyContext = new Context("RefinementsTests");
            MyContext.InitTestSet(TestContext.TestName);
            MyContext.StoreTestBegin();
        }

        [TestCleanup()]
        public void MyTestCleanup()
        {
            MyContext.StoreTestOutput();
        }
        
        [TestMethod]
        public void TestCrossCheck_Motor()
        {
            double[] maxDiffs = new double[] { 0.5, 1.0, 1.5, 2.0, 2.5, 3.0 };

            for(int i = 0; i < maxDiffs.Length; ++i)
            {
                List<DisparityRefinement> refiners = new List<DisparityRefinement>();
                refiners.Add(RefinementTestUtils.PrepareLimitRange(2, 40));
                refiners.Add(RefinementTestUtils.PrepareLowConfidence(0.5));
                refiners.Add(RefinementTestUtils.PrepareCrossCheck(maxDiffs[i]));

                TestCase.AddTestCase(SgmTestUtils.SteroImage.MotorResampled, "MotorResampled", refiners);
            }
        }

        [TestMethod]
        public void TestMedian_Motor()
        {
            List<DisparityRefinement> refiners = new List<DisparityRefinement>();
            refiners.Add(RefinementTestUtils.PrepareLimitRange(2, 40));
            refiners.Add(RefinementTestUtils.PrepareLowConfidence(0.5));
            refiners.Add(RefinementTestUtils.PrepareCrossCheck(1.0));
            refiners.Add(RefinementTestUtils.PrepareMedianFilter());

            TestCase.AddTestCase(SgmTestUtils.SteroImage.MotorResampled, "MotorResampled", refiners);
        }

        [TestMethod]
        public void TestPeakRemoval_NoInterpolate_Motor()
        {
            double[] maxDiffs = new double[15] { 1.0, 1.5, 2.0, 1.0, 1.5, 2.0, 1.0, 1.5, 2.0, 1.0, 1.5, 2.0, 1.0, 1.5, 2.0 };
            int[] minAreas = new int[15] { 2, 2, 2, 4, 4, 4, 6, 6, 6, 8, 8, 8, 10, 10, 10 };

            for(int i = 0; i < maxDiffs.Length; ++i)
            {
                List<DisparityRefinement> refiners = new List<DisparityRefinement>();
                refiners.Add(RefinementTestUtils.PrepareLimitRange(2, 40));
                refiners.Add(RefinementTestUtils.PrepareLowConfidence(0.5));
                refiners.Add(RefinementTestUtils.PrepareCrossCheck(1.0));
                refiners.Add(RefinementTestUtils.PreparePeakRemoval(maxDiffs[i], minAreas[i], false, 0));

                TestCase.AddTestCase(SgmTestUtils.SteroImage.MotorResampled, 
                    "MotorResampled. maxDiff = " + maxDiffs[i].ToString("F1") + ", area = " + minAreas[i], refiners);
            }
        }

        [TestMethod]
        public void TestPeakRemoval_Interpolate_Motor()
        {
            double[] maxDiffs = new double[15] { 1.0, 1.5, 2.0, 1.0, 1.5, 2.0, 1.0, 1.5, 2.0, 1.0, 1.5, 2.0, 1.0, 1.5, 2.0 };
            int[] minAreas = new int[15] { 2, 2, 2, 4, 4, 4, 6, 6, 6, 8, 8, 8, 10, 10, 10 };

            for(int i = 0; i < maxDiffs.Length; ++i)
            {
                List<DisparityRefinement> refiners = new List<DisparityRefinement>();
                refiners.Add(RefinementTestUtils.PrepareLimitRange(2, 40));
                refiners.Add(RefinementTestUtils.PrepareLowConfidence(0.5));
                refiners.Add(RefinementTestUtils.PrepareCrossCheck(1.0));
                refiners.Add(RefinementTestUtils.PreparePeakRemoval(maxDiffs[i], minAreas[i], true, 4));

                TestCase.AddTestCase(SgmTestUtils.SteroImage.MotorResampled,
                    "MotorResampled. maxDiff = " + maxDiffs[i].ToString("F1") + ", area = " + minAreas[i], refiners);
            }
        }

        [TestMethod]
        public void TestDiffusion_Motor()
        {
            // Best between 0.1 and 2.0 -> it does not changes so much
            double[] kernels = new double[3] { 0.5, 0.5, 0.5 };
            // Best around 1
            double[] steps = new double[3] { 0.2, 0.3, 0.4 };

            for(int i = 0; i < kernels.Length; ++i)
            {
                List<DisparityRefinement> refiners = new List<DisparityRefinement>();
                refiners.Add(RefinementTestUtils.PrepareLimitRange(4, 40));
                refiners.Add(RefinementTestUtils.PrepareLowConfidence(0.5));
                refiners.Add(RefinementTestUtils.PrepareCrossCheck(1.0));
                refiners.Add(RefinementTestUtils.PrepareMedianFilter());
                refiners.Add(RefinementTestUtils.PrepareDiffusion(kernels[i], steps[i], 60));
                refiners.Add(RefinementTestUtils.PrepareLimitRange(4, 40));

                TestCase.AddTestCase(SgmTestUtils.SteroImage.MotorResampled,
                    "MotorResampled. kernel = " + kernels[i].ToString("F3") + ", step = " + steps[i].ToString("F3"), refiners);
            }
        }

        [TestMethod]
        public void TestFull_Motor()
        {
            List<DisparityRefinement> refiners = new List<DisparityRefinement>();
            refiners.Add(RefinementTestUtils.PrepareLimitRange(4, 150));
            refiners.Add(RefinementTestUtils.PrepareLowConfidence(0.5));
            refiners.Add(RefinementTestUtils.PrepareCrossCheck(1.0));
            refiners.Add(RefinementTestUtils.PrepareMedianFilter());
            refiners.Add(RefinementTestUtils.PreparePeakRemoval(1.5, 8, true, 4));
            refiners.Add(RefinementTestUtils.PrepareInterpolation(0.5, 0.5, 50));
            refiners.Add(RefinementTestUtils.PrepareLimitRange(4, 150));

            TestCase.AddTestCase(SgmTestUtils.SteroImage.MotorResampled,"MotorResampled", refiners);
        }

        [TestMethod]
        public void TestFull_Pipes()
        {
            List<DisparityRefinement> refiners = new List<DisparityRefinement>();
            refiners.Add(RefinementTestUtils.PrepareLimitRange(5, 150));
            refiners.Add(RefinementTestUtils.PrepareLowConfidence(0.5));
            refiners.Add(RefinementTestUtils.PrepareCrossCheck(1.0));
            refiners.Add(RefinementTestUtils.PrepareMedianFilter());
            refiners.Add(RefinementTestUtils.PreparePeakRemoval(1.5, 8, true, 4));
            refiners.Add(RefinementTestUtils.PrepareInterpolation(0.5, 0.5, 50));
            refiners.Add(RefinementTestUtils.PrepareLimitRange(5, 150));

            TestCase.AddTestCase(SgmTestUtils.SteroImage.PipesResampled, "PipesResampled", refiners);
        }

        class TestCase
        {
            public static void AddTestCase(SgmTestUtils.SteroImage imageType, string info, List<DisparityRefinement> refiners)
            {
                IImage left, right;
                DisparityImage disp;
                RefinementTestUtils.PrepareImages(imageType, out left, out right, out disp);

                DisparityMap mapLeft, mapRight;
                RefinementTestUtils.LoadSgmResultMap(MyContext, imageType, out mapLeft, out mapRight);

                MyContext.RunTest(() =>
                {
                    foreach(var refiner in refiners)
                    {
                        refiner.ImageLeft = left;
                        refiner.ImageRight = right;
                        refiner.MapLeft = mapLeft;
                        refiner.MapRight = mapRight;

                        refiner.RefineMaps();
                        mapLeft = refiner.MapLeft;
                        mapRight = refiner.MapRight;
                    }
                    
                    DateTime time = DateTime.Now;
                    string timestamp = time.Hour + "_" + time.Minute + "_" + time.Second;

                    DisparityMap map2 = (DisparityMap)mapLeft.Clone();
                    DisparityMap map2R = (DisparityMap)mapRight.Clone();
                    for(int r = 0; r < mapLeft.RowCount; ++r)
                    {
                        for(int c = 0; c < mapLeft.ColumnCount; ++c)
                        {
                            map2[r, c].DX = mapLeft[r, c].DX;
                            map2[r, c].SubDX = mapLeft[r, c].SubDX;
                            map2R[r, c].DX = mapRight[r, c].DX;
                            map2R[r, c].SubDX = mapRight[r, c].SubDX;
                        }
                    }
                    
                    SgmTestUtils.StoreDisparityMapAsImage(MyContext, map2, "_left_" + timestamp);
                    SgmTestUtils.StoreDisparityMapAsImage(MyContext, map2R, "_right_" + timestamp);

                    SgmTestUtils.StoreDisparityMapInXml(MyContext, mapLeft,
                       (imageType == SgmTestUtils.SteroImage.PipesResampled ? "_pipes" : "_motor") + "_left_" + timestamp);
                    SgmTestUtils.StoreDisparityMapInXml(MyContext, mapRight,
                        (imageType == SgmTestUtils.SteroImage.PipesResampled ? "_pipes" : "_motor") + "_right_" + timestamp);

                    MyContext.Output.AppendLine("Timestamp: " + timestamp);

                    DisparityMap groundTruth = disp.ToDisparityMap(true);

                    MyContext.Output.AppendLine("Results:");
                    new DisparityMatches(mapLeft, groundTruth).Store(MyContext, false);

                    return true;
                }, info);
            }
        }
    }
}
