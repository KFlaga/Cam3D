using CamCore;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using Point2D = CamCore.TPoint2D<int>;

namespace CamImageProcessing
{
    public class MeanShiftSegmentation : ImageSegmentation
    {
        private bool[,] _converged;
        private bool[,] _visited;

        public int Radius;
        public double ValueBandwidth { get; set; }
        public double SpatialBandwidth { get; set; }
        public double ConvergenceThreshold { get; set; } = 0.001;
        public double MaxSegmentDeviation { get; set; }

        private double _spatialScale;

        public enum KernelType
        {
            Gauss = 0,
            Square = 1,
            Epanechnikov = 2
        }

        KernelType _kernelSpatialType;
        public KernelType UsedSpatialKernel
        {
            get { return _kernelSpatialType; }
            set
            {
                _kernelSpatialType = value;
                switch(value)
                {
                    case KernelType.Gauss:
                        _kernelSpatial = KernelGauss;
                        break;
                    case KernelType.Epanechnikov:
                        _kernelSpatial = KernelEpanechnikov;
                        break;
                }
            }
        }

        KernelType _kernelColorType;
        public KernelType UsedColorKernel
        {
            get { return _kernelColorType; }
            set
            {
                _kernelColorType = value;
                switch(value)
                {
                    case KernelType.Gauss:
                        _kernelColor = KernelGauss;
                        break;
                    case KernelType.Epanechnikov:
                        _kernelColor = KernelEpanechnikov;
                        break;
                }
            }
        }

        public delegate double KernelDelegate(double distSq);
        KernelDelegate _kernelSpatial;
        KernelDelegate _kernelColor;

        public double KernelGauss(double distSq)
        {
            return Math.Exp(distSq);
        }

        public double KernelEpanechnikov(double distSq)
        {
            return Math.Max(0.0, 1.0 - distSq);
        }

        #region SEGMENT_GRAY

        private double[,] _values;

        public override void SegmentGray(Matrix<double> image)
        {
            _converged = new bool[image.RowCount, image.ColumnCount]; // Filled with false
            _values = new double[image.RowCount, image.ColumnCount];
            for(int r = 0; r < image.RowCount; ++r)
                for(int c = 0; c < image.ColumnCount; ++c)
                {
                    _values[r, c] = image.At(r, c);
                }

            // Choose spatial scale for kernel, so that image diagonal have length 2sqrt(2)
            // As squared distance is used as kernel param, then scale = 8 / (R^2+C^2)
            _spatialScale = 8.0 / (image.RowCount * image.RowCount + image.ColumnCount * image.ColumnCount);

            // For now omit border
            IntVector2 pixel = new IntVector2();
            for(int r = Radius; r < image.RowCount - Radius; ++r)
            {
                for(int c = Radius; c < image.ColumnCount - Radius; ++c)
                {
                    if(_converged[r, c] == false)
                    {
                        pixel.X = c;
                        pixel.Y = r;
                        double newMean = ComputeMean_GaussGauss_Gray(pixel);
                        if(Math.Abs(newMean - _values[r,c]) < ConvergenceThreshold)
                        {
                            _converged[r, c] = true;
                            continue;
                        }
                        _values[r, c] = newMean;
                    }
                }
            }

            // We should have converged local maximas in _values
            // Now we need to assign each pixel a cluster according to its maximum
            // For each pixel start flood
            _visited = new bool[image.RowCount, image.ColumnCount];
            Segments = new List<Segment>();
            SegmentAssignments = new int[image.RowCount, image.ColumnCount];
            for(int r = Radius; r < image.RowCount - Radius; ++r)
            {
                for(int c = Radius; c < image.ColumnCount - Radius; ++c)
                {
                    FloodFillSegments_Gray(r, c);
                }
            }
        }

