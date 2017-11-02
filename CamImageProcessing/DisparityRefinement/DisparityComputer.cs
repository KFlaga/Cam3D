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

        public MatchConfidenceComputer ConfidenceComp { get; } = new MatchConfidenceComputer();
        public MatchingCostComputer CostComp { get; set; }
        
        public virtual void Init()
        {
            ConfidenceComp.CostComp = CostComp;
        }

        public abstract void StoreDisparity(IntVector2 pixelBase, IntVector2 pixelMatched, double cost);
        public abstract void StoreDisparity(Disparity disp);
        public abstract void FinalizeForPixel(IntVector2 pixelBase);
        public abstract void FinalizeMap();

        protected List<AlgorithmParameter> _params;
        public List<AlgorithmParameter> Parameters
        {
            get { return _params; }
        }

        public virtual void InitParameters()
        {
            _params = new List<AlgorithmParameter>();
            // Add all available confidence computing methods
            DictionaryParameter confParam =
                new DictionaryParameter("Confidence Computing Method", "CONF");

            confParam.ValuesMap = new Dictionary<string, object>()
            {
                { "Two Against Two", ConfidenceMethod.TwoAgainstTwo },
                { "Two Against Max", ConfidenceMethod.TwoAgainstMax },
                { "Two Against Average", ConfidenceMethod.TwoAgainstAverage }
            };

            _params.Add(confParam);
        }

        public virtual void UpdateParameters()
        {
            ConfidenceComp.UsedConfidenceMethod = AlgorithmParameter.FindValue<ConfidenceMethod>("CONF", _params);
        }

        public abstract string Name { get; }
        public override string ToString()
        {
            return Name;
        }
    }
}
