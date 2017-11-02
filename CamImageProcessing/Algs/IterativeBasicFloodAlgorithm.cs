using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CamCore;

namespace CamAlgorithms
{
    using Point2D = Point2D<int>;

    public class IterativeBasicFloodAlgorithm : IFloodAlgorithm
    {
        Stack<Point2D> _pointStack;

        public override void FloodFill(int y, int x)
        {
            if(RangeCheck(y, x) == false)
                return;

            if(FillCondition(y, x) == false)
                return;

            FillAction(y, x);

            _pointStack = new Stack<Point2D<int>>();
            _pointStack.Push(new Point2D(x, y));
            while(_pointStack.Count > 0)
            {
                Point2D point = _pointStack.Pop();

                if(point.Y > 0 && FillCondition(point.Y - 1, point.X))
                {
                    FillAction(point.Y - 1, point.X);
                    _pointStack.Push(new Point2D(y : point.Y - 1, x : point.X));
                }
                if(point.Y + 1 < ImageHeight && FillCondition(point.Y + 1, point.X))
                {
                    FillAction(point.Y + 1, point.X);
                    _pointStack.Push(new Point2D(y: point.Y + 1, x: point.X));
                }
                if(point.X > 0 && FillCondition(point.Y, point.X - 1))
                {
                    FillAction(point.Y, point.X - 1);
                    _pointStack.Push(new Point2D(y: point.Y, x: point.X - 1));
                }
                if(point.X + 1 < ImageWidth && FillCondition(point.Y, point.X + 1))
                {
                    FillAction(point.Y, point.X + 1);
                    _pointStack.Push(new Point2D(y: point.Y, x: point.X + 1));
                }
            }
        }

        public override bool FloodSearch(int y, int x, ref int foundX, ref int foundY)
        {
            if(RangeCheck(y, x) == false)
                return false;

            if(FillCondition(y, x) == false)
                return false;

            if(SearchCondition(y, x) == true)
            {
                foundX = x;
                foundY = y;
                return true;
            }

            _pointStack.Push(new Point2D(x, y));
            while(_pointStack.Count > 0)
            {
                Point2D point = _pointStack.Pop();

                if(point.Y > 0 && FillCondition(point.Y - 1, point.X))
                {
                    if(SearchCondition(point.Y - 1, point.X) == true)
                    {
                        foundX = point.X;
                        foundY = point.Y - 1;
                        return true;
                    }
                    _pointStack.Push(new Point2D(point.Y - 1, point.X));
                }
                if(point.Y + 1 < ImageHeight && FillCondition(point.Y + 1, point.X))
                {
                    if(SearchCondition(point.Y + 1, point.X) == true)
                    {
                        foundX = point.X;
                        foundY = point.Y + 1;
                        return true;
                    }
                    _pointStack.Push(new Point2D(point.Y + 1, point.X));
                }
                if(point.X > 0 && FillCondition(point.Y, point.X - 1))
                {
                    if(SearchCondition(point.Y, point.X - 1) == true)
                    {
                        foundX = point.X - 1;
                        foundY = point.Y;
                        return true;
                    }
                    _pointStack.Push(new Point2D(point.Y, point.X - 1));
                }
                if(point.X + 1 < ImageWidth && FillCondition(point.Y, point.X + 1))
                {
                    if(SearchCondition(point.Y, point.X + 1) == true)
                    {
                        foundX = point.X + 1;
                        foundY = point.Y;
                        return true;
                    }
                    _pointStack.Push(new Point2D(point.Y, point.X + 1));
                }
            }
            return false;
        }
    }
}
