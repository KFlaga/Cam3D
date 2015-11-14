using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamImageProcessing
{
    public class BinarizeFilter : ImageFilter
    {
        public double Threshold { get; set; }
        // If inverse is true, dark pixels will have value of 1 instead of light
        public bool Inverse { get; set; }

        public override GrayScaleImage ApplyFilter()
        {
            GrayScaleImage image = new GrayScaleImage();
            Matrix<float> imageMat = new DenseMatrix(Image.ImageMatrix.RowCount, Image.ImageMatrix.ColumnCount);

            for(int r = 0; r < imageMat.RowCount; r++ )
            {
                for(int c = 0; c < imageMat.ColumnCount; c++ )
                {
                    if (Image.ImageMatrix[r, c] > Threshold)
                    {
                        imageMat[r, c] = Inverse ? 0 : 1;
                    }
                    else
                    {
                        imageMat[r, c] = Inverse ? 1 : 0;
                    }
                }
            }

            image.ImageMatrix = imageMat;
            return image;
        }

        public override GrayScaleImage ApplyFilterShrink()
        {
            return ApplyFilter();
        }
    }
}
