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
    [DebuggerTypeProxy(typeof(GrayImageDebugView))]
    public class GrayScaleImage
    {
        public Matrix<double> ImageMatrix { get; set; }
        public int SizeX { get { return ImageMatrix.ColumnCount; } }
        public int SizeY { get { return ImageMatrix.RowCount; } }

        // Bitmap data saved when image created from bitmapsource
        public double DpiX { get; set; }
        public double DpiY { get; set; }

        public double this[int y, int x]
        {
            get
            {
                return ImageMatrix[y, x];
            }
            set
            {
                ImageMatrix[y, x] = value;
            }
        }

        public void FromColorImage(ColorImage cimage)
        {
            ImageMatrix = new DenseMatrix(cimage.SizeY, cimage.SizeX);
            int x, y;

            for(x = 0; x < SizeX; ++x)
            {
                for(y = 0; y < SizeY; ++y)
                {
                    ImageMatrix[y, x] = (cimage[y, x, 0] +
                        cimage[y, x, 1] + cimage[y, x, 2]) / 3;
                }
            }
        }

        public void FromBitmapSource(BitmapSource bitmap)
        {
            if(bitmap.Format != PixelFormats.Rgba128Float)
            {
                FormatConvertedBitmap bitmapFormater = new FormatConvertedBitmap();
                bitmapFormater.BeginInit();
                bitmapFormater.Source = bitmap;
                bitmapFormater.DestinationFormat = PixelFormats.Rgba128Float;
                bitmapFormater.EndInit();

                bitmap = bitmapFormater;
            }

            DpiX = bitmap.DpiX;
            DpiY = bitmap.DpiY;

            int stride = bitmap.PixelWidth * 4 * sizeof(float);
            float[] data = new float[bitmap.PixelHeight * bitmap.PixelWidth * 4];
            bitmap.CopyPixels(data, stride, 0);

            ImageMatrix = new DenseMatrix(bitmap.PixelHeight, bitmap.PixelWidth);
            for(int imgy = 0; imgy < bitmap.PixelHeight; ++imgy)
            {
                for(int imgx = 0; imgx < bitmap.PixelWidth; ++imgx)
                {
                    // Bitmap stores data in row-major order and matrix in column major
                    // So store data to transposed matrix and transpose it so bitmap[y,x] == matrix[y,x]
                    ImageMatrix[imgy, imgx] = (data[4 * imgy * bitmap.PixelWidth + 4 * imgx]
                     + data[4 * imgy * bitmap.PixelWidth + 4 * imgx + 1]
                     + data[4 * imgy * bitmap.PixelWidth + 4 * imgx + 2]) / 3.0;
                }
            }
        }

        public void FromBitmapSource(BitmapSource bitmap, Int32Rect area)
        {
            if(bitmap.Format != PixelFormats.Rgba128Float)
            {
                FormatConvertedBitmap bitmapFormater = new FormatConvertedBitmap();
                bitmapFormater.BeginInit();
                bitmapFormater.Source = bitmap;
                bitmapFormater.DestinationFormat = PixelFormats.Rgba128Float;
                bitmapFormater.EndInit();

                bitmap = bitmapFormater;
            }

            DpiX = bitmap.DpiX;
            DpiY = bitmap.DpiY;

            int stride = area.Width * 4 * sizeof(float);
            float[] data = new float[area.Height * area.Width * 4];
            bitmap.CopyPixels(area, data, stride, 0);

            for(int imgy = 0; imgy < area.Height; ++imgy)
            {
                for(int imgx = 0; imgx < area.Width; ++imgx)
                {
                    // Bitmap stores data in row-major order and matrix in column major
                    // So store data to transposed matrix and transpose it so bitmap[y,x] == matrix[y,x]
                    ImageMatrix[imgy, imgx] = (data[imgy * area.Width + imgx]
                     + data[imgy * area.Width + imgx + 1]
                     + data[imgy * area.Width + imgx + 2]) / 3.0;
                }
            }
        }

        public BitmapSource ToBitmapSource()
        {
            int stride = SizeX * sizeof(float);
            float[] data = new float[SizeY * SizeX];

            for(int imgy = 0; imgy < SizeY; ++imgy)
            {
                for(int imgx = 0; imgx < SizeX; ++imgx)
                {
                    data[imgy * SizeX + imgx] = (float)ImageMatrix[imgy, imgx];
                }
            }

            return BitmapSource.Create(SizeX, SizeY, DpiX, DpiY,
                PixelFormats.Gray32Float, null, data, stride);
        }

        public BitmapSource ToBitmapSource(Int32Rect area)
        {
            int stride = area.Width * sizeof(float);
            float[] data = new float[area.Width * area.Height];

            for(int imgy = 0; imgy < area.Height; ++imgy)
            {
                for(int imgx = 0; imgx < area.Width; ++imgx)
                {
                    data[imgy * SizeX + imgx] = (float)ImageMatrix[area.Y + imgy, area.X + imgx];
                }
            }

            return BitmapSource.Create(SizeX, SizeY, DpiX, DpiY, PixelFormats.Gray32Float, null,data, stride);
        }

        internal class GrayImageDebugView
        {
            private GrayScaleImage _image;

            public GrayImageDebugView(GrayScaleImage img)
            {
                _image = img;
            }

            public DoubleMatrixVisualiser ImageMatrix
            {
                get
                {
                    return new DoubleMatrixVisualiser(_image.ImageMatrix);
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
                return "Gray. Rows : " + Rows.ToString() + ", Columns: " + Columns.ToString();
            }
        }
    }
}
