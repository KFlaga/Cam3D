using CamAlgorithms.Calibration;
using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamUnitTest.TestsForThesis
{
    public class TriangulationTestUtils
    {
        public static CameraPair PrepareCalibrationData_AlmostRectified()
        {
            return RectificationTestUtils.PrepareCalibrationData_AlmostRectified();
        }

        public static CameraPair PrepareCalibrationData_FarToRectified()
        {
            return RectificationTestUtils.PrepareCalibrationData_FarToRectified();
        }

        public static List<TriangulatedPoint> PrepareTriangulatedPoints(CameraPair cameras, int pointCount = 100, Range3d range = null, int seed = 0)
        {
            range = range == null ? new Range3d() : range;
            Random rand = seed == 0 ? new Random() : new Random(seed);

            var points = new List<TriangulatedPoint>();
            for(int i = 0; i < pointCount; ++i)
            {
                Vector<double> real = new DenseVector(4);
                real[0] = rand.NextDouble() * (range.MaxX - range.MinX) + range.MinX;
                real[1] = rand.NextDouble() * (range.MaxY - range.MinY) + range.MinY;
                real[2] = rand.NextDouble() * (range.MaxZ - range.MinZ) + range.MinZ;
                real[3] = 1.0;

                var img1 = cameras.Left.Matrix * real;
                var img2 = cameras.Right.Matrix * real;
                TriangulatedPoint p = new TriangulatedPoint()
                {
                    ImageLeft = new Vector2(img1),
                    ImageRight = new Vector2(img2),
                    Real = new Vector3(real)
                };
                points.Add(p);
            }
            return points;
        }

        public static List<TriangulatedPoint> AddNoise(List<TriangulatedPoint> points, double variance, int seed = 0)
        {
            List<Vector2> left = new List<Vector2>(points.Select((p) => { return p.ImageLeft; }));
            List<Vector2> right = new List<Vector2>(points.Select((p) => { return p.ImageRight; }));

            List<Vector2> noisedLeft = TestUtils.AddNoise(left, variance, seed);
            List<Vector2> noisedRight = TestUtils.AddNoise(right, variance, seed * 11);

            return new List<TriangulatedPoint>(points.Select((p, i) =>
            {
                return new TriangulatedPoint()
                {
                    ImageLeft = noisedLeft[i],
                    ImageRight = noisedRight[i],
                    Real = new Vector3()
                };
            }));
        }

        public static void StoreMatrices(Context context, List<MatrixInfo> matrices)
        {
            RectificationTestUtils.StoreMatrices(context, matrices);
        }

        public static void StoreCamerasInfo(Context context, CameraPair cameras, bool shortVer = false)
        {
            if(shortVer) { return; }
            cameras.Update();
            context.Output.AppendLine("Cameras Noised:");
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

        public static List<TriangulatedPoint> PointsFromDisparityMap(DisparityMap map)
        {
            List<TriangulatedPoint> points = new List<TriangulatedPoint>();
            for(int r = 0; r < map.RowCount; ++r)
            {
                for(int c = 0; c < map.ColumnCount; ++c)
                {
                    Disparity d = map[r, c];
                    if(d.IsValid())
                    {
                        points.Add(new TriangulatedPoint()
                        {
                            ImageLeft = new Vector2(y: r, x: c),
                            ImageRight = new Vector2(y: r, x: c + d.SubDX),
                            Real = new Vector3()
                        });
                    }
                }
            }
            return points;
        }

        public enum RealCase
        {
            FullCalib,
            Only600
        }

        public static void LoadRealData(Context context, RealCase realCase, out CameraPair cameras, out List<TriangulatedPoint> calib, out List<TriangulatedPoint> reconstructed)
        {
            string directory = context.ResultDirectory + "\\real_triangulation\\";
            string camera_name, calib_name, rec_name;
            switch(realCase)
            {
                case RealCase.FullCalib:
                    camera_name = "cameras_full.xml";
                    calib_name = "cpoints_full.xml";
                    rec_name = "reconstructed_full.xml";
                    break;
                case RealCase.Only600:
                    camera_name = "cameras_600.xml";
                    calib_name = "cpoints_600.xml";
                    rec_name = "reconstructed_600.xml";
                    break;
                default:
                    throw new Exception();
            }

            LoadRealData(directory + camera_name, directory + calib_name, directory + rec_name, out cameras, out calib, out reconstructed);
        }

        public static void LoadRealData(string pathCameras, string pathCalib, string pathReconstructed, 
            out CameraPair cameras, out List<TriangulatedPoint> calib, out List<TriangulatedPoint> reconstructed)
        {
            cameras = XmlSerialisation.CreateFromFile<CameraPair>(pathCameras);
            calib = XmlSerialisation.CreateFromFile<List<TriangulatedPoint>>(pathCalib);
            reconstructed = XmlSerialisation.CreateFromFile<List<TriangulatedPoint>>(pathReconstructed);
        }
    }
}
