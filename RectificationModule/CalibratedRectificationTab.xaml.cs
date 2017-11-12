using CamAlgorithms;
using CamCore;
using System.Windows;
using System.Windows.Controls;
using CamAlgorithms.Calibration;

namespace RectificationModule
{
    public partial class CalibratedRectificationTab : UserControl
    {
        public MaskedImage ImageLeft { get; set; }
        public MaskedImage ImageRight { get; set; }

        public CameraPair Cameras { get { return CameraPair.Data; } }
        public ImageRectification Rectification { get; private set; } = new ImageRectification(new ImageRectification_ZhangLoop());


        public CalibratedRectificationTab()
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
                RectificationMatrix = Rectification.RectificationLeft,
                RectificationMatrixInverse = Rectification.RectificationLeftInverse,
            };
            MaskedImage rectLeft = transformer.TransfromImageBackwards(ImageLeft, true);

            transformer.Transformation = new RectificationTransformation()
            {
                RectificationMatrix = Rectification.RectificationRight,
                RectificationMatrixInverse = Rectification.RectificationRightInverse,
            };
            MaskedImage rectRight = transformer.TransfromImageBackwards(ImageRight, true);

            _camImageFirst.ImageSource = rectLeft.ToBitmapSource();
            _camImageSec.ImageSource = rectRight.ToBitmapSource();
        }

        private void _butFindRectification_Click(object sender, RoutedEventArgs e)
        {
            if(Cameras.AreCalibrated)
            {
                MessageBox.Show("Cameras must be calibrated");
                return;
            }

            Rectification.ImageHeight = Cameras.Left.ImageHeight;
            Rectification.ImageWidth = Cameras.Left.ImageWidth;
            Rectification.Cameras = Cameras;
            Rectification.ComputeRectificationMatrices();

            Cameras.RectificationLeft = Rectification.RectificationLeft;
            Cameras.RectificationLeftInverse = Rectification.RectificationLeftInverse;
            Cameras.RectificationRight = Rectification.RectificationRight;
            Cameras.RectificationRightInverse = Rectification.RectificationRightInverse;
            ShowRectificationResults();
        }

        private void ShowRectificationResults()
        {
            MessageBox.Show("ZhangLoop Rectification finished with Q = " + Rectification.Quality);
        }

        private void _butLoadRectification_Click(object sender, RoutedEventArgs e)
        {
            FileOperations.LoadFromFile(
               (stream, path) => 
               {
                   Rectification = XmlSerialisation.CreateFromFile<ImageRectification>(stream);
                   Cameras.RectificationLeft = Rectification.RectificationLeft;
                   Cameras.RectificationLeftInverse = Rectification.RectificationLeftInverse;
                   Cameras.RectificationRight = Rectification.RectificationRight;
                   Cameras.RectificationRightInverse = Rectification.RectificationRightInverse;
               },
               "Xml File|*.xml");
        }

        private void _butSaveRectification_Click(object sender, RoutedEventArgs e)
        {
            FileOperations.SaveToFile(
               (stream, path) =>
               {
                   XmlSerialisation.SaveToFile(Rectification, stream);
               },
               "Xml File|*.xml");
        }
    }
}

