using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using Point2D = CamCore.Point2D<int>;

namespace CamImageProcessing
{
    public class FeatureSUSANDetector : FeaturesDetector
    {
        public override string Name
        {
            get
            {
                return "SUSAN Feature Detector";
            }
        }

        private double _t_intensity;
        private double _t_usan;
        //private bool _checkCenterDistance;
        //private bool _checkCenterDirection;
        private bool _nonMaxSup;
        private double _usanRadius = 3.4;
        private short _patchArea = 37;
        private double _t_isFeature;
        private short _borderSize = 4;

        private int[] _ybounds = new int[7]
        {
            1, 2, 3, 3, 3, 2, 1
        };

        public override void Detect()
        {
            Terminate = false;
            int ymax = Image.RowCount - _borderSize;
            int xmax = Image.ColumnCount - _borderSize;
            int x, y, dx, dy;
            double[,] usan = new double[7, 7];

            FeatureMap = new GrayScaleImage()
            {
                ImageMatrix = new DenseMatrix(Image.RowCount, Image.ColumnCount)
            };
            FeaturePoints = new List<IntVector2>();

            double response = 0.0f;
            // Look-up table for current threshold for fast assimiliance computing
            double[] assimilianceLUT = new double[512];
            for(int di = 0; di < 511; di++)
            {
                assimilianceLUT[di] = Math.Exp(-Math.Pow(
                    ((di - 255.0) / 255.0) / _t_intensity, 6));
            }
            int dymax;
            // For each point in image
            for(x = _borderSize; x < xmax; ++x)
            {
                CurrentPixel.X = x;
                for(y = _borderSize; y < ymax; ++y)
                {
                    if(Terminate) return;
                    CurrentPixel.Y = y;
                    if(Image.HaveValueAt(y, x))
                    {
                        response = 0.0;
                        // For each point in usan mask
                        for(dx = -3; dx <= 3; dx++)
                        {
                            dymax = _ybounds[dx + 3];
                            for(dy = -_ybounds[dx + 3]; dy <= dymax; ++dy)
                            {
                                // Find its usan assimiliance coeff
                                // usan[dy + 3, dx + 3] = (double)Math.Exp(
                                //    -(double)Math.Pow(((Image[y,x] - Image[y+dy,x+dx]) / _t_intensity), 6) );
                                // usan[dy + 3, dx + 3] = assimilianceLUT[(int)(Math.Abs(Image[y, x] - Image[y + dy, x + dx]) * 255)];
                                response += assimilianceLUT[(int)((Image[y, x] - Image[y + dy, x + dx]) * 255) + 256];
                            }
                        }
                        // Response >= threshold -> no feature
                        FeatureMap[y, x] = Math.Max(0.0, _t_isFeature - response);
                        if(!_nonMaxSup)
                            FeaturePoints.Add(new IntVector2(x, y));
                    }
                }
            }

            if(_nonMaxSup)
            {
                Matrix<double> resp = FeatureMap.ImageMatrix.Clone();
                //Non max suppresion
                for(x = _borderSize; x < xmax; ++x)
                {
                    CurrentPixel.X = x;
                    for(y = _borderSize; y < ymax; ++y)
                    {
                        if(Terminate) return;
                        CurrentPixel.Y = y;
                        if(FeatureMap[y, x] > 0.0f)
                        {
                            //if(!IsMaximum(resp, y, x, 2))
                            //{
                            //    FeatureMap[y, x] = 0.0f;
                            //}
                            //else
                            //    FeaturePoints.Add(new IntVector2(x, y));
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

            // Scale map so that max feature is 1
            FeatureMap.ImageMatrix.MultiplyThis(1.0 / FeatureMap.ImageMatrix.AbsoulteMaximum().Item3);

            return;
        }

        private bool IsMaximum(Matrix<double> response, int y, int x, int r)
        {
            int dx, dy;
            for(dx = -r; dx <= r; dx++)
            {
                for(dy = -r; dy <= r; ++dy)
                {
                    // Find its usan assimiliance coeff
                    if(response[y, x] < response[y + dy, x + dx])
                    {
                        return false;
                    }
                }
            }
            return true;
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

            AlgorithmParameter intensityThreshold = new DoubleParameter(
                "Intensity Difference Threshold", "TI", 0.05f, 0.0f, 1.0f);

            Parameters.Add(intensityThreshold);

            AlgorithmParameter susanTreshold = new DoubleParameter(
                "SUSAN Factor Threshold (e:0.5,c:0.75)", "TS", 0.75f, 0.0f, 1.0f);

            Parameters.Add(susanTreshold);

            AlgorithmParameter performNonmaxSupression = new BooleanParameter(
               "Perform NonMax Supression (Corners)", "PS", true);

            Parameters.Add(performNonmaxSupression);

            //AlgorithmParameter checkCenterDistance = new BooleanParameter(
            //    "Check Center Distance (Corners)", "CDist", false);

            //Parameters.Add(checkCenterDistance);

            //AlgorithmParameter checkCenterDirection = new BooleanParameter(
            //    "Check Center Direction (Corners)", "CDir", false);

            // Parameters.Add(checkCenterDirection);
        }

        public override void UpdateParameters()
        {
            _t_intensity = AlgorithmParameter.FindValue<double>("TI", Parameters);
            _t_usan = AlgorithmParameter.FindValue<double>("TS", Parameters);
            // _checkCenterDistance = (bool)(AlgorithmParameter.FindValue("CDist", Parameters));
            // _checkCenterDirection = (bool)(AlgorithmParameter.FindValue("CDir", Parameters));
            _nonMaxSup = AlgorithmParameter.FindValue<bool>("PS", Parameters);
            _t_isFeature = _t_usan * _patchArea;
        }
    }
}
