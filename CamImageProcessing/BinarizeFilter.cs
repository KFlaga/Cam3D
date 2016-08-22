using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
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

        public override Matrix<double> ApplyFilter()
        {
            Matrix<double> imageMat = new DenseMatrix(Image.RowCount, Image.ColumnCount);

            for(int r = 0; r < imageMat.RowCount; r++ )
            {
                for(int c = 0; c < imageMat.ColumnCount; c++ )
                {
                    if (Image[r, c] > Threshold)
                    {
                        imageMat[r, c] = Inverse ? 0 : 1;
                    }
                    else
                    {
                        imageMat[r, c] = Inverse ? 1 : 0;
                    }
                }
            }
            
            return imageMat;
        }

        public override Matrix<double> ApplyFilterShrink()
        {
            return ApplyFilter();
        }

        public override void InitParameters()
        {
            Parameters = new List<ProcessorParameter>();
            ProcessorParameter tresh = new ProcessorParameter(
                "Treshold", "TH",
                "System.Single", 0.5f, 0.0f, 1.0f);

            Parameters.Add(tresh);

            ProcessorParameter inversed = new ProcessorParameter(
               "Inverse Brightness", "IB",
               "System.Boolean", false, false, true);

            Parameters.Add(inversed);
        }

        public override void UpdateParameters()
        {
            Threshold = (int)ProcessorParameter.FindValue("TH", Parameters);
            Inverse = (bool)ProcessorParameter.FindValue("IB", Parameters);
        }
    }
}
