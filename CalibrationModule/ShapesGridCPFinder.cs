using CamCore;
using CamImageProcessing;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;
using static CamCore.MatrixExtensions;

namespace CalibrationModule
{
    public class ShapesGridCPFinder : CalibrationPointsFinder
    {
        public List<CalibrationShape> CalibShapes { get; set; }
        private Matrix<double> _pixelCodes;
        private CalibrationShape _currentShape;

        public CalibrationGrid CalibGrid { get; set; }

        private List<TPoint2D<int>> _whiteBorder;
        int _currentWhiteField;

        public double PointSizeTresholdHigh { get; set; } // How much bigger than primary shape calib shape can be to accept it
        public double PointSizeTresholdLow { get; set; } // How much smaller than primary shape calib shape can be to accept it
        public bool UseMedianFilter { get; set; }
        public int MedianFilterRadius { get; set; }

        public bool PerformHistogramStretch { get; set; }
        public double SaturateDarkTreshold { get; set; }
        public double SaturateLightTreshold { get; set; }

        public Vector2 AxisX { get; private set; } // Local grid x-axis in image coords
        public Vector2 AxisY { get; private set; } // Local grid y-axis in image coords

        private IFloodAlgorithm _flood;
        
        enum CellCode : ulong
        {
            Unvisited = 0,
            WhiteField = 1,
            Shape = 2,
            Background = 4,
            CodeMask = 0xF
        }

        bool CompareCellCodes(double pixelCode, CellCode cellCode)
        {
            var bytes = BitConverter.GetBytes(pixelCode);
            uint code = BitConverter.ToUInt32(bytes, 0);
            return (CellCode)(code - ((code>>4)<<4)) == cellCode;
        }

        double CellCodeToFloat(CellCode cellCode)
        {
            var bytes = BitConverter.GetBytes((ulong)cellCode);
            return BitConverter.ToDouble(bytes, 0);
        }

        double CellIndexToFloat(CellCode code, uint index)
        {
            var bytes = BitConverter.GetBytes((ulong)((index << 4) + (ulong)code));
            return BitConverter.ToDouble(bytes, 0);
        }

        uint CellIndexFromFloat(double pixelCode)
        {
            var bytes = BitConverter.GetBytes(pixelCode);
            uint code = BitConverter.ToUInt32(bytes, 0);
            return code >> 4;
        }

        public ShapesGridCPFinder()
        {
            PrimaryShapeChecker = new RedNeighbourhoodChecker();
            LinesExtractor = new ShapeGridLinesExtractor();
        }

        // 1) Move around the border and FF for each 'Unvisited', fill background ( dark ) with 'Background'
        // 2) During FF mark some white field and now start FF and fill light area with 'WhiteField'
        //    We assume white field is one big connected area ( with 'holes' for calibration shapes )
        // 3) If during WF some unvisited dark area is run into, FF it with 'Shape', create CalibrationShape
        //    for it and add its points, save index in pixel codes
        public override void FindCalibrationPoints()
        {
            //if(UseMedianFilter)
            //{
            //    // Preprocess with median filter to remove some noise (edges should stay in same place )
            //    MedianFilter filter = new MedianFilter();
            //    filter.FilterBorder = false;
            //    filter.Image = Image.ImageMatrix;
            //    filter.WindowRadius = MedianFilterRadius;
            //    ImageGray.ImageMatrix = filter.ApplyFilter();
            //}

            PrimaryShapeChecker.Image = Image;

            _pixelCodes = new DenseMatrix(Image.RowCount, Image.ColumnCount);
            CalibShapes = new List<CalibrationShape>(32);

            _whiteBorder = new List<TPoint2D<int>>();
            // Fill whole background first
            FillBackground();

            _currentShape = null;
            CalibShapes.Add(null); // Reserve slot for primary shape
            if(_whiteBorder.Count == 0)
            {
                // No white field detected -> error
                MessageBox.Show("No white field detected on calibration image: no calibration points can be found");
                return;
            }

            // Fill white field and shapes (inside FillWhiteField)
            FillWhiteField();
            if(CalibShapes[0] == null)
            {
                MessageBox.Show("No primary calibration shape (reddish) detected on calibration image: no calibration points can be found");
                return;
            }

            // Now find main white field ( one with primary shape ) and remove all shapes
            // not from this field
            uint primaryField = CalibShapes[0].Index;
            for(int i = 1; i < CalibShapes.Count;)
            {
                if(CalibShapes[i].Index != primaryField)
                {
                    CalibShapes.RemoveAt(i);
                    continue;
                }
                ++i;
            }

            // At this point CalibrationShapes should be created, so find its centers
            // and sort them to form grid
            FillCalibrationGrid();

            // From each valid shape from grid, create CalibrationPoint
            Points = new List<CalibrationPoint>();
            foreach(var shape in CalibGrid)
            {
                if(shape != null && !shape.IsInvalid)
                    Points.Add(new CalibrationPoint()
                    {
                        Img = shape.GravityCenter,
                        RealGridPos = shape.GridPos
                    });
            }

            ((ShapeGridLinesExtractor)LinesExtractor).CalibGrid = CalibGrid;
        }

