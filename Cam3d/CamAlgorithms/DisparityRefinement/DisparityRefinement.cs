using CamCore;
using System.Collections.Generic;

namespace CamAlgorithms.ImageMatching
{
    public abstract class DisparityRefinement : IParameterizable
    {
        public DisparityMap MapLeft { get; set; } // Also used if only one, fused map is used 
        public DisparityMap MapRight { get; set; }

        public IImage ImageLeft { get; set; } // Also used if only one image is used 
        public IImage ImageRight { get; set; }

        public virtual void Init() { }
        public abstract void RefineMaps();

        public List<IAlgorithmParameter> Parameters
        {
            get; protected set;
        }

        public virtual void InitParameters()
        {
            Parameters = new List<IAlgorithmParameter>();
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
