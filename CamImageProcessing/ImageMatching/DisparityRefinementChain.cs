using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CamCore;
using MathNet.Numerics.LinearAlgebra;

namespace CamImageProcessing.ImageMatching
{
    public class DisparityRefinementChain : IParameterizable
    {
        public List<DisparityRefinement> RefinementChain { get; set; }
        
        public DisparityMap MapLeft { get; set; } // Also used if only one, fused map is used 
        public DisparityMap MapRight { get; set; }

        public Matrix<double> ImageLeft { get; set; } // Also used if only one image is used 
        public Matrix<double> ImageRight { get; set; }

        public void RefineMaps()
        {
            foreach(var refiner in RefinementChain)
            {
                refiner.MapLeft = MapLeft;
                refiner.MapRight = MapRight;
                refiner.ImageLeft = ImageLeft;
                refiner.ImageRight = ImageRight;

                refiner.RefineMaps();

                MapLeft = refiner.MapLeft;
                MapRight = refiner.MapRight;
            }
        }

        List<AlgorithmParameter> _params = new List<AlgorithmParameter>();
        public List<AlgorithmParameter> Parameters
        {
            get { return _params; }
        }

        public void InitParameters()
        {

        }

        public void UpdateParameters()
        {

        }

        public override string ToString()
        {
            return "Disparity Refinement Chain";
        }
    }
}
