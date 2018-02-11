using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using static CamCore.MatrixExtensions;

namespace CamCore
{
    public enum HSIChannel : int
    {
        Hue = 0,
        Saturation = 1,
        Intensity = 2
    }

    [DebuggerTypeProxy(typeof(HSIImageDebugView))]
    public class HSIImage : IImage
    {
        public Matrix<double>[] ImageMatrix { get; set; } = new Matrix<double>[3];
        public int ColumnCount { get { return ImageMatrix[0].ColumnCount; } }
        public int RowCount { get { return ImageMatrix[0].RowCount; } }
        public int ChannelsCount { get { return 3; } }

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

        public double this[int y, int x]
        {
            get
            {
                return 0.333333 * (ImageMatrix[0].At(y, x) + ImageMatrix[1].At(y, x) + ImageMatrix[2].At(y, x));
            }
            set
            {
                ImageMatrix[0].At(y, x, value);
                ImageMatrix[1].At(y, x, value);
                ImageMatrix[2].At(y, x, value);
            }
        }

        public bool HaveValueAt(int y, int x) { return true; }

        public Matrix<double> GetMatrix(int channel)
        {
            return ImageMatrix[channel];
        }

        public Matrix<double> GetMatrix()
        {
            Matrix<double> mat = new DenseMatrix(RowCount, ColumnCount);
            for(int ch = 0; ch < ChannelsCount; ++ch)
            {
                mat.PointwiseAddThis(ImageMatrix[ch]);
            }
            mat.MultiplyThis(1.0 / 3.0);
            return mat;
        }

        public void SetMatrix(Matrix<double> matrix, int channel)
        {
            if(ImageMatrix == null)
                ImageMatrix = new Matrix<double>[3];
            ImageMatrix[channel] = matrix;
        }

        public IImage CreateOfSameClass()
        {
            return new HSIImage();
        }

        public IImage CreateOfSameClass(int rows, int cols)
        {
            HSIImage img = new HSIImage();
            img.ImageMatrix[0] = new DenseMatrix(rows, cols);
            img.ImageMatrix[1] = new DenseMatrix(rows, cols);
            img.ImageMatrix[2] = new DenseMatrix(rows, cols);
            return img;
        }

        public IImage CreateOfSameClass(Matrix<double>[] matrices)
        {
            HSIImage img = new HSIImage();
            img.ImageMatrix[0] = matrices[0];
            img.ImageMatrix[1] = matrices[1];
            img.ImageMatrix[2] = matrices[2];
            return img;
        }

        public IImage Clone()
        {
            HSIImage img = new HSIImage()
            {
                ImageMatrix = new Matrix<double>[3],
            };
            img.ImageMatrix[0] = ImageMatrix[0].Clone();
            img.ImageMatrix[1] = ImageMatrix[1].Clone();
            img.ImageMatrix[2] = ImageMatrix[2].Clone();
            return img;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public void FromColorImage(ColorImage cimage)
        {
            ImageMatrix[0] = new DenseMatrix(cimage.RowCount, cimage.ColumnCount);
            ImageMatrix[1] = new DenseMatrix(cimage.RowCount, cimage.ColumnCount);
            ImageMatrix[2] = new DenseMatrix(cimage.RowCount, cimage.ColumnCount);
            int x, y;

            double h, s, i;
            for(x = 0; x < ColumnCount; ++x)
            {
                for(y = 0; y < RowCount; ++y)
                {
                    RGBToHSI(cimage[y, x, RGBChannel.Red], cimage[y, x, RGBChannel.Green], cimage[y, x, RGBChannel.Blue],
                        out h, out s, out i);
                    this[y, x, HSIChannel.Hue] = h;
                    this[y, x, HSIChannel.Saturation] = s;
                    this[y, x, HSIChannel.Intensity] = i;
                }
            }
        }
        
        public void FromBitmapSource(BitmapSource bitmap)
        {

        }

        public BitmapSource ToBitmapSource()
        {
            return null;
        }

        public static void RGBToHSI(double r, double g, double b, out double h, out double s, out double i)
        {
            double cmin = Math.Min(Math.Min(r, g), b);
            double cmax = Math.Max(Math.Max(r, g), b);
            double chrom = cmax - cmin;

            i = (r + g + b) / 3.0f;
            s = i > 0.0 ? Math.Max(0.0f, 1.0 - cmin / i) : 0.0;
            double m  = (r - g) * (r - g) + (r - b) * (g - b);
            h = m > 0.0 ? Math.Acos(((r - g) + (r - b)) / (2 * Math.Sqrt((r - g) * (r - g) + (r - b) * (g - b)))) : 0.0;

            if(b > g)
            {
                h = (2.0 * Math.PI) - h;
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
                    return _image.RowCount;
                }
            }

            public int Columns
            {
                get
                {
                    return _image.ColumnCount;
                }
            }

            public override string ToString()
            {
                return "HSI. Rows : " + Rows.ToString() + ", Columns: " + Columns.ToString();
            }
        }
    }
}
