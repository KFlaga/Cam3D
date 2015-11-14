using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamImageProcessing
{
    public static class LUTs
    {
        public static Matrix LUT_x2y2 { get; private set; }
        public static void LUT_x2y2_Init(int xmax, int ymax)
        {
            LUT_x2y2 = new DenseMatrix(ymax, xmax);
        }

    }
}
