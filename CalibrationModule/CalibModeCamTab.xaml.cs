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

namespace CalibrationModule
{
    public partial class CalibModeCamTab : UserControl, IDisposable
    {
        private List<CalibrationPoint> _currentImageGrid = new List<CalibrationPoint>();
        private CalibrationPointsFinder _currentImagePointFinder = null;

        public List<CalibrationPoint> CalibrationPoints { get; set; }
        public List<RealGridData> RealGrids { get; set; }
        public Matrix<double> CameraMatrix { get; set; }

        private Point _curPoint;
        private List<List<Vector2>> _calibLines;

        private CamCalibrator _calibrator;
        RadialDistortionCorrector _distortionCorrector = new RadialDistortionCorrector();

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
                if(_cameraSnaphotReceiver != null)
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

        private ParametrizedProcessorsSelectionWindow _finderChooseWindow;

        public CalibModeCamTab()
        {
            _curPoint = new Point();
            InitializeComponent();
            CalibrationPoints = new List<CalibrationPoint>();
            RealGrids = new List<RealGridData>();
            _calibLines = new List<List<Vector2>>();
            _imageControl.TemporaryPoint.IsNullChanged += (s, e) =>
            {
                _butAcceptPoint.IsEnabled = !e.IsNewPointNull;
            };
            _imageControl.SelectedPointChanged += (s, e) =>
            {
                _butEditPoint.IsEnabled = e.IsNewPointSelected;
            };

            _finderChooseWindow = new ParametrizedProcessorsSelectionWindow();
            _finderChooseWindow.AddProcessorFamily("Calibration Points Finder");
            _finderChooseWindow.AddToFamily("Calibration Points Finder", new ShapesGridCPFinder());

            _finderChooseWindow.AddProcessorFamily("Primary CalibShape Qualifier");
            _finderChooseWindow.AddToFamily("Primary CalibShape Qualifier", new RedNeighbourhoodChecker());

            _imageControl.ImageSourceChanged += (s, e) =>
            {
                _butAcceptGrid.IsEnabled = false;
            };

            _calibrator = new CamCalibrator();
           // _distortionCorrector = _calibrator.DistortionCorrector;
        }

        private void AcceptImagePoint(object sender, RoutedEventArgs e)
        {
            // Open real point dialog
            ChooseRealGridPointDialog realPointDialog = new ChooseRealGridPointDialog();
            bool? res = realPointDialog.ShowDialog();
            if(res != null && res == true)
            {
                CalibrationPoint cp = new CalibrationPoint()
                {
                    ImgX = (double)_curPoint.X,
                    ImgY = (double)_curPoint.Y,
                    GridNum = realPointDialog.GridNum,
                    RealCol = realPointDialog.X,
                    RealRow = realPointDialog.Y
                };
                _currentImageGrid.Add(cp);
                _imageControl.AcceptTempPoint(cp);
                _butAcceptGrid.IsEnabled = true;
            }
        }

        private void RefreshCalibrationPoints()
        {
            _imageControl.ResetPoints();
            foreach(var cpoint in _currentImageGrid)
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
            if(res != null && res == true)
            {
                RealGrids = gridsManager.RealGrids;
            }
        }

        private void ManagePoints(object sender, RoutedEventArgs e)
        {
            CalibrationPointsManagerWindow pointsManager = new CalibrationPointsManagerWindow();
            pointsManager.CalibrationPoints = CalibrationPoints;
            bool? res = pointsManager.ShowDialog();
            if(res != null && res == true)
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
            if(_imageControl.ImageSource == null)
                return;

            _finderChooseWindow.ShowDialog();
            if(_finderChooseWindow.Accepted)
            {
                _currentImagePointFinder = (CalibrationPointsFinder)
                    _finderChooseWindow.GetSelectedProcessor("Calibration Points Finder");
                _currentImagePointFinder.PrimaryShapeChecker = (ShapeChecker)
                    _finderChooseWindow.GetSelectedProcessor("Primary CalibShape Qualifier");

                _currentImagePointFinder.SetBitmapSource(_imageControl.ImageSource);
                _currentImagePointFinder.FindCalibrationPoints();

                _currentImagePointFinder.LinesExtractor.ExtractLines();

                _currentImageGrid = _currentImagePointFinder.Points;
                RefreshCalibrationPoints();

                _butAcceptGrid.IsEnabled = true;
            }
        }

