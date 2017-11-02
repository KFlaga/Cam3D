using System;
using System.Collections.Generic;
using CamCore;
using Point2D = CamCore.Point2D<int>;
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

        List<List<Point2D>> _segments;

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
            _segments = new List<List<Point2D<int>>>();
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

        private bool CheckIfSegmentIsTooSmall(List<Point2D<int>> segment)
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
                        Disparity =
                            Math.Sqrt(map[r, c].SubDX * map[r, c].SubDX +
                            map[r, c].SubDY * map[r, c].SubDY),
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

        private void InterpolateInvalidatedSegment(DisparityMap map, List<Point2D<int>> segment)
        {
            for(int p = 0; p < segment.Count; ++p)
            {
                Point2D point = segment[p];
                // Omit pixels on border
                if(point.X < 1 || point.Y < 1 || point.X >= map.ColumnCount - 1 || point.Y >= map.RowCount - 1)
                    continue;

                double intDx = 0.0, intDy = 0.0;
                double n = 0;
                for(int dy = -1; dy <= 1; ++dy)
                {
                    for(int dx = -1; dx <= 1; ++dx)
                    {
                        if(map[point.Y + dy, point.X + dx].IsValid())
                        {
                            intDx += map[point.Y + dy, point.X + dx].SubDX;
                            intDy += map[point.Y + dy, point.X + dx].SubDY;
                            n += 1;
                        }
                    }
                }
                if(n > MinValidPixelsCountForInterpolation)
                {
                    map[point.Y, point.X].Flags = (int)DisparityFlags.Valid;
                    map[point.Y, point.X].SubDX = intDx / n;
                    map[point.Y, point.X].SubDY = intDy / n;
                    map[point.Y, point.X].DX = map[point.Y, point.X].SubDX.Round();
                    map[point.Y, point.X].DY = map[point.Y, point.X].SubDY.Round();
                }
            }
        }


        Stack<Point2D> _pointStack = new Stack<Point2D>();
        List<Point2D> _currentSegment;

        public void FloodFillSegments(DisparityMap map, int y, int x)
        {
            if(_cellMap[y, x].Visited)
                return;

            _cellMap[y, x].Visited = true;
            _currentSegment = new List<Point2D>();
            _currentSegment.Add(new Point2D(x, y));
            _cellMap[y, x].SegmentIndex = _segments.Count;

            _pointStack.Push(new Point2D(x, y));
            while(_pointStack.Count > 0)
            {
                Point2D point = _pointStack.Pop();

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
                _currentSegment.Add(new Point2D(y: newY, x: newX));
                _pointStack.Push(new Point2D(y: newY, x: newX));
            }
        }

        public override void InitParameters()
        {
            base.InitParameters();

            DoubleParameter maxDiffParam = new DoubleParameter(
                "Max Disparity Difference", "DIFF", 2.0, 0.0, 10000.0);
            Parameters.Add(maxDiffParam);

            IntParameter minSegmentParam = new IntParameter(
                "Min Segment Size", "SEG", 6, 1, 10000);
            Parameters.Add(minSegmentParam);

            BooleanParameter interpolateParam = new BooleanParameter(
                "Interpolate Invalidated Segments", "INT", false);
            Parameters.Add(interpolateParam);
        }

        public override void UpdateParameters()
        {
            base.UpdateParameters();

            MaxDisparityDiff = AlgorithmParameter.FindValue<double>("DIFF", Parameters);
            MinSegmentSize = AlgorithmParameter.FindValue<int>("SEG", Parameters);
            InterpolateInvalidated = AlgorithmParameter.FindValue<bool>("INT", Parameters);
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
