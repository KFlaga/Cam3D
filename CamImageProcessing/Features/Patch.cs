using MathNet.Numerics.LinearAlgebra;
using System;

namespace CamImageProcessing
{
    public class Patch
    {
        public Matrix<double> ImageMatrix { get; set; }
        public int Rows { get; set; }
        public int Cols { get; set; }
        public int StartRow { get; set; }
        public int StartCol { get; set; }

        public double this[int y, int x]
        {
            get
            {
                return ImageMatrix[StartRow + y, StartCol + x];
            }
        }

        public delegate double CorrelationComputer(Patch patchRef, Patch patchTest, double param); 

        public static double ComputePatchesCorrelation(Patch patchRef, Patch patchTest, double unused)
        {
            // corr = Pr .* Pt / |Pr|*|Pt|

            double corr = 0.0f;
            double sqLenRef = 0.0f, sqLenTest = 0.0f;
            int y, x;
            for(y = 0; y < patchRef.Rows; ++y)
            {
                for(x = 0; x < patchRef.Cols; ++x)
                {
                    double pr = patchRef[y, x];
                    double pt = patchTest[y, x];
                    corr += pr * pt;
                    sqLenRef += pr * pr;
                    sqLenTest += pt * pt;
                }
            }

            corr /= (double)Math.Sqrt(sqLenRef*sqLenTest);

            return corr;
        }

        public static double ComputePatchesSmoothCorrelation(Patch patchRef, Patch patchTest, double sgm_gauss)
        {
            //% Correlation: c = r / sqrt(dev_r^2 * dev_t^2)
            //% r = sum { G(y, x) * Pr(y +y0/2, x + x0/2) * Pt(y +y0/2, x + x0/2) }
            //% sqdev_r = sum{ G(y, x) * Pr(y + y0/2, x + x0/2)^2 }

            int r2 = patchRef.Rows / 2;
            int c2 = patchRef.Cols / 2;

            double corr = 0.0f, gauss = 0.0f;
            double sqDevRef = 0.0f, sqDevTest = 0.0f;
            double sgm2 = 2 * sgm_gauss * sgm_gauss;
            double norm_coeff = 1 / (sgm_gauss * (double)Math.Sqrt(2 * Math.PI));
            int y, x;
            for (y = 0; y < patchRef.Rows; ++y)
            {
                for (x = 0; x < patchRef.Cols; ++x)
                {
                    double pr = patchRef[y, x];
                    double pt = patchTest[y, x];
                    gauss = (double)Math.Exp(((x - c2) * (x - c2) + (y - r2) * (y - r2)) / sgm2) * norm_coeff;
                    corr += gauss * pr * pt;
                    sqDevRef += gauss * pr * pr;
                    sqDevTest += gauss * pt * pt;
                }
            }

            corr /= (double)Math.Sqrt(sqDevRef * sqDevTest);

            return corr;
        }
    }
}
