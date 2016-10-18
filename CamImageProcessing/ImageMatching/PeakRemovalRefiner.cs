﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CamCore;
using Point2D = CamCore.TPoint2D<int>;

namespace CamImageProcessing.ImageMatching
{
    public class PeakRemovalRefiner : DisparityRefinement
    {
        public int MinSegmentSize { get; set; }
        public double MaxDisparityDiff { get; set; }
        public bool InterpolateInvalidated { get; set; }

        struct Cell
        {
            public double Disparity;
            public int SegmentIndex;
            public bool Visited;
        }
        List<List<Point2D>> _segments;
        
        Cell[,] _segmentedMap;

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
            // Initialize pixel cells
            _segmentedMap = new Cell[map.RowCount, map.ColumnCount];
            _segments = new List<List<TPoint2D<int>>>();
            for(int r = 0; r < map.RowCount; ++r)
            {
                for(int c = 0; c < map.ColumnCount; ++c)
                {
                    _segmentedMap[r, c].Disparity =
                        Math.Sqrt(map[r, c].SubDX * map[r, c].SubDX +
                        map[r, c].SubDY * map[r, c].SubDY);
                    _segmentedMap[r, c].Visited = false;
                }
            }

            // For each pixel, find its segment
            for(int r = 0; r < map.RowCount; ++r)
            {
                for(int c = 0; c < map.ColumnCount; ++c)
                {
                    FloodFillSegments(map, r, c);
                }
            }

            // We have found segments 
            for(int i = 0; i < _segments.Count; ++i)
            {
                var segment = _segments[i];
                // For each:
                // 1) Check if it is smaller than MinSegment
                if(segment.Count < MinSegmentSize)
                {
                    // 2) Invalidate all disparities in segment
                    for(int p = 0; p < segment.Count; ++p)
                    {
                        map[segment[p].Y, segment[p].X].Flags = (int)DisparityFlags.Invalid;
                    }

                    // 3) If disparities should be interpolated : do so (but use only valid ones)
                    if(InterpolateInvalidated)
                    {
                        for(int p = 0; p < segment.Count; ++p)
                        {
                            Point2D point = segment[p];
                            if(point.X < 0 || point.Y < 0 || point.X >= map.ColumnCount || point.Y >= map.RowCount)
                                continue;

                            double intDx = 0.0, intDy = 0.0;
                            double n = 0;
                            for(int dy = -1; dy <= 1; ++dy)
                            {
                                for(int dx = -1; dx <= 1; ++dx)
                                {
                                    int py = Math.Max(0, Math.Min(point.Y + dy, map.RowCount - 1));
                                    int px = Math.Max(0, Math.Min(point.X + dx, map.ColumnCount - 1));
                                    if(map[py, px].IsValid())
                                    {
                                        intDx += map[py, px].SubDX;
                                        intDy += map[py, px].SubDY;
                                        n += 1;
                                    }
                                }
                            }
                            if(n > 0)
                            {
                                map[point.Y, point.X].Flags = (int)DisparityFlags.Valid;
                                map[point.Y, point.X].SubDX = intDx / n;
                                map[point.Y, point.X].SubDY = intDy / n;
                                map[point.Y, point.X].DX = map[point.Y, point.X].SubDX.Round();
                                map[point.Y, point.X].DY = map[point.Y, point.X].SubDY.Round();
                            }
                        }
                    }
                }
            }

            return map;
        }


        Stack<Point2D> _pointStack = new Stack<Point2D>();
        List<Point2D> _currentSegment;

        public void FloodFillSegments(DisparityMap map, int y, int x)
        {
            if(_segmentedMap[y, x].Visited)
                return;

            _segmentedMap[y, x].Visited = true;
            _currentSegment = new List<Point2D>();
            _currentSegment.Add(new Point2D(x, y));
            _segmentedMap[y, x].SegmentIndex = _segments.Count;
            
            _pointStack.Push(new Point2D(x, y));
            while(_pointStack.Count > 0)
            {
                Point2D point = _pointStack.Pop();

                if(point.Y > 0)
                {
                    CheckAndAddToSegment(point.X, point.Y, point.X, point.Y - 1);
                }
                if(point.Y + 1 < _segmentedMap.GetLength(0))
                {
                    CheckAndAddToSegment(point.X, point.Y, point.X, point.Y + 1);
                }
                if(point.X > 0)
                {
                    CheckAndAddToSegment(point.X, point.Y, point.X - 1, point.Y);
                }
                if(point.X + 1 < _segmentedMap.GetLength(1))
                {
                    CheckAndAddToSegment(point.X, point.Y, point.X + 1, point.Y);
                }
            }

            _segments.Add(_currentSegment);
        }

        private void CheckAndAddToSegment(int oldX, int oldY, int newX, int newY)
        {
            if(_segmentedMap[newY, newX].Visited == false && 
                Math.Abs(_segmentedMap[newY, newY].Disparity -
                    _segmentedMap[oldY, oldX].Disparity) < MaxDisparityDiff)
            {
                _segmentedMap[newY, newX].Visited = true;
                _segmentedMap[newY, newX].SegmentIndex = _segments.Count;
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

        public override string ToString()
        {
            return "Peaks (small segments) removal";
        }
    }
}
