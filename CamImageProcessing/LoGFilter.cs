using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace CamImageProcessing
{
    public class LoGFilter : ImageFilter
    {
        public Matrix<double> Filter { get; set; }
        public int WindowRadius { get; set; }
        public double Deviation { get; set; }

        public override string Name
        {
            get
            {
                return "LoG Filter";
            }
        }

        public override void InitParameters()
        {
            Parameters = new List<AlgorithmParameter>();
            AlgorithmParameter winRadius = new IntParameter(
                "Filter Window Size", "FWS", 4, 1, 101);

            Parameters.Add(winRadius);

            AlgorithmParameter deviation = new DoubleParameter(
               "Filter Deviation", "FD", 1.6f, 0.01f, 50.0f);

            Parameters.Add(deviation);
        }

        public override void UpdateParameters()
        {
            WindowRadius = (int)AlgorithmParameter.FindValue("FES", Parameters);
            Deviation = (float)AlgorithmParameter.FindValue("FD", Parameters);

            Filter = ImageFilter.GetFilter_LoGNorm(2 * WindowRadius + 1, Deviation);
        }

        public override Matrix<double> ApplyFilter()
        {
            Matrix<double> filtered = Image.Clone();
            ImageFilter.ApplyFilter(filtered, Filter);
            return filtered;
        }

        public override Matrix<double> ApplyFilterShrink()
        {
            Matrix<double> filtered = Image.Clone();
            ImageFilter.ApplyFilterShrink(filtered, Filter);
            return filtered.SubMatrix(WindowRadius, Image.RowCount - 2 * WindowRadius,
                WindowRadius, Image.ColumnCount - 2 * WindowRadius);
        }
    }
}
