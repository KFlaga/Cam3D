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
                    ImageMatrix[y,x] = (cimage[y, x, 0] +
                        cimage[y, x, 1] + cimage[y, x, 2] ) / 3;
                }
            }
        }

        public void FromBitmapSource(BitmapSource bitmap)
        {
            if (bitmap.Format != PixelFormats.Gray32Float)
            {
                FormatConvertedBitmap bitmapFormater = new FormatConvertedBitmap();
                // BitmapSource objects like FormatConvertedBitmap can only have their properties
                // changed within a BeginInit/EndInit block.
                bitmapFormater.BeginInit();
                // Use the BitmapSource object defined above as the source for this new 
                // BitmapSource (chain the BitmapSource objects together).
                bitmapFormater.Source = bitmap;
                // Set the new format to Gray32Float (grayscale).
                bitmapFormater.DestinationFormat = PixelFormats.Gray32Float;
                bitmapFormater.EndInit();

                bitmap = bitmapFormater;
            }

            DpiX = bitmap.DpiX;
            DpiY = bitmap.DpiY;

            int stride = bitmap.PixelWidth * sizeof(double);
            double[] data = new double[bitmap.PixelHeight * bitmap.PixelWidth];
            bitmap.CopyPixels(data, stride, 0);
            // Bitmap stores data in row-major order and matrix in column major
            // So store data to transposed matrix and transpose it so bitmap[y,x] == matrix[y,x]
            ImageMatrix = new DenseMatrix(bitmap.PixelWidth, bitmap.PixelHeight, data);
            ImageMatrix = ImageMatrix.Transpose();
        }

        public void FromBitmapSource(BitmapSource bitmap, Int32Rect area)
        {
            if (bitmap.Format != PixelFormats.Gray32Float)
            {
                FormatConvertedBitmap bitmapFormater = new FormatConvertedBitmap();
                // BitmapSource objects like FormatConvertedBitmap can only have their properties
                // changed within a BeginInit/EndInit block.
                bitmapFormater.BeginInit();
                // Use the BitmapSource object defined above as the source for this new 
                // BitmapSource (chain the BitmapSource objects together).
                bitmapFormater.Source = bitmap;
                // Set the new format to Gray32Float (grayscale).
                bitmapFormater.DestinationFormat = PixelFormats.Gray32Float;
                bitmapFormater.EndInit();

                bitmap = bitmapFormater;
            }

            DpiX = bitmap.DpiX;
            DpiY = bitmap.DpiY;

            int stride = area.Width * sizeof(double);
            double[] data = new double[area.Height * area.Width];
            bitmap.CopyPixels(area, data, stride, 0);
            ImageMatrix = new DenseMatrix(area.Width, area.Height, data);
            ImageMatrix = ImageMatrix.Transpose();
        }

        public BitmapSource ToBitmapSource( )
        {
            int stride = SizeX * sizeof(float);
            return BitmapSource.Create(SizeX, SizeY, DpiX, DpiY, PixelFormats.Gray32Float, null,
               ImageMatrix.Storage.ToRowMajorArray(), stride);
        }

        public BitmapSource ToBitmapSource(Int32Rect area)
        {
            Matrix<double> submat = ImageMatrix.SubMatrix(area.Y, area.Height, area.X, area.Width);
            int stride = area.Width * sizeof(float);
            return BitmapSource.Create(SizeX, SizeY, DpiX, DpiY, PixelFormats.Gray32Float, null,
               submat.Storage.ToRowMajorArray(), stride);
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
