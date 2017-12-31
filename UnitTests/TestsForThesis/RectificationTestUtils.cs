using CamAlgorithms;
using CamAlgorithms.Calibration;
using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;

namespace CamUnitTest.TestsForThesis
{
    public class Range3d
    {
        public double MaxX { get; set; } = 300;
        public double MinX { get; set; } = -150;
        public double MaxY { get; set; } = 250;
        public double MinY { get; set; } = -100;
        public double MaxZ { get; set; } = 800;
        public double MinZ { get; set; } = 500;
    }
    
    public struct MatrixInfo
    {
        public string Name { get; set; }
        public Matrix<double> Matrix { get; set; }

        public MatrixInfo(string n, Matrix<double> m)
        {
            Name = n;
            Matrix = m;
        }
    }

    public class RectificationTestUtils
    {
        public class CameraParams
        {
            public double fx { get; set; } = 10.0;
            public double fy { get; set; } = 10.0;
            public double px { get; set; } = 320.0;
            public double py { get; set; } = 240.0;
            public double rx { get; set; } = 0.0;
            public double ry { get; set; } = 0.0;
            public double rz { get; set; } = 0.0;
            public double cx { get; set; } = 50.0;
            public double cy { get; set; } = 50.0;
            public double cz { get; set; } = 0.0;
        }

        public static CameraPair PrepareCalibrationData_CloseToBeRectified()
        {
            var K_L = DenseMatrix.OfRowArrays(new double[][]
            {
                new double[] { 500.0, 0.0, 300.0 },
                new double[] { 0.0, 500.0, 220.0 },
                new double[] { 0.0,  0.0,   1.0 }
            });
            var K_R = DenseMatrix.OfRowArrays(new double[][]
            {
                new double[] { 530.0, 0.0, 280.0 },
                new double[] { 0.0, 540.0, 200.0 },
                new double[] { 0.0,  0.0,   1.0 }
            });
            var R_L = RotationConverter.EulerToMatrix(new double[3] { Math.PI / 60, 0, 0 });
            var R_R = RotationConverter.EulerToMatrix(new double[3] { 0, Math.PI / 60, 0 });
            var C_L = new DenseVector(new double[] { 0, 0, 0.0 });
            var C_R = new DenseVector(new double[] { 50.0, 10.0, 5.0 });

            return TestUtils.CreateTestCamerasFromMatrices(K_L, K_R, R_L, R_R, C_L, C_R);
        }

        public static CameraPair PrepareCalibrationData_AlmostRectified()
        {
            var K_L = DenseMatrix.OfRowArrays(new double[][]
            {
                new double[] { 500.0, 0.0, 300.0 },
                new double[] { 0.0, 500.0, 250.0 },
                new double[] { 0.0,  0.0,   1.0 }
            });
            var K_R = DenseMatrix.OfRowArrays(new double[][]
            {
                new double[] { 490.0, 0.0, 305.0 },
                new double[] { 0.0, 490.0, 245.0 },
                new double[] { 0.0,  0.0,   1.0 }
            });
            var R_L = RotationConverter.EulerToMatrix(new double[3] { Math.PI / 180, 0, 0 });
            var R_R = RotationConverter.EulerToMatrix(new double[3] { 0, Math.PI / 180, 0 });
            var C_L = new DenseVector(new double[] { 0, 0, 0.0 });
            var C_R = new DenseVector(new double[] { 50, 2.0, 2.0 });

            return TestUtils.CreateTestCamerasFromMatrices(K_L, K_R, R_L, R_R, C_L, C_R);
        }

        public static CameraPair PrepareCalibrationData_FarToRectified()
        {
            var K_L = DenseMatrix.OfRowArrays(new double[][]
            {
                new double[] { 520.0, 0.0, 300.0 },
                new double[] { 0.0, 520.0, 250.0 },
                new double[] { 0.0,  0.0,   1.0 }
            });
            var K_R = DenseMatrix.OfRowArrays(new double[][]
            {
                new double[] { 490.0, 0.0, 320.0 },
                new double[] { 0.0, 480.0, 200.0 },
                new double[] { 0.0,  0.0,   1.0 }
            });
            var R_L = RotationConverter.EulerToMatrix(new double[3] { Math.PI / 30, 0, Math.PI / 30 });
            var R_R = RotationConverter.EulerToMatrix(new double[3] { 0, Math.PI / 30, 0 });
            var C_L = new DenseVector(new double[] { 0, 0, 0.0 });
            var C_R = new DenseVector(new double[] { 50.0, 25.0, 10.0 });

            return TestUtils.CreateTestCamerasFromMatrices(K_L, K_R, R_L, R_R, C_L, C_R);
        }