        private void Calibrate(object sender, RoutedEventArgs e)
        {
            if(CalibrationPoints.Count < 6)
            {
                MessageBox.Show("Need at least 6 points");
                return;
            }
            if(_imageControl.ImageSource == null)
            {
                MessageBox.Show("Calibratiion image must be set");
                return;
            }
            for(int p = 0; p < CalibrationPoints.Count; p++)
            {
                CalibrationPoint cp = CalibrationPoints[p];
                if(cp.GridNum >= RealGrids.Count)
                {
                    // TODO ERROR
                    continue;
                }
                // First compute real point for every calib point
                RealGridData grid = RealGrids[cp.GridNum];
                cp.ImgY = _imageControl.ImageSource.PixelHeight - cp.ImgY;
                cp.ImgX = _imageControl.ImageSource.PixelWidth - cp.ImgX;
                cp.RealX = (double)(cp.RealCol * grid.WidthX + cp.RealRow * grid.HeightX + grid.ZeroX);
                cp.RealY = (double)(cp.RealCol * grid.WidthY + cp.RealRow * grid.HeightY + grid.ZeroY);
                cp.RealZ = (double)(cp.RealCol * grid.WidthZ + cp.RealRow * grid.HeightZ + grid.ZeroZ);
            }

            ParametersSelectionWindow calibOpts = new ParametersSelectionWindow();
            calibOpts.Processor = _calibrator;
            _calibrator.Points = CalibrationPoints;

            calibOpts.ShowDialog();
            if(calibOpts.Accepted)
            {
                _calibrator.Calibrate();

                CameraMatrix = _calibrator.CameraMatrix;
                Calibrated?.Invoke(this, new EventArgs());
            }
        }

        private void ComputeDistortionCorrectionParameters(object sender, RoutedEventArgs e)
        {
            if(_calibLines.Count > 0)
            {
                var img = _imageControl.ImageSource;
                _distortionCorrector.ImageHeight = img.PixelHeight;
                _distortionCorrector.ImageWidth = img.PixelWidth;
                _distortionCorrector.CorrectionLines = _calibLines;

                AlgorithmWindow algWindow = new AlgorithmWindow(_distortionCorrector);
                algWindow.Show();
            }
        }

        private void UndistortCalibrationPoints(object sender, RoutedEventArgs e)
        {
            _calibrator.Points = CalibrationPoints;
           // _calibrator.UndistortCalibrationPoints();
        }

        private void UndistortImage(object sender, RoutedEventArgs e)
        {
            ColorImage img = new ColorImage();
            img.FromBitmapSource(_imageControl.ImageSource);
            int pointsCount = img.SizeX * img.SizeY;

            _distortionCorrector.MeasuredPoints = new List<Vector2>(pointsCount);
            for(int c = 0; c < img.SizeX; ++c)
            {
                for(int r = 0; r < img.SizeY; ++r)
                {
                    _distortionCorrector.MeasuredPoints.Add(new Vector2(r, c));
                }
            }

            ColorImage imgFinal = new ColorImage();
            imgFinal.ImageMatrix[0] = new DenseMatrix(img.SizeY, img.SizeX);
            imgFinal.ImageMatrix[1] = new DenseMatrix(img.SizeY, img.SizeX);
            imgFinal.ImageMatrix[2] = new DenseMatrix(img.SizeY, img.SizeX);

            _distortionCorrector.CorrectPoints();
            for(int c = 0; c < img.SizeX; ++c)
            {
                for(int r = 0; r < img.SizeY; ++r)
                {
                    // For each point -> save its value in closest pixel
                    int closestX = _distortionCorrector.CorrectedPoints[c * img.SizeY + r].X.Round();
                    int closestY = _distortionCorrector.CorrectedPoints[c * img.SizeY + r].Y.Round();
                    imgFinal[closestY, closestX, 0] = img[r, c, 0];
                    imgFinal[closestY, closestX, 1] = img[r, c, 1];
                    imgFinal[closestY, closestX, 2] = img[r, c, 2];
                }
            }

            _imageControl.ImageSource = imgFinal.ToBitmapSource();
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
            if(res != null && res == true)
            {
                cpoint.GridNum = realPointDialog.GridNum;
                cpoint.RealCol = realPointDialog.X;
                cpoint.RealRow = realPointDialog.Y;
            }
        }

        private void _butAcceptGrid_Click(object sender, RoutedEventArgs e)
        {
            if(_currentImageGrid != null && _currentImagePointFinder != null)
            {
                int gridnum = (int)_textGridNum.GetNumber();
                foreach(var cp in _currentImageGrid)
                {
                    cp.GridNum = gridnum;
                }
                CalibrationPoints.AddRange(_currentImageGrid);
                _calibLines.AddRange(_currentImagePointFinder.LinesExtractor.CalibrationLines);

                _currentImageGrid = new List<CalibrationPoint>();
                _currentImagePointFinder = null;

                _butAcceptGrid.IsEnabled = false;
            }
        }

        private void _butResetPoints_Click(object sender, RoutedEventArgs e)
        {
            _currentImageGrid.Clear();
            _butAcceptGrid.IsEnabled = false;
            if(_imageControl.ImageSource != null)
                _imageControl.ResetPoints();
        }
        
        public void Dispose()
        {
            _finderChooseWindow.Close();
        }
    }
}
