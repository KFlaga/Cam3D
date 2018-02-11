using CamCore;

namespace CamAlgorithms.ImageMatching
{
    // Performs matching cost computation on each color channel separately
    // and sums cost or averages ( or performs other action with them )
    // For each channel other cost may be used (or same)
    public class SeperateChannelCostComputer : MatchingCostComputer
    {
        public MatchingCostComputer CostComputer_Red { get; set; } // Actual cost computer used for RED channels
        public MatchingCostComputer CostComputer_Green { get; set; } // Actual cost computer used for GREEN channels
        public MatchingCostComputer CostComputer_Blue { get; set; } // Actual cost computer used for BLUE channels

        public ColorImage ColorImageBase { get; set; }
        public ColorImage ColorImageMatched { get; set; }

        public delegate double CostAggregationFunction(double costRed, double costGreen, double costBlue);
        public enum CostAggregationMethod
        {
            Sum,
            Average,
            Other
        }

        CostAggregationMethod _aggMethod;
        public CostAggregationMethod UsedCostAggregationMethod
        {
            get { return _aggMethod; }
            set
            {
                _aggMethod = value;
                switch(_aggMethod)
                {
                    case CostAggregationMethod.Average:
                        _aggFunction = AverageCosts;
                        break;
                    case CostAggregationMethod.Sum:
                        _aggFunction = SumCosts;
                        break;
                    case CostAggregationMethod.Other:
                        _aggFunction = CustomAggregationFunction;
                        break;
                }
            }
        }
        CostAggregationFunction _aggFunction;

        public CostAggregationFunction CustomAggregationFunction { get; set; }

        public SeperateChannelCostComputer()
        {
            UsedCostAggregationMethod = CostAggregationMethod.Average;
        }

        public override double GetCost(IntVector2 pixelBase, IntVector2 pixelMatched)
        {
            return _aggFunction(
                CostComputer_Red.GetCost(pixelBase, pixelMatched),
                CostComputer_Green.GetCost(pixelBase, pixelMatched),
                CostComputer_Blue.GetCost(pixelBase, pixelMatched));
        }

        public override double GetCost(Vector2 pixelBase, Vector2 pixelMatched)
        {
            return _aggFunction(
                CostComputer_Red.GetCost(pixelBase, pixelMatched),
                CostComputer_Green.GetCost(pixelBase, pixelMatched),
                CostComputer_Blue.GetCost(pixelBase, pixelMatched));
        }

        public override double GetCost_Border(IntVector2 pixelBase, IntVector2 pixelMatched)
        {
            return _aggFunction(
                CostComputer_Red.GetCost_Border(pixelBase, pixelMatched),
                CostComputer_Green.GetCost_Border(pixelBase, pixelMatched),
                CostComputer_Blue.GetCost_Border(pixelBase, pixelMatched));
        }

        public override double GetCost_Border(Vector2 pixelBase, Vector2 pixelMatched)
        {
            return _aggFunction(
                CostComputer_Red.GetCost_Border(pixelBase, pixelMatched),
                CostComputer_Green.GetCost_Border(pixelBase, pixelMatched),
                CostComputer_Blue.GetCost_Border(pixelBase, pixelMatched));
        }

        private double SumCosts(double costRed, double costGreen, double costBlue)
        {
            return costRed + costGreen + costBlue;
        }

        private double AverageCosts(double costRed, double costGreen, double costBlue)
        {
            return (costRed + costGreen + costBlue) / 3.0;
        }

        public override void Init()
        {
            CostComputer_Red.ImageBase = new GrayScaleImage() { ImageMatrix = ColorImageBase[RGBChannel.Red] };
            CostComputer_Red.ImageMatched = new GrayScaleImage() { ImageMatrix = ColorImageMatched[RGBChannel.Red] };
            CostComputer_Green.ImageBase = new GrayScaleImage() { ImageMatrix = ColorImageBase[RGBChannel.Green] };
            CostComputer_Green.ImageMatched = new GrayScaleImage() { ImageMatrix = ColorImageMatched[RGBChannel.Green] };
            CostComputer_Blue.ImageBase = new GrayScaleImage() { ImageMatrix = ColorImageBase[RGBChannel.Blue] };
            CostComputer_Blue.ImageMatched = new GrayScaleImage() { ImageMatrix = ColorImageMatched[RGBChannel.Blue] };

            CostComputer_Red.Init();
            CostComputer_Green.Init();
            CostComputer_Blue.Init();
        }

        public override void InitParameters()
        {
            
        }

        public override void UpdateParameters()
        {
            
        }

        public override string Name
        {
            get
            {
                return "Separate Channels Cost Computer";
            }
        }

        public override void Update()
        {
            CostComputer_Red.Update();
            CostComputer_Green.Update();
            CostComputer_Blue.Update();
        }
    }
}
