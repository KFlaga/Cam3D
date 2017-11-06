using MathNet.Numerics.LinearAlgebra;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using CamCore;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Text;
using CamAlgorithms.Calibration;
using CamAlgorithms.Triangulation;
using CamControls;

namespace TriangulationModule
{
    public partial class TriangulationFromPointsTab : UserControl
    {
        List<Vector2> _imgLeftPoints = new List<Vector2>();
        List<Vector2> _imgRightPoints = new List<Vector2>();
        
        public TriangulationAlgorithmUi Algorithm { get; private set; } = new TriangulationAlgorithmUi();
        public List<TriangulatedPoint> Points;

        public TriangulationFromPointsTab()
        {
            InitializeComponent();
        }

        private void ManagePoints_Left(object sender, RoutedEventArgs e)
        {
            MatchedPointsManagerWindow pointsManager = new MatchedPointsManagerWindow();
            pointsManager.Points = _imgLeftPoints;
            bool? res = pointsManager.ShowDialog();
            if(res != null && res == true)
            {
                _imgLeftPoints = pointsManager.Points;
            }
        }

        private void ManagePoints_Right(object sender, RoutedEventArgs e)
        {
            MatchedPointsManagerWindow pointsManager = new MatchedPointsManagerWindow();
            pointsManager.Points = _imgRightPoints;
            bool? res = pointsManager.ShowDialog();
            if(res != null && res == true)
            {
                _imgRightPoints = pointsManager.Points;
            }
        }

        private void Triangulate(object sender, RoutedEventArgs e)
        {
            if(CameraPair.Data.IsCamLeftCalibrated == false ||
                CameraPair.Data.IsCamRightCalibrated == false)
            {
                MessageBox.Show("Error: Cameras are not calibrated!");
                return;
            }
            
            if(_imgLeftPoints.Count != _imgRightPoints.Count)
            {
                MessageBox.Show("Error: Different count of points for left/right cameras!");
                return;
            }

            Points = new List<TriangulatedPoint>();
            for(int i = 0; i < _imgLeftPoints.Count; ++i)
            {
                Points.Add(new TriangulatedPoint()
                {
                    ImageLeft = _imgLeftPoints[i],
                    ImageRight = _imgRightPoints[i],
                    Real = new Vector3()
                });
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
            Dispatcher.Invoke(() => {
                StringBuilder leftText = new StringBuilder();
                StringBuilder rightText = new StringBuilder();
                StringBuilder realText = new StringBuilder();
                for(int i = 0; i < Points.Count; ++i)
                {
                    leftText.AppendLine(Points[i].ImageLeft.ToString("F1"));
                    rightText.AppendLine(Points[i].ImageRight.ToString("F1"));
                    realText.AppendLine(Points[i].Real.ToString("F2"));
                }

                _textPointsImgLeft.Text = leftText.ToString();
                _textPointsImgRight.Text = rightText.ToString();
                _textPointsReal.Text = realText.ToString();
            });
        }
    }
}