        public static CameraPair PrepareCalibrationData(CameraParams left, CameraParams right)
        {
            var K_L = DenseMatrix.OfRowArrays(new double[][]
            {
                new double[] { left.fx, 0.0, left.px },
                new double[] { 0.0, left.fy, left.py },
                new double[] { 0.0,  0.0,   1.0 }
            });
            var K_R = DenseMatrix.OfRowArrays(new double[][]
            {
                new double[] { right.fx, 0.0, right.px },
                new double[] { 0.0, right.fy, right.py },
                new double[] { 0.0,  0.0,   1.0 }
            });
            var R_L = DenseMatrix.CreateIdentity(3);
            var R_R = DenseMatrix.CreateIdentity(3);
            RotationConverter.EulerToMatrix(new double[] { left.rx, left.ry, left.rz }, R_L);
            RotationConverter.EulerToMatrix(new double[] { right.rx, right.ry, right.rz }, R_R);
            var C_L = new DenseVector(new double[] { left.cx, left.cy, left.cz });
            var C_R = new DenseVector(new double[] { right.cx, right.cy, right.cz });

            return TestUtils.CreateTestCamerasFromMatrices(K_L, K_R, R_L, R_R, C_L, C_R);
        }
        
        public static List<Vector2Pair> PrepareMatchedPoints(CameraPair cameras, int pointCount = 100, Range3d range = null, int seed = 0)
        {
            range = range == null ? new Range3d() : range;
            Random rand = seed == 0 ? new Random() : new Random(seed);

            var matchedPairs = new List<Vector2Pair>();         
            for(int i = 0; i < pointCount; ++i)
            {
                Vector<double> real = new DenseVector(4);
                real[0] = rand.NextDouble() * (range.MaxX - range.MinX) + range.MinX;
                real[1] = rand.NextDouble() * (range.MaxY - range.MinY) + range.MinY;
                real[2] = rand.NextDouble() * (range.MaxZ - range.MinZ) + range.MinZ;
                real[3] = 1.0;

                var img1 = cameras.Left.Matrix * real;
                var img2 = cameras.Right.Matrix * real;
                Vector2Pair pair = new Vector2Pair()
                {
                    V1 = new Vector2(img1),
                    V2 = new Vector2(img2)
                };
                matchedPairs.Add(pair);
            }
            return matchedPairs;
        }
        
        public static void StoreMatrices(Context context, List<MatrixInfo> matrices)
        {
            foreach(var m in matrices)
            {
                context.Output.AppendLine(m.Name + ":");
                context.Output.AppendLine(m.Matrix.CustomToString());
            }
            context.Output.AppendLine();
        }

        public static void StoreRectificationMatrices(Context context, Matrix<double> Hl, Matrix<double> Hr)
        {
            StoreMatrices(context, new List<MatrixInfo>()
            {
                new MatrixInfo( "Left Rectification", Hl),
                new MatrixInfo( "Right Rectification", Hr)
            });
        }