        void FillBackground()
        {
            _flood = new ScanLineFloodAlgorithm();
            _flood.ImageHeight = Image.RowCount;
            _flood.ImageWidth = Image.ColumnCount;
            _flood.FillCondition = BackgroundFillCondition;
            _flood.FillAction = BackgroundFillAction;

            // Left/Right Side
            for(int dy = 0; dy < Image.RowCount; ++dy)
            {
                if(CompareCellCodes(_pixelCodes[dy, 0], CellCode.Unvisited))
                {
                    _flood.FloodFill(dy, 0);
                }

                if(CompareCellCodes(_pixelCodes[dy, Image.ColumnCount - 1], CellCode.Unvisited))
                {
                    _flood.FloodFill(dy, Image.ColumnCount - 1);
                }
            }

            // Top/Bottom Side
            for(int dx = 0; dx < Image.ColumnCount; ++dx)
            {
                if(CompareCellCodes(_pixelCodes[0, dx], CellCode.Unvisited))
                {
                    _flood.FloodFill(0, dx);
                }

                if(CompareCellCodes(_pixelCodes[Image.RowCount - 1, dx], CellCode.Unvisited))
                {
                    _flood.FloodFill(Image.RowCount - 1, dx);
                }
            }
        }

        bool BackgroundFillCondition(int y, int x)
        {
            if(CompareCellCodes(_pixelCodes[y, x], CellCode.Unvisited))
            {
                if(Image.HaveValueAt(y, x) == false || Image[y, x] < 0.5)
                {
                    return true;
                }
                else
                {
                    _pixelCodes[y, x] = CellCodeToFloat(CellCode.WhiteField);
                    _whiteBorder.Add(new TPoint2D<int>(y: y, x: x));
                    return false;
                }
            }
            return false;
        }

        void BackgroundFillAction(int y, int x)
        {
            _pixelCodes[y, x] = CellCodeToFloat(CellCode.Background);
        }

        void FillWhiteField()
        {
            _flood = new ScanLineFloodAlgorithm();
            _flood.ImageHeight = Image.RowCount;
            _flood.ImageWidth = Image.ColumnCount;
            _flood.FillCondition = WhiteFieldFillCondition;
            _flood.FillAction = WhiteFieldFillAction;

            // Reset all white points
            foreach(var point in _whiteBorder)
            {
                _pixelCodes[point.Y, point.X] = CellCodeToFloat(CellCode.Unvisited);
            }

            _currentWhiteField = 0;
            // For each white field start flood
            foreach(var point in _whiteBorder)
            {
                _flood.FloodFill(point.Y, point.X);
                _currentWhiteField += 1;
            }
        }

        bool WhiteFieldFillCondition(int y, int x)
        {
            if(CompareCellCodes(_pixelCodes[y, x], CellCode.Unvisited))
            {
                if(Image[y, x] > 0.5f)
                {
                    return true;
                }
                else
                {
                    _pixelCodes[y, x] = CellCodeToFloat(CellCode.Shape);
                    FillShape(y, x);
                    return false;
                }
            }
            return false;
        }

        void WhiteFieldFillAction(int y, int x)
        {
            _pixelCodes[y, x] = CellCodeToFloat(CellCode.WhiteField);
        }

