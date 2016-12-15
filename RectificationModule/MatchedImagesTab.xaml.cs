using CamControls;
using CamImageProcessing;
using CamCore;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using CamImageProcessing.ImageMatching;
using System;
using System.Windows.Media.Imaging;

namespace RectificationModule
{
    public class FeturesDetectedEventArgs : EventArgs
    {
        public MaskedImage ImageLeft { get; set; }
        public MaskedImage ImageRight { get; set; }
        public GrayScaleImage FeatureImageLeft { get; set; }
        public GrayScaleImage FeatureImageRight { get; set; }
        public List<IntVector2> FeatureListLeft { get; set; }
        public List<IntVector2> FeatureListRight { get; set; }
    }

    public partial class MatchedImagesTab : UserControl
    {
        MaskedImage _imgLeft;
        MaskedImage _imgRight;
        public MaskedImage ImageLeft { get { return _imgLeft; } }
        public MaskedImage ImageRight { get { return _imgRight; } }

        FeatureDetectionAlgorithmController _featureDetector;

        public event EventHandler<FeturesDetectedEventArgs> FeturesDetected;

        public MatchedImagesTab()
        {
            InitializeComponent();

            _camImageFirst.ImageSourceChanged += (s, e) =>
            {
                _imgLeft = new MaskedImage();
                _imgLeft.FromBitmapSource(e.NewImage);
            };

            _camImageSec.ImageSourceChanged += (s, e) =>
            {
                _imgRight = new MaskedImage();
                _imgRight.FromBitmapSource(e.NewImage);
            };

            _featureDetector = new FeatureDetectionAlgorithmController();
            _featureDetector.StatusChanged += _featureDetector_StatusChanged;
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
            if(CalibrationData.Data.IsCamLeftCalibrated == false ||
                CalibrationData.Data.IsCamRightCalibrated == false)
            {
                MessageBox.Show("Cameras must be calibrated");
                return;
            }

            ImageRectification_ZhangLoop rectifier = new ImageRectification_ZhangLoop();
            //ImageRectification_FusielloCalibrated rectifier = new ImageRectification_FusielloCalibrated();

            RectificationTransformation rectTransformation = new RectificationTransformation();
            rectTransformation.Rectifier = rectifier;

            ImageTransformer transformer = new ImageTransformer();
            transformer.Transformation = rectTransformation;
            transformer.UsedInterpolationMethod = ImageTransformer.InterpolationMethod.Quadratic;

            rectifier.ImageHeight = _camImageFirst.ImageSource.PixelHeight;
            rectifier.ImageWidth = _camImageFirst.ImageSource.PixelWidth;
            rectifier.EpiCrossLeft = CalibrationData.Data.EpipoleCrossLeft;
            rectifier.EpiCrossRight = CalibrationData.Data.EpipoleCrossRight;
            rectifier.EpipoleLeft = CalibrationData.Data.EpipoleLeft;
            rectifier.EpipoleRight = CalibrationData.Data.EpipoleRight;
            rectifier.IsEpiLeftInInfinity = CalibrationData.Data.EpiLeftInInfinity;
            rectifier.IsEpiRightInInfinity = CalibrationData.Data.EpiRightInInfinity;
            rectifier.FundamentalMatrix = CalibrationData.Data.Fundamental;
            rectifier.ComputeRectificationMatrices();
            
            rectTransformation.WhichImage = RectificationTransformation.ImageIndex.Left;
            MaskedImage rectLeft = transformer.TransfromImageBackwards(_imgLeft, true);

            rectTransformation.WhichImage = RectificationTransformation.ImageIndex.Right;
            MaskedImage rectRight = transformer.TransfromImageBackwards(_imgRight, true);

            _camImageFirst.ImageSource = rectLeft.ToBitmapSource();
            _camImageSec.ImageSource = rectRight.ToBitmapSource();
        }

        private void FindFeatures(object sender, RoutedEventArgs e)
        {
            _featureDetector.ImageLeft = _imgLeft;
            _featureDetector.ImageRight = _imgRight;

            AlgorithmWindow _matcherWindow = new AlgorithmWindow(_featureDetector);
            _matcherWindow.Show();
        }

        private void _featureDetector_StatusChanged(object sender, CamCore.AlgorithmEventArgs e)
        {
            if(e.CurrentStatus == AlgorithmStatus.Finished)
            {
                Dispatcher.Invoke(() =>
               {
                   FeturesDetected?.Invoke(this, new FeturesDetectedEventArgs()
                   {
                       FeatureImageLeft = _featureDetector.FeatureImageLeft,
                       FeatureImageRight = _featureDetector.FeatureImageRight,
                       FeatureListLeft = _featureDetector.FeatureListLeft,
                       FeatureListRight = _featureDetector.FeatureListRight,
                       ImageLeft = ImageLeft,
                       ImageRight = ImageRight
                   });
               });
            }
        }
    }
}

