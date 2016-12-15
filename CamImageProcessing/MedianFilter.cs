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
    public class MedianFilter : ImageFilter
    {
        public int WindowRadius { get; set; } // If radius = 1, then 3x3 window is used
        public bool FilterBorder { get; set; }

        public override Matrix<double> ApplyFilter()
        {
            int d = WindowRadius;
            Matrix<double> filtered = new DenseMatrix(Image.RowCount, Image.ColumnCount);

            // TODO on borders apply filter using only part of a window that fits image if FilterBorder = true
            if(FilterBorder)
            {

            }
            else
            {
                for(int c = 0; c < Image.ColumnCount; ++c)
                {
                    for(int r = 0; r < d; ++r)
                    {
                        filtered[r, c] = Image[r, c];
                        filtered[Image.RowCount - r - 1, c] = Image[Image.RowCount - r - 1, c];
                    }
                }

                for(int c = 0; c < d; ++c)
                {
                    for(int r = 0; r < Image.RowCount; ++r)
                    {

                        filtered[r, c] = Image[r, c];
                        filtered[r, Image.ColumnCount - c - 1] = Image[r, Image.ColumnCount - c - 1];
                    }
                }
            }

            double[] window = new double[(2 * d + 1) * (2 * d + 1)];
            int middle = 2 * d * (d + 1);
            for(int c = d; c < Image.ColumnCount - d; ++c)
            {
                for(int r = d; r < Image.RowCount - d; ++r)
                {
                    int n = 0;
                    for(int dx = -d; dx <= d; ++dx)
                    {
                        for(int dy = -d; dy <= d; ++dy)
                        {
                            window[n] = Image[r + dy, c + dx];
                            ++n;
                        }
                    }
                    Array.Sort(window);
                    // Set value of image to be median of window
                    filtered[r, c] = window[middle];
                }
            }

            return filtered;
        }

        public override Matrix<double> ApplyFilterShrink()
        {
            int d = WindowRadius;
            Matrix<double> filtered = new DenseMatrix(Image.RowCount - 2 * d, Image.ColumnCount - 2 * d);

            double[] window = new double[(2 * d + 1) * (2 * d + 1)];
            for(int c = d; c < Image.ColumnCount - d; ++c)
            {
                for(int r = d; r < Image.RowCount - d; ++r)
                {
                    for(int dx = 0; dx < d; ++dx)
                    {
                        for(int dy = 0; dy < d; ++dy)
                        {
                            window[dy + dx * d] = Image[r - dy, c - dx];
                        }
                    }
                    Array.Sort(window);
                    // Set value of image to be median of window
                    filtered[r - d, c - d] = window[2 * d * (d + 1)];
                }
            }

            return filtered;
        }

        public override string Name
        {
            get
            {
                return "Median Filter";
            }
        }

        public override void InitParameters()
        {
            Parameters = new List<AlgorithmParameter>();
            AlgorithmParameter winRadius = new IntParameter(
                "Filter Window Size", "FWS", 3, 1, 99);

            Parameters.Add(winRadius);

            AlgorithmParameter filterBorder = new BooleanParameter(
               "Filter Border", "FB", true);

            Parameters.Add(filterBorder);
        }

        public override void UpdateParameters()
        {
            WindowRadius = (int)AlgorithmParameter.FindValue("FWS", Parameters);
            FilterBorder = (bool)AlgorithmParameter.FindValue("FB", Parameters);
        }
    }
}
