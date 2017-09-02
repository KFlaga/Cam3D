using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CamControls;
using CamImageProcessing;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra;

namespace CamImageOperationsModule
{
    public partial class ImageOperationsModePanel : UserControl
    {
        private BitmapSource _baseImage;

        private GrayScaleImage _grayImage = new GrayScaleImage();
        private ColorImage _colorImage = new ColorImage();
        private HSIImage _hsiImage = new HSIImage();
        private bool _dontUpdateImage = false;

        private ParametersSelectionWindow _optsWindow_MedianFilter;
        private ParametersSelectionWindow _optsWindow_DiffusionFilter;
        private ParametersSelectionWindow _optsWindow_GaussFilter;
        private ParametersSelectionWindow _optsWindow_LoGFilter;
        private ParametersSelectionWindow _optsWindow_SaturateHistogram;
        private ParametersSelectionWindow _optsWindow_FloodSelect;
        private ParametersSelectionWindow _optsWindow_FuzzySelect;

        public ImageOperationsModePanel()
        {
            InitializeComponent();

            InitProcessorWindows();

            _imageControl.ImageSourceChanged += UpdateImage;
        }

        private void UpdateImage(object sender, ImageChangedEventArgs e)
        {
            if(!_dontUpdateImage && e.NewImage != null)
            {
                _baseImage = e.NewImage;

                _colorImage.FromBitmapSource(_baseImage);
                _grayImage.FromColorImage(_colorImage);
                _hsiImage.FromColorImage(_colorImage);
            }

            if(e.NewImage == null)
            {
                _butComputeHistogram.IsEnabled = false;
                _butEqualiseHistogram.IsEnabled = false;
                _butFloodSelection.IsEnabled = false;
                _butFuzzySelection.IsEnabled = false;
                _butGaussFilter.IsEnabled = false;
                _butLoGFilter.IsEnabled = false;
                _butMedianFilter.IsEnabled = false;
                _butDiffusionFilter.IsEnabled = false;
                _butResetImage.IsEnabled = false;
                _butSaturateHistogram.IsEnabled = false;
                _butStretchHistogram.IsEnabled = false;
            }
            else
            {
                _butComputeHistogram.IsEnabled = true;
                _butEqualiseHistogram.IsEnabled = true;
                _butFloodSelection.IsEnabled = true;
                _butFuzzySelection.IsEnabled = true;
                _butGaussFilter.IsEnabled = true;
                _butLoGFilter.IsEnabled = true;
                _butMedianFilter.IsEnabled = true;
                _butDiffusionFilter.IsEnabled = true;
                _butResetImage.IsEnabled = true;
                _butSaturateHistogram.IsEnabled = true;
                _butStretchHistogram.IsEnabled = true;
            }
        }

        private void _butResetImage_Click(object sender, RoutedEventArgs e)
        {
            _imageControl.ImageSource = _baseImage;
        }

        private void _butMedianFilter_Click(object sender, RoutedEventArgs e)
        {
            _optsWindow_MedianFilter.ShowDialog();
            if(_optsWindow_MedianFilter.Accepted)
            {
                var medianFilter = (MedianFilter)_optsWindow_MedianFilter.Processor;
                medianFilter.Image = _hsiImage[HSIChannel.Intensity];
                _hsiImage[HSIChannel.Intensity] = medianFilter.ApplyFilter();

                _colorImage.FromHSIImage(_hsiImage);
                _imageControl.ImageSource = _colorImage.ToBitmapSource();
            }
        }

        private void _butDiffusionFilter_Click(object sender, RoutedEventArgs e)
        {
            _optsWindow_DiffusionFilter.ShowDialog();
            if(_optsWindow_DiffusionFilter.Accepted)
            {
                var diffFilter = (AnisotropicDiffusionFilter)_optsWindow_DiffusionFilter.Processor;
                diffFilter.Image = _hsiImage[HSIChannel.Intensity];
                _hsiImage[HSIChannel.Intensity] = diffFilter.ApplyFilter();

                _colorImage.FromHSIImage(_hsiImage);
                _imageControl.ImageSource = _colorImage.ToBitmapSource();
            }
        }

        private void _butGaussFilter_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _butLoGFilter_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _butComputeHistogram_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _butStretchHistogram_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _butEqualiseHistogram_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _butSaturateHistogram_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _checkFloodSelection_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void _butFuzzySelection_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _checkFuzzySelection_Checked(object sender, RoutedEventArgs e)
        {

        }

