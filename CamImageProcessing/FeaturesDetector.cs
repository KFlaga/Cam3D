﻿using CamCore;
using System.Collections.Generic;

namespace CamImageProcessing
{
    public abstract class FeaturesDetector : IParametrizedProcessor
    {
        private List<ProcessorParameter> _parameters;
        public List<ProcessorParameter> Parameters
        {
            get { return _parameters; }
            protected set { _parameters = value; }
        }

        public abstract void InitParameters();
        public abstract void UpdateParameters();

        public GrayScaleImage FeatureMap { get; protected set; }
        public GrayScaleImage Image { get; set; }

        public abstract bool Detect();
        
        public virtual string Name { get { return "Abstract Features Detector"; } }
        public override string ToString()
        {
            return Name;
        }
    }
}
