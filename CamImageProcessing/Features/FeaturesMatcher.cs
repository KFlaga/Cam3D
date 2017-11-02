using System.Collections.Generic;
using CamCore;
using System.Diagnostics;

namespace CamAlgorithms
{
    [DebuggerDisplay("L:{LeftPoint}, R:{RightPoint}, c:{CostString}, t:{ConfidenceString}")]
    public class MatchedPair
    {
        public Vector2 LeftPoint;
        public Vector2 RightPoint;
        public double Cost;
        public double Confidence;

        public string CostString { get { return Cost.ToString("E3"); } }
        public string ConfidenceString { get { return Confidence.ToString("F4"); } }
    }

    public abstract class FeaturesMatcher : IParameterizable
    {
        private List<AlgorithmParameter> _parameters;
        public List<AlgorithmParameter> Parameters
        {
            get { return _parameters; }
            protected set { _parameters = value; }
        }

        public virtual void InitParameters()
        {
            _parameters = new List<AlgorithmParameter>();
        }

        public virtual void UpdateParameters()
        {

        }

        public IImage LeftImage { get; set; }
        public IImage RightImage { get; set; }
        public List<IntVector2> LeftFeaturePoints { get; set; }
        public List<IntVector2> RightFeaturePoints { get; set; }
        
        public List<MatchedPair> Matches { get; protected set; }

        public abstract void Match();
        
        public abstract string Name { get; }
        public override string ToString()
        {
            return Name;
        }
    }
}
