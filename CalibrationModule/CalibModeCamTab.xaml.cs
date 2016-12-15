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
using System.Xml;
using System.IO;

namespace CalibrationModule
{
    public partial class CalibModeCamTab : UserControl, IDisposable
    {
        public CalibrationData.CameraIndex CameraIndex { get; set; }

        private List<CalibrationPoint> _currentImageGrid = new List<CalibrationPoint>();
        private CalibrationPointsFinder _currentImagePointFinder = null;

        public List<CalibrationPoint> CalibrationPoints { get; set; } = new List<CalibrationPoint>();
        public List<RealGridData> RealGrids { get; set; } = new List<RealGridData>();
        public Matrix<double> CameraMatrix { get { return _calibrator.CameraMatrix; } }
        public RadialDistortionModel DistortionModel { get { return _distortionCorrector.DistortionModel; } }

        private Point _curPoint = new Point();
        private List<List<Vector2>> _calibLines = new List<List<Vector2>>();

        private CamCalibrator _calibrator = new CamCalibrator();
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

        private ParametrizedProcessorsSelectionWindow _finderChooseWindow;

        public CalibModeCamTab()
        {
            InitializeComponent();

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

        private void ManageLines(object sender, RoutedEventArgs e)
        {
            CalibrationLinesManagerWindow linesManager = new CalibrationLinesManagerWindow();
            linesManager.CalibrationLines = _calibLines;
            bool? res = linesManager.ShowDialog();
            if(res != null && res == true)
            {
                _calibLines = linesManager.CalibrationLines;
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

                MaskedImage img = new MaskedImage();
                img.FromBitmapSource(_imageControl.ImageSource);
                _currentImagePointFinder.Image = img;
                _currentImagePointFinder.FindCalibrationPoints();

                if(_currentImagePointFinder.Points != null)
                {
                    _currentImagePointFinder.LinesExtractor.ExtractLines();

                    _currentImageGrid = _currentImagePointFinder.Points;
                    RefreshCalibrationPoints();

                    _butAcceptGrid.IsEnabled = true;
                }
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
                cp.Real = grid.GetRealFromCell(cp.RealRow, cp.RealCol);
            }

            _calibrator.Points = CalibrationPoints;
            _calibrator.Grids = RealGrids;

            AlgorithmWindow algWindow = new AlgorithmWindow(_calibrator);
            algWindow.Show();
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
            if(_currentImageGrid != null && _currentImageGrid.Count > 0)
            {
                foreach(var point in _currentImageGrid)
                {
                    DistortionModel.P = point.Img * DistortionModel.ImageScale;
                    DistortionModel.Undistort();
                    point.Img = DistortionModel.Pf / DistortionModel.ImageScale;
                }
                RefreshCalibrationPoints();
            }
        }

        private void UndistortImage(object sender, RoutedEventArgs e)
        {
            ImageTransformer undistort = new ImageTransformer(ImageTransformer.InterpolationMethod.Quadratic, 1);
            undistort.Transformation = new RadialDistortionTransformation(DistortionModel);

            MaskedImage img = new MaskedImage();
            img.FromBitmapSource(_imageControl.ImageSource);

            MaskedImage imgFinal = undistort.TransfromImageBackwards(img, true);
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

        private void AcceptDistortionModel(object sender, RoutedEventArgs e)
        {
            CalibrationData.Data.SetDistortionModel(CameraIndex, _distortionCorrector.DistortionModel);
        }

        private void AcceptCalibration(object sender, RoutedEventArgs e)
        {
            CalibrationData.Data.SetCameraMatrix(CameraIndex, _calibrator.CameraMatrix);
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

                Vector<double> eip = _calibrator.CameraMatrix * rp;
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
            result.AppendLine("Realtive: " + (relerror).ToString("F4"));
            result.AppendLine("Realtive mean: " + (relerror / CalibrationPoints.Count).ToString("F4"));
            result.AppendLine("Realtive in X: " + (rerrx).ToString("F4"));
            result.AppendLine("Realtive in X mean: " + (rerrx / CalibrationPoints.Count).ToString("F4"));
            result.AppendLine("Realtive in Y: " + (rerry).ToString("F4"));
            result.AppendLine("Realtive in Y mean: " + (rerry / CalibrationPoints.Count).ToString("F4"));

            MessageBox.Show(result.ToString());
        }
        
        private void SaveDistortionModel(object sender, RoutedEventArgs e)
        {
            FileOperations.SaveToFile(SaveDistortionModel, "Xml File|*.xml");
        }

        private void SaveDistortionModel(Stream file, string path)
        {
            if(DistortionModel != null)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.AppendChild(XmlExtensions.CreateDistortionModelNode(xmlDoc, DistortionModel));
                xmlDoc.Save(file);
            }
        }

        private void LoadDistortionModel(object sender, RoutedEventArgs e)
        {
            FileOperations.LoadFromFile(LoadDistortionModel, "Xml File|*.xml");
        }

        private void LoadDistortionModel(Stream file, string path)
        {
            XmlDocument dataDoc = new XmlDocument();
            dataDoc.Load(file);

            XmlNodeList models = dataDoc.GetElementsByTagName("DistortionModel");
            if(models.Count == 1)
            {
                XmlNode modelNode = models[0];
                _distortionCorrector.DistortionModel = 
                    XmlExtensions.DistortionModelFromNode(modelNode);
            }
        }

        private void SaveCalibration(object sender, RoutedEventArgs e)
        {
            FileOperations.SaveToFile(SaveCalibration, "Xml File|*.xml");
        }

        private void SaveCalibration(Stream file, string path)
        {
            if(CameraMatrix != null)
            {
                XmlDocument xmlDoc = new XmlDocument();
                var cameraNode = xmlDoc.CreateElement("Camera");

                var cam1AttNum = xmlDoc.CreateAttribute("num");
                cam1AttNum.Value = CameraIndex == CalibrationData.CameraIndex.Left ? "1" : "2";
                cameraNode.Attributes.Append(cam1AttNum);

                cameraNode.AppendChild(XmlExtensions.CreateMatrixNode(xmlDoc, CameraMatrix));

                xmlDoc.AppendChild(cameraNode);
                xmlDoc.Save(file);
            }
        }

        private void LoadCalibration(object sender, RoutedEventArgs e)
        {
            FileOperations.LoadFromFile(LoadCalibration, "Xml File|*.xml");
        }

        private void LoadCalibration(Stream file, string path)
        {
            XmlDocument dataDoc = new XmlDocument();
            dataDoc.Load(file);

            XmlNodeList cameras = dataDoc.GetElementsByTagName("Camera");
            if(cameras.Count == 1)
            {
                XmlNode camNode = cameras[0];
                _calibrator.CameraMatrix = XmlExtensions.MatrixFromNode(camNode.FirstChildWithName("Matrix"));
            }
        }
    }
}
