using CamAlgorithms.Calibration;
using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;

namespace CamUnitTest.TestsForThesis
{
    public class CalibrationTestUtils
    {
        public static Camera PrepareCamera()
        {
            var K = DenseMatrix.OfRowArrays(new double[][]
            {
                new double[] { 520.0, 0.0, 300.0 },
                new double[] { 0.0, 520.0, 250.0 },
                new double[] { 0.0,  0.0,   1.0 }
            });
            var R = RotationConverter.EulerToMatrix(new double[3] { Math.PI / 30, -Math.PI / 30, Math.PI / 180 });
            var C = new DenseVector(new double[] { 50, 50, 50 });

            return TestUtils.CreateTestCameraFromMatrices(K, R, C);
        }

        public class GridRange
        {
            public double MaxX { get; set; }
            public double MinX { get; set; }
            public double MaxY { get; set; }
            public double MinY { get; set; }
            public double Z { get; set; }
            public double Angle { get; set; }

            public static GridRange Standard = new GridRange()
            {
                MinX = -150,
                MaxX = 250,
                MinY = -100,
                MaxY = 250,
                Z = 650,
                Angle = Math.PI / 6
            };

            public static GridRange Side = new GridRange()
            {
                MinX = 350,
                MaxX = 550,
                MinY = -100,
                MaxY = 250,
                Z = 600,
                Angle = Math.PI / 6
            };

            public static GridRange Far = new GridRange()
            {
                MinX = -550,
                MaxX = 650,
                MinY = -400,
                MaxY = 650,
                Z = 3050,
                Angle = Math.PI / 6
            };
        }

        // Prepares 2 grids. Each is rotated by Angle around Y axis on the middle X, where its Z = Z.
        public static List<RealGridData> PrepareCalibrationGrids(int rows = 7, int cols = 7, GridRange range = null)
        {
            range = range == null ? GridRange.Standard : range;

			double halfX = 0.5*(range.MaxX - range.MinX);
            RealGridData gridLeftCloser = new RealGridData()
            {
                Rows = rows,
                Columns = cols,
                TopLeft = new Vector3(range.MinX, range.MaxY, range.Z + halfX * Math.Tan(range.Angle)),
                TopRight = new Vector3(range.MaxX, range.MaxY, range.Z - halfX * Math.Tan(range.Angle)),
                BotLeft = new Vector3(range.MinX, range.MinY, range.Z + halfX * Math.Tan(range.Angle)),
                BotRight = new Vector3(range.MaxX, range.MinY, range.Z - halfX * Math.Tan(range.Angle)),
            };
            RealGridData gridRightCloser = new RealGridData()
            {
                Rows = rows,
                Columns = cols,
                TopLeft = new Vector3(range.MinX, range.MaxY, range.Z - halfX* Math.Tan(range.Angle)),
                TopRight = new Vector3(range.MaxX, range.MaxY, range.Z + halfX * Math.Tan(range.Angle)),
                BotLeft = new Vector3(range.MinX, range.MinY, range.Z - halfX* Math.Tan(range.Angle)),
                BotRight = new Vector3(range.MaxX, range.MinY, range.Z + halfX * Math.Tan(range.Angle)),
            };

            return new List<RealGridData>()
            {
                gridLeftCloser, gridRightCloser
            };
        }

        public static List<CalibrationPoint> PrepareCalibrationPoints(Camera camera, List<RealGridData> grids)
        {
            List<CalibrationPoint> cpoints = new List<CalibrationPoint>();
            for(int g = 0; g < grids.Count; ++g)
            {
                var grid = grids[g];
                for(int r = 0; r < grid.Rows; ++r)
                {
                    for(int c = 0; c < grid.Columns; ++c)
                    {
                        CalibrationPoint cpoint = new CalibrationPoint()
                        {
                            GridNum = g,
                            RealGridPos = new IntVector2(x: c, y: r),
                            Real = grid.GetRealFromCell(r, c)
                        };

                        var img = camera.Matrix * cpoint.Real.ToMathNetVector4();
                        cpoint.Img = new Vector2(img);
                        cpoints.Add(cpoint);
                    }
                }
            }
            return cpoints;
        }

        public static List<CalibrationPoint> AddGaussNoise(List<CalibrationPoint> cpoints, double variationImg, double variationReal, int seed = 0)
        {
            List<Vector2> imgPoints = new List<Vector2>();
            List<Vector3> realPoints = new List<Vector3>();
            foreach(var cp in cpoints)
            {
                imgPoints.Add(cp.Img);
                realPoints.Add(cp.Real);
            }

            if(variationImg > 0) { imgPoints = TestUtils.AddNoise(imgPoints, variationImg, seed * 11); }
            if(variationReal > 0) { realPoints = TestUtils.AddNoise(realPoints, variationReal, seed * 7); }

            List<CalibrationPoint> noised = new List<CalibrationPoint>();
            for(int i = 0; i < cpoints.Count; ++i)
            {
                noised.Add(new CalibrationPoint()
                {
                    GridNum = cpoints[i].GridNum,
                    Img = imgPoints[i],
                    Real = realPoints[i],
                    RealGridPos = cpoints[i].RealGridPos
                });
            }
            return noised;
        }

