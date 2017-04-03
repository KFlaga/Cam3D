using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using CamControls;
using CamCore;
using CamImageProcessing;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Text;

namespace TriangulationModule
{
    public partial class TriangulationFromPointsTab : UserControl
    {
        List<Vector2> _imgLeftPoints = new List<Vector2>();
        List<Vector2> _imgRightPoints = new List<Vector2>();

        TwoPointsTriangulation _triangulation = new TwoPointsTriangulation();
        List<Vector<double>> _left;
        List<Vector<double>> _right;
        List<Vector<double>> _points3D;

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
            if(CalibrationData.Data.IsCamLeftCalibrated == false ||
                CalibrationData.Data.IsCamRightCalibrated == false)
            {
                MessageBox.Show("Error: Cameras are not calibrated!");
                return;
            }
            
            if(_imgLeftPoints.Count != _imgRightPoints.Count)
            {
                MessageBox.Show("Error: Different count of points for left/right cameras!");
                return;
            }

            _left = new List<Vector<double>>();
            _right = new List<Vector<double>>();
            for(int i = 0; i < _imgLeftPoints.Count; ++i)
            {
                var leftPoint = _imgLeftPoints[i];
                var rightPoint = _imgRightPoints[i];
                _left.Add(new DenseVector(new double[]
                {
                    leftPoint.X, leftPoint.Y, 1.0
                }));
                _right.Add(new DenseVector(new double[]
                {
                    rightPoint.X, rightPoint.Y, 1.0
                }));
            }

       //     Matrix<double> normLeft = PointNormalization.Normalize2D(_left);
        //    Matrix<double> normRight = PointNormalization.Normalize2D(_right);

//            for(int i = 0; i < _imgLeftPoints.Count; ++i)
 //           {
  //              _left[i] = normLeft * _left[i];
   //             _right[i] = normRight * _right[i];
   //         }

    //        var oldCamLeft = CalibrationData.Data.CameraLeft.Clone();
     //       var oldCamRight = CalibrationData.Data.CameraRight.Clone();

      //      CalibrationData.Data.CameraLeft = normLeft * oldCamLeft;
       //     CalibrationData.Data.CameraRight = normRight * oldCamRight;

            _triangulation.CalibData = CalibrationData.Data;
            _triangulation.UseLinearEstimationOnly = true;
            _triangulation.PointsLeft = _left;
            _triangulation.PointsRight = _right;

            _triangulation.Estimate3DPoints();

            _points3D = _triangulation.Points3D;

            StringBuilder leftText = new StringBuilder();
            StringBuilder rightText = new StringBuilder();
            StringBuilder realText = new StringBuilder();
            for(int i = 0; i < _points3D.Count; ++i)
            {
                leftText.AppendLine("x: " + _left[i][0].ToString("F2") + ", y: " + _left[i][1].ToString("F2"));
                rightText.AppendLine("x: " + _right[i][0].ToString("F2") + ", y: " + _right[i][1].ToString("F2"));
                realText.AppendLine("X: " + _points3D[i][0].ToString("F2") + ", Y: " + _points3D[i][1].ToString("F2") + ", Z: " + _points3D[i][2].ToString("F2"));
            }

            _textPointsImgLeft.Text = leftText.ToString();
            _textPointsImgRight.Text = rightText.ToString();
            _textPointsReal.Text = realText.ToString();
        }
    }
}
