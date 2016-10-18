using CamControls;
using CamImageProcessing;
using CamCore;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using CamImageProcessing.ImageMatching;
using System;
using System.Windows.Media.Imaging;

namespace ImageMatchingModule
{
    public class DisparityChangedEventArgs : EventArgs
    {
        public DisparityMap NewMap { get; set; }
    }

    public partial class MatchedImagesTab : UserControl
    {
        private AlgorithmWindow _matcherWindow;
        private ImageMatchingAlgorithmController _alg = new ImageMatchingAlgorithmController();
        
        DisparityMap _mapLeft;
        public DisparityMap MapLeft
        {
            get { return _mapLeft; }
            set
            {
                _mapLeft = value;
                LeftMapChanged?.Invoke(this, new DisparityChangedEventArgs() { NewMap = value });
            }
        }

        DisparityMap _mapRight;
        public DisparityMap MapRight
        {
            get { return _mapRight; }
            set
            {
                _mapRight = value;
                RightMapChanged?.Invoke(this, new DisparityChangedEventArgs() { NewMap = value });
            }
        }

        public event EventHandler<DisparityChangedEventArgs> LeftMapChanged;
        public event EventHandler<DisparityChangedEventArgs> RightMapChanged;
        
        public MatchedImagesTab()
        {
            InitializeComponent();
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

            ColorImage imgLeft = new ColorImage();
            imgLeft.FromBitmapSource(_camImageFirst.ImageSource);
            ColorImage imgRight = new ColorImage();
            imgRight.FromBitmapSource(_camImageSec.ImageSource);

            rectTransformation.WhichImage = RectificationTransformation.ImageIndex.Left;
            ColorImage rectLeft = transformer.TransfromImageBackwards(imgLeft, true);

            rectTransformation.WhichImage = RectificationTransformation.ImageIndex.Right;
            ColorImage rectRight = transformer.TransfromImageBackwards(imgRight, true);

            _camImageFirst.ImageSource = rectLeft.ToBitmapSource();
            _camImageSec.ImageSource = rectRight.ToBitmapSource();
        }

        private void MatchImages(object sender, RoutedEventArgs e)
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

            ColorImage imgLeft = new ColorImage();
            imgLeft.FromBitmapSource(_camImageFirst.ImageSource);
            ColorImage imgRight = new ColorImage();
            imgRight.FromBitmapSource(_camImageSec.ImageSource);

            _alg.ImageLeft = imgLeft;
            _alg.ImageRight = imgRight;
            _alg.StatusChanged += _alg_StatusChanged;

            _matcherWindow = new AlgorithmWindow(_alg);
            _matcherWindow.Show();
        }

        private void _alg_StatusChanged(object sender, CamCore.AlgorithmEventArgs e)
        {
            if(e.CurrentStatus == AlgorithmStatus.Finished)
            {
                Dispatcher.Invoke(() =>
               {
                   MapLeft = _alg.MapLeft;
                   MapRight = _alg.MapRight;
               });
            }
        }
    }
}

