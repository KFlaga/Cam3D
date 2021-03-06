﻿using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using CamCore;
using System.Diagnostics;

namespace CamAlgorithms
{
    public abstract class ImageSegmentation : IParameterizable
    {
        [DebuggerDisplay("idx = {SegmentIndex}, count = {Pixels.Count}")]
        public class Segment
        {
            public int SegmentIndex { get; set; } = -1;
            public List<IntPoint2> Pixels { get; set; } = new List<IntPoint2>();
        }

        public class Segment_Gray : Segment
        {
            public double Value { get; set; }
        }

        public class Segment_Color : Segment
        {
            public double Red { get; set; }
            public double Green { get; set; }
            public double Blue { get; set; }
        }

        public class Segment_Disparity : Segment
        {
            public double Disparity { get; set; }
        }

        public List<Segment> Segments { get; set; }
        public int[,] SegmentAssignments { get; set; }

        public abstract void SegmentGray(Matrix<double> imageMatrix);
        public abstract void SegmentColor(ColorImage image);
        public abstract void SegmentDisparity(DisparityMap dispMap);

        protected List<IAlgorithmParameter> _params;
        public List<IAlgorithmParameter> Parameters
        {
            get
            {
                return _params;
            }
        }

        public virtual void InitParameters()
        {
            _params = new List<IAlgorithmParameter>();
        }

        public virtual void UpdateParameters()
        {

        }

        public abstract string Name { get; }
        public override string ToString()
        {
            return Name;
        }
    }
}
