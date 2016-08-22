using CamControls;
using CamImageProcessing;
using CamCore;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Image3DModule
{
    public partial class PointImagesTabs : UserControl
    {
        public List<Camera3DPoint> Points3D { get; set; }

        private Camera3DPoint _curCamPoint = new Camera3DPoint();
        private bool _isPointsSelected = false;
        Image3DWindow _3dwindow;

        private ParametrizedProcessorsSelectionWindow _featuresMatchOpts;

        public PointImagesTabs()
        {
            Points3D = new List<Camera3DPoint>();
            InitializeComponent();
            _camImageFirst.TemporaryPoint.IsNullChanged += TempPointChanged;
            _camImageSec.TemporaryPoint.IsNullChanged += TempPointChanged;
            _camImageFirst.SelectedPointChanged += OnSelectedPointChanged;
            _camImageSec.SelectedPointChanged += OnSelectedPointChanged;

            _featuresMatchOpts = new ParametrizedProcessorsSelectionWindow();

            _featuresMatchOpts.AddProcessorFamily("Feature Detector");
            _featuresMatchOpts.AddToFamily("Feature Detector", new FeatureSUSANDetector());

            _featuresMatchOpts.AddProcessorFamily("Points Matcher");
            _featuresMatchOpts.AddToFamily("Points Matcher", new LoGCorrelationFeaturesMatcher());
            _featuresMatchOpts.AddToFamily("Points Matcher", new AreaBasedCorrelationImageMatcher());
        }


        private void AcceptImagePoints(object sender, RoutedEventArgs e)
        {
                Camera3DPoint camPoint = new Camera3DPoint()
                {
                    Cam1Img = _camImageFirst.TemporaryPoint.Position,
                    Cam2Img = _camImageSec.TemporaryPoint.Position,
                };
                _butAcceptPoint.IsEnabled = false;
                _camImageFirst.AcceptTempPoint(camPoint).PositionChanged += UpdatePointPosition;
                _camImageSec.AcceptTempPoint(camPoint).PositionChanged += UpdatePointPosition;
                // Compute 3D point
                
            _curCamPoint = new Camera3DPoint();

            // Update window
        }

        private void RemoveImagePoints(object sender, RoutedEventArgs e)
        {
            PointImagePoint toRemove1 = _camImageFirst.SelectedPoint;
            PointImagePoint toRemove2 = _camImageSec.SelectedPoint;
            _camImageFirst.SelectPointQuiet(new PointImagePoint(true));
            _camImageSec.SelectPointQuiet(new PointImagePoint(true));
            _camImageFirst.RemovePoint(toRemove1);
            _camImageSec.RemovePoint(toRemove2);
            Points3D.Remove(_curCamPoint);

            // Update window
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

            // Update window
        }

        private void AutoCorners(object sender, RoutedEventArgs e) ////// EXTRACT METHODS
        {
            // find corners in images and perform corner-based matching
            // 1 ) Show menu with options to choose corner detector and matcher 
            _featuresMatchOpts.ShowDialog();
            if(_featuresMatchOpts.Accepted)
            {
                // Get detector and matcher and compute
                FeaturesDetector detector = (FeaturesDetector)_featuresMatchOpts.GetSelectedProcessor("Feature Detector");
                ImagesMatcher matcher = (ImagesMatcher)_featuresMatchOpts.GetSelectedProcessor("Points Matcher");

                GrayScaleImage leftImage = new GrayScaleImage();
                leftImage.FromBitmapSource(_camImageFirst.ImageSource);
                GrayScaleImage rightImage = new GrayScaleImage();
                rightImage.FromBitmapSource(_camImageSec.ImageSource);

                if (matcher is FeaturesMatcher)
                {
                    ((FeaturesMatcher)matcher).FeatureDetector = detector;
                }
                matcher.LeftImage = leftImage;
                matcher.RightImage = rightImage;
                
                if(matcher.Match())
                {
                    _camImageFirst.ResetPoints();
                    _camImageSec.ResetPoints();
                    _camImageFirst.ImageSource = matcher.LeftImage.ToBitmapSource();
                    _camImageSec.ImageSource = matcher.RightImage.ToBitmapSource();

                    Points3D.Clear();
                    Points3D = matcher.MatchedPoints;
                    foreach(var point in Points3D)
                    {
                        PointImagePoint pipLeft = new PointImagePoint()
                        {
                            Position = point.Cam1Img,
                            Value = point
                        };
                        PointImagePoint pipRight = new PointImagePoint()
                        {
                            Position = point.Cam2Img,
                            Value = point
                        };
                        _camImageFirst.AddPoint(pipLeft);
                        _camImageSec.AddPoint(pipRight);
                    }
                }
            }
        }

        private void Build3DImage(object sender, RoutedEventArgs e)
        {
            Points3D.Clear();
            Points3D.Add(new Camera3DPoint()
            {
                Real = new Point3D(0.0f, 0.0f, 0.0f)
            });

            Points3D.Add(new Camera3DPoint()
            {
                Real = new Point3D(1.0f, 1.0f, 0.0f)
            });

            Points3D.Add(new Camera3DPoint()
            {
                Real = new Point3D(1.0f, 0.0f, 0.0f)
            });

            Points3D.Add(new Camera3DPoint()
            {
                Real = new Point3D(0.0f, 0.0f, -1.0f)
            });

            Points3D.Add(new Camera3DPoint()
            {
                Real = new Point3D(1.0f, 1.0f, -1.0f)
            });

            Points3D.Add(new Camera3DPoint()
            {
                Real = new Point3D(1.0f, 0.0f, -1.0f)
            });

            if (_3dwindow == null)
            {
                _3dwindow = new Image3DWindow();
                _3dwindow.Show();
            }
            else if(!_3dwindow.IsVisible)
            {
                _3dwindow.Hide();
                _3dwindow = new Image3DWindow();
                _3dwindow.Show();
            }

            foreach (var point in Points3D)
            {
                _3dwindow.AddPoint(point);
            }
        }

        private void TempPointChanged(object sender, PointImageEventArgs e)
        {
            // If both temp points are present, point can be accepted
            if (_camImageFirst.TemporaryPoint.IsNullPoint ||
                _camImageSec.TemporaryPoint.IsNullPoint)
            {
                _butAcceptPoint.IsEnabled = false;
            }
            else
            {
                _butAcceptPoint.IsEnabled = true;
            }
            if (_isPointsSelected) // If deselected on one, deselect on other one too
            {
                if (sender == _camImageFirst.TemporaryPoint)
                    _camImageSec.SelectPointQuiet(new PointImagePoint(true));
                else
                    _camImageFirst.SelectPointQuiet(new PointImagePoint(true));
            }
        }

        private void OnSelectedPointChanged(object sender, PointImageEventArgs e)
        {
            if (e.NewImagePoint.IsNullPoint) // Deselected -> deselect on second
            {
                if (sender == _camImageFirst)
                    _camImageSec.SelectPointQuiet(new PointImagePoint(true));
                else
                    _camImageFirst.SelectPointQuiet(new PointImagePoint(true));
                _butRemovePoint.IsEnabled = false;
                _isPointsSelected = false;
            }
            else // If selected on one image, find coupled point on second one and select it
            {
                Camera3DPoint point3d;
                if (sender == _camImageFirst)
                {
                    point3d = (Camera3DPoint)_camImageFirst.SelectedPoint.Value;
                    _camImageSec.SelectPointQuiet(_camImageSec.FindPointByValue(point3d));
                }
                else
                {
                    point3d = (Camera3DPoint)_camImageSec.SelectedPoint.Value;
                    _camImageFirst.SelectPointQuiet(_camImageFirst.FindPointByValue(point3d));
                }
                _butRemovePoint.IsEnabled = true;
                _curCamPoint = point3d;
                _isPointsSelected = true;
            }
        }

        private void UpdatePointPosition(object sender, PointImageEventArgs e)
        {
            if (sender == _camImageFirst)
                ((Camera3DPoint)((PointImagePoint)sender).Value).Cam1Img = e.NewPointPosition;
            else
                ((Camera3DPoint)((PointImagePoint)sender).Value).Cam2Img = e.NewPointPosition;
            // Compute 3d point

            // Update window
        }
    }
}    

