namespace CamAlgorithms
{
    public abstract class IFloodAlgorithm
    {
        //public Matrix<double> Image { get; set; }
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }

        public delegate bool FillConditionDelegate(int y, int x);
        public delegate void FillActionDelegate(int y, int x);

        public FillConditionDelegate FillCondition { get; set; }
        public FillConditionDelegate SearchCondition { get; set; }
        public FillActionDelegate FillAction { get; set; }

        public abstract void FloodFill(int y, int x);

        public abstract bool FloodSearch(int y, int x, ref int foundX, ref int foundY);

        protected bool RangeCheck(int y, int x)
        {
            return !(y < 0 || y >= ImageHeight || x < 0 || x >= ImageWidth);
        }
    }

}
