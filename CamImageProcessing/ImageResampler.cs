using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamImageProcessing
{
    // Scales up/down image 2 times
    public class ImageResampler
    {
        public delegate Matrix<double> ResampleDelegate(Matrix<double> baseImage);

        public Matrix<double> ImageMatrix;
        public Matrix<double> ImageResampled;

        public enum DownsampleMethod
        {
            SkipPixel,
            Average22,
            Gauss33
        }

        public enum UpsampleMethod
        {
            CrossFill,
            SimpleCopy
        }

        private DownsampleMethod _methodDown;
        public DownsampleMethod UsedDownsampleMethod
        {
            get { return _methodDown; }
            set
            {
                _methodDown = value;
                switch(_methodDown)
                {
                    case DownsampleMethod.SkipPixel:
                        _downsampleFunc = Downsample_Skippixel;
                        break;
                    case DownsampleMethod.Average22:
                        _downsampleFunc = Downsample_Average22;
                        break;
                    case DownsampleMethod.Gauss33:
                        _downsampleFunc = Downsample_Gauss33;
                        break;
                }
            }
        }

        private UpsampleMethod _methodUp;
        public UpsampleMethod UsedUpsampleMethod
        {
            get { return _methodUp; }
            set
            {
                _methodUp = value;
                switch(_methodUp)
                {
                    case UpsampleMethod.CrossFill:
                        _upsampleFunc = Upsample_CrossAverage;
                        break;
                    case UpsampleMethod.SimpleCopy:
                        _upsampleFunc = Upsample_Copy;
                        break;
                }
            }
        }

        ResampleDelegate _downsampleFunc;
        ResampleDelegate _upsampleFunc;

        public void Downsample()
        {
            ImageResampled = _downsampleFunc(ImageMatrix);
        }

        public void Upsample()
        {
            ImageResampled = _downsampleFunc(ImageMatrix);
        }

        public Matrix<double> Downsample_Skippixel(Matrix<double> baseImage)
        {
            int rows2 = baseImage.RowCount / 2;
            int cols2 = baseImage.ColumnCount / 2;

            var scaledImage = new DenseMatrix(rows2, cols2);

            // Save each second pixel
            for(int c = 0; c < scaledImage.ColumnCount; ++c)
            {
                for(int r = 0; r < scaledImage.RowCount; ++r)
                {
                    scaledImage.At(r, c,
                        baseImage.At(2 * r, 2 * c));
                }
            }

            return scaledImage;
        }

        public Matrix<double> Downsample_Average22(Matrix<double> baseImage)
        {
            int rows2 = baseImage.RowCount / 2;
            int cols2 = baseImage.ColumnCount / 2;

            var scaledImage = new DenseMatrix(rows2, cols2);

            // For each pixel set average of 2x2 neighbourhood
            for(int c = 0; c < scaledImage.ColumnCount; ++c)
            {
                for(int r = 0; r < scaledImage.RowCount; ++r)
                {
                    double sum = baseImage.At(2 * r, 2 * c) + baseImage.At(2 * r, 2 * c + 1) +
                        baseImage.At(2 * r + 1, 2 * c) + baseImage.At(2 * r + 1, 2 * c + 1);
                    scaledImage.At(r, c, 0.25 * sum);
                }
            }

            return scaledImage;
        }

        // Kernel:
        // 0.077847    0.123317    0.077847
        // 0.123317    0.195346    0.123317
        // 0.077847    0.123317    0.077847
        double[] _gauss33 = new double[]
        {
            0.077847, 0.123317, 0.195346
        };

        public Matrix<double> Downsample_Gauss33(Matrix<double> baseImage)
        {
            int rows2 = baseImage.RowCount / 2;
            int cols2 = baseImage.ColumnCount / 2;

            var scaledImage = new DenseMatrix(rows2, cols2);
            double sum;
            int lr = scaledImage.RowCount - 1;
            int lc = scaledImage.ColumnCount - 1;
            // TODO: borders
            for(int c = 0; c < lc; ++c)
            {
                for(int r = 0; r < lr; ++r)
                {
                    sum = baseImage.At(2 * r, 2 * c) * _gauss33[0] +
                        baseImage.At(2 * r, 2 * c + 1) * _gauss33[1] +
                        baseImage.At(2 * r, 2 * c + 2) * _gauss33[0] +
                        baseImage.At(2 * r + 1, 2 * c) * _gauss33[1] +
                        baseImage.At(2 * r + 1, 2 * c + 1) * _gauss33[2] +
                        baseImage.At(2 * r + 1, 2 * c + 2) * _gauss33[1] +
                        baseImage.At(2 * r + 2, 2 * c) * _gauss33[0] +
                        baseImage.At(2 * r + 2, 2 * c + 1) * _gauss33[1] +
                        baseImage.At(2 * r + 2, 2 * c + 2) * _gauss33[0];

                    scaledImage.At(r, c, sum);
                }
                // Last row -> mirror one out of bounds
                sum = baseImage.At(2 * lr, 2 * c) * _gauss33[0] * 2.0 +
                    baseImage.At(2 * lr, 2 * c + 1) * _gauss33[1] * 2.0 +
                    baseImage.At(2 * lr, 2 * c + 2) * _gauss33[0] * 2.0 +
                    baseImage.At(2 * lr + 1, 2 * c) * _gauss33[1] +
                    baseImage.At(2 * lr + 1, 2 * c + 1) * _gauss33[2] +
                    baseImage.At(2 * lr + 1, 2 * c + 2) * _gauss33[1];

                scaledImage.At(lr, c, sum);
            }
            // Last column -> mirror one out of bounds
            for(int r = 0; r < lr; ++r)
            {
                sum = baseImage.At(2 * r, 2 * lc) * _gauss33[0] * 2.0 +
                    baseImage.At(2 * r, 2 * lc + 1) * _gauss33[1] +
                    baseImage.At(2 * r + 1, 2 * lc) * _gauss33[1] * 2.0 +
                    baseImage.At(2 * r + 1, 2 * lc + 1) * _gauss33[2] +
                    baseImage.At(2 * r + 2, 2 * lc) * _gauss33[0] * 2.0 +
                    baseImage.At(2 * r + 2, 2 * lc + 1) * _gauss33[1];

                scaledImage.At(r, lc, sum);
            }
            // Last row -> mirror row/col out of bounds=
            sum = baseImage.At(2 * lr, 2 * lc) * _gauss33[0] * 4.0 +
                baseImage.At(2 * lr, 2 * lc + 1) * _gauss33[1] * 2.0 +
                baseImage.At(2 * lr + 1, 2 * lc) * _gauss33[1] * 2.0 +
                baseImage.At(2 * lr + 1, 2 * lc + 1) * _gauss33[2];

            scaledImage.At(lr, lc, sum);

            return scaledImage;
        }

        public Matrix<double> Upsample_CrossAverage(Matrix<double> baseImage)
        {
            int rows2 = baseImage.RowCount * 2;
            int cols2 = baseImage.ColumnCount * 2;
            double sum;
            int lr = baseImage.RowCount - 1;
            int lc = baseImage.ColumnCount - 1;
            var scaledImage = new DenseMatrix(rows2, cols2);

            // TODO consider borders
            // 1) Base image pixels have (2r0,2c0) coords
            // 2) For each (2r0+1,2c0+1) let it be average of 4 diagonal neighbours
            // 3 For each (2r0,2c0+1),(2r0+2,2c0) let it be average of 4 edge neighbours (including thise from 2)
            for(int c = 0; c < lc; ++c)
            {
                for(int r = 0; r < lr; ++r)
                {
                    sum = baseImage.At(r, c) + baseImage.At(r + 1, c + 1)
                        + baseImage.At(r, c + 1) + baseImage.At(r + 1, c);

                    scaledImage.At(2 * r, 2 * c, baseImage.At(r, c));
                    scaledImage.At(2 * r + 1, 2 * c + 1, 0.25 * sum);
                }

                sum = baseImage.At(lr, c) + baseImage.At(lr - 1, c + 1)
                    + baseImage.At(lr, c + 1) + baseImage.At(lr - 1, c);

                scaledImage.At(2 * lr, 2 * c, baseImage.At(lr, c));
                scaledImage.At(2 * lr + 1, 2 * c + 1, 0.25 * sum);

                scaledImage.At(lr, c, sum);
            }
            // Last column -> mirror one out of bounds
            for(int r = 0; r < lr; ++r)
            {
                sum = baseImage.At(r, lc) + baseImage.At(r + 1, lc - 1)
                    + baseImage.At(r, lc - 1) + baseImage.At(r + 1, lc);

                scaledImage.At(2 * r, 2 * lc, baseImage.At(r, lc));
                scaledImage.At(2 * r + 1, 2 * lc + 1, 0.25 * sum);
            }
            // Last row -> mirror row/col out of bounds
            sum = baseImage.At(lr, lc) + baseImage.At(lr - 1, lc - 1)
                + baseImage.At(lr, lc - 1) + baseImage.At(lr - 1, lc);

            scaledImage.At(2 * lr, 2 * lc, baseImage.At(lr, lc));
            scaledImage.At(2 * lr + 1, 2 * lc + 1, 0.25 * sum);

            // First row/column
            for(int r = 0; r < lr; ++r)
            {
                sum = baseImage.At(r, 0) + baseImage.At(r + 1, 0) +
                    scaledImage.At(2 * r + 1, 1) * 2.0;
                scaledImage.At(2 * r + 1, 0, 0.25 * sum);
            }
            sum = baseImage.At(lr, 0) + baseImage.At(lr - 1, 0) +
                scaledImage.At(2 * lr + 1, 1) * 2.0;
            scaledImage.At(2 * lr + 1, 0, 0.25 * sum);

            for(int c = 0; c < lc; ++c)
            {
                sum = baseImage.At(0, c) + baseImage.At(0, c + 1) +
                    scaledImage.At(1, 2 * c + 1) * 2.0;
                scaledImage.At(0, 2 * c + 1, 0.25 * sum);
            }
            sum = baseImage.At(0, lc) + baseImage.At(0, lc - 1) +
                scaledImage.At(1, 2 * lc + 1) * 2.0;
            scaledImage.At(0, 2 * lc + 1, 0.25 * sum);

            for(int c = 0; c < lc; ++c)
            {
                for(int r = 0; r < lr; ++r)
                {
                    sum = baseImage.At(r, c) + baseImage.At(r + 1, c) +
                        scaledImage.At(2 * r + 1, 2 * c + 1) + scaledImage.At(2 * r + 1, 2 * c - 1);
                    scaledImage.At(2 * r + 1, 2 * c, 0.25 * sum);

                    sum = baseImage.At(r, c) + baseImage.At(r, c + 1) +
                        scaledImage.At(2 * r + 1, 2 * c + 1) + scaledImage.At(2 * r - 1, 2 * c + 1);
                    scaledImage.At(2 * r, 2 * c + 1, 0.25 * sum);
                }
                sum = baseImage.At(lr, c) + baseImage.At(lr - 1, c) +
                    scaledImage.At(2 * lr + 1, 2 * c + 1) + scaledImage.At(2 * lr + 1, 2 * c - 1);
                scaledImage.At(2 * lr + 1, 2 * c, 0.25 * sum);

                sum = baseImage.At(lr, c) + baseImage.At(lr, c + 1) +
                    scaledImage.At(2 * lr + 1, 2 * c + 1) + scaledImage.At(2 * lr - 1, 2 * c + 1);
                scaledImage.At(2 * lr, 2 * c + 1, 0.25 * sum);
            }

            for(int r = 0; r < lr; ++r)
            {
                sum = baseImage.At(r, lc) + baseImage.At(r + 1, lc) +
                    scaledImage.At(2 * r + 1, 2 * lc + 1) + scaledImage.At(2 * r + 1, 2 * lc - 1);
                scaledImage.At(2 * r + 1, 2 * lc, 0.25 * sum);

                sum = baseImage.At(r, lc) + baseImage.At(r, lc - 1) +
                    scaledImage.At(2 * r + 1, 2 * lc + 1) + scaledImage.At(2 * r - 1, 2 * lc + 1);
                scaledImage.At(2 * r, 2 * lc + 1, 0.25 * sum);
            }

            sum = baseImage.At(lr, lc) + baseImage.At(lr - 1, lc) +
                scaledImage.At(2 * lr + 1, 2 * lc + 1) + scaledImage.At(2 * lr + 1, 2 * lc - 1);
            scaledImage.At(2 * lr + 1, 2 * lc, 0.25 * sum);

            sum = baseImage.At(lr, lc) + baseImage.At(lr, lc - 1) +
                scaledImage.At(2 * lr + 1, 2 * lc + 1) + scaledImage.At(2 * lr - 1, 2 * lc + 1);
            scaledImage.At(2 * lr, 2 * lc + 1, 0.25 * sum);

            return scaledImage;
        }

        public Matrix<double> Upsample_Copy(Matrix<double> baseImage)
        {
            int rows2 = baseImage.RowCount * 2;
            int cols2 = baseImage.ColumnCount * 2;

            var scaledImage = new DenseMatrix(rows2, cols2);

            for(int c = 0; c < baseImage.ColumnCount; ++c)
            {
                for(int r = 0; r < baseImage.RowCount; ++r)
                {
                    scaledImage.At(2 * r, 2 * c, baseImage.At(r, c));
                    scaledImage.At(2 * r + 1, 2 * c, baseImage.At(r, c));
                    scaledImage.At(2 * r, 2 * c + 1, baseImage.At(r, c));
                    scaledImage.At(2 * r + 1, 2 * c + 1, baseImage.At(r, c));
                }
            }

            return scaledImage;
        }
    }
}
