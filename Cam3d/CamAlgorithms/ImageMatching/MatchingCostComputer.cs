using System.Collections.Generic;
using CamCore;

namespace CamAlgorithms.ImageMatching
{
    // Base class for computing cost of matching pixels between images
    // If cost is lower, then match is better
    public abstract class MatchingCostComputer : IParameterizable
    {
        public IImage ImageBase { get; set; }
        public IImage ImageMatched { get; set; }

        public double MaxCost { get; protected set; }
        public int BorderWidth { get; set; }
        public int BorderHeight { get; set; }

        // Initialises / pre-computes all needed parameters/look-ups, filters images etc
        // Needed to be called after images are set and before computing any cost
        public abstract void Init();

        // Updates pre-computed parameters if needed after disparity computation iteration 
        public abstract void Update();

        // Returns cost of matching pixels from base / matched image for int pixel coords
        // Smaller cost means better match
        // For best possible fit, cost should equal 0 (and so cost must be positive)
        public abstract double GetCost(IntVector2 pixelBase, IntVector2 pixelMatched);

        // Returns cost of matching pixels from base / matched image for real pixel coords
        // Final method may not support sub-pixel computation, pixels are casted to ints then
        public virtual double GetCost(Vector2 pixelBase, Vector2 pixelMatched)
        {
            return GetCost(new IntVector2(pixelBase), new IntVector2(pixelMatched));
        }

        // Returns cost of matching pixels from base / matched image for int pixel coords
        // Check if matching mask is inside bounds of image and if outside bounds uses mirrored values
        public abstract double GetCost_Border(IntVector2 pixelBase, IntVector2 pixelMatched);

        // Returns cost of matching pixels from base / matched image for real pixel coords
        // Final method may not support sub-pixel computation, pixels are casted to ints then
        // Check if matching mask is inside bounds of image and if outside bounds uses mirrored values
        public virtual double GetCost_Border(Vector2 pixelBase, Vector2 pixelMatched)
        {
            return GetCost_Border(new IntVector2(pixelBase), new IntVector2(pixelMatched));
        }

        protected List<IAlgorithmParameter> _parameters;
        public List<IAlgorithmParameter> Parameters { get { return _parameters; } }

        public virtual void InitParameters()
        {
            _parameters = new List<IAlgorithmParameter>();
        }

        public virtual void UpdateParameters()
        {

        }

        public abstract string Name { get; }
        public override string ToString()
        {
            return Name;
        }
    }
}
