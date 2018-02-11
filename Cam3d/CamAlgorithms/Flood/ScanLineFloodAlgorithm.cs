using System;
using System.Collections.Generic;

namespace CamAlgorithms
{
    public class ScanLineFloodAlgorithm : IFloodAlgorithm
    {
        struct Segment
        {
            public Segment(int startX, int endX, int y, Direction dir, bool scanLeft, bool scanRight)
            {
                StartX = startX;
                EndX = endX;
                Y = y;
                Dir = dir;
                ScanLeft = scanLeft;
                ScanRight = scanRight;
            }
            public int StartX, EndX, Y;
            public Direction Dir;
            public bool ScanLeft, ScanRight;
        }

        enum Direction : sbyte
        {
            Up = -1,
            Down = 1,
            NoDir = 0
        }

        Stack<Segment> _stack;

        public override void FloodFill(int y, int x)
        {
            // Check first point and push it to stack
            if(!(RangeCheck(y, x) && FillCondition(y, x)))
                return;

            FillAction(y, x);

            int h = ImageHeight, w = ImageWidth;
            _stack = new Stack<Segment>();
            _stack.Push(new Segment(x, x + 1, y, 0, true, true));

            while(_stack.Count > 0) // Untill there's some points left to fill
            {
                Segment seg = _stack.Pop(); 
                int startX = seg.StartX, endX = seg.EndX; // Start/end of extended segment
                if(seg.ScanLeft) // Segment should be extended towards left
                {
                    // Check points on left side until reached segment edge/filled point
                    while(startX > 0 && FillCondition(seg.Y, startX - 1))
                        FillAction(seg.Y, --startX); // Fill cell and move to next left ( fill after move, as start is in segment )
                }
                if(seg.ScanRight) // Segment should be extended towards right
                {
                    // Check points on right side until reached segment edge/filled point
                    while(endX < w && FillCondition(seg.Y, endX))
                        FillAction(seg.Y, endX++); // Fill cell and move to next right ( fill before move, as end is not in segment )
                }
                // At this point, the [startX, endX) is filled.

                if(seg.Y > 0)
                {
                    // Check line above segment for new segments
                    if(seg.Dir != Direction.Down)
                        AddLineSameDirection(startX, endX, seg.Y - 1, Direction.Up); // Current segment was created from below one, so direction's same
                    else
                    {
                        // Check line above segment for new segments
                        // Ignore bounds (non-extended bounds of region) can be extended by one
                        // (they can be ignored when if checking opposite direction line is filled, as it surely is
                        //  + extened by one are surely 'bad' cells )
                        AddLineOppositeDirection(startX, endX, seg.Y - 1, seg.StartX - 1, seg.EndX + 1, Direction.Up);
                    }
                }

                if(seg.Y < h - 1)
                {
                    if(seg.Dir != Direction.Up)
                        AddLineSameDirection(startX, endX, seg.Y + 1, Direction.Down);
                    else
                        AddLineOppositeDirection(startX, endX, seg.Y + 1, seg.StartX - 1, seg.EndX + 1, Direction.Down);
                }
            }
        }

        void AddLineSameDirection(int startX, int endX, int y, Direction dir)
        {
            int x;
            int newSegmentStart = -1;
            for(x = startX; x < endX; x++) // Scan all x-es of parent segment
            {
                if(FillCondition(y, x)) // Cell adjacent to parent is to be filled
                { 
                    FillAction(y, x);
                    if(newSegmentStart < 0) // Start new segment from this cell
                        newSegmentStart = x;
                }
                else if(newSegmentStart >= 0) // Run into 'bad' cell -> end segment and push it
                {
                    _stack.Push(new Segment(newSegmentStart, x, y, dir, newSegmentStart == startX, false));
                    newSegmentStart = -1; // Reset segment
                }
            }

            if(newSegmentStart >= 0)
                _stack.Push(new Segment(newSegmentStart, x, y, dir, newSegmentStart == startX, true));
        }

        void AddLineOppositeDirection(int startX, int endX, int y,
                            int ignoreStart, int ignoreEnd, Direction dir)
        {
            int x;
            int newSegmentStart = -1;
            // Scan non-ignored x-es of parent
            for(x = startX; x < ignoreStart; x++)
            {
                if(FillCondition(y, x)) // Cell adjacent to parent is to be filled
                {                        
                    FillAction(y, x);
                    if(newSegmentStart < 0) // Start new segment from this cell
                        newSegmentStart = x;
                }
                else if(newSegmentStart >= 0) // Run into 'bad' cell -> end segment and push it
                {
                    _stack.Push(new Segment(newSegmentStart, x, y, dir, newSegmentStart == startX, false)); // push the segment
                    newSegmentStart = -1; // and end it
                }
            }

            for(x = ignoreEnd - 1; x < endX; x++)
            {
                if(FillCondition(y, x))
                {
                    FillAction(y, x);
                    if(newSegmentStart < 0) // Start new segment from this cell
                        newSegmentStart = x;
                }
                else if(newSegmentStart >= 0)  // Run into 'bad' cell -> end segment and push it
                {
                    _stack.Push(new Segment(newSegmentStart, x, y, dir, newSegmentStart == startX, false));
                    newSegmentStart = -1;
                }
            }

            if(newSegmentStart >= 0)
                _stack.Push(new Segment(newSegmentStart, x, y, dir, newSegmentStart == startX, true));
        }

        public override bool FloodSearch(int y, int x, ref int foundX, ref int foundY)
        {
            throw new NotImplementedException();
        }
    }
}
