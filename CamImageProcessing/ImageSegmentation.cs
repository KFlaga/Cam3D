using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;
using CamCore;
using Point2D = CamCore.TPoint2D<int>;

namespace CamImageProcessing
{
    public abstract class ImageSegmentation : IParameterizable
    {
        public struct Segment_Gray
        {
            public double Value;
            public int SegmentIndex;
            public List<Point2D> Pixels;
        }

        public struct Segment_Color
        {
            public double Red;
            public double Green;
            public double Blue;
            public int SegmentIndex;
            public List<Point2D> Pixels;
        }

        public List<Segment_Gray> Segments_Gray { get; set; }
        public List<Segment_Color> Segments_Color { get; set; }
        public int[,] SegmentAssignments { get; set; }

        public abstract void SegmentGray(Matrix<double> imageMatrix);
        public abstract void SegmentColor(ColorImage image);


        List<AlgorithmParameter> _params;
        public List<AlgorithmParameter> Parameters
        {
            get
            {
                return _params;
            }
        }

        public virtual void InitParameters()
        {
            _params = new List<AlgorithmParameter>();
        }

        public virtual void UpdateParameters()
        {

        }
    }
}
