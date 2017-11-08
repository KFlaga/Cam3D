using CamControls;
using CamAlgorithms;
using CamCore;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using CamAlgorithms.ImageMatching;
using System;
using System.Windows.Media.Imaging;
using System.IO;
using System.Xml;
using CamAlgorithms.Calibration;

namespace RectificationModule
{
    public class FeaturesMatchedEventArgs : EventArgs
    {
        public List<Vector2> MatchedPointsLeft { get; set; }
        public List<Vector2> MatchedPointsRight { get; set; }
        public ColorImage ImageLeft { get; set; }
        public ColorImage ImageRight { get; set; }
    }

    public partial class FeatureImagesTab : UserControl
    {
        GrayScaleImage _featureImgLeft;
        GrayScaleImage _featureImgRight;
        List<IntVector2> _featuresLeft;
        List<IntVector2> _featuresRight;
        FeatureMatchingAlgorithm _matcher;
        List<MatchedPair> _matches;
        private bool _isPointsSelected;

        public MaskedImage ImageLeft { get; set; }
        public MaskedImage ImageRight { get; set; }

        public GrayScaleImage FeatureImageLeft
        {
            get { return _featureImgLeft; }
            set
            {
                _featureImgLeft = value;
                if(_featureImgLeft != null)
                {
                    _camImageFirst.ImageSource = _featureImgLeft.ToBitmapSource();
                }
            }
        }

        public GrayScaleImage FeatureImageRight
        {
            get { return _featureImgRight; }
            set
            {
                _featureImgRight = value;
                if(_featureImgRight != null)
                {
                    _camImageSec.ImageSource = _featureImgRight.ToBitmapSource();
                }
            }
        }

        public List<IntVector2> FeatureListLeft
        {
            get { return _featuresLeft; }
            set { _featuresLeft = value; }
        }

        public List<IntVector2> FeatureListRight
        {
            get { return _featuresRight; }
            set { _featuresRight = value; }
        }

        public FeatureImagesTab()
        {
            InitializeComponent();

            _camImageFirst.ImageSourceChanged += (s, e) =>
            {
                ImageLeft = new MaskedImage();
                ImageLeft.FromBitmapSource(e.NewImage);
            };

            _camImageSec.ImageSourceChanged += (s, e) =>
            {
                ImageRight = new MaskedImage();
                ImageRight.FromBitmapSource(e.NewImage);
            };

            _matcher = new FeatureMatchingAlgorithm();
            _matcher.StatusChanged += _matcher_StatusChanged;
            
            _camImageFirst.SelectedPointChanged += OnSelectedPointChanged;
            _camImageSec.SelectedPointChanged += OnSelectedPointChanged;
        }

        private void RectifyImages(object sender, RoutedEventArgs e)
        {
            if(_camImageFirst.ImageSource == null || _camImageSec.ImageSource == null)
            {
                MessageBox.Show("Images must be set");
                return;
            }
            if(_camImageFirst.ImageSource.PixelWidth != _camImageSec.ImageSource.PixelWidth ||
                _camImageFirst.ImageSource.PixelHeight != _camImageSec.ImageSource.PixelHeight)
            {
                MessageBox.Show("Images must have same size");
                return;
            }
            // if(CalibrationData.Data.IsCamLeftCalibrated == false ||
            //     CalibrationData.Data.IsCamRightCalibrated == false)
            // {
            //     MessageBox.Show("Cameras must be calibrated");
            //     return;
            // }

            ImageRectification rectifier = new ImageRectification(new ImageRectification_FussieloUncalibrated()
            {
                UseInitialCalibration = false
            });
            rectifier.ImageHeight = _camImageFirst.ImageSource.PixelHeight;
            rectifier.ImageWidth = _camImageFirst.ImageSource.PixelWidth;
            rectifier.Cameras = CameraPair.Data;
            rectifier.MatchedPairs = new List<Vector2Pair>();
            foreach(var m in _matches)
            {
                rectifier.MatchedPairs.Add(new Vector2Pair()
                {
                    V1 = m.LeftPoint,
                    V2 = m.RightPoint
                });
            }

            rectifier.ComputeRectificationMatrices();

            ImageTransformer transformer = new ImageTransformer();
            transformer.UsedInterpolationMethod = ImageTransformer.InterpolationMethod.Quadratic;

            transformer.Transformation = new RectificationTransformation()
            {
                RectificationMatrix = rectifier.RectificationLeft,
                RectificationMatrixInverse = rectifier.RectificationLeft_Inverse,
            }; ;
            MaskedImage rectLeft = transformer.TransfromImageBackwards(ImageLeft, true);

            transformer.Transformation = new RectificationTransformation()
            {
                RectificationMatrix = rectifier.RectificationRight,
                RectificationMatrixInverse = rectifier.RectificationRight_Inverse,
            }; ;
            MaskedImage rectRight = transformer.TransfromImageBackwards(ImageRight, true);

            _camImageFirst.ImageSource = rectLeft.ToBitmapSource();
           _camImageSec.ImageSource = rectRight.ToBitmapSource();
        }