        public static List<RealGridData> AddGaussNoise(List<RealGridData> grids, double variation, int seed = 0)
        {
            List<Vector3> points = new List<Vector3>();
            foreach(var grid in grids)
            {
                points.Add(grid.TopLeft);
                points.Add(grid.TopRight);
                points.Add(grid.BotLeft);
                points.Add(grid.BotRight);
            }
            
            if(variation > 0) { points = TestUtils.AddNoise(points, variation, seed * 3); }

            List<RealGridData> noised = new List<RealGridData>();
            for(int i = 0; i < grids.Count; ++i)
            {
                noised.Add(new RealGridData()
                {
                    Num = grids[i].Num,
                    Rows = grids[i].Rows, 
                    Columns = grids[i].Columns,
                    TopLeft = points[4 * i],
                    TopRight = points[4 * i + 1],
                    BotLeft = points[4 * i + 2],
                    BotRight = points[4 * i + 3],
                });
            }
            return noised;
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
        }

        public static void StoreCameraInfo(Context context, Camera cameraIdeal, Camera cameraEstimated, bool shortVer = false)
        {
            cameraEstimated.Decompose();
            cameraIdeal.Decompose();
            if(shortVer)
            {
                var R = cameraEstimated.RotationMatrix[0, 0] > 0 ? cameraEstimated.RotationMatrix : -cameraEstimated.RotationMatrix;
                context.Output.AppendLine((cameraEstimated.InternalMatrix - cameraIdeal.InternalMatrix).FrobeniusNorm().ToString());
                context.Output.AppendLine((R - cameraIdeal.RotationMatrix).FrobeniusNorm().ToString());
                context.Output.AppendLine((cameraEstimated.Center - cameraIdeal.Center).L2Norm().ToString());
            }
            else
            {
                context.Output.AppendLine("Camera id:");
                StoreMatrices(context, new List<MatrixInfo>()
                {
                    new MatrixInfo("Internal", cameraIdeal.InternalMatrix),
                    new MatrixInfo("Rotation", cameraIdeal.RotationMatrix),
                    new MatrixInfo("Rotation", RotationConverter.MatrixToEuler(cameraIdeal.RotationMatrix).Multiply(180 / Math.PI).ToRowMatrix()),
                    new MatrixInfo("Center", cameraIdeal.Center.ToRowMatrix()),
                });
                context.Output.AppendLine("Camera Estimated:");

                var R = cameraEstimated.RotationMatrix[0, 0] > 0 ? cameraEstimated.RotationMatrix : -cameraEstimated.RotationMatrix;
                StoreMatrices(context, new List<MatrixInfo>()
                {
                    new MatrixInfo("Internal", cameraEstimated.InternalMatrix),
                    new MatrixInfo("Rotation", R),
                    new MatrixInfo("Rotation", RotationConverter.MatrixToEuler(R).Multiply(180 / Math.PI).ToRowMatrix()),
                    new MatrixInfo("Center", cameraEstimated.Center.ToRowMatrix()),
                });
                StoreMatrices(context, new List<MatrixInfo>()
                {
                    new MatrixInfo("InternalDiff", cameraEstimated.InternalMatrix - cameraIdeal.InternalMatrix),
                    new MatrixInfo("EulerDiff", (RotationConverter.MatrixToEuler(R) - RotationConverter.MatrixToEuler(cameraIdeal.RotationMatrix)).Multiply(180 / Math.PI).ToRowMatrix()),
                    new MatrixInfo("RotationDiff", (R - cameraIdeal.RotationMatrix)),
                    new MatrixInfo("CenterDiff", (cameraEstimated.Center - cameraIdeal.Center).ToRowMatrix()),
                });

                context.Output.AppendLine("Internals estimation error: " + (cameraEstimated.InternalMatrix - cameraIdeal.InternalMatrix).FrobeniusNorm());
                context.Output.AppendLine("Rotation estimation error: " + (R - cameraIdeal.RotationMatrix).FrobeniusNorm());
                context.Output.AppendLine("Center estimation error: " + (cameraEstimated.Center - cameraIdeal.Center).L2Norm());
            }
        }

        public static void StoreMinimalizationInfo(Context context, CalibrationAlgorithm calib, bool shortVer = false)
        {
            if(shortVer)
            {
                return;
            }
            else
            {
                context.Output.AppendLine("Nonlinear Minimalization:");
                context.Output.AppendLine("Iterations: " + calib.NonlinearMinimalization.CurrentIteration);
                context.Output.AppendLine("Max Residiual: " + calib.NonlinearMinimalization.MaximumResidiual);
                context.Output.AppendLine("Initial Residiual: " + calib.NonlinearMinimalization.BaseResidiual);
                context.Output.AppendLine("Best Residiual: " + calib.NonlinearMinimalization.MinimumResidiual);
            }
        }
    }
}


