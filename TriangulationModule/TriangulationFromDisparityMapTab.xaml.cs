using CamAlgorithms.Calibration;
using CamControls;
using CamCore;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace TriangulationModule
{
    public partial class TriangulationFromDisparityMapTab : UserControl
    {
        DisparityMap _dispMap;

        public TriangulationAlgorithmUi Algorithm { get; private set; } = new TriangulationAlgorithmUi();
        public List<TriangulatedPoint> Points { get; set; }

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
            PerformTriangulation(1);
        }

        private void TriangulateSparse(object sender, RoutedEventArgs e)
        {
            PerformTriangulation(2);
        }

        private void PerformTriangulation(int increment)
        {
            if(CameraPair.Data.AreCalibrated)
            {
                MessageBox.Show("Error: Cameras are not calibrated!");
                return;
            }

            if(_dispMap == null)
            {
                MessageBox.Show("Error: disparity map needs to be loaded");
                return;
            }

            Points = new List<TriangulatedPoint>();
            for(int r = 0; r < _dispMap.RowCount; r += increment)
            {
                for(int c = 0; c < _dispMap.ColumnCount; c += increment)
                {
                    if(_dispMap[r, c].IsValid())
                    {
                        Points.Add(new TriangulatedPoint()
                        {
                            ImageLeft = new Vector2(y: r, x: c),
                            ImageRight = new Vector2(y: r, x: c + _dispMap[r, c].SubDX),
                            Real = new Vector3()
                        });
                    }
                }
            }

            Algorithm.Cameras = CameraPair.Data;
            Algorithm.Points = Points;
            Algorithm.Recitifed = false;
            Algorithm.StatusChanged += Algorithm_StatusChanged;
            AlgorithmWindow window = new AlgorithmWindow(Algorithm);
            window.Show();
        }

        private void Algorithm_StatusChanged(object sender, AlgorithmEventArgs e)
        {
            if(e.CurrentStatus == AlgorithmStatus.Finished || e.CurrentStatus == AlgorithmStatus.Terminated)
            {
                Dispatcher.Invoke(() =>
                {

                });
            }
        }

        private void Save3DPoints(object sender, RoutedEventArgs e)
        {
            FileOperations.SaveToFile(Save3DPoints, "Xml File|*.xml");
        }

        private void Save3DPoints(Stream file, string path)
        {
            CamCore.XmlSerialisation.SaveToFile(Points, file);
        }
    }
}