        private void MatchFeatures(object sender, RoutedEventArgs e)
        {
            if(ImageLeft == null || ImageRight == null)
            {
                MessageBox.Show("Base images must be set");
                return;
            }

            //if(_featuresLeft == null || _featuresRight == null)
            //{
            //    MessageBox.Show("Features must be detected");
            //    return;
            //}

            _matcher.ImageLeft = ImageLeft;
            _matcher.ImageRight = ImageRight;
            _matcher.FeatureListLeft = _featuresLeft;
            _matcher.FeatureListRight = _featuresRight;

            AlgorithmWindow _matcherWindow = new AlgorithmWindow(_matcher);
            _matcherWindow.Show();
        }

        //public event EventHandler<FeaturesMatchedEventArgs> FeaturesMatched;
        private void _matcher_StatusChanged(object sender, CamCore.AlgorithmEventArgs e)
        {
            if(e.CurrentStatus == AlgorithmStatus.Finished)
            {
                Dispatcher.Invoke(() =>
               {
                   _matches = _matcher.Matches;
                   UpdateMatchedPoints();
               });
            }
        }

        private void UpdateMatchedPoints()
        {
            _camImageFirst.ResetPoints();
            _camImageSec.ResetPoints();

            foreach(var match in _matches)
            {
                PointImagePoint pipLeft = new PointImagePoint()
                {
                    Position = new Point(match.LeftPoint.X, match.LeftPoint.Y),
                    Value = match
                };
                PointImagePoint pipRight = new PointImagePoint()
                {
                    Position = new Point(match.RightPoint.X, match.RightPoint.Y),
                    Value = match
                };
                _camImageFirst.AddPoint(pipLeft);
                _camImageSec.AddPoint(pipRight);
            }
        }

        private void OnSelectedPointChanged(object sender, PointImageEventArgs e)
        {
            if(e.NewImagePoint.IsNullPoint) // Deselected -> deselect on second
            {
                if(sender == _camImageFirst)
                    _camImageSec.SelectPointQuiet(new PointImagePoint(true));
                else
                    _camImageFirst.SelectPointQuiet(new PointImagePoint(true));
                _isPointsSelected = false;
            }
            else // If selected on one image, find coupled point on second one and select it
            {
                MatchedPair match;
                if(sender == _camImageFirst)
                {
                    match = (MatchedPair)_camImageFirst.SelectedPoint.Value;
                    _camImageSec.SelectPointQuiet(_camImageSec.FindPointByValue(match));
                }
                else
                {
                    match = (MatchedPair)_camImageSec.SelectedPoint.Value;
                    _camImageFirst.SelectPointQuiet(_camImageFirst.FindPointByValue(match));
                };
                _isPointsSelected = true;
            }
        }

        private void LoadPoints(object sender, RoutedEventArgs e)
        {
            CamCore.FileOperations.LoadFromFile(LoadPoints, "Xml File|*.xml");
        }

        public void LoadPoints(Stream file, string path)
        {
            XmlDocument dataDoc = new XmlDocument();
            dataDoc.Load(file);


            _matches = new List<MatchedPair>();
            XmlNodeList points = dataDoc.GetElementsByTagName("Point");
            foreach(XmlNode pointNode in points)
            {
                MatchedPair mp = new MatchedPair()
                {
                    LeftPoint = new Vector2(),
                    RightPoint = new Vector2()
                };

                var imgx = pointNode.Attributes["imgx"];
                if(imgx != null)
                    mp.LeftPoint.X = double.Parse(imgx.Value);

                var imgy = pointNode.Attributes["imgy"];
                if(imgy != null)
                    mp.LeftPoint.Y = double.Parse(imgy.Value);

                var imgx2 = pointNode.Attributes["imgx2"];
                if(imgx2 != null)
                    mp.RightPoint.X = double.Parse(imgx2.Value);

                var imgy2 = pointNode.Attributes["imgy2"];
                if(imgy2 != null)
                    mp.RightPoint.Y = double.Parse(imgy2.Value);

                _matches.Add(mp);
            }

            UpdateMatchedPoints();
        }
    }
}