        public double ComputeMean_GaussGauss_Gray(IntVector2 pixel)
        {
            double EK = 0.0;
            double EKx = 0.0;

            // K'(x,xi) = exp(-||xi-x||^2/h^2)
            // m(x) = E[K'(x,xi)*xi]/E[K'(x,xi)]
            int r = Radius;
            double h2i_v = 1.0 / (ValueBandwidth * ValueBandwidth);
            double h2i_s = 1.0 / (SpatialBandwidth * SpatialBandwidth);
            double ip = _values[pixel.Y, pixel.X];
            for(int y = pixel.Y - r; y <= pixel.Y + r; ++y)
            {
                for(int x = pixel.X - r; x <= pixel.X + r; ++x)
                {
                    double e_v = _kernelColor((ip - _values[y, x]) * (ip - _values[y, x]) * h2i_v);
                    double d2 = _spatialScale * ((pixel.X - x) * (pixel.X - x) + (pixel.Y - y) * (pixel.Y - y));
                    double e_s = _kernelSpatial(d2 * h2i_s);
                    EK += e_v * e_s;
                    EKx += e_v * e_s * _values[y, x];
                }
            }

            double m = EKx / EK;
            return m;
        }

        Stack<Point2D> _pointStack = new Stack<Point2D>();
        Segment_Gray _currentSegment_Gray;

        public void FloodFillSegments_Gray(int y, int x)
        {
            if(_visited[y, x])
                return;

            _visited[y, x] = true;
            _currentSegment_Gray = new Segment_Gray()
            {
                Value = _values[y, x],
                SegmentIndex = Segments.Count,
                Pixels = new List<Point2D>()
            };
            _currentSegment_Gray.Pixels.Add(new Point2D(x, y));
            SegmentAssignments[y, x] = _currentSegment_Gray.SegmentIndex;

            _pointStack.Push(new Point2D(x, y));
            while(_pointStack.Count > 0)
            {
                Point2D point = _pointStack.Pop();

                if(point.Y > Radius)
                {
                    CheckAndAddToSegment_Gray(point.X, point.Y, point.X, point.Y - 1);
                }
                if(point.Y < _values.GetLength(0) - Radius - 1)
                {
                    CheckAndAddToSegment_Gray(point.X, point.Y, point.X, point.Y + 1);
                }
                if(point.X > Radius)
                {
                    CheckAndAddToSegment_Gray(point.X, point.Y, point.X - 1, point.Y);
                }
                if(point.X < _values.GetLength(1) - Radius - 1)
                {
                    CheckAndAddToSegment_Gray(point.X, point.Y, point.X + 1, point.Y);
                }
            }

            Segments.Add(_currentSegment_Gray);
        }

        private void CheckAndAddToSegment_Gray(int oldX, int oldY, int newX, int newY)
        {
            if(_visited[newY, newX] == false &&
                Math.Abs(_currentSegment_Gray.Value - _values[newY, newX]) < MaxSegmentDeviation)
            {
                _visited[newY, newX] = true;
                SegmentAssignments[newY, newX] = _currentSegment_Gray.SegmentIndex;

                // Compute new segment mean ( m_new = (m_old * N + x) / N+1 )
                _currentSegment_Gray.Value = (_currentSegment_Gray.Value * _currentSegment_Gray.Pixels.Count + _values[newY, newX]) /
                    (_currentSegment_Gray.Pixels.Count + 1);

                _currentSegment_Gray.Pixels.Add(new Point2D(y: newY, x: newX));
                _pointStack.Push(new Point2D(y: newY, x: newX));
            }
        }

        #endregion
        #region SEGMENT_COLOR

        double[,] _red, _green, _blue;

