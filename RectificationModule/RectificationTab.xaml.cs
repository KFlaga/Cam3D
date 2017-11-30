using CamAlgorithms;
using CamCore;
using System.Windows;
using System.Windows.Controls;
using CamAlgorithms.Calibration;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using CamControls;

namespace RectificationModule
{
    public partial class RectificationTab : UserControl
    {
        public MaskedImage ImageLeft { get; set; }
        public MaskedImage ImageRight { get; set; }
        public CameraPair Cameras { get { return CameraPair.Data; } }
        public List<Vector2Pair> MatchedPoints { get; set; } = new List<Vector2Pair>();

        RectificationAlgorithmUi _algorithm = new RectificationAlgorithmUi();

        public RectificationTab()
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
        }
        
        private void _butRectifyImages_Click(object sender, RoutedEventArgs e)
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
            if(Cameras.RectificationLeft == null || Cameras.RectificationRight == null)
            {
                MessageBox.Show("Rectifiction matrices must be set");
                return;
            }

            TransofrmImages();
        }

        private void TransofrmImages()
        {
            ImageTransformer transformer = new ImageTransformer();
            transformer.UsedInterpolationMethod = ImageTransformer.InterpolationMethod.Quadratic;

            transformer.Transformation = new RectificationTransformation()
            {
                RectificationMatrix = Cameras.RectificationLeft,
                RectificationMatrixInverse = Cameras.RectificationLeftInverse,
            };
            MaskedImage rectLeft = transformer.TransfromImageBackwards(ImageLeft, true);

            transformer.Transformation = new RectificationTransformation()
            {
                RectificationMatrix = Cameras.RectificationRight,
                RectificationMatrixInverse = Cameras.RectificationRightInverse,
            };
            MaskedImage rectRight = transformer.TransfromImageBackwards(ImageRight, true);

            _camImageFirst.ImageSource = rectLeft.ToBitmapSource();
            _camImageSec.ImageSource = rectRight.ToBitmapSource();
        }

        private void _butUndostort_Click(object sender, RoutedEventArgs e)
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
            if(Cameras.Left.Distortion.Model == null || Cameras.Right.Distortion.Model == null)
            {
                MessageBox.Show("Distortion must be set");
                return;
            }
            _camImageFirst.ImageSource = UndistortImage(Cameras.Left.Distortion.Model, _camImageFirst.ImageSource);
            _camImageSec.ImageSource = UndistortImage(Cameras.Right.Distortion.Model, _camImageSec.ImageSource);
        }

        BitmapSource UndistortImage(RadialDistortionModel model, BitmapSource source)
        {
            ImageTransformer undistort = new ImageTransformer(ImageTransformer.InterpolationMethod.Cubic, 1)
            {
                Transformation = new RadialDistortionTransformation(model)
            };

            MaskedImage img = new MaskedImage();
            img.FromBitmapSource(source);

            MaskedImage imgFinal = undistort.TransfromImageBackwards(img, true);
            return imgFinal.ToBitmapSource();
        }
        
        private void _butFindRectification_Click(object sender, RoutedEventArgs e)
        {
            if(Cameras.AreCalibrated == false)
            {
                MessageBox.Show("Cameras must be calibrated");
                return;
            }

            _algorithm.Algorithm.ImageWidth = Cameras.Left.ImageWidth;
            _algorithm.Algorithm.ImageHeight = Cameras.Left.ImageHeight;
            _algorithm.Algorithm.Cameras = Cameras;
            _algorithm.Algorithm.MatchedPairs = MatchedPoints;
            _algorithm.StatusChanged += _algorithm_StatusChanged;

            AlgorithmWindow algWindow = new AlgorithmWindow(_algorithm);
            algWindow.Show();
        }

        private void _algorithm_StatusChanged(object sender, AlgorithmEventArgs e)
        {
            if(e.CurrentStatus == AlgorithmStatus.Finished || e.CurrentStatus == AlgorithmStatus.Terminated)
            {
                Dispatcher.Invoke(() =>
                {
                    Cameras.RectificationLeft = _algorithm.Algorithm.RectificationLeft;
                    Cameras.RectificationLeftInverse = _algorithm.Algorithm.RectificationLeftInverse;
                    Cameras.RectificationRight = _algorithm.Algorithm.RectificationRight;
                    Cameras.RectificationRightInverse = _algorithm.Algorithm.RectificationRightInverse;
                });
            }
        }

        private void _butLoad_Click(object sender, RoutedEventArgs e)
        {
            FileOperations.LoadFromFile((f, p) => 
            {
                ImageRectification rectification = XmlSerialisation.CreateFromFile<ImageRectification>(f);
                Cameras.RectificationLeft = rectification.RectificationLeft;
                Cameras.RectificationRight = rectification.RectificationRight;
                Cameras.RectificationLeftInverse = rectification.RectificationLeftInverse;
                Cameras.RectificationRightInverse = rectification.RectificationRightInverse;
            }, "Xml File|*.xml");
        }

        private void _butSave_Click(object sender, RoutedEventArgs e)
        {
            if(Cameras.RectificationLeft == null || Cameras.RectificationRight == null)
            {
                MessageBox.Show("Rectification matrices must be set");
                return;
            }

            FileOperations.SaveToFile((f, p) =>
            {
                ImageRectification rectification = new ImageRectification();
                rectification.RectificationLeft = Cameras.RectificationLeft;
                rectification.RectificationRight = Cameras.RectificationRight;
                rectification.RectificationLeftInverse = Cameras.RectificationLeftInverse;
                rectification.RectificationRightInverse = Cameras.RectificationRightInverse;
                XmlSerialisation.SaveToFile(rectification, f);
            }, "Xml File|*.xml");
        }

        private void _butLoadPoints_Click(object sender, RoutedEventArgs e)
        {
            MatchedPointsManagerWindow pointsManager = new MatchedPointsManagerWindow();
            pointsManager.Points = MatchedPoints;
            bool? res = pointsManager.ShowDialog();
            if(res != null && res == true)
            {
                MatchedPoints = pointsManager.Points;
            }
        }
    }
}

