﻿using CamCore;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamImageProcessing.ImageMatching
{
    public abstract class DisparityComputer : IParameterizable
    {
        public bool IsLeftImageBase { get; set; }

        public Matrix<double> ImageBase { get; set; }
        public Matrix<double> ImageMatched { get; set; }
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

        List<AlgorithmParameter> _params = new List<AlgorithmParameter>();
        public List<AlgorithmParameter> Parameters
        {
            get { return _params; }
        }

        public virtual void InitParameters()
        {
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

        public override string ToString()
        {
            return "Disparity Computer - Base";
        }
    }
}
