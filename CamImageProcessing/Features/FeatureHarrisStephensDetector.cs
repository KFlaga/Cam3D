using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using Point2D = CamCore.Point2D<int>;

namespace CamAlgorithms
{
    public class FeatureHarrisStephensDetector : FeaturesDetector
    {
        public int WindowRadius { get; set; }
        public double Variance { get; set; }
        public double TraceCoeff { get; set; } = 0.04;
        public double TreshCorner { get; set; }

        public override string Name
        {
            get
            {
                return "Harris-Stephens Feature Detector";
            }
        }

        private int[] _ybounds;

        public override void Detect()
        {
            Terminate = false;
            int ymax = Image.RowCount - WindowRadius;
            int xmax = Image.ColumnCount - WindowRadius;
            int x, y, dx, dy;

            FeatureMap = new GrayScaleImage()
            {
                ImageMatrix = new DenseMatrix(Image.RowCount, Image.ColumnCount)
            };
            FeaturePoints = new List<IntVector2>();

            double response = 0.0;
            int r21 = 2 * WindowRadius + 1;

            // Find circural mask bounds:
            // for x = [-r,r] -> y = [ -sqrt(r^2 - x^2), sqrt(r^2 - x^2) ]
            _ybounds = new int[r21];
            for(x = -WindowRadius; x <= WindowRadius; ++x)
            {
                _ybounds[x + WindowRadius] = (int)Math.Sqrt(WindowRadius * WindowRadius - x * x);
            }

            // Gaussian weights LUT
            double[,] gaussLUT = new double[r21, r21];
            double coeff = 1.0 / (2.0 * Variance);
            for(x = -WindowRadius; x <= WindowRadius; ++x)
            {
                for(y = -_ybounds[x + WindowRadius]; y <= _ybounds[x + WindowRadius]; ++y)
                {
                    gaussLUT[x + WindowRadius, y + WindowRadius] = Math.Exp(-((y * y + x * x) * coeff));
                }
            }

            // We need gradients in x and y directions : D(x,y) = [D(x+1,y)-D(x-1,y) D(x,y+1)-D(x,y-1)]^T
            Matrix<double> gradX = new DenseMatrix(Image.RowCount, Image.ColumnCount);
            Matrix<double> gradY = new DenseMatrix(Image.RowCount, Image.ColumnCount);

            for(x = 1; x < Image.ColumnCount - 1; ++x)
            {
                for(y = 1; y < Image.RowCount - 1; ++y)
                {
                    gradX.At(y, x, Image[y, x + 1] - Image[y, x - 1]);
                    gradY.At(y, x, Image[y + 1, x] - Image[y - 1, x]);
                }
            }

            double m00 = 0.0, m11 = 0.0, m01 = 0.0;
            // For each point in image
            for(x = WindowRadius; x < xmax; ++x)
            {
                CurrentPixel.X = x;
                for(y = WindowRadius; y < ymax; ++y)
                {
                    if(Terminate) return;
                    CurrentPixel.Y = y;
                    if(Image.HaveValueAt(y, x))
                    {
                        // M(x,y) = sum{in window}(w(u,v)[gx(u,v)^2 gx(u,v)gy(u,v); gy(u,v)gx(u,v) gy(u,v)^2]
                        // R = det(M) - k*(trace(M))^2 (k = 0.04)
                        // det(M) = M11*M22 - M12*M21, trace = M11 + M22
                        // if R >> 0 then point is corner
                        m00 = 0.0; m01 = 0.0; m11 = 0.0;
                        for(dx = -WindowRadius; dx <= WindowRadius; ++dx)
                        {
                            for(dy = -_ybounds[dx + WindowRadius]; dy <= _ybounds[dx + WindowRadius]; ++dy)
                            {
                                double w = gaussLUT[dx + WindowRadius, dy + WindowRadius];
                                m00 += w * gradX[y + dy, x + dx] * gradX[y + dy, x + dx];
                                m01 += w * gradX[y + dy, x + dx] * gradY[y + dy, x + dx];
                                m11 += w * gradY[y + dy, x + dx] * gradY[y + dy, x + dx];
                            }
                        }

                        double det = m00 * m11 - m01 * m01;
                        double trace = m00 + m11;
                        response = det - TraceCoeff * trace * trace;
                        FeatureMap[y, x] = response;
                    }
                }
            }

            ScaleMap();

            for(x = 0; x < Image.ColumnCount; ++x)
            {
                for(y = 0; y < Image.RowCount; ++y)
                {
                    if(FeatureMap[y, x] > 0.0)
                    {
                        FloodFindMaximum(y, x);
                    }
                }
            }

            for(x = 0; x < Image.ColumnCount; ++x)
            {
                for(y = 0; y < Image.RowCount; ++y)
                {
                    if(FeatureMap[y, x] > 0.0)
                    {
                        FeaturePoints.Add(new IntVector2(x, y));
                    }
                }
            }
        }

