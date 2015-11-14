using CamCore;
using MathNet.Numerics.LinearAlgebra.Single;
using System.Collections.Generic;

namespace CamImageProcessing
{
    public abstract class ImagesMatcher
    {
        public List<ProcessorParameter> Parameters { get; protected set; }
        public GrayScaleImage LeftImage { get; set; }
        public GrayScaleImage RightImage { get; set; }

        public List<Camera3DPoint> MatchedPoints { get; protected set; }

        public abstract bool Match();
        protected abstract void InitParameters();

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
