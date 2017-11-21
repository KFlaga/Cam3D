using System;
using System.Collections.Generic;
using CamCore;
using System.Diagnostics;

namespace CamAlgorithms.ImageMatching
{
    public class PeakRemovalRefiner : DisparityRefinement
    {
        public int MinSegmentSize { get; set; }
        public double MaxDisparityDiff { get; set; }
        public bool InterpolateInvalidated { get; set; }
        public int MinValidPixelsCountForInterpolation { get; set; } = 3;

        [DebuggerDisplay("d = {Disparity}, i = {SegmentIndex}")]
        class Cell
        {
            public double Disparity;
            public int SegmentIndex;
            public bool Visited;
        }

        List<List<IntPoint2>> _segments;

        Cell[,] _cellMap;

        public override void Init()
        {

        }

        public override void RefineMaps()
        {
            if(MapLeft != null)
            {
                MapLeft = FilterMap(MapLeft);
            }

            if(MapRight != null)
            {
                MapRight = FilterMap(MapRight);
            }
        }

        public DisparityMap FilterMap(DisparityMap map)
        {
            _segments = new List<List<IntPoint2>>();
            InitCellMap(map);
            FindCellSegments(map);

            // We have found segments 
            for(int i = 0; i < _segments.Count; ++i)
            {
                var segment = _segments[i];
                if(CheckIfSegmentIsTooSmall(segment))
                {
                    // Invalidate all disparities in segment
                    for(int p = 0; p < segment.Count; ++p)
                    {
                        map[segment[p].Y, segment[p].X].Flags = (int)DisparityFlags.Invalid;
                    }
                    
                    if(InterpolateInvalidated)
                    {
                        InterpolateInvalidatedSegment(map, segment);
                    }
                }
            }

            return map;
        }

        private bool CheckIfSegmentIsTooSmall(List<IntPoint2> segment)
        {
            return segment.Count < MinSegmentSize;
        }

        private void InitCellMap(DisparityMap map)
        {
            _cellMap = new Cell[map.RowCount, map.ColumnCount];
            for(int r = 0; r < map.RowCount; ++r)
            {
                for(int c = 0; c < map.ColumnCount; ++c)
                {
                    _cellMap[r, c] = new Cell()
                    {
                        Disparity = map[r, c].SubDX,
                        Visited = false,
                        SegmentIndex = 0
                    };
                }
            }
        }

        private void FindCellSegments(DisparityMap map)
        {
            for(int r = 0; r < map.RowCount; ++r)
            {
                for(int c = 0; c < map.ColumnCount; ++c)
                {
                    FloodFillSegments(map, r, c);
                }
            }
        }

        private void InterpolateInvalidatedSegment(DisparityMap map, List<IntPoint2> segment)
        {
            for(int p = 0; p < segment.Count; ++p)
            {
                IntPoint2 point = segment[p];
                // Omit pixels on border
                if(point.X < 1 || point.Y < 1 || point.X >= map.ColumnCount - 1 || point.Y >= map.RowCount - 1)
                    continue;

                double interpDx = 0.0;
                double n = 0;
                for(int dy = -1; dy <= 1; ++dy)
                {
                    for(int dx = -1; dx <= 1; ++dx)
                    {
                        if(map[point.Y + dy, point.X + dx].IsValid())
                        {
                            interpDx += map[point.Y + dy, point.X + dx].SubDX;
                            n += 1;
                        }
                    }
                }
                if(n > MinValidPixelsCountForInterpolation)
                {
                    map[point.Y, point.X].Flags = (int)DisparityFlags.Valid;
                    map[point.Y, point.X].SubDX = interpDx / n;
                    map[point.Y, point.X].DX = map[point.Y, point.X].SubDX.Round();
                }
            }
        }


        Stack<IntPoint2> _pointStack = new Stack<IntPoint2>();
        List<IntPoint2> _currentSegment;

        public void FloodFillSegments(DisparityMap map, int y, int x)
        {
            if(_cellMap[y, x].Visited)
                return;

            _cellMap[y, x].Visited = true;
            _currentSegment = new List<IntPoint2>();
            _currentSegment.Add(new IntPoint2(x, y));
            _cellMap[y, x].SegmentIndex = _segments.Count;

            _pointStack.Push(new IntPoint2(x, y));
            while(_pointStack.Count > 0)
            {
                IntPoint2 point = _pointStack.Pop();

                if(point.Y > 0)
                {
                    CheckAndAddToSegment(point.X, point.Y, point.X, point.Y - 1);
                }
                if(point.Y + 1 < _cellMap.GetLength(0))
                {
                    CheckAndAddToSegment(point.X, point.Y, point.X, point.Y + 1);
                }
                if(point.X > 0)
                {
                    CheckAndAddToSegment(point.X, point.Y, point.X - 1, point.Y);
                }
                if(point.X + 1 < _cellMap.GetLength(1))
                {
                    CheckAndAddToSegment(point.X, point.Y, point.X + 1, point.Y);
                }
            }

            _segments.Add(_currentSegment);
        }

        private void CheckAndAddToSegment(int oldX, int oldY, int newX, int newY)
        {
            if(_cellMap[newY, newX].Visited == false &&
                Math.Abs(_cellMap[newY, newX].Disparity -
                    _cellMap[oldY, oldX].Disparity) <= MaxDisparityDiff)
            {
                _cellMap[newY, newX].Visited = true;
                _cellMap[newY, newX].SegmentIndex = _segments.Count;
                _currentSegment.Add(new IntPoint2(y: newY, x: newX));
                _pointStack.Push(new IntPoint2(y: newY, x: newX));
            }
        }

        public override void InitParameters()
        {
            base.InitParameters();
            
            Parameters.Add(new DoubleParameter(
                "Max Disparity Difference", "MaxDisparityDiff", 2.0, 0.0, 10000.0));
            Parameters.Add(new IntParameter(
                "Min Segment Size", "MinSegmentSize", 6, 1, 10000));
            Parameters.Add(new BooleanParameter(
                "Interpolate Invalidated Segments", "InterpolateInvalidated", false));
        }

        public override void UpdateParameters()
        {
            base.UpdateParameters();

            MaxDisparityDiff = IAlgorithmParameter.FindValue<double>("MaxDisparityDiff", Parameters);
            MinSegmentSize = IAlgorithmParameter.FindValue<int>("MinSegmentSize", Parameters);
            InterpolateInvalidated = IAlgorithmParameter.FindValue<bool>("InterpolateInvalidated", Parameters);
        }

        public override string Name
        {
            get
            {
                return "Peaks removal";
            }
        }
    }
}
