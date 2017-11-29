using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using CamCore;
using System.Text;
using CamAlgorithms.Calibration;
using CamAlgorithms.Triangulation;
using CamControls;

namespace TriangulationModule
{
    public partial class TriangulationFromPointsTab : UserControl
    {
        List<Vector2Pair> _matchedPointsInput = new List<Vector2Pair>();
        List<TriangulatedPoint> _triangulatedPointsInput = new List<TriangulatedPoint>();

        public TriangulationAlgorithmUi Algorithm { get; private set; } = new TriangulationAlgorithmUi();
        public List<TriangulatedPoint> PointsOutput { get; set; }

        public bool UseMatchedInput { get; set; } = false;
        public bool UseTriangulatedInput { get; set; } = false;

        public TriangulationFromPointsTab()
        {
            InitializeComponent();
        }

        private void ManageMatchedPoints(object sender, RoutedEventArgs e)
        {
            MatchedPointsManagerWindow pointsManager = new MatchedPointsManagerWindow();
            pointsManager.Points = _matchedPointsInput;
            bool? res = pointsManager.ShowDialog();
            if(res != null && res == true)
            {
                _matchedPointsInput = pointsManager.Points;
                UseMatchedInput = true;
            }
        }

        private void ManageTriangulatedPoints(object sender, RoutedEventArgs e)
        {
            TriangulatedPointManagerWindow pointsManager = new TriangulatedPointManagerWindow();
            pointsManager.Points = _triangulatedPointsInput;
            bool? res = pointsManager.ShowDialog();
            if(res != null && res == true)
            {
                _triangulatedPointsInput = pointsManager.Points;
            }
        }

        private void SaveTriangulatedPoints(object sender, RoutedEventArgs e)
        {
            FileOperations.SaveToFile((s, p) => { XmlSerialisation.SaveToFile(PointsOutput, s); }, "Xml File|*.xml");
        }

        private void Triangulate(object sender, RoutedEventArgs e)
        {
            if(CameraPair.Data.AreCalibrated == false)
            {
                MessageBox.Show("Error: Cameras are not calibrated!");
                return;
            }

            if(UseMatchedInput && _matchedPointsInput.Count == 0)
            {
                MessageBox.Show("Need to set some points (UseMatched = true)");
                return;
            }

            if(UseTriangulatedInput && _triangulatedPointsInput.Count == 0)
            { 
                MessageBox.Show("Need to set some points (UseTriangulated = true)");
                return;
            }

            PointsOutput = new List<TriangulatedPoint>();
            if(UseMatchedInput)
            {
                for(int i = 0; i < _matchedPointsInput.Count; ++i)
                {
                    PointsOutput.Add(new TriangulatedPoint()
                    {
                        ImageLeft = _matchedPointsInput[i].V1,
                        ImageRight = _matchedPointsInput[i].V2,
                        Real = new Vector3()
                    });
                }
            }
            else if(UseTriangulatedInput)
            {
                for(int i = 0; i < _triangulatedPointsInput.Count; ++i)
                {
                    PointsOutput.Add(new TriangulatedPoint()
                    {
                        ImageLeft = _triangulatedPointsInput[i].ImageLeft,
                        ImageRight = _triangulatedPointsInput[i].ImageRight,
                        Real = new Vector3()
                    });
                }
            }
            else
            {
                MessageBox.Show("Need to set some points (both types = false)");
                return;
            }


            Algorithm.Cameras = CameraPair.Data;
            Algorithm.Points = PointsOutput;
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
                    StringBuilder leftText = new StringBuilder();
                    StringBuilder rightText = new StringBuilder();
                    StringBuilder realText = new StringBuilder();
                    for(int i = 0; i < PointsOutput.Count; ++i)
                    {
                        leftText.AppendLine(PointsOutput[i].ImageLeft.ToString("F1"));
                        rightText.AppendLine(PointsOutput[i].ImageRight.ToString("F1"));
                        realText.AppendLine(PointsOutput[i].Real.ToString("F2"));
                    }

                    _textPointsImgLeft.Text = leftText.ToString();
                    _textPointsImgRight.Text = rightText.ToString();
                    _textPointsReal.Text = realText.ToString();
                });
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
