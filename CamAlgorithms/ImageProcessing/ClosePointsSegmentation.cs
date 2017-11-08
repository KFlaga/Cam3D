using CamCore;
using System;
using System.Collections.Generic;
using Point2D = CamCore.Point2D<int>;
using MathNet.Numerics.LinearAlgebra;

namespace CamAlgorithms
{
    public class ClosePointsSegmentation : ImageSegmentation
    {
        public double MaxDiffSquared { get; set; }
        Stack<Point2D> _pointStack = new Stack<Point2D>();
        Segment _currentSegment;

        Matrix<double> _imageMatrix;
        DisparityMap _map;
        ColorImage _colorImage;

        delegate void CheckAndAddToSegmentDelegate(int oldX, int oldY, int newX, int newY);
        CheckAndAddToSegmentDelegate _checkAndAddToSegment;

        public override void SegmentGray(Matrix<double> imageMatrix)
        {
            _checkAndAddToSegment = CheckAndAddToSegment_Gray;
            _imageMatrix = imageMatrix;
            _colorImage = null;
            _map = null;
            SegmentInternal(imageMatrix.RowCount, imageMatrix.ColumnCount);
        }

        public override void SegmentColor(ColorImage image)
        {
            _checkAndAddToSegment = CheckAndAddToSegment_Color;
            _colorImage = image;
            _imageMatrix = null;
            _map = null;
            SegmentInternal(image.RowCount, image.ColumnCount);
        }

        public override void SegmentDisparity(DisparityMap map)
        {
            _checkAndAddToSegment = CheckAndAddToSegment_Disparity;
            _map = map;
            _colorImage = null;
            _imageMatrix = null;
            SegmentInternal(map.RowCount, map.ColumnCount);
        }

        void SegmentInternal(int rows, int cols)
        {
            SegmentAssignments = new int[rows, cols];
            Segments = new List<Segment>();
            for(int r = 0; r < rows; ++r)
            {
                for(int c = 0; c < cols; ++c)
                {
                    SegmentAssignments[r, c] = -1;
                }
            }

            // For each pixel, find its segment
            for(int r = 0; r < rows; ++r)
            {
                for(int c = 0; c < cols; ++c)
                {
                    FloodFillSegments(r, c, rows, cols);
                }
            }
        }
        
        void FloodFillSegments(int y, int x, int rows, int cols)
        {
            if(_map != null && _map[y, x].IsInvalid())
                return;
            if(SegmentAssignments[y, x] != -1)
                return;

            _currentSegment = new Segment();
            _currentSegment.Pixels.Add(new Point2D(x, y));
            _currentSegment.SegmentIndex = Segments.Count;
            SegmentAssignments[y, x] = _currentSegment.SegmentIndex;

            _pointStack.Push(new Point2D(x, y));
            while(_pointStack.Count > 0)
            {
                Point2D point = _pointStack.Pop();

                if(point.Y > 0)
                {
                    _checkAndAddToSegment(point.X, point.Y, point.X, point.Y - 1);
                }
                if(point.Y + 1 < rows)
                {
                    _checkAndAddToSegment(point.X, point.Y, point.X, point.Y + 1);
                }
                if(point.X > 0)
                {
                    _checkAndAddToSegment(point.X, point.Y, point.X - 1, point.Y);
                }
                if(point.X + 1 < cols)
                {
                    _checkAndAddToSegment(point.X, point.Y, point.X + 1, point.Y);
                }
            }

            Segments.Add(_currentSegment);
        }

        private double GetDisparity(int y, int x)
        {
            return _map[y, x].SubDX * _map[y, x].SubDX;
        }

        private void CheckAndAddToSegment_Disparity(int oldX, int oldY, int newX, int newY)
        {
            if(SegmentAssignments[newY, newX] == -1 &&
                _map[newY, newX].IsValid() &&
                Math.Abs(GetDisparity(oldY, oldX) - GetDisparity(newY, newX)) <= MaxDiffSquared)
            {
                SegmentAssignments[newY, newX] = _currentSegment.SegmentIndex;
                _currentSegment.Pixels.Add(new Point2D(y: newY, x: newX));
                _pointStack.Push(new Point2D(y: newY, x: newX));
            }
        }

        private double GetGrayValue(int y, int x)
        {
            return _imageMatrix.At(y, x) * _imageMatrix.At(y, x);
        }

        private void CheckAndAddToSegment_Gray(int oldX, int oldY, int newX, int newY)
        {
            if(SegmentAssignments[newY, newX] == -1 &&
                Math.Abs(GetGrayValue(oldY, oldX) - GetGrayValue(newY, newX)) <= MaxDiffSquared)
            {
                SegmentAssignments[newY, newX] = _currentSegment.SegmentIndex;
                _currentSegment.Pixels.Add(new Point2D(y: newY, x: newX));
                _pointStack.Push(new Point2D(y: newY, x: newX));
            }
        }
        private double GetColorValue(int y, int x, RGBChannel channel)
        {
            return _colorImage[y, x, channel] * _colorImage[y, x, channel];
        }

        private void CheckAndAddToSegment_Color(int oldX, int oldY, int newX, int newY)
        {
            if(SegmentAssignments[newY, newX] == -1 &&
                Math.Abs(GetColorValue(oldY, oldX, RGBChannel.Red) - GetColorValue(newY, newX, RGBChannel.Red)) <= MaxDiffSquared &&
                Math.Abs(GetColorValue(oldY, oldX, RGBChannel.Green) - GetColorValue(newY, newX, RGBChannel.Green)) <= MaxDiffSquared &&
                Math.Abs(GetColorValue(oldY, oldX, RGBChannel.Blue) - GetColorValue(newY, newX, RGBChannel.Blue)) <= MaxDiffSquared)
            {
                SegmentAssignments[newY, newX] = _currentSegment.SegmentIndex;
                _currentSegment.Pixels.Add(new Point2D(y: newY, x: newX));
                _pointStack.Push(new Point2D(y: newY, x: newX));
            }
        }

        public override void InitParameters()
        {
            base.InitParameters();
            
            Parameters.Add(new DoubleParameter(
                "Max Points Difference (Squared)", "MaxDiffSquared", 2.0, 0.0, 10000.0));
        }

        public override void UpdateParameters()
        {
            base.UpdateParameters();

            MaxDiffSquared = AlgorithmParameter.FindValue<double>("MaxDiffSquared", Parameters);
        }

        public override string Name
        {
            get
            {
                return "Close Points Segmentation";
            }
        }
    }
}
