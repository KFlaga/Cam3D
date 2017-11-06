namespace CamAlgorithms
{
    public class BasicFloodAlgorithm : IFloodAlgorithm
    {
        public override void FloodFill(int y, int x)
        {
            if(y < 0 || y >= ImageHeight || x < 0 || x >= ImageWidth)
                return;

            if(FillCondition(y, x) == true)
            {
                FillAction(y, x);
                FloodFill(y - 1, x);
                FloodFill(y + 1, x);
                FloodFill(y, x - 1);
                FloodFill(y, x + 1);
            }
        }

        public override bool FloodSearch(int y, int x, ref int foundX, ref int foundY)
        {
            if(y < 0 || y >= ImageHeight || x < 0 || x >= ImageWidth)
                return false;

            if(FillCondition(y, x) == true)
            {
                if(SearchCondition(y, x) == true)
                {
                    foundX = x;
                    foundY = y;
                    return true;
                }

                if(FloodSearch(y - 1, x, ref foundX, ref foundY) == true)
                    return true;
                if(FloodSearch(y + 1, x, ref foundX, ref foundY) == true)
                    return true;
                if(FloodSearch(y, x - 1, ref foundX, ref foundY) == true)
                    return true;
                if(FloodSearch(y, x + 1, ref foundX, ref foundY) == true)
                    return true;
            }
            return false;
        }
    }

}
