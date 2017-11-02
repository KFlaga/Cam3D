using System;
using System.Collections.Generic;
using CamCore;
using CamAlgorithms;
using System.Windows;

namespace CalibrationModule.PointsExtraction
{
    public class CalibrationShapesExtractor
    {
        public List<CalibrationShape> CalibShapes { get; private set; }

        private IImage _image;
        private double _tBrightness;

        enum CellCode
        {
            Unvisited = 0,
            WhiteField,
            Shape,
            DarkBackground
        }
        private CellCode[,] _pixelCodes;

        private List<Point2D<int>> _whiteBorder;
        int _currentWhiteField;
        CalibrationShape _currentShape;
        
        // IMAGE:
        // - dark background (DarkBackground)
        // - white background (WhiteField)
        // - dark shapes (Shape)
        public List<CalibrationShape> FindCalibrationShapes(IImage image, double brightnessTreshold = 0.5)
        {
            CalibShapes = new List<CalibrationShape>();
            
            _pixelCodes = new CellCode[_image.RowCount, _image.ColumnCount];
            _whiteBorder = new List<Point2D<int>>();
            _image = image;
            _tBrightness = brightnessTreshold;

            // Fill whole background first
            FillBackgroundAroundTheEdgesAndFindWhiteFieldBorder();
            if(_whiteBorder.Count == 0)
            {
                throw new Exception("No white field detected on calibration image: no calibration points can be found");
            }
            
            // Fill white field and shapes (inside FillWhiteField)
            FloodFillWhiteFieldAndShapesInside();

            return CalibShapes;
        }

        void FillBackgroundAroundTheEdgesAndFindWhiteFieldBorder()
        {
            var flood = new ScanLineFloodAlgorithm()
            {
                ImageHeight = _image.RowCount,
                ImageWidth = _image.ColumnCount,
                FillCondition = IfUnvisitedMarkAsDarkOrWhiteBackground,
                FillAction = (y, x) => { }
            };

            // Left/Right Side
            for(int dy = 0; dy < _image.RowCount; ++dy)
            {
                if(_pixelCodes[dy, 0] == CellCode.Unvisited)
                {
                    flood.FloodFill(dy, 0);
                }

                if(_pixelCodes[dy, _image.ColumnCount - 1] == CellCode.Unvisited)
                {
                    flood.FloodFill(dy, _image.ColumnCount - 1);
                }
            }

            // Top/Bottom Side
            for(int dx = 0; dx < _image.ColumnCount; ++dx)
            {
                if(_pixelCodes[0, dx] == CellCode.Unvisited)
                {
                    flood.FloodFill(0, dx);
                }

                if(_pixelCodes[_image.RowCount - 1, dx] == CellCode.Unvisited)
                {
                    flood.FloodFill(_image.RowCount - 1, dx);
                }
            }
        }

        bool IfUnvisitedMarkAsDarkOrWhiteBackground(int y, int x)
        {
            if(_pixelCodes[y, x] != CellCode.Unvisited) { return false; }
            if(_image.HaveValueAt(y, x) == false || _image[y, x] < _tBrightness)
            {
                _pixelCodes[y, x] = CellCode.DarkBackground;
                return true;
            }
            else
            {
                _pixelCodes[y, x] = CellCode.WhiteField;
                _whiteBorder.Add(new Point2D<int>(y: y, x: x));
                return false;
            }
        }

        void FloodFillWhiteFieldAndShapesInside()
        {
            var flood = new ScanLineFloodAlgorithm()
            {
                ImageHeight = _image.RowCount,
                ImageWidth = _image.ColumnCount,
                FillCondition = IfUnvisitedMarkAsWhiteFieldOrFloodFillDarkShape,
                FillAction = (y, x) => { }
            };

            // Reset all white points
            foreach(var point in _whiteBorder)
            {
                _pixelCodes[point.Y, point.X] = CellCode.Unvisited;
            }

            _currentWhiteField = 0;
            // For each white field start flood
            foreach(var point in _whiteBorder)
            {
                flood.FloodFill(point.Y, point.X);
                _currentWhiteField += 1;
            }
        }

        bool IfUnvisitedMarkAsWhiteFieldOrFloodFillDarkShape(int y, int x)
        {
            if(_pixelCodes[y, x] != CellCode.Unvisited) { return false; }
            if(_image[y, x] > _tBrightness)
            {
                _pixelCodes[y, x] = CellCode.WhiteField;
                return true;
            }
            else
            {
                _pixelCodes[y, x] = CellCode.Shape;
                FillShape(y, x);
                return false;
            }
        }

        void FillShape(int y, int x)
        {
            var flood = new ScanLineFloodAlgorithm()
            {
                ImageHeight = _image.RowCount,
                ImageWidth = _image.ColumnCount,
                FillCondition = IfUnvisitedAndDarkAddToCurrentShape,
                FillAction = (yy, xx) => { }
            };

            // Create new CalibrationShape and set index in pixelcodes
            _currentShape = new CalibrationShape()
            {
                Index = _currentWhiteField
            };

            _pixelCodes[y, x] = CellCode.Unvisited;
            flood.FloodFill(y, x);

            _currentShape.FindCenter();
        }
        
        bool IfUnvisitedAndDarkAddToCurrentShape(int y, int x)
        {
            if(_pixelCodes[y, x] != CellCode.Unvisited) { return false; }
            if(_image[y, x] < _tBrightness)
            {
                _pixelCodes[y, x] = CellCode.Shape;
                _currentShape.AddPoint(new Vector2(y: y, x: x));
                return true;
            }
            return false;
        }
    }
}
