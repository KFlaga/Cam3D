using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using CamControls;
using CamCore;
using CamAlgorithms;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Text;
using System.Linq;
using System.IO;
using CamAlgorithms.Calibration;

namespace CalibrationModule
{
    public partial class CalibrationForOneCameraTab : UserControl, IDisposable
    {
        public SideIndex CameraIndex { get; set; }

        private List<CalibrationPoint> _currentImagePoints = new List<CalibrationPoint>();
        private List<List<Vector2>> _currentImageLines = new List<List<Vector2>>();

        public List<CalibrationPoint> CalibrationPoints { get; set; } = new List<CalibrationPoint>();
        public List<RealGridData> RealGrids { get; set; } = new List<RealGridData>();
        public Camera Camera { get; set; } = new Camera();
        public RadialDistortion Distortion { get { return _distortionCorrector.Distortion; } }

        private Point _curPoint = new Point();
        public List<List<Vector2>> CalibrationLines { get; set; } = new List<List<Vector2>>();

        CameraCalibrationAlgorithmUi _calibrator = new CameraCalibrationAlgorithmUi();
        RadialDistrotionCorrectionAlgorithmUi _distortionCorrector = new RadialDistrotionCorrectionAlgorithmUi();
        PointsExtractionAlgorithmUi _pointsExtractor = new PointsExtractionAlgorithmUi();

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
        
        public CalibrationForOneCameraTab()
        {
            InitializeComponent();
            
            _imageControl.ImageSourceChanged += (s, e) =>
            {
                _butAcceptGrid.IsEnabled = false;
            };
        }

        private void SetImageSize(object sender, RoutedEventArgs e)
        {
            SetImageSizeWindow imageSizeDialog = new SetImageSizeWindow();

            if(_imageControl.ImageSource != null)
            {
                imageSizeDialog.X = _imageControl.ImageSource.PixelWidth;
                imageSizeDialog.Y = _imageControl.ImageSource.PixelHeight;
            }

            bool? res = imageSizeDialog.ShowDialog();
            if(res != null && res == true)
            {
                Camera.ImageWidth = imageSizeDialog.X;
                Camera.ImageHeight = imageSizeDialog.Y;
                CameraPair.Data.GetCamera(CameraIndex).ImageHeight = Camera.ImageHeight;
                CameraPair.Data.GetCamera(CameraIndex).ImageWidth = Camera.ImageWidth;
            }
        }
        
        private void RefreshCalibrationPoints()
        {
            _imageControl.ResetPoints();
            foreach(var cpoint in _currentImagePoints)
            {
                _imageControl.AddPoint(new CamControls.PointImagePoint()
                {
                    Position = new Point(cpoint.ImgX, cpoint.ImgY),
                    Value = cpoint
                });
            }
        }

        private void LoadImageFromMemory(object sender, RoutedEventArgs e)
        {
            if(_cameraCaptureImage != null)
            {
                _imageControl.ImageSource = _cameraCaptureImage;
            }
        }
        
        private void _butAcceptGrid_Click(object sender, RoutedEventArgs e)
        {
            int gridnum = (int)_textGridNum.GetNumber();
            foreach(var cp in _currentImagePoints)
            {
                cp.GridNum = gridnum;
            }
            CalibrationPoints.AddRange(_currentImagePoints);
            CalibrationLines.AddRange(_currentImageLines);

            _currentImagePoints = new List<CalibrationPoint>();
            _currentImageLines = new List<List<Vector2>>();

            _butAcceptGrid.IsEnabled = false;
        }

