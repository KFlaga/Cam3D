using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CamCore
{
    // Wrapper around another image with only some of columns valid for each row
    public class MaskedImage : IImage
    {
        internal IImage _image;
        public IImage Image
        {
            get { return _image; }
            set
            {
                if(value != null)
                {
                    _image = value;
                    _mask = new bool[ColumnCount, RowCount];
                    for(int c = 0; c < ColumnCount; ++c)
                    {
                        for(int r = 0; r < RowCount; ++r)
                        {
                            _mask[c, r] = _image.HaveValueAt(r, c);
                        }
                    }
                    while(_image is MaskedImage)
                    {
                        _image = ((MaskedImage)_image).Image;
                    }
                }
                else
                {
                    _image = null;
                    _mask = null;
                }
            }
        }
        protected bool[,] _mask;

        public int RowCount { get { return _image.RowCount; } }
        public int ColumnCount { get { return _image.ColumnCount; } }
        public int ChannelsCount { get { return _image.ChannelsCount; } }

        public MaskedImage()
        {
            _image = null;
            _mask = null;
        }

        public MaskedImage(IImage baseImage)
        {
            Image = baseImage;
        }

        public double this[int y, int x]
        {
            get { return _image[y, x]; }
            set { _image[y, x] = value; }
        }

        public double this[int y, int x, int channel]
        {
            get { return _image[y, x, channel]; }
            set { _image[y, x, channel] = value; }
        }

        public Matrix<double> GetMatrix(int channel)
        {
            return _image.GetMatrix(channel);
        }

        public Matrix<double> GetMatrix()
        {
            return _image.GetMatrix();
        }

        public void SetMatrix(Matrix<double> matrix, int channel)
        {
            _image.SetMatrix(matrix, channel);
        }

        // Assumes x and y are in row/cols count range
        public bool HaveValueAt(int y, int x)
        {
            return _mask[x, y];
        }

        public void SetMaskAt(int y, int x, bool mask)
        {
            _mask[x, y] = mask;
        }

        public IImage CreateOfSameClass()
        {
            return new MaskedImage();
        }

        public IImage CreateOfSameClass(int rows, int cols)
        {
            MaskedImage img = new MaskedImage();

            if(_image != null)
            {
                img.Image = _image.CreateOfSameClass(rows, cols);
            }

            return img;
        }

        public IImage CreateOfSameClass(Matrix<double>[] matrices)
        {
            MaskedImage img = new MaskedImage();

            if(_image != null)
            {
                img.Image = _image.CreateOfSameClass(matrices);
            }

            return img;
        }

        public IImage Clone()
        {
            MaskedImage img = new MaskedImage()
            {
                _mask = (bool[,])_mask.Clone(),
                _image = _image.Clone()
            };
            return img;
        }

        object ICloneable.Clone()
        {
            return Clone();
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

            ColorImage cimg = new ColorImage();
            int stride = bitmap.PixelWidth * 4 * sizeof(float);
            float[] data = new float[bitmap.PixelHeight * bitmap.PixelWidth * 4];
            bitmap.CopyPixels(data, stride, 0);

            cimg.ImageMatrix[0] = new DenseMatrix(bitmap.PixelHeight, bitmap.PixelWidth);
            cimg.ImageMatrix[1] = new DenseMatrix(bitmap.PixelHeight, bitmap.PixelWidth);
            cimg.ImageMatrix[2] = new DenseMatrix(bitmap.PixelHeight, bitmap.PixelWidth);
            _mask = new bool[bitmap.PixelWidth, bitmap.PixelHeight];

            for(int imgy = 0; imgy < bitmap.PixelHeight; ++imgy)
            {
                for(int imgx = 0; imgx < bitmap.PixelWidth; ++imgx)
                {
                    // Bitmap stores data in row-major order and matrix in column major
                    // So store data to transposed matrix and transpose it so bitmap[y,x] == matrix[y,x]
                    // Format is Rgba128, so first float is r, then g and b and a
                    // False-Mask is indicated by low alpha
                    cimg.ImageMatrix[0][imgy, imgx] = data[4 * imgy * bitmap.PixelWidth + 4 * imgx];
                    cimg.ImageMatrix[1][imgy, imgx] = data[4 * imgy * bitmap.PixelWidth + 4 * imgx + 1];
                    cimg.ImageMatrix[2][imgy, imgx] = data[4 * imgy * bitmap.PixelWidth + 4 * imgx + 2];
                    _mask[imgx, imgy] = data[4 * imgy * bitmap.PixelWidth + 4 * imgx + 3] > 0.1f;
                }
            }

            _image = cimg;
        }

        public BitmapSource ToBitmapSource()
        {
            if(_image == null)
                return null;

            int stride = ColumnCount * 4 * sizeof(float);
            float[] data = new float[RowCount * ColumnCount * 4];

            if(_image.ChannelsCount == 1)
            {
                for(int imgy = 0; imgy < RowCount; ++imgy)
                {
                    for(int imgx = 0; imgx < ColumnCount; ++imgx)
                    {
                        data[4 * imgy * ColumnCount + 4 * imgx] = (float)_image.GetMatrix()[imgy, imgx];
                        data[4 * imgy * ColumnCount + 4 * imgx + 1] = (float)_image.GetMatrix()[imgy, imgx];
                        data[4 * imgy * ColumnCount + 4 * imgx + 2] = (float)_image.GetMatrix()[imgy, imgx];
                        data[4 * imgy * ColumnCount + 4 * imgx + 3] = _mask[imgx, imgy] ? 1.0f : 0.0f;
                    }
                }
            }
            else if(_image.ChannelsCount == 3)
            {
                for(int imgy = 0; imgy < RowCount; ++imgy)
                {
                    for(int imgx = 0; imgx < ColumnCount; ++imgx)
                    {
                        data[4 * imgy * ColumnCount + 4 * imgx] = (float)_image.GetMatrix(0)[imgy, imgx];
                        data[4 * imgy * ColumnCount + 4 * imgx + 1] = (float)_image.GetMatrix(1)[imgy, imgx];
                        data[4 * imgy * ColumnCount + 4 * imgx + 2] = (float)_image.GetMatrix(2)[imgy, imgx];
                        data[4 * imgy * ColumnCount + 4 * imgx + 3] = _mask[imgx, imgy] ? 1.0f : 0.0f;
                    }
                }
            }

            return BitmapSource.Create(ColumnCount, RowCount, 96, 96,
                PixelFormats.Rgba128Float, null, data, stride);
        }
    }
}
