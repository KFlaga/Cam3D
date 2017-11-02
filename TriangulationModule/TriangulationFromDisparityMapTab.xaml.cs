using CamAlgorithms;
using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Xml;

namespace TriangulationModule
{
    public partial class TriangulationFromDisparityMapTab : UserControl
    {
        DisparityMap _dispMap;

        TwoPointsTriangulation _triangulation = new TwoPointsTriangulation();
        List<Vector<double>> _left;
        List<Vector<double>> _right;
        List<Vector<double>> _points3D;
        List<TriangulatedPoint> _triangulatedPoints;

        public TriangulationFromDisparityMapTab()
        {
            InitializeComponent();

            _dispImage.MapLoaded += (s, e) =>
            {
                _dispMap = _dispImage.Map;
            };
        }

        private void Triangulate(object sender, RoutedEventArgs e)
        {
            TriangulateBase(1);
        }

        private void TriangulateSparse(object sender, RoutedEventArgs e)
        {
            TriangulateBase(2);
        }

        private void TriangulateBase(int increment)
        {
            if(CalibrationData.Data.IsCamLeftCalibrated == false ||
                CalibrationData.Data.IsCamRightCalibrated == false)
            {
                MessageBox.Show("Error: Cameras are not calibrated!");
                return;
            }

            if(_dispMap == null)
            {
                MessageBox.Show("Error: disparity map needs to be loaded");
                return;
            }

            _left = new List<Vector<double>>();
            _right = new List<Vector<double>>();
            for(int r = 0; r < _dispMap.RowCount; r += increment)
            {
                for(int c = 0; c < _dispMap.ColumnCount; c += increment)
                {
                    if(_dispMap[r, c].IsValid())
                    {
                        _left.Add(new DenseVector(new double[]
                        {
                            c, r, 1.0
                        }));
                        _right.Add(new DenseVector(new double[]
                        {
                            c + _dispMap[r, c].SubDX, r + _dispMap[r, c].SubDY, 1.0
                        }));
                    }
                }
            }

            _triangulation.CalibData = CalibrationData.Data;
            _triangulation.UseLinearEstimationOnly = true;
            _triangulation.PointsLeft = _left;
            _triangulation.PointsRight = _right;

            _triangulation.Estimate3DPoints();

            _points3D = _triangulation.Points3D;
            StoreTraingulatedPoints(_left, _right, _points3D);

            MessageBox.Show("Triangulation finished");
        }

        private void StoreTraingulatedPoints(
            List<Vector<double>> pointsLeft,
            List<Vector<double>> pointsRight,
            List<Vector<double>> points3d)
        {
            _triangulatedPoints = new List<TriangulatedPoint>();
            for(int i = 0; i < points3d.Count; ++i)
            {
                _triangulatedPoints.Add(new TriangulatedPoint()
                {
                    ImageLeft = new Vector2(pointsLeft[i]),
                    ImageRight = new Vector2(pointsRight[i]),
                    Real = new Vector3(points3d[i])
                });
            }
        }

        private void Save3DPoints(object sender, RoutedEventArgs e)
        {
            FileOperations.SaveToFile(Save3DPoints, "Xml File|*.xml");
        }

        private void Save3DPoints(Stream file, string path)
        {
            CamCore.XmlSerialisation.SaveToFile(_triangulatedPoints, file);
        }
    }
}