        public static void StoreCamerasInfo(Context context, CameraPair cameras, Matrix<double> Hl, Matrix<double> Hr)
        {
            cameras.Update();
            context.Output.AppendLine("Cameras Initial:");
            StoreMatrices(context, new List<MatrixInfo>()
            {
                new MatrixInfo( "Left Camera", cameras.Left.Matrix),
                new MatrixInfo( "Left Camera Internal", cameras.Left.InternalMatrix),
                new MatrixInfo( "Left Camera Center", cameras.Left.Center.ToRowMatrix()),
                new MatrixInfo( "Right Camera", cameras.Right.Matrix),
                new MatrixInfo( "Right Camera Internal", cameras.Right.InternalMatrix),
                new MatrixInfo( "Right Camera Center", cameras.Right.Center.ToRowMatrix()),
                new MatrixInfo( "Fundamental", cameras.Fundamental)
            });

            cameras = new CameraPair()
            {
                Left = cameras.Left.Clone(),
                Right = cameras.Right.Clone()
            };
            cameras.Left.Matrix = Hl * cameras.Left.Matrix;
            cameras.Right.Matrix = Hr * cameras.Right.Matrix;
            cameras.Update();

            context.Output.AppendLine("Cameras Final:");
            StoreMatrices(context, new List<MatrixInfo>()
            {
                new MatrixInfo( "Left Camera", cameras.Left.Matrix),
                new MatrixInfo( "Left Camera Internal", cameras.Left.InternalMatrix),
                new MatrixInfo( "Left Camera Center", cameras.Left.Center.ToRowMatrix()),
                new MatrixInfo( "Right Camera", cameras.Right.Matrix),
                new MatrixInfo( "Right Camera Internal", cameras.Right.InternalMatrix),
                new MatrixInfo( "Right Camera Center", cameras.Right.Center.ToRowMatrix()),
                new MatrixInfo( "Fundamental", cameras.Fundamental)
            });
        }

        public enum RealRect
        {
            FullCalib_ZhangLoop,
            FullCalib_FussielloTruccoVerri,
            FullCalib_FussielloIrsara,
            NoDistortionCalib_ZhangLoop,
            NoDistortionCalib_FussielloTruccoVerri,
            NoDistortionCalib_FussielloIrsara,
            Only600Calib_ZhangLoop,
            Only600Calib_FussielloTruccoVerri,
            Only600Calib_FussielloIrsara,
        }

        public static void LoadRectification(Context context, RealRect rectCase, out RectificationAlgorithm rect, out List<Vector2Pair> matched)
        {
            string directory = context.ResultDirectory + "\\real_rectification\\";
            string rect_name;
            string matched_name;
            switch(rectCase)
            {
                case RealRect.FullCalib_FussielloIrsara:
                    rect_name = "rect_fi_full.xml";
                    matched_name = "matched_full.xml";
                    break;
                case RealRect.FullCalib_FussielloTruccoVerri:
                    rect_name = "rect_ftv_full.xml";
                    matched_name = "matched_full.xml";
                    break;
                case RealRect.FullCalib_ZhangLoop:
                    rect_name = "rect_zl_full.xml";
                    matched_name = "matched_full.xml";
                    break;
                case RealRect.NoDistortionCalib_FussielloIrsara:
                    rect_name = "rect_fi_noud.xml";
                    matched_name = "matched_noud.xml";
                    break;
                case RealRect.NoDistortionCalib_FussielloTruccoVerri:
                    rect_name = "rect_ftv_noud.xml";
                    matched_name = "matched_noud.xml";
                    break;
                case RealRect.NoDistortionCalib_ZhangLoop:
                    rect_name = "rect_zl_noud.xml";
                    matched_name = "matched_noud.xml";
                    break;
                case RealRect.Only600Calib_FussielloIrsara:
                    rect_name = "rect_fi_600.xml";
                    matched_name = "matched_600.xml";
                    break;
                case RealRect.Only600Calib_FussielloTruccoVerri:
                    rect_name = "rect_ftv_600.xml";
                    matched_name = "matched_600.xml";
                    break;
                case RealRect.Only600Calib_ZhangLoop:
                    rect_name = "rect_zl_600.xml";
                    matched_name = "matched_600.xml";
                    break;
                default:
                    throw new Exception();
            }
            LoadRectification(directory + rect_name, directory + matched_name, out rect, out matched);
        }

        public static void LoadRectification(string pathRect, string pathMatched, out RectificationAlgorithm rect, out List<Vector2Pair> matched)
        {
            rect = XmlSerialisation.CreateFromFile<RectificationAlgorithm>(pathRect);
            matched = XmlSerialisation.CreateFromFile<List<Vector2Pair>>(pathMatched);
        }
    }
}
