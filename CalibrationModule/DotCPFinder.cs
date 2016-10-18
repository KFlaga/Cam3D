using CamImageProcessing;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;

namespace CalibrationModule
{
    class CalibrationDot
    {
        public List<Point> Points { get; set; }
        private Rect _bbox;
        public Rect BoundingBox
        {
            get { return _bbox; }
        }
        private Point _center;
        public Point Center
        {
            get { return _center; }
        }

        public CalibrationDot()
        {
            Points = new List<Point>();
            _bbox = new Rect(0, 0, 0, 0);
        }

        public void AddPoint(Point point)
        {
            Points.Add(point);

            if(Points.Count == 1)
            {
                // if its first point set BBox X/Y
                _bbox.X = point.X;
                _bbox.Y = point.Y;
            }
            else
            {
                // if not first update bbox size
                if(point.X < _bbox.X)
                    _bbox.X = point.X;
                else if(point.X > _bbox.X + _bbox.Width)
                    _bbox.Width = point.X - _bbox.X;

                if(point.Y < _bbox.Y)
                    _bbox.Y = point.Y;
                else if(point.Y > _bbox.Y + _bbox.Height)
                    _bbox.Height = point.Y - _bbox.Y;
            }
        }

        public Point FindCenter()
        {
            double sumx = 0, sumy = 0;

            foreach(Point point in Points)
            {
                sumx += point.X;
                sumy += point.Y;
            }

            _center = new Point(sumx / Points.Count, sumy / Points.Count);

            return _center;
        }
    }

    public class DotCPFinder : CalibrationPointsFinder
    {
        public GrayScaleImage Image { get; set; }
        private List<CalibrationDot> _dots;
        
        public override void SetBitmapSource(BitmapSource source)
        {
            Image = new GrayScaleImage();
            Image.FromBitmapSource(source);
        }

        public override void FindCalibrationPoints()
        {
            Points = new List<CalibrationPoint>();

            // 1) Binarize image
            BinarizeImage();

            // 2) Find all black dots:
            // - find black pixel
            // - add to list all adjacent black points
            // - add this list to dot list
            _dots = new List<CalibrationDot>();
            for(int y = 0; y < Image.SizeY; y++)
            {
                for(int x = 0; x < Image.SizeX; x++)
                {
                    // Check if point is black
                    if(Image[y, x] > 0)
                    {
                        if(CheckIfPointIsAlreadyInDot(y, x))
                            continue;

                        // Point is not in any dot, so create new one and add all linked points ( with flood fill alg.)
                        CalibrationDot newdot = new CalibrationDot();
                        FloodSearchForDotPoints(ref newdot, y, x);
                        _dots.Add(newdot);

                        // Move to right side of the dot
                        x = (int)(newdot.BoundingBox.X + newdot.BoundingBox.Width);
                    }
                }
            }

            // 3) For each dot compute gravity center -> it's calibration point!
            foreach(CalibrationDot dot in _dots)
            {
                dot.FindCenter();
                Points.Add(new CalibrationPoint()
                {
                    ImgX = (double)dot.Center.X,
                    ImgY = (double)dot.Center.Y
                });
            }
        }

        private void BinarizeImage()
        {
            BinarizeFilter binarizer = new BinarizeFilter();
            binarizer.Image = Image.ImageMatrix;
            binarizer.Threshold = 0.5;
            binarizer.Inverse = true;
            Image.ImageMatrix = binarizer.ApplyFilter();
        }

        // Search for all adjacent black points using algorithm similar to flood fill:
        // - after meeting black point it is added to dot points and its color is changed to white
        // - same is repeated for adjacent points
        private void FloodSearchForDotPoints(ref CalibrationDot dot, int y, int x)
        {
            if(y < 0 || y >= Image.SizeY || x < 0 || x >= Image.SizeX)
                return;

            if(Image[y, x] > 0.5f)
            {
                dot.AddPoint(new Point(x, y));
                Image[y, x] = 0.0f;
                FloodSearchForDotPoints(ref dot, y - 1, x);
                FloodSearchForDotPoints(ref dot, y + 1, x);
                FloodSearchForDotPoints(ref dot, y, x - 1);
                FloodSearchForDotPoints(ref dot, y, x + 1);
            }
        }

        private bool CheckIfPointIsAlreadyInDot(int y, int x)
        {
            foreach(var dot in _dots)
            {
                if(x >= dot.BoundingBox.X &&
                   x <= dot.BoundingBox.X + dot.BoundingBox.Width &&
                   y >= dot.BoundingBox.Y &&
                   y <= dot.BoundingBox.Y + dot.BoundingBox.Height)
                {
                    return true;
                }
            }
            return false;
        }

        public override void InitParameters()
        {
            Parameters = new List<CamCore.AlgorithmParameter>();
        }

        public override void UpdateParameters()
        {
        }
    }
}