        private void _butResetPoints_Click(object sender, RoutedEventArgs e)
        {
            _currentImagePoints.Clear();
            _butAcceptGrid.IsEnabled = false;
            if(_imageControl.ImageSource != null)
                _imageControl.ResetPoints();
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

        private void ManageLines(object sender, RoutedEventArgs e)
        {
            CalibrationLinesManagerWindow linesManager = new CalibrationLinesManagerWindow();
            linesManager.CalibrationLines = CalibrationLines;
            bool? res = linesManager.ShowDialog();
            if(res != null && res == true)
            {
                CalibrationLines = linesManager.CalibrationLines;
            }
        }

        // Finds automatically calibration points on standard calibration image
        // (that is big black dots on white background )
        // In point management one still have to set correct grid number for each point
        private void FindCalibrationPoints(object sender, RoutedEventArgs e)
        {
            if(_imageControl.ImageSource == null) { return; }

            MaskedImage img = new MaskedImage();
            img.FromBitmapSource(_imageControl.ImageSource);
            _pointsExtractor.Image = img;
            
            AlgorithmWindow window = new AlgorithmWindow(_pointsExtractor);
            _pointsExtractor.StatusChanged += _pointsExtractor_StatusChanged;
            window.Show();
        }

        private void _pointsExtractor_StatusChanged(object sender, AlgorithmEventArgs e)
        {
            if(e.CurrentStatus == AlgorithmStatus.Finished)
            {
                Dispatcher.Invoke(() =>
                {
                    var algorithm = (PointsExtractionAlgorithmUi)e.Algorithm;
                    _currentImagePoints = algorithm.Points;
                    _currentImageLines = algorithm.CalibrationLines;
                    RefreshCalibrationPoints();

                    _butAcceptGrid.IsEnabled = true;
                });
            }
        }
        
        private void ComputeDistortionCorrectionParameters(object sender, RoutedEventArgs e)
        {
            if(!(Camera.ImageHeight > 0 || Camera.ImageWidth > 0))
            {
                MessageBox.Show("Image size must be set");
                return;
            }
            if(CalibrationLines.Count == 0)
            {
                MessageBox.Show("Calibration lines must be set");
                return;
            }
            
            _distortionCorrector.ImageHeight = Camera.ImageHeight;
            _distortionCorrector.ImageWidth = Camera.ImageWidth;
            _distortionCorrector.CorrectionLines = CalibrationLines;

            AlgorithmWindow algWindow = new AlgorithmWindow(_distortionCorrector);
            algWindow.Show();
        }

        private void UndistortCalibrationPoints(object sender, RoutedEventArgs e)
        {
            if(CalibrationPoints == null || CalibrationPoints.Count == 0)
            {
                MessageBox.Show("CalibrationPoints must be set");
                return;
            }
            if(Distortion.Model == null)
            {
                MessageBox.Show("Distortion Model must be set");
                return;
            }
            if(!(Camera.ImageHeight > 0 || Camera.ImageWidth > 0))
            {
                MessageBox.Show("Image size must be set");
                return;
            }
            
            List<Vector2> imgPoints = new List<Vector2>( CalibrationPoints.Select( (cp) => { return cp.Img; } ));
            _distortionCorrector.ImageHeight = Camera.ImageHeight;
            _distortionCorrector.ImageWidth = Camera.ImageWidth;
            imgPoints = _distortionCorrector.Algorithm.CorrectPoints(imgPoints);
            for(int i = 0; i < CalibrationPoints.Count; ++i)
            {
                CalibrationPoints[i].Img = imgPoints[i];
            }
        }

        private void UndistortImage(object sender, RoutedEventArgs e)
        {
            if(_imageControl.ImageSource == null)
            {
                MessageBox.Show("Image must be set");
                return;
            }

            ImageTransformer undistort = new ImageTransformer(ImageTransformer.InterpolationMethod.Cubic, 1)
            {
                Transformation = new RadialDistortionTransformation(Distortion.Model)
            };

            MaskedImage img = new MaskedImage();
            img.FromBitmapSource(_imageControl.ImageSource);

            MaskedImage imgFinal = undistort.TransfromImageBackwards(img, true);
            _imageControl.ImageSource = imgFinal.ToBitmapSource();
        }
        
        private void AcceptDistortionModel(object sender, RoutedEventArgs e)
        {
            CameraPair.Data.GetCamera(CameraIndex).Distortion = _distortionCorrector.Distortion;
        }
        
        private void SaveDistortionModel(object sender, RoutedEventArgs e)
        {
            if(Distortion.Model == null)
            {
                MessageBox.Show("Distortion model must be set");
                return;
            }
            FileOperations.SaveToFile((f, p) => { XmlSerialisation.SaveToFile(Distortion, f); }, "Xml File|*.xml");
        }

        private void LoadDistortionModel(object sender, RoutedEventArgs e)
        {
            FileOperations.LoadFromFile((f, p) => { _distortionCorrector.Distortion = XmlSerialisation.CreateFromFile<RadialDistortion>(f); }, "Xml File|*.xml");
        }

        private void Calibrate(object sender, RoutedEventArgs e)
        {
            if(CalibrationPoints.Count < 6)
            {
                MessageBox.Show("Need at least 6 points");
                return;
            }
            if(!(Camera.ImageHeight > 0 || Camera.ImageWidth > 0))
            {
                MessageBox.Show("Image size must be set");
                return;
            }

            for(int p = 0; p < CalibrationPoints.Count; p++)
            {
                CalibrationPoint cp = CalibrationPoints[p];
                if(cp.GridNum >= RealGrids.Count)
                {
                    MessageBox.Show("Calibration Point have grid number = " + cp.GridNum + " but only " + RealGrids.Count + " grids defined");
                    return;
                }
                // First compute real point for every calib point
                RealGridData grid = RealGrids[cp.GridNum];

                cp.RealCol += CameraIndex == SideIndex.Left
                    ? grid.OffsetLeft.X : grid.OffsetRight.X;
                cp.RealRow += CameraIndex == SideIndex.Left
                    ? grid.OffsetLeft.Y : grid.OffsetRight.Y;

                cp.Real = grid.GetRealFromCell(cp.RealRow, cp.RealCol);
            }

            _calibrator.Camera = Camera;
            _calibrator.Points = CalibrationPoints;
            _calibrator.Grids = RealGrids;
            _calibrator.StatusChanged += _calibrator_StatusChanged;

            AlgorithmWindow algWindow = new AlgorithmWindow(_calibrator);
            algWindow.Show();
        }

        private void _calibrator_StatusChanged(object sender, AlgorithmEventArgs e)
        {
            if(e.CurrentStatus == AlgorithmStatus.Finished)
            {
                Dispatcher.Invoke(() =>
                {
                    Camera = _calibrator.Camera;
                    RealGrids = _calibrator.Grids;
                });
            }
        }

        private void AcceptCalibration(object sender, RoutedEventArgs e)
        {
            CameraPair.Data.SetCameraMatrix(CameraIndex, Camera.Matrix);
        }

        private void TestCalibration(object sender, RoutedEventArgs e)
        {
            double error = 0.0;
            double relerror = 0.0;
            double rerrx = 0.0;
            double rerry = 0.0;
            for(int p = 0; p < CalibrationPoints.Count; p++)
            {
                CalibrationPoint cp = CalibrationPoints[p];
                // First compute real point for every calib point
                RealGridData grid = RealGrids[cp.GridNum];
                cp.Real = grid.GetRealFromCell(cp.RealRow, cp.RealCol);

                Vector<double> ip = new DenseVector(new double[] { cp.ImgX, cp.ImgY, 1.0 });
                Vector<double> rp = new DenseVector(new double[] { cp.RealX, cp.RealY, cp.RealZ, 1.0 });

                Vector<double> eip = _calibrator.Camera.Matrix * rp;
                eip.DivideThis(eip[2]);

                var d = (ip - eip);
                error += d.L2Norm();
                ip[2] = 0.0;
                relerror += d.L2Norm() / ip.L2Norm();
                rerrx += Math.Abs(d[0]) / Math.Abs(ip[0]);
                rerry += Math.Abs(d[1]) / Math.Abs(ip[1]);
            }

            StringBuilder result = new StringBuilder();
            result.AppendLine();
            result.AppendLine("Projection error ( d(xi, PXr)^2 ): ");
            result.AppendLine("Points count: " + CalibrationPoints.Count.ToString());
            result.AppendLine("Total: " + error.ToString("F4"));
            result.AppendLine("Mean: " + (error / CalibrationPoints.Count).ToString("F4"));
            result.AppendLine("Relative: " + (relerror).ToString("F4"));
            result.AppendLine("Relative mean: " + (relerror / CalibrationPoints.Count).ToString("F4"));
            result.AppendLine("Relative in X: " + (rerrx).ToString("F4"));
            result.AppendLine("Relative in X mean: " + (rerrx / CalibrationPoints.Count).ToString("F4"));
            result.AppendLine("Relative in Y: " + (rerry).ToString("F4"));
            result.AppendLine("Relative in Y mean: " + (rerry / CalibrationPoints.Count).ToString("F4"));

            MessageBox.Show(result.ToString());
        }

        private void SaveCalibration(object sender, RoutedEventArgs e)
        {
            FileOperations.SaveToFile((f, p) => { XmlSerialisation.SaveToFile(Camera, f); }, "Xml File|*.xml");
        }

        private void LoadCalibration(object sender, RoutedEventArgs e)
        {
            FileOperations.LoadFromFile((f, p) => { Camera = XmlSerialisation.CreateFromFile<Camera>(f); }, "Xml File|*.xml");
        }
        
        public void Dispose()
        {

        }
    }
}
