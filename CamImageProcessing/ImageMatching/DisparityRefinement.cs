using CamCore;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamImageProcessing.ImageMatching
{
    public abstract class DisparityRefinement : IParameterizable
    {
        public DisparityMap MapLeft { get; set; } // Also used if only one, fused map is used 
        public DisparityMap MapRight { get; set; }

        public Matrix<double> ImageLeft { get; set; } // Also used if only one image is used 
        public Matrix<double> ImageRight { get; set; }

        public virtual void Init() { }
        public abstract void RefineMaps();

        List<AlgorithmParameter> _params = new List<AlgorithmParameter>();
        public List<AlgorithmParameter> Parameters
        {
            get { return _params; }
        }

        public virtual void InitParameters()
        {

        }

        public virtual void UpdateParameters()
        {

        }

        public override string ToString()
        {
            return "Disparity Refinement - Base";
        }
    }
}
