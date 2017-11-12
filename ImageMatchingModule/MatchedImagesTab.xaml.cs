using CamControls;
using CamAlgorithms;
using CamCore;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using CamAlgorithms.ImageMatching;
using System;
using System.Windows.Media.Imaging;
using CamAlgorithms.Calibration;

namespace ImageMatchingModule
{
    public class DisparityChangedEventArgs : EventArgs
    {
        public DisparityMap NewMap { get; set; }
    }

    public partial class MatchedImagesTab : UserControl
    {
        private AlgorithmWindow _matcherWindow;
        private ImageMatchingAlgorithmUi _alg = new ImageMatchingAlgorithmUi();
        
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

        ColorImage _imgLeft;
        ColorImage _imgRight;
        public ColorImage ImageLeft { get { return _imgLeft; } }
        public ColorImage ImageRight { get { return _imgRight; } }

        public MatchedImagesTab()
        {
            InitializeComponent();

            _camImageFirst.ImageSourceChanged += (s, e) =>
            {
                _imgLeft = new ColorImage();
                _imgLeft.FromBitmapSource(e.NewImage);
            };

            _camImageSec.ImageSourceChanged += (s, e) =>
            {
                _imgRight = new ColorImage();
                _imgRight.FromBitmapSource(e.NewImage);
            };
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
            if(CameraPair.Data.AreCalibrated)
            {
                MessageBox.Show("Cameras must be calibrated");
                return;
            }

            ImageRectification rectifier = new ImageRectification(new ImageRectification_ZhangLoop());
            rectifier.ImageHeight = _camImageFirst.ImageSource.PixelHeight;
            rectifier.ImageWidth = _camImageFirst.ImageSource.PixelWidth;
            rectifier.Cameras = CameraPair.Data;
            rectifier.ComputeRectificationMatrices();

            ImageTransformer transformer = new ImageTransformer();
            transformer.UsedInterpolationMethod = ImageTransformer.InterpolationMethod.Quadratic;
            
            transformer.Transformation = new RectificationTransformation()
            {
                RectificationMatrix = rectifier.RectificationLeft,
                RectificationMatrixInverse = rectifier.RectificationLeftInverse,
            }; ;
            MaskedImage rectLeft = transformer.TransfromImageBackwards(_imgLeft, true);

            transformer.Transformation = new RectificationTransformation()
            {
                RectificationMatrix = rectifier.RectificationRight,
                RectificationMatrixInverse = rectifier.RectificationRightInverse,
            }; ;
            MaskedImage rectRight = transformer.TransfromImageBackwards(_imgRight, true);

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
            if(CameraPair.Data.AreCalibrated)
            {
                MessageBox.Show("Cameras must be calibrated");
                return;
            }

            _alg.ImageLeft = _imgLeft;
            _alg.ImageRight = _imgRight;
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

