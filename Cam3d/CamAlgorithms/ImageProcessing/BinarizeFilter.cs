using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace CamAlgorithms
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

        public override string Name
        {
            get
            {
                return "Binarize Filter";
            }
        }

        public override void InitParameters()
        {
            base.InitParameters();
            IAlgorithmParameter tresh = new DoubleParameter(
                "Treshold", "TH", 0.5, 0.0, 1.0);

            Parameters.Add(tresh);

            IAlgorithmParameter inversed = new BooleanParameter(
               "Inverse Brightness", "IB", false);

            Parameters.Add(inversed);
        }

        public override void UpdateParameters()
        {
            base.UpdateParameters();
            Threshold = IAlgorithmParameter.FindValue<int>("TH", Parameters);
            Inverse = IAlgorithmParameter.FindValue<bool>("IB", Parameters);
        }
    }
}
