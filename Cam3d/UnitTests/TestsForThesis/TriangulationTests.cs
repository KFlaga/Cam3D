using CamAlgorithms.Calibration;
using CamAlgorithms.Triangulation;
using CamCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CamUnitTest.TestsForThesis
{
    [TestClass]
    public class TriangulationTests
    {
        public TestContext TestContext { get; set; }
        public static Context MyContext { get; set; }

        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            MyContext = new Context("TriangulationTests");
        }

        [TestInitialize()]
        public void MyTestInitialize()
        {
            MyContext = new Context("TriangulationTests");
            MyContext.InitTestSet(TestContext.TestName);
        }

        [TestCleanup()]
        public void MyTestCleanup()
        {
            MyContext.StoreTestOutput();
        }

        [TestMethod]
        public void AlmostFull()
        {
            double[] deviations = new double[] { 0,  0.002, 0.005, 0.01, 0.02, 0.05, 0.1 };
            foreach(double d in deviations)
            {
                TestCase.AddTestCase(CaseType.AlmostFull, "AlmostFull", noiseRelative: d);
            }
        }

        [TestMethod]
        public void FarFull()
        {
            double[] deviations = new double[] { 0,  0.002, 0.005, 0.01, 0.02, 0.05, 0.1 };
            foreach(double d in deviations)
            {
                TestCase.AddTestCase(CaseType.FarFull, "FarFull", noiseRelative: d);
            }
        }

        [TestMethod]
        public void FarFullImageNoise()
        {
            double[] deviations = new double[] { 0, 0.002, 0.005, 0.01, 0.02, 0.05, 0.1 };
            foreach(double d in deviations)
            {
                TestCase.AddTestCase(CaseType.FarFullImageNoise, "FarFullImageNoise", noiseRelative: d);
            }
        }

        [TestMethod]
        public void FarFullMatrixNoise()
        {
            double[] deviations = new double[] { 0, 0.002, 0.005, 0.01, 0.02, 0.05, 0.1 };
            foreach(double d in deviations)
            {
                TestCase.AddTestCase(CaseType.FarFullMatrixNoise, "FarFullMatrixNoise", noiseRelative: d);
            }
        }

        [TestMethod]
        public void AlmostLinear()
        {
            double[] deviations = new double[] { 0,  0.002, 0.005, 0.01, 0.02, 0.05, 0.1 };
            foreach(double d in deviations)
            {
                TestCase.AddTestCase(CaseType.AlmostLinear, "AlmostLinear", noiseRelative: d);
            }
        }

        [TestMethod]
        public void FarLinear()
        {
            double[] deviations = new double[] { 0,  0.002, 0.005, 0.01, 0.02, 0.05, 0.1 };
            foreach(double d in deviations)
            {
                TestCase.AddTestCase(CaseType.FarLinear, "FarLinear", noiseRelative: d);
            }
        }
        [TestMethod]
        public void TestRealImages()
        {
            TestCase.AddTestCase(CaseType.RealImages, "Real Images");
        }
        public enum CaseType
        {
            AlmostFull,
            FarFull,
            AlmostLinear,
            FarLinear,
            FarFullImageNoise,
            FarFullMatrixNoise,
            RealImages
        }

        class TestCase
        {
            static int _pointsSeed = 3457;
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

            public static void AddTestCase(CaseType caseType, string info, double noiseRelative = 0)
            {
                if(caseType == CaseType.AlmostFull)
                {
                    MyContext.RunTest(() =>
                    {
                        return TestNoisedTriangulation(new TriangulationAlgorithm()
                        {
                            Method = TriangulationAlgorithm.TriangulationMethod.TwoPointsEpilineFit
                        }, TriangulationTestUtils.PrepareCalibrationData_AlmostRectified(), noiseRelative, noiseRelative);
                    }, info);
                }
                if(caseType == CaseType.FarFull)
                {
                    MyContext.RunTest(() =>
                    {
                        return TestNoisedTriangulation(new TriangulationAlgorithm()
                        {
                            Method = TriangulationAlgorithm.TriangulationMethod.TwoPointsEpilineFit
                        }, TriangulationTestUtils.PrepareCalibrationData_FarToRectified(), noiseRelative, noiseRelative);
                    }, info);
                }
                if(caseType == CaseType.FarFullImageNoise)
                {
                    MyContext.RunTest(() =>
                    {
                        return TestNoisedTriangulation(new TriangulationAlgorithm()
                        {
                            Method = TriangulationAlgorithm.TriangulationMethod.TwoPointsEpilineFit
                        }, TriangulationTestUtils.PrepareCalibrationData_FarToRectified(), noiseRelative, 0.0);
                    }, info);
                }
                if(caseType == CaseType.FarFullMatrixNoise)
                {
                    MyContext.RunTest(() =>
                    {
                        return TestNoisedTriangulation(new TriangulationAlgorithm()
                        {
                            Method = TriangulationAlgorithm.TriangulationMethod.TwoPointsEpilineFit
                        }, TriangulationTestUtils.PrepareCalibrationData_FarToRectified(), 0.0, noiseRelative);
                    }, info);
                }
                if(caseType == CaseType.AlmostLinear)
                {
                    MyContext.RunTest(() =>
                    {
                        return TestNoisedTriangulation(new TriangulationAlgorithm()
                        {
                            Method = TriangulationAlgorithm.TriangulationMethod.TwoPointsLinear
                        }, TriangulationTestUtils.PrepareCalibrationData_AlmostRectified(), noiseRelative, noiseRelative);
                    }, info);
                }
                if(caseType == CaseType.FarLinear)
                {
                    MyContext.RunTest(() =>
                    {
                        return TestNoisedTriangulation(new TriangulationAlgorithm()
                        {
                            Method = TriangulationAlgorithm.TriangulationMethod.TwoPointsLinear
                        }, TriangulationTestUtils.PrepareCalibrationData_FarToRectified(), noiseRelative, noiseRelative);
                    }, info);
                }
                if(caseType == CaseType.RealImages)
                {
                    MyContext.RunTest(() =>
                    {
                        TestRealImages();
                        return true;
                    }, info);
                }
            }

            public static bool TestNoisedTriangulation(TriangulationAlgorithm triangulation, CameraPair cameras,
                double noiseImage = 0, double noiseMatrix = 0)
            {
                List<TriangulatedPoint> idealPoints = TriangulationTestUtils.PrepareTriangulatedPoints(cameras, 100, seed: PointsSeed);
                List<TriangulatedPoint> noisedPoints = new List<TriangulatedPoint>();
                CameraPair noisedCameras = cameras.Clone();

                if(noiseImage > 0.0)
                {
                    double pointsVariance = noiseImage * noiseImage * 10000;
                    noisedPoints = TriangulationTestUtils.AddNoise(idealPoints, pointsVariance, PointsSeed * 3);
                }
                else
                {
                    foreach(var p in idealPoints)
                    {
                        noisedPoints.Add(new TriangulatedPoint()
                        {
                            ImageLeft = new Vector2(p.ImageLeft),
                            ImageRight = new Vector2(p.ImageRight)
                        });
                    }
                }

                if(noiseMatrix > 0.0)
                {
                    double matVariance = noiseMatrix * noiseMatrix;
                    noisedCameras.Left.Matrix = TestUtils.AddNoise(cameras.Left.Matrix, matVariance, PointsSeed * 5);
                    noisedCameras.Right.Matrix = TestUtils.AddNoise(cameras.Right.Matrix, matVariance, PointsSeed * 7);
                }

                noisedCameras.Update();
                cameras.Update();
                triangulation.Cameras = noisedCameras;

                var inputPoints = new List<TriangulatedPoint>();
                foreach(var p in noisedPoints)
                {
                    inputPoints.Add(new TriangulatedPoint()
                    {
                        ImageLeft = new Vector2(p.ImageLeft),
                        ImageRight = new Vector2(p.ImageRight)
                    });
                }

                triangulation.Points = inputPoints;
                triangulation.Find3DPoints();
                
                MyContext.Output.AppendLine("Noise Image = " + noiseImage.ToString());
                MyContext.Output.AppendLine("Noise Matrix = " + noiseMatrix.ToString());
                new ReprojectionErrorForCameraPair(cameras.Left.Matrix, cameras.Right.Matrix, triangulation.Points).Store(MyContext, "Cameras ideal", shortVer: true);
                new ReprojectionErrorForCameraPair(noisedCameras.Left.Matrix, noisedCameras.Right.Matrix, triangulation.Points).Store(MyContext, "Cameras noised", shortVer: true);
                new ReconstructionError(idealPoints, triangulation.Points).Store(MyContext, shortVer: true);

                //TriangulationTestUtils.StoreCamerasInfo(MyContext, noisedCameras);

                return true;
            }

            public static void TestRealImages()
            {
                foreach(var realCase in Enum.GetValues(typeof(TriangulationTestUtils.RealCase)).Cast<TriangulationTestUtils.RealCase>())
                {
                    CameraPair cameras;
                    List<TriangulatedPoint> calibration;
                    List<TriangulatedPoint> reconstructed;
                    TriangulationTestUtils.LoadRealData(MyContext, realCase, out cameras, out calibration, out reconstructed);

                    List<TriangulatedPoint> testPoints = new List<TriangulatedPoint>();
                    for(int i = 0; i < calibration.Count; ++i)
                    {
                        testPoints.Add(new TriangulatedPoint()
                        {
                            ImageLeft = new Vector2(calibration[i].ImageLeft),
                            ImageRight = new Vector2(calibration[i].ImageRight),
                            Real = new Vector3(reconstructed[i].Real)
                        });
                    }

                    MyContext.Output.AppendLine("CASE: " + realCase.ToString());
                    new ReprojectionErrorForCameraPair(cameras.Left.Matrix, cameras.Right.Matrix, calibration).Store(MyContext, "Calibration", shortVer: false);
                    new ReprojectionErrorForCameraPair(cameras.Left.Matrix, cameras.Right.Matrix, reconstructed).Store(MyContext, "Reconstructed", shortVer: false);
                    new ReprojectionErrorForCameraPair(cameras.Left.Matrix, cameras.Right.Matrix, testPoints).Store(MyContext, "Test", shortVer: false);
                    new ReconstructionError(calibration, reconstructed).Store(MyContext, shortVer: false);
                }
            }
        }
    }
}
