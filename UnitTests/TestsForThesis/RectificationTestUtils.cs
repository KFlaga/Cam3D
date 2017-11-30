using CamAlgorithms.Calibration;
using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;

namespace CamUnitTest.TestsForThesis
{
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
                new double[] { 550.0, 0.0, 280.0 },
                new double[] { 0.0, 570.0, 200.0 },
                new double[] { 0.0,  0.0,   1.0 }
            });
            var R_L = RotationConverter.EulerToMatrix(new double[3] { Math.PI / 90, 0, 0 });
            var R_R = RotationConverter.EulerToMatrix(new double[3] { 0, Math.PI / 90, 0 });
            var C_L = new DenseVector(new double[] { 50.0, 50.0, 0.0 });
            var C_R = new DenseVector(new double[] { 40.0, 40.0, 5.0 });

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
            var R_L = DenseMatrix.CreateIdentity(3);
            var R_R = DenseMatrix.CreateIdentity(3);
            var C_L = new DenseVector(new double[] { 50.0, 50.0, 0.0 });
            var C_R = new DenseVector(new double[] { 30.0, 50.0, 2.0 });

            return TestUtils.CreateTestCamerasFromMatrices(K_L, K_R, R_L, R_R, C_L, C_R);
        }

        public static CameraPair PrepareCalibrationData_FarToRectified()
        {
            var K_L = DenseMatrix.OfRowArrays(new double[][]
            {
                new double[] { 550.0, 0.0, 300.0 },
                new double[] { 0.0, 550.0, 250.0 },
                new double[] { 0.0,  0.0,   1.0 }
            });
            var K_R = DenseMatrix.OfRowArrays(new double[][]
            {
                new double[] { 450.0, 0.0, 320.0 },
                new double[] { 0.0, 450.0, 200.0 },
                new double[] { 0.0,  0.0,   1.0 }
            });
            var R_L = RotationConverter.EulerToMatrix(new double[3] { Math.PI / 30, 0, Math.PI / 30 });
            var R_R = RotationConverter.EulerToMatrix(new double[3] { 0, Math.PI / 30, 0 });
            var C_L = new DenseVector(new double[] { 50.0, 50.0, 0.0 });
            var C_R = new DenseVector(new double[] { 30.0, 40.0, 10.0 });

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

        public class Range
        {
            public double MaxX { get; set; } = 100;
            public double MinX { get; set; } = -100;
            public double MaxY { get; set; } = 100;
            public double MinY { get; set; } = -100;
            public double MaxZ { get; set; } = 100;
            public double MinZ { get; set; } = 50;
        }

        public static List<Vector2Pair> PrepareMatchedPoints(CameraPair cameras, int pointCount = 100, Range range = null, int seed = 0)
        {
            range = range == null ? new Range() : range;
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
                new MatrixInfo( "Left Camera Center", cameras.Left.Translation.ToRowMatrix()),
                new MatrixInfo( "Right Camera", cameras.Right.Matrix),
                new MatrixInfo( "Right Camera Internal", cameras.Right.InternalMatrix),
                new MatrixInfo( "Right Camera Center", cameras.Right.Translation.ToRowMatrix()),
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
                new MatrixInfo( "Left Camera Center", cameras.Left.Translation.ToRowMatrix()),
                new MatrixInfo( "Right Camera", cameras.Right.Matrix),
                new MatrixInfo( "Right Camera Internal", cameras.Right.InternalMatrix),
                new MatrixInfo( "Right Camera Center", cameras.Right.Translation.ToRowMatrix()),
                new MatrixInfo( "Fundamental", cameras.Fundamental)
            });
        }
    }
}
