using CamCore;
using System.Collections.Generic;

namespace CamImageProcessing
{
    public abstract class FeaturesDetector : IParameterizable
    {
        protected List<AlgorithmParameter> _parameters;
        public List<AlgorithmParameter> Parameters
        {
            get { return _parameters; }
        }

        public virtual void InitParameters()
        {
            _parameters = new List<AlgorithmParameter>();
        }

        public virtual void UpdateParameters() { }

        public GrayScaleImage FeatureMap { get; protected set; }
        public List<IntVector2> FeaturePoints { get; protected set; }
        public IImage Image { get; set; }
        public IntVector2 CurrentPixel { get; protected set; } = new IntVector2();
        public bool Terminate { get; set; }

        public abstract void Detect();

        public abstract string Name { get; }
        public override string ToString()
        {
            return Name;
        }
    }
}
