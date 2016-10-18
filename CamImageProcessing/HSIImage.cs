using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static CamCore.MatrixExtensions;

namespace CamImageProcessing
{
    public enum HSIChannel : int
    {
        Hue = 0,
        Saturation = 1,
        Intensity = 2
    }

    [DebuggerTypeProxy(typeof(HSIImageDebugView))]
    public class HSIImage
    {
        public Matrix<double>[] ImageMatrix { get; set; } = new Matrix<double>[3];
        public int SizeX { get { return ImageMatrix[0].ColumnCount; } }
        public int SizeY { get { return ImageMatrix[0].RowCount; } }

        // Bitmap data saved when image created from bitmapsource
        public double DpiX { get; set; }
        public double DpiY { get; set; }

        public Matrix<double> this[HSIChannel channel]
        {
            get
            {
                return (ImageMatrix[(int)channel]);
            }
            set
            {
                (ImageMatrix[(int)channel]) = value;
            }
        }

        public double this[int y, int x, HSIChannel channel]
        {
            get
            {
                return (ImageMatrix[(int)channel])[y, x];
            }
            set
            {
                (ImageMatrix[(int)channel])[y, x] = value;
            }
        }

        public double this[int y, int x, int channel]
        {
            get
            {
                return (ImageMatrix[channel])[y, x];
            }
            set
            {
                (ImageMatrix[channel])[y, x] = value;
            }
        }

        public void FromColorImage(ColorImage cimage)
        {
            ImageMatrix[0] = new DenseMatrix(cimage.SizeY, cimage.SizeX);
            ImageMatrix[1] = new DenseMatrix(cimage.SizeY, cimage.SizeX);
            ImageMatrix[2] = new DenseMatrix(cimage.SizeY, cimage.SizeX);
            int x, y;

            double h, s, i;
            for(x = 0; x < SizeX; ++x)
            {
                for(y = 0; y < SizeY; ++y)
                {
                    RGBToHSI(cimage[y, x, RGBChannel.Red], cimage[y, x, RGBChannel.Green], cimage[y, x, RGBChannel.Blue],
                        out h, out s, out i);
                    this[y, x, HSIChannel.Hue] = h;
                    this[y, x, HSIChannel.Saturation] = s;
                    this[y, x, HSIChannel.Intensity] = i;
                }
            }
        }

        public static void RGBToHSI(double r, double g, double b, out double h, out double s, out double i)
        {
            double cmin = Math.Min(Math.Min(r, g), b);
            double cmax = Math.Max(Math.Max(r, g), b);
            double chrom = cmax - cmin;

            i = (r + g + b) / 3.0f;
            s = i > 0.0f ? Math.Max(0.0f, 1.0f - cmin / i) : 0.0f;
            double m  = (r - g) * (r - g) + (r - b) * (g - b);
            h = m > 0.0f ? (double)Math.Acos(((r - g) + (r - b)) / (2 * Math.Sqrt((r - g) * (r - g) + (r - b) * (g - b)))) : 0.0f;

            if(r > g)
            {
                h = (double)(2.0 * Math.PI) - h;
            }
        }
        
        internal class HSIImageDebugView
        {
            private HSIImage _image;

            public HSIImageDebugView(HSIImage img)
            {
                _image = img;
            }

            public DoubleMatrixVisualiser Hue
            {
                get
                {
                    return new DoubleMatrixVisualiser(_image[HSIChannel.Hue]);
                }
            }

            public DoubleMatrixVisualiser Saturation
            {
                get
                {
                    return new DoubleMatrixVisualiser(_image[HSIChannel.Saturation]);
                }
            }

            public DoubleMatrixVisualiser Intensity
            {
                get
                {
                    return new DoubleMatrixVisualiser(_image[HSIChannel.Intensity]);
                }
            }

            public int Rows
            {
                get
                {
                    return _image.SizeY;
                }
            }

            public int Columns
            {
                get
                {
                    return _image.SizeX;
                }
            }

            public override string ToString()
            {
                return "HSI. Rows : " + Rows.ToString() + ", Columns: " + Columns.ToString();
            }
        }
    }
}
