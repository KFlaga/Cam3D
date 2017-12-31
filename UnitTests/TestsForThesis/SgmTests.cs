using CamAlgorithms.ImageMatching;
using CamCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;


namespace CamUnitTest.TestsForThesis
{
    [TestClass]
    public class SgmTests
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
            MyContext = new Context("SgmTests");
            MyContext.InitTestSet(TestContext.TestName);
            MyContext.StoreTestBegin();
        }

        [TestCleanup()]
        public void MyTestCleanup()
        {
            MyContext.StoreTestOutput();
        }

        [TestMethod]
        public void TestAlfaBeta_Pipes()
        {
            double[] lowCoeff = new double[1] { 0.02 };
            double[] highCoeff = new double[1] { 0.03 };
            //double[] lowCoeff = new double[17] { 0.005, 0.02, 0.05, 0.1, 0.15, 0.2, 0.005, 0.02, 0.05, 0.1, 0.15, 0.2, 0.02, 0.05, 0.1, 0.15, 0.2 };
            //double[] highCoeff = new double[17] { 0.005, 0.02, 0.05, 0.1, 0.15, 0.2, 0.01, 0.04, 0.1, 0.2, 0.3, 0.4, 0.06, 0.15, 0.3, 0.45, 0.6 };
            double imageTreshold = 0.1;
            int censusRadius = 6;
            double confidenceCoeff = 3;

            for(int i = 0; i < lowCoeff.Length; ++i)
            {
                TestCase.AddTestCase(SgmTestUtils.SteroImage.PipesResampled, "PipesResampled",
                    lowCoeff[i], highCoeff[i], imageTreshold, censusRadius, confidenceCoeff);
                MyContext.StoreTestOutput();
            }
        }

        [TestMethod]
        public void TestAlfaBeta_Motor()
        {
            double[] lowCoeff = new double[1] { 0.02 };
            double[] highCoeff = new double[1] { 0.03 };
            //double[] lowCoeff = new double[17] { 0.005, 0.02, 0.05, 0.1, 0.15, 0.2, 0.005, 0.02, 0.05, 0.1, 0.15, 0.2, 0.02, 0.05, 0.1, 0.15, 0.2 };
            //double[] highCoeff = new double[17] { 0.005, 0.02, 0.05, 0.1, 0.15, 0.2, 0.01, 0.04, 0.1, 0.2, 0.3, 0.4, 0.06, 0.15, 0.3, 0.45, 0.6 };
            double imageTreshold = 0.1;
            int censusRadius = 6;
            double confidenceCoeff = 3;

            for(int i = 0; i < lowCoeff.Length; ++i)
            {
                TestCase.AddTestCase(SgmTestUtils.SteroImage.MotorResampled, "MotorResampled",
                    lowCoeff[i], highCoeff[i], imageTreshold, censusRadius, confidenceCoeff);
                MyContext.StoreTestOutput();
            }
        }

        [TestMethod]
        public void TestImageTreshold_LowAlfa_Pipes()
        {
            double lowCoeff = 0.005;
            double highCoeff = 0.005;
            double[] imageTreshold = new double[3] { 0.01, 0.1, 0.90 };
            int censusRadius = 6;
            double confidenceCoeff = 3;

            for(int i = 0; i < imageTreshold.Length; ++i)
            {
                TestCase.AddTestCase(SgmTestUtils.SteroImage.PipesResampled, "PipesResampled",
                    lowCoeff, highCoeff, imageTreshold[i], censusRadius, confidenceCoeff);
                MyContext.StoreTestOutput();
            }
        }

        [TestMethod]
        public void TestImageTreshold_LowAlfa_Motor()
        {
            double lowCoeff = 0.005;
            double highCoeff = 0.005;
            double[] imageTreshold = new double[3] { 0.01, 0.1, 0.90 };
            int censusRadius = 6;
            double confidenceCoeff = 3;

            for(int i = 0; i < imageTreshold.Length; ++i)
            {
                TestCase.AddTestCase(SgmTestUtils.SteroImage.MotorResampled, "MotorResampled",
                    lowCoeff, highCoeff, imageTreshold[i], censusRadius, confidenceCoeff);
                MyContext.StoreTestOutput();
            }
        }

        [TestMethod]
        public void TestImageTreshold_HighAlfa_Pipes()
        {
            double lowCoeff = 0.2;
            double highCoeff = 0.4;
            double[] imageTreshold = new double[3] { 0.01, 0.1, 0.90 };
            int censusRadius = 6;
            double confidenceCoeff = 3;

            for(int i = 0; i < imageTreshold.Length; ++i)
            {
                TestCase.AddTestCase(SgmTestUtils.SteroImage.PipesResampled, "PipesResampled",
                    lowCoeff, highCoeff, imageTreshold[i], censusRadius, confidenceCoeff);
                MyContext.StoreTestOutput();
            }
        }

        [TestMethod]
        public void TestImageTreshold_HighAlfa_Motor()
        {
            double lowCoeff = 0.2;
            double highCoeff = 0.4;
            double[] imageTreshold = new double[3] { 0.01, 0.1, 0.90 };
            int censusRadius = 6;
            double confidenceCoeff = 3;

            for(int i = 0; i < imageTreshold.Length; ++i)
            {
                TestCase.AddTestCase(SgmTestUtils.SteroImage.MotorResampled, "MotorResampled",
                    lowCoeff, highCoeff, imageTreshold[i], censusRadius, confidenceCoeff);
                MyContext.StoreTestOutput();
            }
        }

        [TestMethod]
        public void TestCensusRadius_Pipes()
        {
            double lowCoeff = 0.1;
            double highCoeff = 0.2;
            double imageTreshold = 0.1;
            int[] censusRadius = new int[6] { 2, 3, 4, 5, 6, 7 };
            double confidenceCoeff = 3;

            for(int i = 0; i < censusRadius.Length; ++i)
            {
                TestCase.AddTestCase(SgmTestUtils.SteroImage.PipesResampled, "PipesResampled",
                    lowCoeff, highCoeff, imageTreshold, censusRadius[i], confidenceCoeff);
                MyContext.StoreTestOutput();
            }
        }

        [TestMethod]
        public void TestCensusRadius_Motor()
        {
            double lowCoeff = 0.1;
            double highCoeff = 0.2;
            double imageTreshold = 0.1;
            int[] censusRadius = new int[6] { 2, 3, 4, 5, 6, 7 };
            double confidenceCoeff = 3;

            for(int i = 0; i < censusRadius.Length; ++i)
            {
                TestCase.AddTestCase(SgmTestUtils.SteroImage.MotorResampled, "MotorResampled",
                    lowCoeff, highCoeff, imageTreshold, censusRadius[i], confidenceCoeff);
                MyContext.StoreTestOutput();
            }
        }

        [TestMethod]
        public void TestConfidenceCoeff_Pipes()
        {
            double lowCoeff = 0.1;
            double highCoeff = 0.2;
            double imageTreshold = 0.1;
            int censusRadius = 6;
            double[] confidenceCoeff = new double[8] { 1.0, 2.0, 2.5, 3.0, 3.5, 4.0, 5.0, 6.0 };

            for(int i = 0; i < confidenceCoeff.Length; ++i)
            {
                TestCase.AddTestCase(SgmTestUtils.SteroImage.PipesResampled, "PipesResampled",
                    lowCoeff, highCoeff, imageTreshold, censusRadius, confidenceCoeff[i]);
                MyContext.StoreTestOutput();
            }
        }

        [TestMethod]
        public void TestConfidenceCoeff_Motor()
        {
            double lowCoeff = 0.1;
            double highCoeff = 0.2;
            double imageTreshold = 0.1;
            int censusRadius = 6;
            double[] confidenceCoeff = new double[8] { 1.0, 2.0, 2.5, 3.0, 3.5, 4.0, 5.0, 6.0 };

            for(int i = 0; i < confidenceCoeff.Length; ++i)
            {
                TestCase.AddTestCase(SgmTestUtils.SteroImage.MotorResampled, "MotorResampled",
                    lowCoeff, highCoeff, imageTreshold, censusRadius, confidenceCoeff[i]);
                MyContext.StoreTestOutput();
            }
        }

        class TestCase
        {
            public static void AddTestCase(SgmTestUtils.SteroImage imageType, string info,
                double lowPenaltyCoeff, double highPenaltyCoeff, double imagePenatlyTreshold,
                int censusRadius, double disparityConfidenceCoeff)
            {
                IImage left, right;
                DisparityImage disp;
                SgmTestUtils.PrepareImages(imageType, out left, out right, out disp);

                SgmTestUtils.SaveImage(left, "d:/xxx.png");

                SgmAlgorithm sgm = new SgmAlgorithm()
                {
                    ImageLeft = left,
                    ImageRight = right
                };

                MyContext.RunTest(() =>
                {
                    var sgmAgg = new SgmAggregator()
                    {
                        LowPenaltyCoeff = lowPenaltyCoeff,
                        HighPenaltyCoeff = highPenaltyCoeff,
                        IntensityThreshold = imagePenatlyTreshold
                    };
                    sgmAgg.CostComp = new CensusCostComputer()
                    {
                        HeightRadius = censusRadius,
                        WidthRadius = censusRadius
                    };
                    sgmAgg.DispComp = new SgmDisparityComputer()
                    {
                        CostMethod = SgmDisparityComputer.CostMethods.DistanceToMean,
                        CostMethodPower = disparityConfidenceCoeff,
                        MeanMethod = SgmDisparityComputer.MeanMethods.SimpleAverage
                    };
                    sgm.Aggregator = sgmAgg;

                    sgm.MatchImages();

                    DisparityMap mapLeft, mapRight;
                    InvalidateBadDisparities(sgm, out mapLeft, out mapRight);

                    SgmTestUtils.StoreDisparityMapInXml(MyContext, mapLeft,
                        (imageType == SgmTestUtils.SteroImage.PipesResampled ? "_pipes" : "_motor") + "_left");
                    SgmTestUtils.StoreDisparityMapInXml(MyContext, mapRight,
                        (imageType == SgmTestUtils.SteroImage.PipesResampled ? "_pipes" : "_motor") + "_right");

                    DateTime time = DateTime.Now;
                    string timestamp = time.Hour + "_" + time.Minute + "_" + time.Second;

                    DisparityMap map2 = (DisparityMap)mapLeft.Clone();
                    DisparityMap map2R = (DisparityMap)mapRight.Clone();
                    //for(int r = 0; r < mapLeft.RowCount; ++r)
                    //{
                    //    for(int c = 0; c < mapLeft.ColumnCount; ++c)
                    //    {
                    //        map2[r, c].DX = mapLeft[r, c].DX * 2;
                    //        map2[r, c].SubDX = mapLeft[r, c].SubDX * 2;
                    //        map2R[r, c].DX = mapRight[r, c].DX * 2;
                    //        map2R[r, c].SubDX = mapRight[r, c].SubDX * 2;
                    //    }
                    //}

                    SgmTestUtils.StoreDisparityMapAsImage(MyContext, map2, "_left_" + timestamp, sgm.MapLeft.ColumnCount / 2);
                    SgmTestUtils.StoreDisparityMapAsImage(MyContext, map2R, "_right_" + timestamp, sgm.MapRight.ColumnCount / 2);

                    MyContext.Output.AppendLine("Timestamp: " + timestamp);
                    MyContext.Output.AppendLine("Params:");
                    MyContext.Output.AppendLine("alfa: " + lowPenaltyCoeff.ToString("F3"));
                    MyContext.Output.AppendLine("beta_1: " + highPenaltyCoeff.ToString("F3"));
                    MyContext.Output.AppendLine("tI: " + imagePenatlyTreshold.ToString("F3"));
                    MyContext.Output.AppendLine("census: " + censusRadius.ToString());
                    MyContext.Output.AppendLine("lambda: " + disparityConfidenceCoeff.ToString("F1"));

                    DisparityMap groundTruth = disp.ToDisparityMap(true);

                    MyContext.Output.AppendLine("Results:");
                    new DisparityMatches(mapLeft, groundTruth).Store(MyContext, true);
                    return true;
                }, info);
            }

            private static void InvalidateBadDisparities(SgmAlgorithm sgm, out DisparityMap mapLeft, out DisparityMap mapRight)
            {
                LimitRangeRefiner limitRange = new LimitRangeRefiner()
                {
                    MapLeft = sgm.MapLeft,
                    MapRight = sgm.MapRight,
                    MaxDisparity = 150,
                    MinDisparity = 3
                };
                limitRange.RefineMaps();
                mapLeft = limitRange.MapLeft;
                mapRight = limitRange.MapRight;
                InvalidateLowConfidenceRefiner lowConfidence = new InvalidateLowConfidenceRefiner()
                {
                    MapLeft = mapLeft,
                    MapRight = mapRight,
                    ConfidenceTreshold = 0.33
                };
                lowConfidence.RefineMaps();
                mapLeft = lowConfidence.MapLeft;
                mapRight = lowConfidence.MapRight;
            }
        }
    }
}
