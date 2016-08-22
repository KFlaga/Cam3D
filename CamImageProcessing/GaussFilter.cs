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
    public class GaussFilter : ImageFilter
    {
        public Matrix<double> Filter { get; set; }
        public int WindowRadius { get; set; }
        public double Deviation { get; set; }

        public override void InitParameters()
        {
            Parameters = new List<ProcessorParameter>();
            ProcessorParameter winRadius = new ProcessorParameter(
                "Filter Window Size", "FWS",
                "System.Int32", 4, 1, 101);

            Parameters.Add(winRadius);

            ProcessorParameter deviation = new ProcessorParameter(
               "Filter Deviation", "FD",
               "System.Single", 1.6f, 0.01f, 50.0f);

            Parameters.Add(deviation);
        }

        public override void UpdateParameters()
        {
            WindowRadius = (int)ProcessorParameter.FindValue("FES", Parameters);
            Deviation = (double)ProcessorParameter.FindValue("FD", Parameters);

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