        void FillShape(int y, int x)
        {
            _flood = new IterativeBasicFloodAlgorithm();
            _flood.ImageHeight = Image.RowCount;
            _flood.ImageWidth = Image.ColumnCount;
            _flood.FillCondition = ShapeFillCondition;
            _flood.FillAction = ShapeFillAction;

            // Create new CalibrationShape and set index in pixelcodes
            _currentShape = new CalibrationShape();
            _currentShape.Index = (uint)_currentWhiteField;

            _pixelCodes[y, x] = CellCodeToFloat(CellCode.Unvisited);
            _flood.FloodFill(y, x);

            _currentShape.FindCenter();

            // Check if its primary shape -> should have very high red value
            if(PrimaryShapeChecker.CheckShape(_currentShape))
            {
                CalibShapes[0] = _currentShape;
            }
            else
            {
                CalibShapes.Add(_currentShape);
            }
        }


        bool ShapeFillCondition(int y, int x)
        {
            return CompareCellCodes(_pixelCodes[y, x], CellCode.Unvisited) &&
                Image[y, x] < 0.5f;
        }

        void ShapeFillAction(int y, int x)
        {
            _pixelCodes[y, x] = CellIndexToFloat(CellCode.Shape, (uint)_currentWhiteField);
            _currentShape.AddPoint(new Vector2(y: y, x: x));
        }

        void FillCalibrationGrid()
        {
            // Eleminate outliners by comparing shape size with primary shape size
            for(int i = 1; i < CalibShapes.Count;)
            {
                if(CalibShapes[i].Points.Count < CalibShapes[0].Points.Count * PointSizeTresholdLow ||
                    CalibShapes[i].Points.Count > CalibShapes[0].Points.Count * PointSizeTresholdHigh)
                {
                    CalibShapes.RemoveAt(i);
                    continue;
                }
                ++i;
            }

            CalibrationShape shapeX, shapeY; // shapeX, shapeY centers defines local axes
            FindLocalAxes(out shapeX, out shapeY);

            if(shapeX == null || shapeY == null)
                return;

            // Init shapes grid [y,x], add primary shape and closest cells
            InitCalibrationGrid(shapeX, shapeY);

            // Distance cost function : distance from estimated point
            // when moving along X: eP(x,y) = P(x-1,y) + (P(x-1,y) - P(x-2,y))
            // when moving along Y: eP(x,y) = P(x,y-1) + (P(x,y-1) - P(x,y-2))
            // 
            // If x = 0, then d(x,y) = P(x+1,y-1) - P(x,y-1)
            // else d(x,y) = P(x,y) - P(x-1,y)
            // eP(x+1,y) = P(x,y) + d(x,y)
            // Having eP, find closest point to eP and it will be P

            // 1st fill 1st row and column
            FillCalibGridLine(2, 0, 0.5f, 2.0f, true);
            FillCalibGridLine(0, 2, 2.0f, 0.5f, false);

            for(int row = 1; row < CalibGrid.RowCount; ++row)
            {
                // Add all rows
                FillCalibGridLine(1, row, 0.33f, 2.0f, true);
            }

            // TODO add more robustness : 
            // - column fill with false-positives elimination
            // - flood-like fill, so irregular grid will be properly scaned
        }

        private void FillCalibGridLine(int x, int y, double distTreshSq_x, double distTreshSq_y, bool isRow)
        {
            Vector2 d = new Vector2();
            bool haveHole = false;

            while(CalibShapes.Count > 0)
            {
                TPoint2D<int> fromPoint;
                if(isRow)
                    ComputeDistanceToNextPoint_Row(x, y, ref d, out fromPoint);
                else
                    ComputeDistanceToNextPoint_Column(x, y, ref d, out fromPoint);

                double dx = (d * AxisX).LengthSquared();
                double dy = (d * AxisY).LengthSquared();
                int closestIndex;
                double edx, edy;
                Vector2 eP;

                ComputeEstimatedPoint(x, y, d, fromPoint, out eP, out closestIndex, out edx, out edy);

                // Check if point is close enough to estimated point
                if(edx < dx * distTreshSq_x + 1.0f && edy < dy * distTreshSq_y + 1.0f)
                {
                    CalibGrid.Add(y, x, CalibShapes[closestIndex]);
                    CalibShapes.RemoveAt(closestIndex);
                    haveHole = false;
                }
                else if(haveHole)
                {
                    // Grid have 2 holes -> assume then that it have ended
                    break;
                }
                else
                {
                    // We have a hole, fill slot with dummy shape
                    AddDummyShape(x, y, eP);
                    haveHole = true;
                }

                if(isRow)
                    x += 1;
                else
                    y += 1;
            }
        }

