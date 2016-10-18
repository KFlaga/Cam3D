using CamCore;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Collections.Generic;

namespace CamImageProcessing
{
    public abstract class ImagesMatcher : IParameterizable
    {
        private List<AlgorithmParameter> _parameters;
        public List<AlgorithmParameter> Parameters
        {
            get { return _parameters; }
            protected set { _parameters = value; }
        }

        public abstract void InitParameters();
        public abstract void UpdateParameters();

        public GrayScaleImage LeftImage { get; set; }
        public GrayScaleImage RightImage { get; set; }

        public List<Camera3DPoint> MatchedPoints { get; protected set; }

        public abstract bool Match();

        public ImagesMatcher()
        {
            MatchedPoints = new List<Camera3DPoint>();
        }

        public virtual string Name { get { return "Abstract Image Matcher"; } }
        public override string ToString()
        {
            return Name;
        }
    }
}
