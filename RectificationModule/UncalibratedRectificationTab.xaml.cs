using CamAlgorithms;
using CamAlgorithms.Calibration;
using CamCore;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace RectificationModule
{
    /// <summary>
    /// Interaction logic for UncalibratedRectificationTab.xaml
    /// </summary>
    public partial class UncalibratedRectificationTab : UserControl
    {
        public MaskedImage ImageLeft { get; set; }
        public MaskedImage ImageRight { get; set; }

        public CameraPair Cameras { get { return CameraPair.Data; } }

        public List<Vector2Pair> MatchedPoints { get; set; }
        
        private ImageRectification _rectificationWithInitial = 
            new ImageRectification(new ImageRectification_FussieloUncalibrated()
            {
                UseInitialCalibration = true
            });
        private ImageRectification _rectificationUncalibrated =
            new ImageRectification(new ImageRectification_FussieloUncalibrated()
            {
                UseInitialCalibration = false
            });


        public UncalibratedRectificationTab()
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
                MessageBox.Show("Rectifiction matrices must be found");
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
            }; ;
            MaskedImage rectLeft = transformer.TransfromImageBackwards(ImageLeft, true);

            transformer.Transformation = new RectificationTransformation()
            {
                RectificationMatrix = Cameras.RectificationRight,
                RectificationMatrixInverse = Cameras.RectificationRightInverse,
            }; ;
            MaskedImage rectRight = transformer.TransfromImageBackwards(ImageRight, true);

            _camImageFirst.ImageSource = rectLeft.ToBitmapSource();
            _camImageSec.ImageSource = rectRight.ToBitmapSource();
        }

        private void _butFindRectification_Click(object sender, RoutedEventArgs e)
        {
            FindRectification(_rectificationUncalibrated);
        }

        private void _butFindRectificationWithInitial_Click(object sender, RoutedEventArgs e)
        {
            FindRectification(_rectificationWithInitial);
        }

        private void ShowRectificationResults(ImageRectification rectification)
        {
            MessageBox.Show("Fussiello Rectification finished with Q = " + rectification.Quality);
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

        private void _butLoadMatchedPoints_Click(object sender, RoutedEventArgs e)
        {
            FileOperations.LoadFromFile((s, p) => { MatchedPoints = XmlSerialisation.CreateFromFile<List<Vector2Pair>>(s); }, "Xml File|*.xml");
        }
        
        void FindRectification(ImageRectification rectification)
        {
            if(Cameras.AreCalibrated == false)
            {
                MessageBox.Show("Cameras must be calibrated");
                return;
            }
            if(MatchedPoints == null || MatchedPoints.Count == 0)
            {
                MessageBox.Show("MatchedPoints must be set");
                return;
            }

            rectification.ImageHeight = Cameras.Left.ImageHeight;
            rectification.ImageWidth = Cameras.Left.ImageWidth;
            rectification.Cameras = Cameras;
            rectification.MatchedPairs = MatchedPoints;
            rectification.ComputeRectificationMatrices();

            Cameras.RectificationLeft = rectification.RectificationLeft;
            Cameras.RectificationLeftInverse = rectification.RectificationLeftInverse;
            Cameras.RectificationRight = rectification.RectificationRight;
            Cameras.RectificationRightInverse = rectification.RectificationRightInverse;
            ShowRectificationResults(rectification);
        }
    }
}
