using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamImageProcessing.ImageMatching
{
    public enum ConfidenceMethod
    {
        TwoAgainstMax,
        TwoAgainstTwo,
        TwoAgainstAverage,
        BestNeighbourhoodsComparsion
    }

    public class MatchConfidenceComputer
    {
        public delegate double ConfidenceFunction(List<Disparity> disparitiesForPixel, int bestIdx, int secondIdx);
        ConfidenceFunction _confFunc;

        ConfidenceMethod _method;
        public ConfidenceMethod UsedConfidenceMethod
        {
            get { return _method; }
            set
            {
                _method = value;
                switch(_method)
                {
                    case ConfidenceMethod.TwoAgainstAverage:
                        _confFunc = ComputeConfidence_TwoAverage;
                        break;
                    case ConfidenceMethod.TwoAgainstMax:
                        _confFunc = ComputeConfidence_TwoMax;
                        break;
                    case ConfidenceMethod.TwoAgainstTwo:
                        _confFunc = ComputeConfidence_TwoTwo;
                        break;
                }
            }
        }

        public MatchingCostComputer CostComp { get; set; }

        public MatchConfidenceComputer()
        {
            _method = ConfidenceMethod.TwoAgainstMax;
        }

        // If supplied disparity map is sorted in cost-ascending order (so d[0] is best match) isSorted = true
        // After function returns, supplied list is sorted in cost-ascending order, unless neighboorhood-based
        // method is choosen
        public double ComputeConfidence(List<Disparity> disparitiesForPixel, int bestIdx, int secondIdx)
        {
            if(disparitiesForPixel.Count < 2)
            {
                return 1.0;
            }

            return _confFunc(disparitiesForPixel, bestIdx, secondIdx);
        }
        
        public double ComputeConfidence_TwoMax(List<Disparity> ds, int bestIdx, int secondIdx)
        {
            return (ds[secondIdx].Cost - ds[bestIdx].Cost) / CostComp.MaxCost;
        }

        public double ComputeConfidence_TwoTwo(List<Disparity> ds, int bestIdx, int secondIdx)
        {
            return ds[secondIdx].Cost - ds[bestIdx].Cost < 1e-6 ? 0.0 : 
                (ds[secondIdx].Cost - ds[bestIdx].Cost)/ (ds[secondIdx].Cost + ds[bestIdx].Cost);
        }

        public double ComputeConfidence_TwoAverage(List<Disparity> ds, int bestIdx, int secondIdx)
        {
            if(ds[secondIdx].Cost - ds[bestIdx].Cost < 1e-6)
                return 0.0;

            double cost = 0.0;
            foreach(var d in ds)
            {
                cost += d.Cost;
            }

            return (ds[secondIdx].Cost - ds[bestIdx].Cost) / cost;
        }
    }
}
