using CamAlgorithms.Calibration;
using CamAlgorithms.Triangulation;
using CamCore;
using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CamUnitTest.TestsForThesis
{
    //[TestClass] -> needs generation of maps and triangulated points first
    public class PrepareVisualisationData
    {
        public TestContext TestContext { get; set; }
        public static Context MyContext { get; set; }

        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            MyContext = new Context("PrepareVisualisationData");
        }

        string Directory { get { return MyContext.ResultDirectory + "\\middlebury\\"; } }
        
        [TestMethod]
        public void PrepareMotorLowAlfa()
        {
            CameraPair cameras = PrepareCamerasForMotor();
            string mapPath = Directory + "disparity_map_motor_low_alfa_s.xml";
            string outPath = Directory + "points3d_motor_low_alfa_s.xml";
            Save3dPoints(cameras, mapPath, outPath);
        }

        [TestMethod]
        public void PrepareMotorHighAlfa()
        {
            CameraPair cameras = PrepareCamerasForMotor();
            string mapPath = Directory + "disparity_map_motor_high_alfa_s.xml";
            string outPath = Directory + "points3d_motor_high_alfa_s.xml";
            Save3dPoints(cameras, mapPath, outPath);
        }

        [TestMethod]
        public void PreparePipesLowAlfa()
        {
            CameraPair cameras = PrepareCamerasForMotor();
            string mapPath = Directory + "disparity_map_pipes_low_alfa_s.xml";
            string outPath = Directory + "points3d_pipes_low_alfa_s.xml";
            Save3dPoints(cameras, mapPath, outPath);
        }

        [TestMethod]
        public void PreparePipesHighAlfa()
        {
            CameraPair cameras = PrepareCamerasForMotor();
            string mapPath = Directory + "disparity_map_pipes_high_alfa_s.xml";
            string outPath = Directory + "points3d_pipes_high_alfa_s.xml";
            Save3dPoints(cameras, mapPath, outPath);
        }

        [TestMethod]
        public void PrepareMotorIdeal()
        {
            CameraPair cameras = PrepareCamerasForMotor();

            DisparityImage disp = SgmTestUtils.LoadImage_MotorDisparity();
            DisparityMap map = disp.ToDisparityMap(true);
            
            string outPath = Directory + "points3d_motor_ideal_s.xml";
            string mapPath = Directory + "disparity_map_motor_ideal_s.xml";
            Save3dPoints(cameras, map, outPath);
            SgmTestUtils.SaveMapXml(map, mapPath);
        }

        [TestMethod]
        public void PreparePipesIdeal()
        {
            CameraPair cameras = PrepareCamerasForMotor();

            DisparityImage disp = SgmTestUtils.LoadImage_PipesDisparity();
            DisparityMap map = disp.ToDisparityMap(true);

            string outPath = Directory + "points3d_pipes_ideal.xml";
            string mapPath = Directory + "disparity_map_pipes_ideal.xml";
            Save3dPoints(cameras, map, outPath);
            SgmTestUtils.SaveMapXml(map, mapPath);
        }

        public void Save3dPoints(CameraPair cameras, string mapPath, string outPath)
        {
            DisparityMap map = RefinementTestUtils.LoadMapXml(mapPath);
            Save3dPoints(cameras, map, outPath);
        }

        public void Save3dPoints(CameraPair cameras, DisparityMap map, string outPath)
        {
            List<TriangulatedPoint> points = TriangulationTestUtils.PointsFromDisparityMap(map);

            TriangulationAlgorithm triangulation = new TriangulationAlgorithm();
            triangulation.Method = TriangulationAlgorithm.TriangulationMethod.TwoPointsRectified;
            triangulation.Cameras = cameras;
            triangulation.Points = points;
            triangulation.Find3DPoints();

            XmlSerialisation.SaveToFile(triangulation.Points, outPath);
        }

        public CameraPair PrepareCamerasForMotor()
        {
            var K_L = DenseMatrix.OfRowArrays(new double[][]
            {
                new double[] { 999.421, 0.0, 294.182 },
                new double[] { 0.0, 999.421, 252.932 },
                new double[] { 0.0,  0.0,   1.0 }
            });
            var K_R = DenseMatrix.OfRowArrays(new double[][]
            {
                new double[] { 999.421, 0.0, 326.96 },
                new double[] { 0.0, 999.421, 252.932 },
                new double[] { 0.0,  0.0,   1.0 }
            });
            var R_L = DenseMatrix.CreateIdentity(3);
            var R_R = DenseMatrix.CreateIdentity(3);
            var C_L = new DenseVector(new double[] { -96.5, 0, -500.0 });
            var C_R = new DenseVector(new double[] { 96.5, 0, -500.0 });

            return TestUtils.CreateTestCamerasFromMatrices(K_L, K_R, R_L, R_R, C_L, C_R);
        }

        public CameraPair PrepareCamerasForPipes()
        {
            var K_L = DenseMatrix.OfRowArrays(new double[][]
            {
                new double[] { 989.886, 0.0, 392.942 },
                new double[] { 0.0, 989.886, 243.221 },
                new double[] { 0.0, 0.0, 1.0 }
            });
            var K_R = DenseMatrix.OfRowArrays(new double[][]
            {
                new double[] { 989.886, 0.0, 412.274 },
                new double[] { 0.0, 989.886, 243.221 },
                new double[] { 0.0, 0.0, 1.0 }
            });
            var R_L = DenseMatrix.CreateIdentity(3);
            var R_R = DenseMatrix.CreateIdentity(3);
            var C_L = new DenseVector(new double[] { -118.461, 0, -500.0 });
            var C_R = new DenseVector(new double[] { 118.461, 0, -500.0 });

            return TestUtils.CreateTestCamerasFromMatrices(K_L, K_R, R_L, R_R, C_L, C_R);
        }

        public void SaveCalibrationsForTriangulation(Context context)
        {
            string outDir = context.ResultDirectory + "\\middlebury\\";

            XmlSerialisation.SaveToFile(PrepareCamerasForMotor(), outDir + "calib_motor.xml");
            XmlSerialisation.SaveToFile(PrepareCamerasForPipes(), outDir + "calib_pipes.xml");
        }
    }
}
