using CamImageProcessing;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace CamControls
{
    public class DisparityRange
    {
        public int Max; // Defined by min/max of diparity map
        public int Min;
        public int TempMax; // Custom min/max for better coloring regions of interest
        public int TempMin;
        public double[][] Colors; // Defined in DisparityLegend, for each disparity from range [Min,Max] stores 
                                  // its color in float[3] ( Colors[disp_idx][channel] )

        public int GetDisparityIndex(int disp)
        {
            return disp - TempMin;
        }

        public int GetDisparityRange()
        {
            return Max - Min + 1;
        }

        public int GetTempDisparityRange()
        {
            return TempMax - TempMin + 1;
        }
    }

    public partial class DisparityLegend : UserControl
    {
        DisparityRange _range;
        public DisparityRange Range
        {
            get { return _range; }
            set
            {
                _range = value;
                _range.Colors = (double[][])Array.CreateInstance(typeof(double[]), _range.GetTempDisparityRange());
                for(int i = 0; i < _range.Colors.Length; ++i)
                {
                    _range.Colors[i] = new double[3];
                }
                UpdateColorsRange();
            }
        }

        ColorImage _legend = new ColorImage();

        public DisparityLegend()
        {
            InitializeComponent();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
            {

            }
            else
            {

            }
        }

        private void UpdateColorsRange()
        {
            int len = _range.TempMax - _range.TempMin + 1;
            _legend.ImageMatrix[0] = new DenseMatrix(len, 30);
            _legend.ImageMatrix[1] = new DenseMatrix(len, 30);
            _legend.ImageMatrix[2] = new DenseMatrix(len, 30);

            // Hue:
            // 0 for max; 2/3 pi for mid; 4/3 pi for min
            // hue = 4/3pi * i / range
            // Saturation:
            // 1 for all
            // Intensity:
            // 0.5 for min; 1 for mid 0.5 for max
            // int = 1 - 0.5|val - mid|/|max-min|
            int half = len / 2;
            double pi23 = Math.PI * 2.0 / 3.0;
            for(int i = 0; i < len; ++i)
            {
                double s = 1.0;
                double h;
                // double h = pi43 * ((double)i / (double)len);
                if(i <= half / 2)
                {
                    double cos = Math.Sqrt(Math.Abs(Math.Cos(Math.PI * i / half)));
                    h = Math.PI * (1.0 - cos) / 3.0;
                }
                else if(i < half)
                {
                    double cos = Math.Sqrt(Math.Abs(Math.Cos(Math.PI * i / half)));
                    h = Math.PI * (1.0 + cos) / 3.0;
                }
                else if(i < half * 1.5)
                {
                    double cos = Math.Sqrt(Math.Abs(Math.Cos(Math.PI * (i - half) / half)));
                    h = pi23 + Math.PI * (1.0 - cos) / 3.0;
                }
                else
                {
                    double cos = Math.Sqrt(Math.Abs(Math.Cos(Math.PI * (i - half) / half)));
                    h = pi23 + Math.PI * (1.0 + cos) / 3.0;
                }

                // double l = 1.0 - 0.5 * Math.Abs(i - half) / len;

                double r, g, b;
                ColorImage.HSIToRGB(h, s, 1.0, out r, out g, out b);
                _range.Colors[i][0] = r;
                _range.Colors[i][1] = g;
                _range.Colors[i][2] = b;

                for(int c = 0; c < _legend.SizeX; ++c)
                {
                    _legend[i, c] = _range.Colors[i];
                }
            }

            _legendImage.Source = _legend.ToBitmapSource();
            _labelMax.Content = _range.TempMax.ToString();
            _labelMin.Content = _range.TempMin.ToString();
            _labelMid.Content = (_range.TempMin + half).ToString();
        }
    }
}