        private void ComputeDistanceToNextPoint_Row(int x, int y, ref Vector2 d, out TPoint2D<int> fromPoint)
        {
            if(x > 1)
            {
                d = CalibGrid[y, x - 1].GravityCenter - CalibGrid[y, x - 2].GravityCenter;
                fromPoint = new TPoint2D<int>(y: y, x: x - 1);
            }
            else if(x == 1)
            {
                // Adding second in row -> use d from point 'above'
                d = CalibGrid[y - 1, x].GravityCenter - CalibGrid[y - 1, x - 1].GravityCenter;
                fromPoint = new TPoint2D<int>(y: y, x: x - 1);
            }
            else // x == 0
            {
                // Adding first in row -> move in y direction from 'above'
                d = CalibGrid[y - 1, x].GravityCenter - CalibGrid[y - 2, x].GravityCenter;
                fromPoint = new TPoint2D<int>(y: y - 1, x: x);
            }
        }

        private void ComputeDistanceToNextPoint_Column(int x, int y, ref Vector2 d, out TPoint2D<int> fromPoint)
        {
            if(y > 1)
            {
                d = CalibGrid[y - 1, x].GravityCenter - CalibGrid[y - 2, x].GravityCenter;
                fromPoint = new TPoint2D<int>(y: y - 1, x: x);
            }
            else if(y == 1)
            {
                // Adding second in column -> use d from point on 'left'
                d = CalibGrid[y, x - 1].GravityCenter - CalibGrid[y - 1, x - 1].GravityCenter;
                fromPoint = new TPoint2D<int>(y: y - 1, x: x);
            }
            else //y == 0
            {
                // Adding first in column -> move in x direction on 'left'
                d = CalibGrid[y, x - 1].GravityCenter - CalibGrid[y, x - 2].GravityCenter;
                fromPoint = new TPoint2D<int>(y: y, x: x - 1);
            }
        }

        private void AddDummyShape(int x, int y, Vector2 eP)
        {
            CalibrationShape dummy = new CalibrationShape();
            dummy.GravityCenter = eP;
            dummy.IsInvalid = true;
            CalibGrid.Add(y, x, dummy);
        }

        private void ComputeEstimatedPoint(int x, int y, Vector2 d, TPoint2D<int> fromPoint, out Vector2 eP, out int minIndex, out double edx, out double edy)
        {
            eP = CalibGrid[fromPoint.Y, fromPoint.X].GravityCenter + d;
            minIndex = 0;
            double minDist = (CalibShapes[0].GravityCenter - eP).LengthSquared();
            for(int i = 0; i < CalibShapes.Count; ++i)
            {
                double dist = (CalibShapes[i].GravityCenter - eP).LengthSquared();
                if(minDist > dist)
                {
                    minDist = dist;
                    minIndex = i;
                }
            }

            edx = ((CalibShapes[minIndex].GravityCenter - eP) * AxisX).LengthSquared();
            edy = ((CalibShapes[minIndex].GravityCenter - eP) * AxisY).LengthSquared();
        }

        private void InitCalibrationGrid(CalibrationShape shapeX, CalibrationShape shapeY)
        {
            CalibGrid = new CalibrationGrid(4, 4);
            CalibGrid.Add(0, 0, CalibShapes[0]);
            CalibShapes.RemoveAt(0);

            CalibGrid.Add(0, 1, shapeX);
            CalibShapes.Remove(shapeX);

            CalibGrid.Add(1, 0, shapeY);
            CalibShapes.Remove(shapeY);
        }

