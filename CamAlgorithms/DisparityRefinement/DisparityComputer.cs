using CamCore;
using System.Collections.Generic;

namespace CamAlgorithms.ImageMatching
{
    public abstract class DisparityComputer : IParameterizable
    {
        public bool IsLeftImageBase { get; set; }

        public IImage ImageBase { get; set; }
        public IImage ImageMatched { get; set; }
        public DisparityMap DisparityMap { get; set; }
        
        public MatchingCostComputer CostComp { get; set; }
        
        public virtual void Init()
        {

        }

        public abstract void StoreDisparity(IntVector2 pixelBase, IntVector2 pixelMatched, double cost);
        public abstract void StoreDisparity(Disparity disp);
        public abstract void FinalizeForPixel(IntVector2 pixelBase);
        public abstract void FinalizeMap();
        
        public List<IAlgorithmParameter> Parameters { get; protected set; }

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
