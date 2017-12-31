using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CamAlgorithms.Calibration;
using CamCore;
using CamAlgorithms;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Linq;

namespace CamUnitTest.TestsForThesis
{
    [TestClass]
    public class CalibrationTests
    {
        public TestContext TestContext { get; set; }
        public static Context MyContext { get; set; }
        
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            MyContext = new Context("CalibrationTests");
        }

        [TestInitialize()]
        public void MyTestInitialize()
        {
            MyContext = new Context("CalibrationTests");
            MyContext.InitTestSet(TestContext.TestName);
        }

        [TestCleanup()]
        public void MyTestCleanup()
        {
            MyContext.StoreTestOutput();
        }
        
        [TestMethod]
        public void CalibStandardTestFar()
        {
            double[] deviations = new double[] { 0, 0.001, 0.002, 0.005, 0.01, 0.02, 0.05, 0.1 };
            foreach(double d in  deviations)
            {
                TestCase.AddTestCase(CaseType.CalibStandardTestFar, "CalibStandardTestFar", noiseDeviation: d);
            }
        }
        
        [TestMethod]
        public void PointsStandard()
        {
            double[] deviations = new double[] { 0, 0.001, 0.002, 0.005, 0.01, 0.02, 0.05, 0.1 };
            foreach(double d in deviations)
            {
                TestCase.AddTestCase(CaseType.PointsStandard, "PointsStandard", noiseDeviation: d);
            }
        }

        [TestMethod]
        public void PointsOnSide()
        {
            double[] deviations = new double[] { 0, 0.001, 0.002, 0.005, 0.01, 0.02, 0.05, 0.1 };
            foreach(double d in deviations)
            {
                TestCase.AddTestCase(CaseType.PointsOnSide, "PointsOnSide", noiseDeviation: d);
            }
        }

        [TestMethod]
        public void PointsFar()
        {
            double[] deviations = new double[] { 0, 0.001, 0.002, 0.005, 0.01, 0.02, 0.05, 0.1 };
            foreach(double d in deviations)
            {
                TestCase.AddTestCase(CaseType.PointsFar, "PointsFar", noiseDeviation: d);
            }
        }

        [TestMethod]
        public void CalibStandardTestSide()
        {
            double[] deviations = new double[] { 0, 0.001, 0.002, 0.005, 0.01, 0.02, 0.05, 0.1 };
            foreach(double d in deviations)
            {
                TestCase.AddTestCase(CaseType.CalibStandardTestSide, "CalibStandardTestSide", noiseDeviation: d);
            }
        }

        public enum CaseType
        {
            CompareAlgorithms,
            PointsStandard,
            PointsOnSide,
            PointsFar,
            CalibStandardTestFar,
            CalibStandardTestSide,
        }

        class TestCase
        {
            static int _noiseSeed = 5433;
            static int NoiseSeed
            {
                get
                {
                    if(_noiseSeed == 0)
                    {
                        _noiseSeed = (int)(new Random().NextDouble() * 10000);
                    }
                    return _noiseSeed;
                }
            }
            
            public static void AddTestCase(CaseType caseType, string info, double noiseDeviation = 0.0)
            {
                if(caseType == CaseType.PointsStandard)
                {
                    MyContext.RunTest(() =>
                    {
                        return PerformTestForGivenData(new CalibrationWithGrids(), 
                            CalibrationTestUtils.PrepareCamera(), 
                            CalibrationTestUtils.PrepareCalibrationGrids(10, 10, CalibrationTestUtils.GridRange.Standard),
                            CalibrationTestUtils.PrepareCalibrationGrids(10, 10, CalibrationTestUtils.GridRange.Standard),
                            imgDeviation: noiseDeviation, realDeviation: noiseDeviation, noiseSeed: NoiseSeed);
                    }, info);
                }
                if(caseType == CaseType.CalibStandardTestFar)
                {
                    MyContext.RunTest(() =>
                    {
                        return PerformTestForGivenData(new CalibrationWithGrids(),
                            CalibrationTestUtils.PrepareCamera(),
                            CalibrationTestUtils.PrepareCalibrationGrids(10, 10, CalibrationTestUtils.GridRange.Standard),
                            CalibrationTestUtils.PrepareCalibrationGrids(10, 10, CalibrationTestUtils.GridRange.Far),
                            imgDeviation: noiseDeviation, realDeviation: noiseDeviation, noiseSeed: NoiseSeed);
                    }, info);
                }
                if(caseType == CaseType.CalibStandardTestSide)
                {
                    MyContext.RunTest(() =>
                    {
                        return PerformTestForGivenData(new CalibrationWithGrids(),
                            CalibrationTestUtils.PrepareCamera(),
                            CalibrationTestUtils.PrepareCalibrationGrids(10, 10, CalibrationTestUtils.GridRange.Standard),
                            CalibrationTestUtils.PrepareCalibrationGrids(10, 10, CalibrationTestUtils.GridRange.Side),
                            imgDeviation: noiseDeviation, realDeviation: noiseDeviation, noiseSeed: NoiseSeed);
                    }, info);
                }
                if(caseType == CaseType.PointsOnSide)
                {
                    MyContext.RunTest(() =>
                    {
                        return PerformTestForGivenData(new CalibrationWithGrids(),
                            CalibrationTestUtils.PrepareCamera(),
                            CalibrationTestUtils.PrepareCalibrationGrids(10, 10, CalibrationTestUtils.GridRange.Side),
                            CalibrationTestUtils.PrepareCalibrationGrids(10, 10, CalibrationTestUtils.GridRange.Side),
                            imgDeviation: noiseDeviation, realDeviation: noiseDeviation, noiseSeed: NoiseSeed);
                    }, info);
                }
                if(caseType == CaseType.PointsFar)
                {
                    MyContext.RunTest(() =>
                    {
                        return PerformTestForGivenData(new CalibrationWithGrids(),
                            CalibrationTestUtils.PrepareCamera(),
                            CalibrationTestUtils.PrepareCalibrationGrids(10, 10, CalibrationTestUtils.GridRange.Far),
                            CalibrationTestUtils.PrepareCalibrationGrids(10, 10, CalibrationTestUtils.GridRange.Far),
                            imgDeviation: noiseDeviation, realDeviation: noiseDeviation, noiseSeed: NoiseSeed);
                    }, info);
                }
            }