        void ScaleMap()
        {
            double maxr = 0.0;
            for(int x = 0; x < Image.ColumnCount; ++x)
                for(int y = 0; y < Image.RowCount; ++y)
                    maxr = Math.Max(FeatureMap[y, x], maxr);

            for(int x = 0; x < Image.ColumnCount; ++x)
            {
                for(int y = 0; y < Image.RowCount; ++y)
                {
                    if(FeatureMap[y, x] > TreshCorner)
                        FeatureMap[y, x] = FeatureMap[y, x] / maxr;
                    else
                        FeatureMap[y, x] = 0.0;
                }
            }
        }
        
        Stack<Point2D> _pointStack;
        double _t_seg = 0.1;
        public void FloodFindMaximum(int y, int x)
        {
            _pointStack = new Stack<Point2D<int>>();
            _pointStack.Push(new Point2D(x, y));

            Point2D maxp = new Point2D(y, x);
            double max = FeatureMap[y, x];

            while(_pointStack.Count > 0)
            {
                Point2D point = _pointStack.Pop();
                double f = FeatureMap[point.Y, point.X];
                if(f >= max)
                {
                    maxp = point;
                    max = f;
                }
                else
                    FeatureMap[point.Y, point.X] = 0.0;

                if(point.Y > 0 && 
                    FeatureMap[point.Y - 1, point.X] > 0.0)
                {
                    _pointStack.Push(new Point2D(y: point.Y - 1, x: point.X));
                }
                if(point.Y + 1 < Image.RowCount &&
                    FeatureMap[point.Y + 1, point.X] > 0.0)
                {
                    _pointStack.Push(new Point2D(y: point.Y + 1, x: point.X));
                }
                if(point.X > 0 &&
                    FeatureMap[point.Y, point.X - 1] > 0.0)
                {
                    _pointStack.Push(new Point2D(y: point.Y, x: point.X - 1));
                }
                if(point.X + 1 < Image.ColumnCount &&
                    FeatureMap[point.Y, point.X + 1] > 0.0)
                {
                    _pointStack.Push(new Point2D(y: point.Y, x: point.X + 1));
                }
            }
        }

        public override void InitParameters()
        {
            base.InitParameters();

            AlgorithmParameter windowRadiusParam = new IntParameter(
                "Gauss Window Radius", "WRAD", 3, 1, 10);
            Parameters.Add(windowRadiusParam);

            AlgorithmParameter traceCoeffParam = new DoubleParameter(
                "Trace Coeff", "TRCOEFF", 0.04, 0.0, 0.25);
            Parameters.Add(traceCoeffParam);

            AlgorithmParameter treshCornerParam = new DoubleParameter(
                "Corner Threshold", "CORT", 0.1, 0.001, 2.0);
            Parameters.Add(treshCornerParam);
        }

        public override void UpdateParameters()
        {
            WindowRadius = AlgorithmParameter.FindValue<int>("WRAD", Parameters);
            TraceCoeff = AlgorithmParameter.FindValue<double>("TRCOEFF", Parameters);
            TreshCorner = AlgorithmParameter.FindValue<double>("CORT", Parameters);

            Variance = WindowRadius * WindowRadius * 0.25; // r = 2 * sgm => sgm^2 = r^2 / 4
        }
    }
}
