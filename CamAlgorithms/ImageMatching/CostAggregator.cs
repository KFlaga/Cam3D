using CamCore;
using MathNet.Numerics.LinearAlgebra;
using System.Collections.Generic;

namespace CamAlgorithms.ImageMatching
{
    public abstract class CostAggregator : IParameterizable
    {
        public bool IsLeftImageBase { get; set; }
        public IImage ImageBase { get; set; }
        public IImage ImageMatched { get; set; }
        public DisparityMap DisparityMap { get; set; } // For each pixel p in base image stores corresponding pixels
        public Matrix<double> Fundamental { get; set; }

        public MatchingCostComputer CostComp { get; set; }
        public DisparityComputer DispComp { get; set; }

        public IntVector2 CurrentPixel { get; set; } = new IntVector2();

        public virtual void Init()
        {
            CostComp.ImageBase = ImageBase;
            CostComp.ImageMatched = ImageMatched;         
            CostComp.Init();

                DispComp.CostComp = CostComp;
                DispComp.ImageBase = ImageBase;
                DispComp.ImageMatched = ImageMatched;
                DispComp.IsLeftImageBase = IsLeftImageBase;
                DispComp.DisparityMap = DisparityMap;
                DispComp.Init();
        }

        public abstract void ComputeMatchingCosts();
        public abstract void ComputeMatchingCosts_Rectified();
        
        List<AlgorithmParameter> _params = new List<AlgorithmParameter>();
        public List<AlgorithmParameter> Parameters
        {
            get { return _params; }
        }
        
        public virtual void InitParameters()
        {
            // Add all available cost computers
            ParametrizedObjectParameter costParam =
                new ParametrizedObjectParameter("Matching Cost Computer", "COST");

            costParam.Parameterizables = new List<IParameterizable>();
            var cens = new CensusCostComputer();
            cens.InitParameters();
            costParam.Parameterizables.Add(cens);
            var rank = new RankCostComputer();
            rank.InitParameters();
            costParam.Parameterizables.Add(rank);

            costParam.DefaultValue = cens;

            _params.Add(costParam);
        }

        public virtual void UpdateParameters()
        {
            CostComp = AlgorithmParameter.FindValue<MatchingCostComputer>("COST", _params);
            CostComp.UpdateParameters();
        }

        public abstract string Name { get; }
        public override string ToString()
        {
            return Name;
        }
    }
}