        private void FindLocalAxes(out CalibrationShape shapeX, out CalibrationShape shapeY)
        {

            // Find calibration grid axes 
            // 1) Sort shapes by distance to primary shape
            CalibShapes.Sort((s1, s2) =>
            {
                // Compares distance of s1 and s2 to primary shape
                return (s1.GravityCenter - CalibShapes[0].GravityCenter).LengthSquared().CompareTo(
                     (s2.GravityCenter - CalibShapes[0].GravityCenter).LengthSquared());
            });

            // 2) Find direction vectors from PS to closest one and then find
            //    closest one with direction rotated by 90deg ( by comparing its dot product ->
            //    for 90deg its 0, having like 10deg tolerance we have :
            //    dot(A,B) / (|A||B|) = cos(a) < 0.17
            // Use 2 next closest ones
            shapeX = null;
            shapeY = null;
            Vector2 direction_1 = (CalibShapes[1].GravityCenter - CalibShapes[0].GravityCenter);
            direction_1.Normalise();
            shapeX = CalibShapes[1];

            //Vector2 direction_2 = direction_1;
            //int pointNum = 2;
            //for(; pointNum < CalibShapes.Count; ++pointNum)
            //{
            //    // Find direction and dot product
            //    direction_2 = (CalibShapes[pointNum].GravityCenter - CalibShapes[0].GravityCenter);
            //    direction_2.Normalise();

            //    if(Math.Abs(direction_1.CosinusTo(direction_2)) < 0.17)
            //    {
            //        // Found prependicular direction
            //        shapeY = CalibShapes[pointNum];
            //        break;
            //    }
            //}
            
            Vector2 direction_2 = (CalibShapes[2].GravityCenter - CalibShapes[0].GravityCenter);
            direction_2.Normalise();
            double cos2 = Math.Abs(direction_1.CosinusTo(direction_2));

            Vector2 direction_3 = (CalibShapes[3].GravityCenter - CalibShapes[0].GravityCenter);
            direction_3.Normalise();
            double cos3 = Math.Abs(direction_1.CosinusTo(direction_3));

            // Choose one with angle closer to 90deg (so cos closer to 0)
            direction_2 = cos2 < cos3 ? direction_2 : direction_3;
            shapeY = cos2 < cos3 ? CalibShapes[2] : CalibShapes[3];

            // Now depending on sign of sin(a), we can determine which axis is X and Y
            // Using cross-product of d1 and d2 ( casting it to 3d with z = 0 ) we have sin(a) = (d1.x * d2.y - d1.y * d2.x)
            // If sin(a) > 0, then d1 is local x axis, d2 is y
            if(direction_1.SinusTo(direction_2) > 0.0)
            {
                AxisX = direction_1;
                AxisY = direction_2;
            }
            else
            {
                AxisX = direction_2;
                AxisY = direction_1;

                var temp = shapeY;
                shapeY = shapeX;
                shapeX = temp;
            }
        }

        public override string Name
        {
            get
            {
                return "CalibShape";
            }
        }

        public override void InitParameters()
        {
            base.InitParameters();

            AlgorithmParameter treshLow = new DoubleParameter(
                "Size Elimination Threshold Low", "SETL", 0.1, 0.0001, 1.0);
            Parameters.Add(treshLow);

            AlgorithmParameter treshHigh = new DoubleParameter(
               "Size Elimination Threshold High", "SETH", 10.0, 1.0, 1000.0);
            Parameters.Add(treshHigh);

            AlgorithmParameter useMedianFilter = new BooleanParameter(
               "Use Median Prefiltering", "UMF", false);
            Parameters.Add(useMedianFilter);

            AlgorithmParameter medianFilterRadous = new IntParameter(
               "Median Filter Radius (opt)", "MFR", 3, 1, 9);
            Parameters.Add(medianFilterRadous);

            AlgorithmParameter doStretch = new BooleanParameter(
               "Stretch/Saturate histogram", "SSH", false);
            Parameters.Add(doStretch);

            AlgorithmParameter saturateDark = new DoubleParameter(
               "Dark saturation treshold", "DSH", 0.2, 0.0, 1.0);
            Parameters.Add(saturateDark);

            AlgorithmParameter saturateLight = new DoubleParameter(
               "Light saturation treshold", "LSH", 0.8, 0.0, 1.0);
            Parameters.Add(saturateLight);
        }

        public override void UpdateParameters()
        {
            base.UpdateParameters();

            PointSizeTresholdLow = AlgorithmParameter.FindValue<double>("SETL", Parameters);
            PointSizeTresholdHigh = AlgorithmParameter.FindValue<double>("SETH", Parameters);
            UseMedianFilter = AlgorithmParameter.FindValue<bool>("UMF", Parameters);
            MedianFilterRadius = AlgorithmParameter.FindValue<int>("MFR", Parameters);
            PerformHistogramStretch = AlgorithmParameter.FindValue<bool>("SSH", Parameters);
            SaturateDarkTreshold = AlgorithmParameter.FindValue<double>("DSH", Parameters);
            SaturateLightTreshold = AlgorithmParameter.FindValue<double>("LSH", Parameters);
        }

        public override string ToString()
        {
            return "ShapesGridCPFinder";
        }
    }
}
