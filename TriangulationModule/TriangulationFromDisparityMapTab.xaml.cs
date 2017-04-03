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
            for(int r = 0; r < _dispMap.RowCount; ++r)
            {
                for(int c = 0; c < _dispMap.ColumnCount; ++c)
                {
                    if(_dispMap[r, c].IsValid())
                    {
                        _left.Add(new DenseVector(new double[]
                        {
                            c, r, 1.0
                        }));
                        _right.Add(new DenseVector(new double[]
                        {
                            c + _dispMap[r, c].DX, r + _dispMap[r, c].DY, 1.0
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

            MessageBox.Show("Triangulation finished");
        }
        
        private void Save3DPoints(object sender, RoutedEventArgs e)
        {
            FileOperations.SaveToFile(Save3DPoints, "Xml File|*.xml");
        }
        
        private void Save3DPoints(Stream file, string path)
        {
            XmlDocument dataDoc = new XmlDocument();
            var rootNode = dataDoc.CreateElement("Points");

            for(int i= 0; i < _points3D.Count; ++i)
            {
                var point3D = _points3D[i];
                var pointImg = _left[i];

                var pointNode = dataDoc.CreateElement("Point");

                var attRealX = dataDoc.CreateAttribute("realx");
                attRealX.Value = point3D.At(0).ToString();
                var attRealY = dataDoc.CreateAttribute("realy");
                attRealY.Value = point3D.At(1).ToString();
                var attRealZ = dataDoc.CreateAttribute("realz");
                attRealZ.Value = point3D.At(2).ToString();

                var attImgX = dataDoc.CreateAttribute("imgx");
                attImgX.Value = pointImg.At(0).ToString();
                var attImgY = dataDoc.CreateAttribute("imgy");
                attImgY.Value = pointImg.At(1).ToString();

                pointNode.Attributes.Append(attRealX);
                pointNode.Attributes.Append(attRealY);
                pointNode.Attributes.Append(attRealZ);
                pointNode.Attributes.Append(attImgX);
                pointNode.Attributes.Append(attImgY);

                rootNode.AppendChild(pointNode);
            }

            dataDoc.InsertAfter(rootNode, dataDoc.DocumentElement);
            dataDoc.Save(file);
        }
    }
}
