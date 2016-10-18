using CamCore;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamImageProcessing
{
    public abstract class ImageRectification
    {
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
        public Matrix<double> FundamentalMatrix { get; set; }
        public Matrix<double> EpiCrossLeft { get; set; }
        public Matrix<double> EpiCrossRight { get; set; }
        public Vector<double> EpipoleLeft { get; set; }
        public Vector<double> EpipoleRight { get; set; }

        public bool IsEpiLeftInInfinity { get; set; }
        public bool IsEpiRightInInfinity { get; set; }

        public Matrix<double> RectificationLeft { get; set; }
        public Matrix<double> RectificationRight { get; set; }

        public Matrix<double> RectificationLeft_Inverse { get; set; }
        public Matrix<double> RectificationRight_Inverse { get; set; }

        public List<Vector2Pair> MatchedPairs { get; set; } // Needed for uncalibrated methods

        public abstract void ComputeRectificationMatrices();
    }
}
