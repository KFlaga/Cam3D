using System.Collections.Generic;

namespace CamImageProcessing
{
    public abstract class FeaturesDetector
    {
        public List<ProcessorParameter> Parameters { get; protected set; }
        public GrayScaleImage FeatureMap { get; protected set; }
        public GrayScaleImage Image { get; set; }

        public abstract bool Detect();
        protected abstract void InitParameters();

        public FeaturesDetector()
        {

        }

        public virtual string Name { get { return "Abstract Features Detector"; } }
        public override string ToString()
        {
            return Name;
        }
    }
}
