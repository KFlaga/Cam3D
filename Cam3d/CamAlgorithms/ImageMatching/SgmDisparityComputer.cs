﻿using System;
using System.Collections.Generic;
using CamCore;
using System.Collections;

namespace CamAlgorithms.ImageMatching
{
    // Just set disparity with lowest cost
    public class SgmDisparityComputer : DisparityComputer
    {
        Disparity[] _dispForPixel;
        int _idx;
        int PathLengthThreshold { get; set; }

        public enum MeanMethods
        {
            SimpleAverage,
            WeightedAverageWithPathLength,
        }

        delegate double MeanComputer(int start, int count);
        MeanComputer _meanComputer;

        public enum CostMethods
        {
            DistanceToMean,
            DistanceSquredToMean
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
                        _costComputer = FindCost_Squared;
                        break;
                    case CostMethods.DistanceToMean:
                    default:
                        _costComputer = FindCost_Simple;
                        break;
                }
            }
        }
        public double CostMethodPower { get; set; } = 2.0;

        public SgmDisparityComputer()
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
                DX = mean.Round(),
                SubDX = mean,
                Cost = CostComp.GetCost_Border(pixelBase, new IntVector2(pixelBase.X + mean.Round(), pixelBase.Y)),
                Confidence = ((double)count / (double)_idx) * (1.0 / (cost + 1.0)),
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
                w = Math.Min(1.0, pathLength * PathLengthThreshold) /
                    ((double)_dispForPixel[start + i].Cost + 1.0);
                wsum += w;
                mean += w * _dispForPixel[start + i].DX;
            }
            mean /= wsum;
            return mean;
        }

        double FindCost_Simple(double mean, int start, int count)
        {
            double cost = 0.0;

            // 1) C = sum(||m - d||) / n^2 
            for(int i = 0; i < count; ++i)
            {
                cost += Math.Abs(mean - (double)_dispForPixel[start + i].DX);
            }
            cost /= Math.Pow(count, CostMethodPower * 0.5);
            return cost;
        }

        double FindCost_Squared(double mean, int start, int count)
        {
            double cost = 0.0;

            // 2) C = sqrt(sum(||m - d||^2)) / n^2 -> same results as sum(||m - d||^2)/n^4
            double d;
            for(int i = 0; i < count; ++i)
            {
                d = mean - (double)_dispForPixel[start + i].DX;
                cost += d * d;
            }
            cost /= Math.Pow(count, CostMethodPower);

            return cost;
        }

        public override void FinalizeMap()
        {

        }


        public override void InitParameters()
        {
            base.InitParameters();

            DictionaryParameter meanParam =
                new DictionaryParameter("Mean Computing Method", "MeanMethod");

            meanParam.ValuesMap = new Dictionary<string, object>()
            {
                { "Simple Average", MeanMethods.SimpleAverage },
                { "Weighted Average", MeanMethods.WeightedAverageWithPathLength }
            };

            Parameters.Add(meanParam);

            DictionaryParameter costParam =
                new DictionaryParameter("Cost Computing Method", "CostMethod");

            costParam.ValuesMap = new Dictionary<string, object>()
            {
                { "E(||d - m||) / n^(0.5P)", CostMethods.DistanceToMean },
                { "E(||d - m||^2) / n^P", CostMethods.DistanceSquredToMean }
            };

            Parameters.Add(costParam);

            Parameters.Add(new DoubleParameter(
                "Cost Method Coefficient", "CostMethodPower", 2.0, 0.1, 10.0));
            Parameters.Add(new DoubleParameter(
                "Path Length Threshold", "PathLengthThreshold", 3, 1, 1000));
        }

        public override void UpdateParameters()
        {
            MeanMethod = IAlgorithmParameter.FindValue<MeanMethods>("MeanMethod", Parameters);
            CostMethod = IAlgorithmParameter.FindValue<CostMethods>("CostMethod", Parameters);
            CostMethodPower = IAlgorithmParameter.FindValue<double>("CostMethodPower", Parameters);
            PathLengthThreshold = IAlgorithmParameter.FindValue<int>("PathLengthThreshold", Parameters);
        }


        public override string Name
        {
            get
            {
                return "SGM Disparity Computer";
            }
        }
    }
}
