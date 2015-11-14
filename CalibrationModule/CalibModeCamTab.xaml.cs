using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace CalibrationModule
{
    public partial class CalibModeCamTab : UserControl
    {
        public List<CalibrationPoint> CalibrationPoints { get; set; }
        public List<RealGridData> RealGrids { get; set; }
        public Matrix<float> CameraMatrix { get; set; }

        private bool _radialCorrection = false;
        private Point _curPoint;

        private CamCore.InterModularDataReceiver _cameraSnaphotReceiver;
        private BitmapSource _cameraCaptureImage;
        private string _cameraCaptureLabel = null;
        public string CameraCaptureLabel
        {
            get
            {
                return _cameraCaptureLabel;
            }
            set
            {
                if (_cameraSnaphotReceiver != null)
                    CamCore.InterModularConnection.UnregisterDataReceiver(_cameraSnaphotReceiver);
                _cameraCaptureLabel = value;
                _cameraSnaphotReceiver = CamCore.InterModularConnection.RegisterDataReceiver(_cameraCaptureLabel, (img) =>
                {
                    _cameraCaptureImage = (BitmapSource)img;
                }, true);
            }
        }
        public bool IsCameraCaptureInMemory { get { return _cameraCaptureImage != null; } }

        public event EventHandler<EventArgs> Calibrated;

        public CalibModeCamTab()
        {
            _curPoint = new Point();
            InitializeComponent();
            CalibrationPoints = new List<CalibrationPoint>();
            RealGrids = new List<RealGridData>();
            _imageControl.TemporaryPoint.IsNullChanged += (s, e) =>
            {
                _butAcceptPoint.IsEnabled = !e.IsNewPointNull;
            };
            _imageControl.SelectedPointChanged += (s, e) =>
            {
                _butEditPoint.IsEnabled = e.IsNewPointSelected;
            };
        }
        
        private void AcceptImagePoint(object sender, RoutedEventArgs e)
        {
            // Open real point dialog
            ChooseRealGridPointDialog realPointDialog = new ChooseRealGridPointDialog();
            bool? res = realPointDialog.ShowDialog();
            if (res != null && res == true)
            {
                CalibrationPoint cp = new CalibrationPoint()
                {
                    ImgX = (float)_curPoint.X,
                    ImgY = (float)_curPoint.Y,
                    GridNum = realPointDialog.GridNum,
                    RealCol = realPointDialog.X,
                    RealRow = realPointDialog.Y
                };
                CalibrationPoints.Add(cp);
                _imageControl.AcceptTempPoint(cp);
            }
        }

        private void RefreshCalibrationPoints()
        {
            _imageControl.ResetPoints();
            foreach (var cpoint in CalibrationPoints)
            {
                _imageControl.AddPoint(new CamControls.PointImagePoint()
                {
                    Position = new Point(cpoint.ImgX, cpoint.ImgY),
                    Value = cpoint
                });
            }
        }

        private void ManageGrids(object sender, RoutedEventArgs e)
        {
            RealGridsManagerWindow gridsManager = new RealGridsManagerWindow();
            gridsManager.RealGrids = RealGrids;
            bool? res = gridsManager.ShowDialog();
            if (res != null && res == true)
            {
                RealGrids = gridsManager.RealGrids;
            }
        }

        private void ManagePoints(object sender, RoutedEventArgs e)
        {
            CalibrationPointsManagerWindow pointsManager = new CalibrationPointsManagerWindow();
            pointsManager.CalibrationPoints = CalibrationPoints;
            bool? res = pointsManager.ShowDialog();
            if (res != null && res == true)
            {
                CalibrationPoints = pointsManager.CalibrationPoints;
                RefreshCalibrationPoints();
            }
        }

        // Finds automatically calibration points on standard calibration image
        // (that is big black dots on white background )
        // In point managment one still have to set correct grid number for each point
        private void FindCalibrationPoints(object sender, RoutedEventArgs e)
        {
            if (_imageControl.ImageSource == null)
                return;

            CalibrationPointsFinder pointFinder = new CalibrationPointsFinder();
            pointFinder.SetBitmapSource(_imageControl.ImageSource);
            pointFinder.FindCalibrationPoints();

            CalibrationPoints = pointFinder.Points;
            RefreshCalibrationPoints();
        }

        private void Calibrate(object sender, RoutedEventArgs e)
        {
            if(CalibrationPoints.Count < 6)
            {
                MessageBox.Show("Need at least 6 points");
                return;
            }
            for (int p = 0; p < CalibrationPoints.Count; p++)
            {
                CalibrationPoint cp = CalibrationPoints[p];
                if (cp.GridNum >= RealGrids.Count)
                {
                    // TODO ERROR
                    continue;
                }
                // First compute real point for every calib point
                RealGridData grid = RealGrids[cp.GridNum];
                cp.ImgY = _imageControl.ImageSource.PixelHeight - cp.ImgY;
                cp.ImgX = _imageControl.ImageSource.PixelWidth - cp.ImgX;
                cp.RealX = (float)(cp.RealCol * grid.WidthX + cp.RealRow * grid.HeightX + grid.ZeroX);
                cp.RealY = (float)(cp.RealCol * grid.WidthY + cp.RealRow * grid.HeightY + grid.ZeroY);
                cp.RealZ = (float)(cp.RealCol * grid.WidthZ + cp.RealRow * grid.HeightZ + grid.ZeroZ);
            }

            CamCalibrator calibrator = new CamCalibrator();
            calibrator.Points = CalibrationPoints;
            calibrator.PerformRadialCorrection = _radialCorrection;
            calibrator.Calibrate();

            CameraMatrix = calibrator.CameraMatrix;
            if (Calibrated != null)
                Calibrated(this, new EventArgs());

          //  for (int cp = 0; cp < CalibrationPoints.Count; cp++)
          //  {
          //      CalibrationPoints[cp].RealX = calibrator.CorrectedRealPoints[0, cp];
          //      CalibrationPoints[cp].RealY = calibrator.CorrectedRealPoints[1, cp];
          //      CalibrationPoints[cp].RealZ = calibrator.CorrectedRealPoints[2, cp];
          //  }
          //  RefreshCalibrationPoints();
        }

        private void RadialDistortionCorrection(object sender, RoutedEventArgs e)
        {
            _radialCorrection = _cbRadialCorrection.IsChecked.HasValue && _cbRadialCorrection.IsChecked.Value == true;
        }

        private void LoadImageFromMemory(object sender, RoutedEventArgs e)
        {
            if(_cameraCaptureImage != null)
            {
                _imageControl.ImageSource = _cameraCaptureImage;
            }
        }

        private void _butEditPoint_Click(object sender, RoutedEventArgs e)
        {
            var cpoint = (CalibrationPoint)_imageControl.SelectedPoint.Value;

            ChooseRealGridPointDialog realPointDialog = new ChooseRealGridPointDialog();
            realPointDialog.X = cpoint.RealCol;
            realPointDialog.Y = cpoint.RealRow;
            realPointDialog.GridNum = cpoint.GridNum;
            bool? res = realPointDialog.ShowDialog();
            if (res != null && res == true)
            {
                cpoint.GridNum = realPointDialog.GridNum;
                cpoint.RealCol = realPointDialog.X;
                cpoint.RealRow = realPointDialog.Y;
            }
        }
    }
}