            public static bool PerformTestForGivenData(CalibrationAlgorithm calib, 
                Camera idealCamera, List<RealGridData> gridsForCalibration, List<RealGridData> gridsForTests,
                double imgDeviation = 0.0, double realDeviation = 0.0, bool noiseGrids = true, int noiseSeed = 0)
            {
                List<CalibrationPoint> idealPoints = CalibrationTestUtils.PrepareCalibrationPoints(idealCamera, gridsForCalibration);
                List<CalibrationPoint> testPoints = CalibrationTestUtils.PrepareCalibrationPoints(idealCamera, gridsForTests);
                List<CalibrationPoint> noisedPoints;
                List<RealGridData> noisedGrids;

                double imageVaraition = imgDeviation * imgDeviation * 100 * 100;
                double realVaraition = realDeviation * realDeviation * 200 * 200;

                calib.UseCovarianceMatrix = true;
                SetVariances(calib, imageVaraition, realVaraition);

                if(noiseGrids)
                {
                    noisedGrids = CalibrationTestUtils.AddGaussNoise(gridsForCalibration, realVaraition, noiseSeed);
                    noisedPoints = CalibrationTestUtils.AddGaussNoise(idealPoints, imageVaraition, 0.0, noiseSeed);
                    foreach(var cp in noisedPoints)
                    {
                        cp.Real = noisedGrids[cp.GridNum].GetRealFromCell(cp.RealGridPos);
                    }
                }
                else
                {
                    noisedPoints = CalibrationTestUtils.AddGaussNoise(idealPoints, imageVaraition, realVaraition, noiseSeed);
                    noisedGrids = gridsForCalibration;
                }

                Camera estimated = new Camera();
                calib.Camera = estimated;
                calib.Grids = noisedGrids;
                calib.Points = noisedPoints;

                calib.EliminateOuliers = true;
                calib.OutliersCoeff = 2;
                calib.NormalizeLinear = true;
                calib.NormalizeIterative = true;
                calib.LinearOnly = false;
                calib.MaxIterations = 200;
                calib.MinimalizeSkew = true;

                if(calib as CalibrationWithGrids != null)
                {
                    ((CalibrationWithGrids)calib).UseExplicitParametrization = false;
                }

                calib.Calibrate();

                MyContext.Output.AppendLine("Noise Image: " + imgDeviation.ToString("F3") + " = " + (200 * imgDeviation).ToString("F3"));
                //MyContext.Output.AppendLine("Noise Real: " + realDeviation.ToString("F3") + " = " + (100 * realDeviation).ToString("F3"));
                new ReprojectionErrorForOneCamera(calib.LinearEstimation.Matrix, testPoints).Store(MyContext, "Linear", shortVer: true);
                new ReprojectionErrorForOneCamera(estimated.Matrix, testPoints).Store(MyContext, "Final", shortVer: true);
                //CalibrationTestUtils.StoreCameraInfo(MyContext, idealCamera, estimated, shortVer: true);
                //CalibrationTestUtils.StoreMinimalizationInfo(MyContext, calib, shortVer: true);

                return true;
            }

            private static void SetVariances(CalibrationAlgorithm calib, double imageVaraition, double realVaraition)
            {
                if(imageVaraition > 0)
                {
                    calib.ImageMeasurementVariance_X = imageVaraition;
                    calib.ImageMeasurementVariance_Y = imageVaraition;
                }
                else
                {
                    calib.ImageMeasurementVariance_X = 1e-6;
                    calib.ImageMeasurementVariance_Y = 1e-6;
                }
                if(realVaraition > 0)
                {
                    calib.RealMeasurementVariance_X = realVaraition;
                    calib.RealMeasurementVariance_Y = realVaraition;
                    calib.RealMeasurementVariance_Z = realVaraition;
                }
                else
                {
                    calib.RealMeasurementVariance_X = 1e-6;
                    calib.RealMeasurementVariance_Y = 1e-6;
                    calib.RealMeasurementVariance_Z = 1e-6;
                }
            }
        }


        //double minX = 1e12, minY = 1e12, maxX = 0, maxY = 0;
        //foreach(var cp in idealPoints)
        //{
        //    minX = Math.Min(minX, cp.ImgX);
        //    minY = Math.Min(minY, cp.ImgY);
        //    maxX = Math.Max(maxX, cp.ImgX);
        //    maxY = Math.Max(maxY, cp.ImgY);
        //}
}
}