        void InitProcessorWindows()
        {
            _optsWindow_MedianFilter = new ParametersSelectionWindow();
            _optsWindow_MedianFilter.Processor = new MedianFilter();

            _optsWindow_GaussFilter = new ParametersSelectionWindow();
            _optsWindow_GaussFilter.Processor = new GaussFilter();

            _optsWindow_DiffusionFilter = new ParametersSelectionWindow();
            _optsWindow_DiffusionFilter.Processor = new AnisotropicDiffusionFilter();

            _optsWindow_LoGFilter = new ParametersSelectionWindow();
            _optsWindow_LoGFilter.Processor = new LoGFilter();

            _optsWindow_SaturateHistogram = new ParametersSelectionWindow();
            _optsWindow_SaturateHistogram.Processor = new HistogramSaturator();

            _optsWindow_FloodSelect = new ParametersSelectionWindow();

            _optsWindow_FuzzySelect = new ParametersSelectionWindow();
        }

        private void _butFloodSelection_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _butSegment_Click(object sender, RoutedEventArgs e)
        {
            if(_imageControl.ImageSource != null)
            {
                ColorImage img = new ColorImage();
                img.FromBitmapSource(_imageControl.ImageSource);

                var window = new ParametrizedProcessorsSelectionWindow();
                window.AddProcessorFamily("Segmentation");
                window.AddToFamily("Segmentation", new MeanShiftSegmentation());

                window.ShowDialog();
                if(window.Accepted)
                {
                    ImageSegmentation segmentation = (ImageSegmentation)window.GetSelectedProcessor("Segmentation");

                    GrayScaleImage imgGray = new GrayScaleImage();
                    imgGray.FromColorImage(img);

                    //segmentation.SegmentColor(img);
                    segmentation.SegmentGray(imgGray.ImageMatrix);

                    //var segments = segmentation.Segments_Color;
                    var segments = segmentation.Segments;
                    int[,] indices = segmentation.SegmentAssignments;

                    ColorImage imgFinal = new ColorImage();
                    imgFinal.ImageMatrix[0] = new DenseMatrix(img.RowCount, img.ColumnCount);
                    imgFinal.ImageMatrix[1] = new DenseMatrix(img.RowCount, img.ColumnCount);
                    imgFinal.ImageMatrix[2] = new DenseMatrix(img.RowCount, img.ColumnCount);

                    for(int r = 0; r < img.RowCount; ++r)
                    {
                        for(int c = 0; c < img.ColumnCount; ++c)
                        {
                            imgFinal[r, c, RGBChannel.Red] = ((ImageSegmentation.Segment_Gray)segments[indices[r, c]]).Value;
                            imgFinal[r, c, RGBChannel.Green] = ((ImageSegmentation.Segment_Gray)segments[indices[r, c]]).Value;
                            imgFinal[r, c, RGBChannel.Blue] = ((ImageSegmentation.Segment_Gray)segments[indices[r, c]]).Value;
                            //imgFinal[r, c, RGBChannel.Red] = segments[indices[r, c]].Red;
                            // imgFinal[r, c, RGBChannel.Green] = segments[indices[r, c]].Green;
                            //imgFinal[r, c, RGBChannel.Blue] = segments[indices[r, c]].Blue;
                        }
                    }

                    _imageControl.ImageSource = imgFinal.ToBitmapSource();
                }
            }
        }

        private void _butLowerCompletion_Click(object sender, RoutedEventArgs e)
        {
            if(_imageControl.ImageSource != null)
            {
                ColorImage img = new ColorImage();
                img.FromBitmapSource(_imageControl.ImageSource);

                GrayScaleImage imgGray = new GrayScaleImage();
                imgGray.FromColorImage(img);

                Matrix<double> lowerComp = WatershedSegmentation.ComputeLowerCompletion(imgGray.GetMatrix());

                ColorImage imgFinal = new ColorImage();
                imgFinal.ImageMatrix[0] = new DenseMatrix(lowerComp.RowCount, lowerComp.ColumnCount);
                imgFinal.ImageMatrix[1] = new DenseMatrix(lowerComp.RowCount, lowerComp.ColumnCount);
                imgFinal.ImageMatrix[2] = new DenseMatrix(lowerComp.RowCount, lowerComp.ColumnCount);

                for(int r = 0; r < img.RowCount; ++r)
                {
                    for(int c = 0; c < img.ColumnCount; ++c)
                    {
                        imgFinal[r, c, RGBChannel.Red] = lowerComp[r, c];
                        imgFinal[r, c, RGBChannel.Green] = lowerComp[r, c];
                        imgFinal[r, c, RGBChannel.Blue] = lowerComp[r, c];
                    }
                }

                _imageControl.ImageSource = imgFinal.ToBitmapSource();
            }
        }
    }
}

