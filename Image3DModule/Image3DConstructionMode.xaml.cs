using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CamMain
{
    /// <summary>
    /// Interaction logic for Image3DConstructionMode.xaml
    /// </summary>
    public partial class Image3DConstructionMode : UserControl
    {
        public List<Camera3DPoint> Points3D { get; set; }

        private Point _curCam1Point = new Point(-1,-1);
        private Point _curCam2Point = new Point(-1,-1);
        private Camera3DPoint _curCamPoint = new Camera3DPoint();
        private bool _isPointsSelected = false;

        public Image3DConstructionMode()
        {
            Points3D = new List<Camera3DPoint>();
            InitializeComponent();
            _camImageFirst.TempPointChanged += OnTempPointChangedFirst;
            _camImageSec.TempPointChanged += OnTempPointChangedSecond;
            _camImageFirst.SelectedPointChanged += OnSelectedPointChangedFirst;
            _camImageSec.SelectedPointChanged += OnSelectedPointChangedSecond;
        }

        private void AcceptImagePoints(object sender, RoutedEventArgs e)
        {
            if (_isPointsSelected)
            {

            }
            else
            {
                _camImageFirst.AddPoint(_curCam1Point);
                _camImageSec.AddPoint(_curCam2Point);

                Points3D.Add(new Camera3DPoint()
                {
                    Cam1Point = _curCam1Point,
                    Cam2Point = _curCam2Point
                });
                _butAcceptPoint.IsEnabled = false;
                // Compute 3D point

            }
            _curCam1Point = new Point(-1, -1);
            _curCam2Point = new Point(-1, -1);
            _curCamPoint = new Camera3DPoint();
        }

        private void RemoveImagePoints(object sender, RoutedEventArgs e)
        {
            _camImageFirst.SelectedPoint = new Point(-1, -1);
            _camImageSec.SelectedPoint = new Point(-1, -1);
            _camImageFirst.RemovePoint(_curCamPoint.Cam1Point);
            _camImageSec.RemovePoint(_curCamPoint.Cam2Point);
            Points3D.Remove(_curCamPoint);
            
        }

        private void ManagePoints(object sender, RoutedEventArgs e)
        {
            //CalibrationPointsManagerWindow pointsManager = new CalibrationPointsManagerWindow();
            //pointsManager.CalibrationPoints = CalibrationPoints;
            //bool? res = pointsManager.ShowDialog();
            //if (res != null && res == true)
            //{
              //  CalibrationPoints = pointsManager.CalibrationPoints;
            //}
        }

        private void AutoCorners(object sender, RoutedEventArgs e)
        {
            
        }

        private void Build3DImage(object sender, RoutedEventArgs e)
        {
            
        }

        private void OnTempPointChangedFirst(object sender, Point point)
        {
            _curCam1Point = point;
            CheckIfValidPointsChoosen();
        }

        private void OnTempPointChangedSecond(object sender, Point point)
        {
            _curCam2Point = point;
            CheckIfValidPointsChoosen();
        }

        private void CheckIfValidPointsChoosen()
        {
            if (_curCam1Point.X < 0 || _curCam1Point.Y < 0
                || _curCam2Point.X < 0 || _curCam2Point.Y < 0)
            {
                _butAcceptPoint.IsEnabled = false;
            }
            else
            {
                _butAcceptPoint.IsEnabled = true;
            }
        }

        private void OnSelectedPointChangedFirst(object sender, Point point)
        {
            // If selected on one image, find coupled point on second one
            if (point.X < 0)
            {
                _camImageSec.SelectedPoint = point;
                _butRemovePoint.IsEnabled = false;
                _butAcceptPoint.IsEnabled = false;
                _isPointsSelected = false;
                return;
            }
            foreach (var point3d in Points3D)
            {
                if (point.Equals(point3d.Cam1Point))
                {
                    _camImageSec.SelectedPoint = point3d.Cam2Point;
                    _butRemovePoint.IsEnabled = true;
                    _butAcceptPoint.IsEnabled = true;
                    _curCamPoint = point3d;
                    _isPointsSelected = true;
                    return;
                }
            }
            CheckIfValidPointsChoosen();
        }


        // Selection and temp point should follow ones on CamersImage
        // and change corresponding camera3dpoint in real time
        // Undoing from this level
        private void OnSelectedPointChangedSecond(object sender, Point point)
        {
            if (point.X < 0)
            {
                _camImageFirst.SelectedPoint = point;
                _butRemovePoint.IsEnabled = false;
                _butAcceptPoint.IsEnabled = true;
                _isPointsSelected = false;
                return;
            }
            foreach (var point3d in Points3D)
            {
                if (point.Equals(point3d.Cam2Point))
                {
                    _camImageFirst.SelectedPoint = point3d.Cam1Point;
                    _butRemovePoint.IsEnabled = true;
                    _curCamPoint = point3d;
                    _isPointsSelected = true;
                    return;
                }
            }
            CheckIfValidPointsChoosen();
        }
    }
}
