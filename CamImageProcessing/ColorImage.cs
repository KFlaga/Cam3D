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
    public enum RGBChannel
    {
        Red = 0,
        Green = 1,
        Blue = 2
    }

    [DebuggerTypeProxy(typeof(ColorImageDebugView))]
    public class ColorImage : IImage
    {
        public Matrix<double>[] ImageMatrix { get; set; } = new Matrix<double>[3];
        public int ColumnCount { get { return ImageMatrix[0].ColumnCount; } }
        public int RowCount { get { return ImageMatrix[0].RowCount; } }
        public int ChannelsCount { get { return 3; } }

        // Bitmap data saved when image created from bitmapsource
        public double DpiX { get; set; } = 96;
        public double DpiY { get; set; } = 96;

        public Matrix<double> this[RGBChannel channel]
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

        public double this[int y, int x, RGBChannel channel]
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
                return 0.333333 * ( ImageMatrix[0].At(y,x) + ImageMatrix[1].At(y, x) + ImageMatrix[2].At(y, x));
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

        public IImage Clone()
        {
            ColorImage img = new ColorImage()
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

        public void FromGrayImage(GrayScaleImage gimage)
        {
            ImageMatrix[0] = new DenseMatrix(gimage.RowCount, gimage.ColumnCount);
            ImageMatrix[1] = new DenseMatrix(gimage.RowCount, gimage.ColumnCount);
            ImageMatrix[2] = new DenseMatrix(gimage.RowCount, gimage.ColumnCount);
            int x, y;

            for(x = 0; x < ColumnCount; ++x)
            {
                for(y = 0; y < RowCount; ++y)
                {
                    this[y, x, 0] = gimage[y, x];
                    this[y, x, 1] = gimage[y, x];
                    this[y, x, 2] = gimage[y, x];
                }
            }
        }

        public void FromHSIImage(HSIImage hslimage)
        {
            ImageMatrix[0] = new DenseMatrix(hslimage.RowCount, hslimage.ColumnCount);
            ImageMatrix[1] = new DenseMatrix(hslimage.RowCount, hslimage.ColumnCount);
            ImageMatrix[2] = new DenseMatrix(hslimage.RowCount, hslimage.ColumnCount);
            int x, y;

            double r, g, b;
            for(x = 0; x < ColumnCount; ++x)
            {
                for(y = 0; y < RowCount; ++y)
                {
                    HSIToRGB(hslimage[y, x, HSIChannel.Hue], hslimage[y, x, HSIChannel.Saturation], hslimage[y, x, HSIChannel.Intensity],
                        out r, out g, out b);
                    this[y, x, RGBChannel.Red] = r;
                    this[y, x, RGBChannel.Green] = g;
                    this[y, x, RGBChannel.Blue] = b;
                }
            }
        }

        public void FromBitmapSource(BitmapSource bitmap)
        {
            if(bitmap.Format != PixelFormats.Rgba128Float)
            {
                FormatConvertedBitmap bitmapFormater = new FormatConvertedBitmap();
                // BitmapSource objects like FormatConvertedBitmap can only have their properties
                // changed within a BeginInit/EndInit block.
                bitmapFormater.BeginInit();
                // Use the BitmapSource object defined above as the source for this new 
                // BitmapSource (chain the BitmapSource objects together).
                bitmapFormater.Source = bitmap;
                // Set the new format to Rgba128Float
                bitmapFormater.DestinationFormat = PixelFormats.Rgba128Float;
                bitmapFormater.EndInit();

                bitmap = bitmapFormater;
            }

            DpiX = bitmap.DpiX;
            DpiY = bitmap.DpiY;

            int stride = bitmap.PixelWidth * 4 * sizeof(float);
            float[] data = new float[bitmap.PixelHeight * bitmap.PixelWidth * 4];
            bitmap.CopyPixels(data, stride, 0);
            
            ImageMatrix[0] = new DenseMatrix(bitmap.PixelHeight, bitmap.PixelWidth);
            ImageMatrix[1] = new DenseMatrix(bitmap.PixelHeight, bitmap.PixelWidth);
            ImageMatrix[2] = new DenseMatrix(bitmap.PixelHeight, bitmap.PixelWidth);

            for(int imgy = 0; imgy < bitmap.PixelHeight; ++imgy)
            {
                for(int imgx = 0; imgx < bitmap.PixelWidth; ++imgx)
                {
                    // Bitmap stores data in row-major order and matrix in column major
                    // So store data to transposed matrix and transpose it so bitmap[y,x] == matrix[y,x]
                    // Format is Rgba32, so first float is r, then g and b
                    ImageMatrix[0][imgy, imgx] = data[4 * imgy * bitmap.PixelWidth + 4 * imgx];
                    ImageMatrix[1][imgy, imgx] = data[4 * imgy * bitmap.PixelWidth + 4 * imgx + 1];
                    ImageMatrix[2][imgy, imgx] = data[4 * imgy * bitmap.PixelWidth + 4 * imgx + 2];
                }
            }
        }

        public void FromBitmapSource(BitmapSource bitmap, Int32Rect area)
        {
            if(bitmap.Format != PixelFormats.Rgba128Float)
            {
                FormatConvertedBitmap bitmapFormater = new FormatConvertedBitmap();
                // BitmapSource objects like FormatConvertedBitmap can only have their properties
                // changed within a BeginInit/EndInit block.
                bitmapFormater.BeginInit();
                // Use the BitmapSource object defined above as the source for this new 
                // BitmapSource (chain the BitmapSource objects together).
                bitmapFormater.Source = bitmap;
                // Set the new format to Rgba128Float
                bitmapFormater.DestinationFormat = PixelFormats.Rgba128Float;
                bitmapFormater.EndInit();

                bitmap = bitmapFormater;
            }

            DpiX = bitmap.DpiX;
            DpiY = bitmap.DpiY;

            int stride = area.Width * 4 * sizeof(float);
            float[] data = new float[area.Height * area.Width * 4];
            bitmap.CopyPixels(area, data, stride, 0);
            
            ImageMatrix[0] = new DenseMatrix(area.Height, area.Width);
            ImageMatrix[1] = new DenseMatrix(area.Height, area.Width);
            ImageMatrix[2] = new DenseMatrix(area.Height, area.Width);

            for(int imgy = 0; imgy < area.Height; ++imgy)
            {
                for(int imgx = 0; imgx < area.Width; ++imgx)
                {
                    // Bitmap stores data in row-major order and matrix in column major
                    // So store data to transposed matrix and transpose it so bitmap[y,x] == matrix[y,x]
                    // Format is Rgba32, so first float is r, then g and b
                    ImageMatrix[0][imgy, imgx] = data[imgy * area.Width + imgx];
                    ImageMatrix[1][imgy, imgx] = data[imgy * area.Width + imgx + 1];
                    ImageMatrix[2][imgy, imgx] = data[imgy * area.Width + imgx + 2];
                }
            }
        }

        public BitmapSource ToBitmapSource()
        {
            int stride = ColumnCount * 4 * sizeof(float);
            float[] data = new float[RowCount * ColumnCount * 4];

            for(int imgy = 0; imgy < RowCount; ++imgy)
            {
                for(int imgx = 0; imgx < ColumnCount; ++imgx)
                {
                    data[4 * imgy * ColumnCount + 4 * imgx] = (float)ImageMatrix[0][imgy, imgx];
                    data[4 * imgy * ColumnCount + 4 * imgx + 1] = (float)ImageMatrix[1][imgy, imgx];
                    data[4 * imgy * ColumnCount + 4 * imgx + 2] = (float)ImageMatrix[2][imgy, imgx];
                    data[4 * imgy * ColumnCount + 4 * imgx + 3] = 1.0f;
                }
            }

            return BitmapSource.Create(ColumnCount, RowCount, DpiX, DpiY,
                PixelFormats.Rgba128Float, null, data, stride);
        }

        public BitmapSource ToBitmapSource(Int32Rect area)
        {
            int stride = area.Width * 4 * sizeof(float);
            float[] data = new float[area.Width * area.Height * 4];

            for(int imgy = 0; imgy < area.Height; ++imgy)
            {
                for(int imgx = 0; imgx < area.Width; ++imgx)
                {
                    data[4 * imgy * ColumnCount + 4 * imgx] = (float)ImageMatrix[0][area.Y + imgy, area.X + imgx];
                    data[4 * imgy * ColumnCount + 4 * imgx + 1] = (float)ImageMatrix[1][area.Y + imgy, area.X + imgx];
                    data[4 * imgy * ColumnCount + 4 * imgx + 2] = (float)ImageMatrix[2][area.Y + imgy, area.X + imgx];
                    data[4 * imgy * ColumnCount + 4 * imgx + 3] = 1.0f;
                }
            }

            return BitmapSource.Create(area.Height, area.Width, DpiX, DpiY,
                PixelFormats.Rgba128Float, null, data, stride);
        }

        public static void HSIToRGB(double h, double s, double i, out double r, out double g, out double b)
        {
            if(s < 0.002f)
            {
                r = i / 3.0f;
                g = r;
                b = r;
            }
            else if(h < Math.PI * 0.6666667)
            {
                b = i * (1.0f - s);
                r = (i * (1 + (s * Math.Cos(h) / Math.Cos(Math.PI * 0.333 - h))));
                g = 3 * i - (r + b);
            }
            else if(h < Math.PI * 1.3333333)
            {
                h = h - Math.PI * 0.6666667;

                r = i * (1.0f - s);
                g = (i * (1 + (s * Math.Cos(h) / Math.Cos(Math.PI * 0.333 - h))));
                b = 3 * i - (r + g);
            }
            else
            {
                h = h - Math.PI * 1.3333333;

                g = i * (1.0f - s);
                b = (i * (1 + (s * Math.Cos(h) / Math.Cos(Math.PI * 0.333 - h))));
                r = 3 * i - (b + g);
            }
        }

        internal class ColorImageDebugView
        {
            private ColorImage _image;

            public ColorImageDebugView(ColorImage img)
            {
                _image = img;
            }

            public DoubleMatrixVisualiser Red
            {
                get
                {
                    return new DoubleMatrixVisualiser(_image[RGBChannel.Red]);
                }
            }

            public DoubleMatrixVisualiser Green
            {
                get
                {
                    return new DoubleMatrixVisualiser(_image[RGBChannel.Green]);
                }
            }

            public DoubleMatrixVisualiser Blue
            {
                get
                {
                    return new DoubleMatrixVisualiser(_image[RGBChannel.Blue]);
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
                return "RGB. Rows : " + Rows.ToString() + ", Columns: " + Columns.ToString();
            }
        }
    }
}
