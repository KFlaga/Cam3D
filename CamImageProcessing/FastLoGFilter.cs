using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamImageProcessing
{
    // Specialized version of ImageFilter, as name suggests filter
    // image with LoG filter, but uses 2 x 1D filtering, so computational
    // complexity is smaller by an order ( theoreticly noticable improvement 
    // for masks larger or equal 9 pixels )
    public class FastLoGFilter : ImageFilter
    {
        private Vector<float> _mask_Gx;
        private Vector<float> _mask_Gy;
        private Vector<float> _mask_Lx;
        private Vector<float> _mask_Ly;

        public override GrayScaleImage ApplyFilter()
        {
            return base.ApplyFilter();
        }

        public void SetMasks(int size, float sgm)
        {
            _mask_Gx = new DenseVector(size);
            _mask_Gy = new DenseVector(size);
            _mask_Lx = new DenseVector(size);
            _mask_Ly = new DenseVector(size);

            float sgmDenum = 1 / (float)(Math.Sqrt(2 * (float)(Math.PI)) * sgm);
        }
    }
}
