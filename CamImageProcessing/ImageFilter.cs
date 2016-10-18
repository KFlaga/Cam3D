using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;

namespace CamImageProcessing
{
    // Universal image filter which compute convolution of image with
    // given filter mask
    public abstract class ImageFilter : IParameterizable
    {
        private List<AlgorithmParameter> _parameters;
        public List<AlgorithmParameter> Parameters
        {
            get { return _parameters; }
            protected set { _parameters = value; }
        }

        public virtual void InitParameters() { }
        public virtual void UpdateParameters() { }

        public Matrix<double> Image { get; set; }

        public abstract Matrix<double> ApplyFilter();
        public abstract Matrix<double> ApplyFilterShrink();

        public static Matrix<double> ApplyFilter(Matrix<double> image, Matrix<double> filter)
        {
            return MatrixConvolution.Convolve(image, filter);
        }

        public static Matrix<double> ApplyFilterShrink(Matrix<double> image, Matrix<double> filter)
        {
            return MatrixConvolution.Convolve(image, filter);
        }

        public static Matrix<double> GetFilter_LoGNorm(int size, double sgm)
        {
            Matrix < double > filter = new DenseMatrix(size);
            int win =   size / 2;
            double sgm2 = 1 / (2 * sgm * sgm);
            double x2y2 = 0;

            for (int x = -win; x <= win; ++x)
            {
                for(int y = -win; y <= win; ++y)
                {
                    x2y2 = (x * x + y * y) * sgm2;
                    filter[x + win, y + win] = (1 - x2y2) * (double)Math.Exp(-x2y2);
                }
            }

            return filter;
        }

        public static Matrix<double> GetFilter_Gauss(int size, double sgm)
        {
            Matrix<double>  filter = new DenseMatrix(size);
            int win = size / 2;
            double sgm2 = 1 / (2 * sgm * sgm);
            double sgm1 = 1 / (sgm * (double)Math.Sqrt(2 * Math.PI));
            double x2y2 = 0;

            for (int x = -win; x <= win; ++x)
            {
                for (int y = -win; y <= win; ++y)
                {
                    x2y2 = (x * x + y * y) * sgm2;
                    filter[x + win, y + win] = sgm1 * (double)Math.Exp(x2y2);
                }
            }
            return filter;
        }
    }
}
