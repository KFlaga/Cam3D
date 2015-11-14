using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using System;
using System.Collections.Generic;

namespace CamImageProcessing
{
    // Universal image filter which compute convolution of image with
    // given filter mask
    public class ImageFilter
    {
        public GrayScaleImage Image { get; set; }
        public Matrix<float> Filter { get; set; }

        public virtual GrayScaleImage ApplyFilter()
        {
            return new GrayScaleImage()
            {
                ImageMatrix = MatrixConvolution.Convolve(Image.ImageMatrix, Filter)
            };
        }

        public virtual GrayScaleImage ApplyFilterShrink()
        {
            return new GrayScaleImage()
            {
                ImageMatrix = MatrixConvolution.ConvolveShrink(Image.ImageMatrix, Filter)
            };
        }

        public static Matrix<float> GetFilter_LoGNorm(int size, float sgm)
        {
            Matrix < float > filter = new DenseMatrix(size);
            int win =   size / 2;
            float sgm2 = 1 / (2 * sgm * sgm);
            float x2y2 = 0;

            for (int x = -win; x <= win; ++x)
            {
                for(int y = -win; y <= win; ++y)
                {
                    x2y2 = (x * x + y * y) * sgm2;
                    filter[x + win, y + win] = (1 - x2y2) * (float)Math.Exp(-x2y2);
                }
            }

            return filter;
        }

        public static Matrix<float> GetFilter_Gauss(int size, float sgm)
        {
            Matrix<float>  filter = new DenseMatrix(size);
            int win = size / 2;
            float sgm2 = 1 / (2 * sgm * sgm);
            float sgm1 = 1 / (sgm * (float)Math.Sqrt(2 * Math.PI));
            float x2y2 = 0;

            for (int x = -win; x <= win; ++x)
            {
                for (int y = -win; y <= win; ++y)
                {
                    x2y2 = (x * x + y * y) * sgm2;
                    filter[x + win, y + win] = sgm1 * (float)Math.Exp(x2y2);
                }
            }
            return filter;
        }
    }
}