        public override void SegmentColor(ColorImage image)
        {
            _converged = new bool[image.RowCount, image.ColumnCount]; // Filled with false
            _red = new double[image.RowCount, image.ColumnCount];
            _green = new double[image.RowCount, image.ColumnCount];
            _blue = new double[image.RowCount, image.ColumnCount];
            for(int r = 0; r < image.RowCount; ++r)
                for(int c = 0; c < image.ColumnCount; ++c)
                {
                    _red[r, c] = image[RGBChannel.Red].At(r, c);
                    _green[r, c] = image[RGBChannel.Green].At(r, c);
                    _blue[r, c] = image[RGBChannel.Blue].At(r, c);
                }


            // For now omit border
            IntVector2 pixel = new IntVector2();
            double red, green, blue;
            bool allConverged = false;
            int iteration = 0;
            while(allConverged == false)
            {
                ++iteration;
                allConverged = true;
                for(int r = Radius; r < image.RowCount - Radius; ++r)
                {
                    for(int c = Radius; c < image.ColumnCount - Radius; ++c)
                    {
                        if(_converged[r, c] == false)
                        {
                            allConverged = false;
                            pixel.X = c;
                            pixel.Y = r;
                            ComputeShift_Gauss_Color(pixel, out red, out green, out blue);
                            if(Math.Abs(red - _red[r, c]) < ConvergenceThreshold &&
                                Math.Abs(green - _green[r, c]) < ConvergenceThreshold &&
                                Math.Abs(blue - _blue[r, c]) < ConvergenceThreshold)
                            {
                                _converged[r, c] = true;
                                continue;
                            }
                            _red[r, c] = red;
                            _green[r, c] = green;
                            _blue[r, c] = blue;
                        }
                    }
                }
            }

            // We should have converged local maximas in _values
            // Now we need to assign each pixel a cluster according to its maximum
            // For each pixel start flood
            _visited = new bool[image.RowCount, image.ColumnCount];
            Segments = new List<Segment>();
            SegmentAssignments = new int[image.RowCount, image.ColumnCount];
            for(int r = Radius; r < image.RowCount - Radius; ++r)
            {
                for(int c = Radius; c < image.ColumnCount - Radius; ++c)
                {
                    FloodFillSegments_Color(r, c);
                }
            }
        }

        public void ComputeShift_Gauss_Color(IntVector2 pixel, out double red, out double green, out double blue)
        {
            double EK = 0.0;
            double EKxr = 0.0, EKxg = 0.0, EKxb = 0.0;

            // K'(x,xi) = exp(-||xi-x||^2/h^2)
            // m(x) = E[K'(x,xi)*xi]/E[K'(x,xi)]
            int r = Radius;
            double h2inv = 1.0 / (ValueBandwidth * ValueBandwidth);
            double ipr = _red[pixel.Y, pixel.X];
            double ipg = _green[pixel.Y, pixel.X];
            double ipb = _blue[pixel.Y, pixel.X];
            for(int y = pixel.Y - r; y <= pixel.Y + r; ++y)
            {
                for(int x = pixel.X - r; x <= pixel.X + r; ++x)
                {
                    double d2 = (ipr - _red[y, x]) * (ipr - _red[y, x]) +
                        (ipg - _green[y, x]) * (ipg - _green[y, x]) +
                        (ipb - _blue[y, x]) * (ipb - _blue[y, x]);
                    double e = Math.Exp(d2 * h2inv);
                    EK += e;
                    EKxr += e * _red[y, x];
                    EKxg += e * _green[y, x];
                    EKxb += e * _blue[y, x];
                }
            }

            red = EKxr / EK;
            green = EKxg / EK;
            blue = EKxb / EK;
        }

        Segment_Color _currentSegment_Color;
        public void FloodFillSegments_Color(int y, int x)
        {
            if(_visited[y, x])
                return;

            _visited[y, x] = true;
            _currentSegment_Color = new Segment_Color()
            {
                Red = _red[y, x],
                Green = _green[y, x],
                Blue = _blue[y, x],
                SegmentIndex = Segments.Count,
                Pixels = new List<Point2D>()
            };
            _currentSegment_Color.Pixels.Add(new Point2D(x, y));
            SegmentAssignments[y, x] = _currentSegment_Color.SegmentIndex;

            _pointStack.Push(new Point2D(x, y));
            while(_pointStack.Count > 0)
            {
                Point2D point = _pointStack.Pop();

                if(point.Y > Radius)
                {
                    CheckAndAddToSegment_Color(point.X, point.Y, point.X, point.Y - 1);
                }
                if(point.Y < _visited.GetLength(0) - Radius - 1)
                {
                    CheckAndAddToSegment_Color(point.X, point.Y, point.X, point.Y + 1);
                }
                if(point.X > Radius)
                {
                    CheckAndAddToSegment_Color(point.X, point.Y, point.X - 1, point.Y);
                }
                if(point.X < _visited.GetLength(1) - Radius - 1)
                {
                    CheckAndAddToSegment_Color(point.X, point.Y, point.X + 1, point.Y);
                }
            }

            Segments.Add(_currentSegment_Color);
        }

