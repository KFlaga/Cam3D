using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static CamCore.MatrixExtensions;

namespace CamCore
{
    [DebuggerTypeProxy(typeof(GrayImageDebugView))]
    public class GrayScaleImage : IImage
    {
        public Matrix<double> ImageMatrix { get; set; }
        public int ColumnCount { get { return ImageMatrix.ColumnCount; } }
        public int RowCount { get { return ImageMatrix.RowCount; } }
        public int ChannelsCount { get { return 1; } }

        // Bitmap data saved when image created from bitmapsource
        public double DpiX { get; set; }
        public double DpiY { get; set; }

        public double this[int y, int x]
        {
            get
            {
                return ImageMatrix.At(y, x);
            }
            set
            {
                ImageMatrix.At(y, x, value);
            }
        }

        public double this[int y, int x, int channel]
        {
            get
            {
                return this[y, x];
            }
            set
            {
                this[y, x] = value;
            }
        }

        public bool HaveValueAt(int y, int x) { return true; }

        public Matrix<double> GetMatrix(int channel)
        {
            return ImageMatrix;
        }

        public Matrix<double> GetMatrix()
        {
            return ImageMatrix;
        }

        public void SetMatrix(Matrix<double> matrix, int channel)
        {
            ImageMatrix = matrix;
        }

        public IImage CreateOfSameClass()
        {
            return new GrayScaleImage();
        }

        public IImage CreateOfSameClass(int rows, int cols)
        {
            GrayScaleImage img = new GrayScaleImage();
            img.ImageMatrix = new DenseMatrix(rows, cols);
            return img;
        }

        public IImage CreateOfSameClass(Matrix<double>[] matrices)
        {
            GrayScaleImage img = new GrayScaleImage();
            img.ImageMatrix = matrices[0];
            return img;
        }

        public IImage Clone()
        {
            return new GrayScaleImage() { ImageMatrix = ImageMatrix.Clone() };
        }

        object ICloneable.Clone()
        {
            return new GrayScaleImage() { ImageMatrix = ImageMatrix.Clone() };
        }

        public void FromColorImage(ColorImage cimage)
        {
            ImageMatrix = new DenseMatrix(cimage.RowCount, cimage.ColumnCount);
            int x, y;

            for(x = 0; x < ColumnCount; ++x)
            {
                for(y = 0; y < RowCount; ++y)
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
            int stride = ColumnCount * sizeof(float);
            float[] data = new float[RowCount * ColumnCount];

            for(int imgy = 0; imgy < RowCount; ++imgy)
            {
                for(int imgx = 0; imgx < ColumnCount; ++imgx)
                {
                    data[imgy * ColumnCount + imgx] = (float)ImageMatrix[imgy, imgx];
                }
            }

            return BitmapSource.Create(ColumnCount, RowCount, DpiX, DpiY,
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
                    data[imgy * ColumnCount + imgx] = (float)ImageMatrix[area.Y + imgy, area.X + imgx];
                }
            }

            return BitmapSource.Create(ColumnCount, RowCount, DpiX, DpiY, PixelFormats.Gray32Float, null,data, stride);
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
                return "Gray. Rows : " + Rows.ToString() + ", Columns: " + Columns.ToString();
            }
        }
    }
}
