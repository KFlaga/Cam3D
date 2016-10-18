using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;

namespace CamImageProcessing
{
    public class FeatureSUSANDetector : FeaturesDetector
    {
        public override string Name { get { return "SUSAN Feature Detector"; } }

        private double _t_intensity;
        private double _t_usan;
        private bool _checkCenterDistance;
        private bool _checkCenterDirection;
        private bool _nonMaxSup;
        private double _usanRadius = 3.4;
        private short _patchArea = 37;
        private double _t_isFeature;
        private short _borderSize = 4;
        
        private int[] _ybounds = new int[7]
        {
            1, 2, 3, 3, 3, 2, 1
        };

        public override bool Detect()
        {
            int ymax = Image.SizeY - _borderSize;
            int xmax = Image.SizeX - _borderSize;
            int x, y, dx, dy;
            double[,] usan = new double[7, 7];

            FeatureMap = new GrayScaleImage()
            {
                ImageMatrix = new DenseMatrix(Image.SizeY, Image.SizeX)
            };

            double response = 0.0f;
            // Look-up table for current threshold for fast assimiliance computing
            double[] assimilianceLUT = new double[512];
            for(int di = 0; di < 511; di++)
            {
                assimilianceLUT[di] = (double)Math.Exp(
                                -Math.Pow( (((double)(di-255))/255.0f) / _t_intensity, 6) );
            }
            int dymax;
            // For each point in image
            for (x = _borderSize; x < xmax; ++x)
            {
                for (y = _borderSize; y < ymax; ++y)
                {
                    response = 0.0f;
                    // For each point in usan mask
                    for(dx = -3; dx <= 3; dx++ )
                    {
                        dymax = _ybounds[dx + 3];
                        for (dy = -_ybounds[dx+3]; dy <= dymax; ++dy)
                        {
                            // Find its usan assimiliance coeff
                            // usan[dy + 3, dx + 3] = (double)Math.Exp(
                            //    -(double)Math.Pow(((Image[y,x] - Image[y+dy,x+dx]) / _t_intensity), 6) );
                           // usan[dy + 3, dx + 3] = assimilianceLUT[(int)(Math.Abs(Image[y, x] - Image[y + dy, x + dx]) * 255)];
                            response += assimilianceLUT[(int)((Image[y, x] - Image[y + dy, x + dx]) * 255) + 256];
                        }
                    }
                    // Response <= threshold -> no feature
                    FeatureMap[y, x] = (double)Math.Max(0.0, -_t_isFeature + response);
                }
            }

            if (_nonMaxSup)
            {
                Matrix<double> resp = FeatureMap.ImageMatrix.Clone();
                //Non max suppresion
                for (x = _borderSize; x < xmax; ++x)
                {
                    for (y = _borderSize; y < ymax; ++y)
                    {
                        if (FeatureMap[y, x] > 0.0f)
                            if(!IsMaximum(resp, y, x, 2))
                            {
                                FeatureMap[y, x] = 0.0f;
                            }
                    }
                }
            }
            
            return true;
        }

        private bool IsMaximum(Matrix<double> response, int y, int x, int r)
        {
            int dx, dy;
            for (dx = -r; dx <= r; dx++)
            {
                for (dy = -r; dy <= r; ++dy)
                {
                    // Find its usan assimiliance coeff
                    if (response[y, x] < response[y + dy, x + dx])
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        
        public override void InitParameters()
        {
            Parameters = new List<AlgorithmParameter>();

            AlgorithmParameter intensityThreshold = new DoubleParameter(
                "Intensity Difference Threshold", "TI", 0.05f, 0.0f, 1.0f);

            Parameters.Add(intensityThreshold);

            AlgorithmParameter susanTreshold = new DoubleParameter(
                "SUSAN Factor Threshold", "TS", 0.5f, 0.0f, 1.0f);

            Parameters.Add(susanTreshold);

            AlgorithmParameter performNonmaxSupression = new BooleanParameter(
               "Perform NonMax Supression (Corners)", "PS", false);

            Parameters.Add(performNonmaxSupression);

            AlgorithmParameter checkCenterDistance = new BooleanParameter(
                "Check Center Distance (Corners)", "CDist", false);

            Parameters.Add(checkCenterDistance);

            AlgorithmParameter checkCenterDirection = new BooleanParameter(
                "Check Center Direction (Corners)", "CDir", false);

            Parameters.Add(checkCenterDirection);
        }

        public override void UpdateParameters()
        {
            _t_intensity = (float)(AlgorithmParameter.FindValue("TI", Parameters));
            _t_usan = (float)(AlgorithmParameter.FindValue("TS", Parameters));
            _checkCenterDistance = (bool)(AlgorithmParameter.FindValue("CDist", Parameters));
            _checkCenterDirection = (bool)(AlgorithmParameter.FindValue("CDir", Parameters));
            _nonMaxSup = (bool)(AlgorithmParameter.FindValue("PS", Parameters));
            _t_isFeature = _t_usan * _patchArea;
        }
    }
}