        private void CheckAndAddToSegment_Color(int oldX, int oldY, int newX, int newY)
        {
            if(_visited[newY, newX] == false &&
                Math.Abs(_currentSegment_Color.Red - _red[newY, newX]) < MaxSegmentDeviation &&
                Math.Abs(_currentSegment_Color.Green - _green[newY, newX]) < MaxSegmentDeviation &&
                Math.Abs(_currentSegment_Color.Blue - _blue[newY, newX]) < MaxSegmentDeviation)
            {
                _visited[newY, newX] = true;
                SegmentAssignments[newY, newX] = _currentSegment_Color.SegmentIndex;

                // Compute new segment mean ( m_new = (m_old * N + x) / N+1 )
                double N1 = 1.0 / (_currentSegment_Color.Pixels.Count + 1);
                _currentSegment_Color.Red = (_currentSegment_Color.Red * _currentSegment_Color.Pixels.Count + _red[newY, newX]) * N1;
                _currentSegment_Color.Green = (_currentSegment_Color.Green * _currentSegment_Color.Pixels.Count + _green[newY, newX]) * N1;
                _currentSegment_Color.Blue = (_currentSegment_Color.Blue * _currentSegment_Color.Pixels.Count + _blue[newY, newX]) * N1;

                _currentSegment_Color.Pixels.Add(new Point2D(y: newY, x: newX));
                _pointStack.Push(new Point2D(y: newY, x: newX));
            }
        }
        #endregion

        public override void SegmentDisparity(DisparityMap dispMap)
        {

        }

        public override void InitParameters()
        {
            base.InitParameters();
            IntParameter radiusParam = new IntParameter(
                "Window Radius", "RADIUS", 3, 1, 100);
            Parameters.Add(radiusParam);

            DictionaryParameter colorKernelParam = new DictionaryParameter(
                "Color Kernel Type", "KER_COL");

            colorKernelParam.ValuesMap = new Dictionary<string, object>()
            {
                { "Gaussian", KernelType.Gauss },
                { "Epanechnikov", KernelType.Epanechnikov }
            };

            Parameters.Add(colorKernelParam);

            DoubleParameter colorParam = new DoubleParameter(
                "Color Bandwith Coeff", "BAND_COL", 1.0, 0.0, 10000.0);
            Parameters.Add(colorParam);

            DictionaryParameter spatialKernelParam = new DictionaryParameter(
                "Spatial Kernel Type", "KER_SPT");

            spatialKernelParam.ValuesMap = new Dictionary<string, object>()
            {
                { "Gaussian", KernelType.Gauss },
                { "Epanechnikov", KernelType.Epanechnikov }
            };

            Parameters.Add(spatialKernelParam);

            DoubleParameter spatialParam = new DoubleParameter(
                "Spatial Bandwith Coeff", "BAND_SPT", 1.0, 0.0, 10000.0);
            Parameters.Add(spatialParam);

            DoubleParameter convParam = new DoubleParameter(
                "Convergence Threshold", "CONV", 0.001, 0.0, 1.0);
            Parameters.Add(convParam);

            DoubleParameter devParam = new DoubleParameter(
                "Max Value Deviation In Segment", "DEV", 0.01, 0.0, 1.0);
            Parameters.Add(devParam);
        }

        public override void UpdateParameters()
        {
            base.UpdateParameters();
            Radius = AlgorithmParameter.FindValue<int>("RADIUS", Parameters);
            ValueBandwidth = AlgorithmParameter.FindValue<double>("BAND_COL", Parameters);
            SpatialBandwidth = AlgorithmParameter.FindValue<double>("BAND_SPT", Parameters);
            ConvergenceThreshold = AlgorithmParameter.FindValue<double>("CONV", Parameters);
            MaxSegmentDeviation = AlgorithmParameter.FindValue<double>("DEV", Parameters);

            UsedColorKernel = AlgorithmParameter.FindValue<KernelType>("KER_COL", Parameters);
            UsedSpatialKernel = AlgorithmParameter.FindValue<KernelType>("KER_SPT", Parameters);
        }

        public override string Name
        {
            get
            {
                return "Mean-Shift Segmentation";
            }
        }
    }
}
