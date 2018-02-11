using System.Collections.Generic;
using CamCore;
using MathNet.Numerics.LinearAlgebra;

namespace CamAlgorithms
{
    public class GaussFilter : ImageFilter
    {
        public Matrix<double> Filter { get; set; }
        public int WindowRadius { get; set; }
        public double Deviation { get; set; }

        public override string Name
        {
            get
            {
                return "Gauss Filter";
            }
        }

        public override void InitParameters()
        {
            Parameters = new List<IAlgorithmParameter>();
            IAlgorithmParameter winRadius = new IntParameter(
                "Filter Window Size", "FWS", 4, 1, 101);

            Parameters.Add(winRadius);

            IAlgorithmParameter deviation = new DoubleParameter(
               "Filter Deviation", "FD", 1.6f, 0.01f, 50.0f);

            Parameters.Add(deviation);
        }

        public override void UpdateParameters()
        {
            WindowRadius = (int)IAlgorithmParameter.FindValue("FES", Parameters);
            Deviation = (double)IAlgorithmParameter.FindValue("FD", Parameters);

            Filter = ImageFilter.GetFilter_Gauss(2 * WindowRadius + 1, Deviation);
        }

        public override Matrix<double> ApplyFilter()
        {
            Matrix<double> filtered = ImageFilter.ApplyFilter(Image, Filter);
            return filtered;
        }

        public override Matrix<double> ApplyFilterShrink()
        {
            Matrix<double> filtered = ImageFilter.ApplyFilterShrink(Image, Filter);

            return filtered.SubMatrix(WindowRadius, Image.RowCount - 2* WindowRadius,
                WindowRadius, Image.ColumnCount - 2 * WindowRadius);
        }
    }
}
