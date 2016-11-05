using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CamCore;
using System.Collections;

namespace CamImageProcessing.ImageMatching
{
    // Just set disparity with lowest cost
    public class SGMDisparityComputer : DisparityComputer
    {
        Disparity[] _dispForPixel;
        int _idx;
        double _pathLengthTreshold;

        public enum MeanMethods
        {
            SimpleAverage,
            WeightedAverage,
            WeightedAverageWithPathLength,
        }

        delegate double MeanComputer(int start, int count);
        MeanComputer _meanComputer;

        public enum CostMethods
        {
            DistanceToMean,
            DistanceSquredToMean,
        }

        delegate double CostComputer(double mean, int start, int count);
        CostComputer _costComputer;

        MeanMethods _meanMethod;
        public MeanMethods MeanMethod
        {
            get { return _meanMethod; }
            set
            {
                _meanMethod = value;
                switch(value)
                {
                    case MeanMethods.WeightedAverageWithPathLength:
                        _meanComputer = FindMean_WeightedPath;
                        break;
                    case MeanMethods.WeightedAverage:
                        _meanComputer = FindMean_Weighted;
                        break;
                    case MeanMethods.SimpleAverage:
                    default:
                        _meanComputer = FindMean_Simple;
                        break;
                }
            }
        }

        CostMethods _costMethod;
        public CostMethods CostMethod
        {
            get { return _costMethod; }
            set
            {
                _costMethod = value;
                switch(value)
                {
                    case CostMethods.DistanceSquredToMean:
                        _costComputer = FindCost;
                        break;
                    case CostMethods.DistanceToMean:
                    default:
                        _costComputer = FindCost;
                        break;
                }
            }
        }

        public SGMDisparityComputer()
        {
            MeanMethod = MeanMethods.SimpleAverage;
            CostMethod = CostMethods.DistanceToMean;
        }

        public override void Init()
        {
            _dispForPixel = new Disparity[16];
            _idx = 0;
        }

        public override void StoreDisparity(IntVector2 pixelBase, IntVector2 pixelMatched, double cost)
        {
            StoreDisparity(new Disparity(pixelBase, pixelMatched, cost, 0.0, (int)DisparityFlags.Valid));
        }

        public override void StoreDisparity(Disparity disp)
        {
            _dispForPixel[_idx] = disp;
            ++_idx;
        }

        public class DisparityComparer : IComparer
        {
            public int Compare(Object x, Object y)
            {
                return Compare((Disparity)x, (Disparity)y);
            }

            public int Compare(Disparity x, Disparity y)
            {
                return x.DX < y.DX ? 1 : (x.DX > y.DX ? -1 : 0);
            }
        }

        public override void FinalizeForPixel(IntVector2 pixelBase)
        {
            if(_idx == 0)
            {
                // There was no disparity for pixel : set as invalid
                DisparityMap.Set(pixelBase.Y, pixelBase.X,
                    new Disparity(pixelBase, pixelBase, double.PositiveInfinity, 0.0, (int)DisparityFlags.Invalid));
                return;
            }

            // 1) Sort by disparity
            Array.Sort(_dispForPixel, 0, _idx, new DisparityComparer());

            int start = 0;
            int count = _idx;
            bool costLower = true;

            // 2) Find weighted mean m of disparities and distances s to m, s = ||m - d|| (or ||m - d||^2)
            // Cost function: C = sum(||m - d||) / n^2 || C = sqrt(sum(||m - d||^2)) / n^2   
            double mean = _meanComputer(start, count);
            double cost = _costComputer(mean, start, count);
            do
            {
                // 3) Remove one disp from ends and check if cost is lower
                double mean1 = _meanComputer(start + 1, count - 1);
                double cost1 = _costComputer(mean1, start + 1, count - 1);
                double mean2 = _meanComputer(start, count - 1);
                double cost2 = _costComputer(mean2, start, count - 1);

                if(cost > cost1 || cost > cost2)
                {
                    if(cost1 < cost2) // Remove first one -> move start by one pos
                    {
                        start += 1;
                        cost = cost1;
                        mean = mean1;
                    }
                    else // Remove last one -> just decrement count
                    {
                        cost = cost2;
                        mean = mean2;
                    }
                    count -= 1;
                    costLower = true;
                }
                else
                    costLower = false;
            }
            while(costLower && count > 3); // 4) Repeat untill cost is minimised

            // 5) Confidence ?

            DisparityMap.Set(pixelBase.Y, pixelBase.X, new Disparity()
            {
                Cost = cost,
                DX = (int)mean,
                DY = 0,
                SubDX = mean,
                SubDY = 0.0,
                Confidence = 1.0,
                Flags = (int)DisparityFlags.Valid
            });

            _idx = 0;
        }

        double FindMean_Simple(int start, int count)
        {
            double mean = 0.0;

            // 1) Standard average
            for(int i = 0; i < count; ++i)
            {
                mean += (double)_dispForPixel[start + i].DX;
            }
            mean /= count;
            return mean;
        }

        double FindMean_Weighted(int start, int count)
        {
            double mean = 0.0;

            // 2) Weighted average by matching cost (stored in Disparity)
            double wsum = 0.0;
            double w;
            for(int i = 0; i < count; ++i)
            {
                w = 1.0 / ((double)_dispForPixel[start + i].Cost + 1.0);
                wsum += w;
                mean += w * _dispForPixel[start + i].DX;
            }
            mean /= wsum;
            return mean;
        }

        double FindMean_WeightedPath(int start, int count)
        {
            double mean = 0.0;

            // 3) Same as 2 but includes path length
            double wsum = 0.0;
            double w;
            double pathLength = 1.0; // TODO: how to get this
            for(int i = 0; i < count; ++i)
            {
                w = Math.Min(1.0, pathLength * _pathLengthTreshold) /
                    ((double)_dispForPixel[start + i].Cost + 1.0);
                wsum += w;
                mean += w * _dispForPixel[start + i].DX;
            }
            mean /= wsum;
            return mean;
        }

        double FindCost(double mean, int start, int count)
        {
            double cost = 0.0;

            // Cost function: 
            // 1) C = sum(||m - d||) / n^2 
            for(int i = 0; i < count; ++i)
            {
                cost += Math.Abs(mean - (double)_dispForPixel[start + i].DX);
            }
            cost /= (count * count);

            // 2) C = sqrt(sum(||m - d||^2)) / n^2
            //double d;
            //for(int i = 0; i < count; ++i)
            //{
            //    d = Math.Abs(mean - (double)_dispForPixel[start + i].DX);
            //    cost += d * d;
            //}
            //cost /= (count * count * count * count);

            return cost;
        }

        public override void FinalizeMap()
        {

        }


        public override void InitParameters()
        {
            _params = new List<AlgorithmParameter>();

            DictionaryParameter meanParam =
                new DictionaryParameter("Mean Computing Method", "MEAN");

            meanParam.ValuesMap = new Dictionary<string, object>()
            {
                { "Simple Average", MeanMethods.SimpleAverage },
                { "Weighted Average", MeanMethods.WeightedAverage }
            };

            _params.Add(meanParam);
        }

        public override void UpdateParameters()
        {
            MeanMethod = AlgorithmParameter.FindValue<MeanMethods>("MEAN", Parameters);
        }

        public override string ToString()
        {
            return "SGM Disparity Computer";
        }
    }
}
