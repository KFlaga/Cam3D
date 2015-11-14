using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamImageProcessing
{
    public abstract class FeaturesMatcher : ImagesMatcher
    {
        public override string Name {  get { return "Abstract Feature-based Matcher"; } }

        public FeaturesDetector FeatureDetector { get; set; }

        public ImageFilter PreMatchFilter { get; set; }

        public int FinalSizeX { get; set; }
        public int FinalSizeY { get; set; }
    }
}
