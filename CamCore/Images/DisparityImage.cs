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
    [DebuggerTypeProxy(typeof(DisparityImageDebugView))]
    public class DisparityImage : IImage
    {
        public Matrix<double> ImageMatrix { get; set; }
        public int ColumnCount { get { return ImageMatrix.ColumnCount; } }
        public int RowCount { get { return ImageMatrix.RowCount; } }
        public int ChannelsCount { get { return 1; } }

        public int InvalidDisparity { get; set; } = 254;

        // Bitmap data saved when image created from bitmapsource
        public double DpiX { get; set; } = 72;
        public double DpiY { get; set; } = 72;

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
            return new DisparityImage();
        }

        public IImage CreateOfSameClass(int rows, int cols)
        {
            DisparityImage img = new DisparityImage();
            img.ImageMatrix = new DenseMatrix(rows, cols);
            return img;
        }

        public IImage CreateOfSameClass(Matrix<double>[] matrices)
        {
            DisparityImage img = new DisparityImage();
            img.ImageMatrix = matrices[0];
            return img;
        }

        public IImage Clone()
        {
            return new DisparityImage() { ImageMatrix = ImageMatrix.Clone() };
        }

        object ICloneable.Clone()
        {
            return new DisparityImage() { ImageMatrix = ImageMatrix.Clone() };
        }
        
        public void FromBitmapSource(BitmapSource bitmap)
        {
            if(bitmap.Format != PixelFormats.Gray8)
            {
                FormatConvertedBitmap bitmapFormater = new FormatConvertedBitmap();
                bitmapFormater.BeginInit();
                bitmapFormater.Source = bitmap;
                bitmapFormater.DestinationFormat = PixelFormats.Gray8;
                bitmapFormater.EndInit();

                bitmap = bitmapFormater;
            }

            DpiX = bitmap.DpiX;
            DpiY = bitmap.DpiY;

            int stride = bitmap.PixelWidth;
            byte[] data = new byte[bitmap.PixelHeight * bitmap.PixelWidth];
            bitmap.CopyPixels(data, stride, 0);

            ImageMatrix = new DenseMatrix(bitmap.PixelHeight, bitmap.PixelWidth);
            for(int imgy = 0; imgy < bitmap.PixelHeight; ++imgy)
            {
                for(int imgx = 0; imgx < bitmap.PixelWidth; ++imgx)
                {
                    // Bitmap stores data in row-major order and matrix in column major
                    // So store data to transposed matrix and transpose it so bitmap[y,x] == matrix[y,x]
                    ImageMatrix[imgy, imgx] = data[imgy * bitmap.PixelWidth + imgx];
                }
            }
        }

        public BitmapSource ToBitmapSource()
        {
            int stride = ColumnCount;
            byte[] data = new byte[RowCount * ColumnCount];

            for(int imgy = 0; imgy < RowCount; ++imgy)
            {
                for(int imgx = 0; imgx < ColumnCount; ++imgx)
                {
                    data[imgy * ColumnCount + imgx] = (byte)ImageMatrix[imgy, imgx];
                }
            }

            return BitmapSource.Create(ColumnCount, RowCount, DpiX, DpiY,
                PixelFormats.Gray8, null, data, stride);
        }

        public void FromDisparityMap(DisparityMap map)
        {
            ImageMatrix = new DenseMatrix(map.RowCount, map.ColumnCount);
            for(int c = 0; c < ColumnCount; ++c)
            {
                for(int r = 0; r < RowCount; ++r)
                {
                    if(map[r, c].IsValid())
                    {
                        ImageMatrix[r, c] = Math.Abs(map[r, c].SubDX);
                    }
                    else
                    {
                        ImageMatrix[r, c] = InvalidDisparity;
                    }
                }
            }
        }

        public DisparityMap ToDisparityMap(bool negate = false)
        {
            DisparityMap map = new DisparityMap(RowCount, ColumnCount);

            for(int c = 0; c < ColumnCount; ++c)
            {
                for(int r = 0; r < RowCount; ++r)
                {
                    Disparity disparity = new Disparity();
                    double d = ImageMatrix[r, c];
                    if(d.Round() >= InvalidDisparity)
                    {
                        disparity.Flags = (int)DisparityFlags.Invalid;
                    }
                    else
                    {
                        disparity.Flags = (int)DisparityFlags.Valid;
                        disparity.SubDX = d * (negate ? -1.0 : 1.0);
                        disparity.DX = d.Round();
                    }
                    map[r, c] = disparity;
                }
            }

            return map; 
        }

        internal class DisparityImageDebugView
        {
            private GrayScaleImage _image;

            public DisparityImageDebugView(GrayScaleImage img)
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
