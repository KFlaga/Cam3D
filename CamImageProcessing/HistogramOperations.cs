using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamImageProcessing
{
    public static class HistogramOperations
    {
        public const double ColorStep = 1.0 / 255.0;
        public const double ColorsRange = 255.0;

        public static int[] ComputeHistogram(Matrix<double> image)
        {
            int[] histogram = new int[256];
            
            for(int c = 0; c < image.ColumnCount; ++c)
            {
                for(int r = 0; r < image.RowCount; ++r)
                {
                    histogram[(int)(image[r, c] * ColorStep)] += 1;
                }
            }

            return histogram;
        }

        public static double[] ComputeHistogramNormalised(Matrix<double> image)
        {
            double[] histogram = new double[256];
            double pointNormal = 1.0f / (image.RowCount * image.ColumnCount);

            for(int c = 0; c < image.ColumnCount; ++c)
            {
                for(int r = 0; r < image.RowCount; ++r)
                {
                    histogram[(int)(image[r, c] * ColorStep)] += pointNormal;
                }
            }

            return histogram;
        }

        // Stretches histogram of given image matrix to range [0-1]
        public static void StretchHistogram(Matrix<double> image)
        {
            double min = image[0, 0];
            double max = image[0, 0];

            for(int c = 0; c < image.ColumnCount; ++c)
            {
                for(int r = 0; r < image.RowCount; ++r)
                {
                    min = Math.Min(min, image[r, c]);
                    max = Math.Max(max, image[r, c]);
                }
            }
            
            for(int c = 0; c < image.ColumnCount; ++c)
            {
                for(int r = 0; r < image.RowCount; ++r)
                {
                    image[r, c] = (image[r, c] - min) / (max - min);
                }
            }
        }

        // Equalizes histogram of given image matrix
        public static void EqualizeHistogram(Matrix<double> image)
        {
            var histogram = ComputeHistogramNormalised(image);
            
            double[] lut = new double[256];

            int firstNonZero = 0;
            for(firstNonZero = 0; firstNonZero < 256; ++firstNonZero)
            {
                if(histogram[firstNonZero] > 0.0f)
                    break;
            }

            double cp0 = histogram[firstNonZero];
            double cpi = 0.0f;
            for(int i = firstNonZero; i < 256; ++i)
            {
                cpi = cpi + histogram[i];
                lut[i] = (cpi - cp0) * ColorsRange / (1 - cp0);
            }
            
            for(int c = 0; c < image.ColumnCount; ++c)
            {
                for(int r = 0; r < image.RowCount; ++r)
                {
                    image[r, c] = lut[(int)(image[r, c] / ColorStep)];
                }
            }
        }
        
        // Saturates all values outside [treshLow, treshHigh]
        // Streches rest of values
        public static void SaturateHistogram(Matrix<double> image, double treshLow, double treshHigh)
        {
            double[] lut = new double[256];
            
            for(int i = 0; i < 256; ++i)
            {
                lut[i] = i * ColorStep < treshLow ?
                    0.0f : (i * ColorStep > treshHigh ?
                    1.0f : (i - treshLow) / (treshHigh - treshLow));
            }

            for(int c = 0; c < image.ColumnCount; ++c)
            {
                for(int r = 0; r < image.RowCount; ++r)
                {
                    image[r, c] = lut[(int)(image[r, c] * ColorsRange)];
                }
            }
        }

        // Saturates all values outside [treshLow, treshHigh] where treshLow/High is level where ratioLow/High 
        // if pixels have lower/higher values
        // Streches rest of values
        public static void SaturateRatioHistogram(Matrix<double> image, double ratioLow, double ratioHigh)
        {
            // 1) Find tresholds -> compute CP
            var histogram = ComputeHistogramNormalised(image);
            double tl = 0.0f, th = 0.0f;
            
            double cp = 0.0f;
            int i = 0;
            for(; i < 256; ++i)
            {
                cp += histogram[i];
                if(cp > ratioLow)
                    break;
            }

            tl = i * ColorStep;
            for(i = 255; i >= 0; --i)
            {
                cp += histogram[i];
                if(cp > ratioHigh)
                    break;
            }
            th = i * ColorStep;

            SaturateHistogram(image, tl, th);
        }

        public static void StretchHistogram(this GrayScaleImage image)
        {
            StretchHistogram(image.ImageMatrix);
        }

        public static void EqualizeHistogram(this GrayScaleImage image)
        {
            EqualizeHistogram(image.ImageMatrix);
        }

        // Saturates all values outside [treshLow, treshHigh]
        // Streches rest of values
        public static void SaturateHistogram(this GrayScaleImage image, double treshLow, double treshHigh)
        {
            SaturateHistogram(image.ImageMatrix, treshLow, treshHigh);
        }

        // Saturates all values outside [treshLow, treshHigh] where treshLow/High is level where ratioLow/High 
        // if pixels have lower/higher values
        // Streches rest of values
        public static void SaturateRatioHistogram(this GrayScaleImage image, double ratioLow, double ratioHigh)
        {
            SaturateRatioHistogram(image.ImageMatrix, ratioLow, ratioHigh);
        }

        public static void StretchHistogram(this HSIImage image)
        {
            StretchHistogram(image[HSIChannel.Intensity]);
        }

        public static void EqualizeHistogram(this HSIImage image)
        {
            EqualizeHistogram(image[HSIChannel.Intensity]);
        }

        // Saturates all values outside [treshLow, treshHigh]
        // Streches rest of values
        public static void SaturateHistogram(this HSIImage image, double treshLow, double treshHigh)
        {
            SaturateHistogram(image[HSIChannel.Intensity], treshLow, treshHigh);
        }

        // Saturates all values outside [treshLow, treshHigh] where treshLow/High is level where ratioLow/High 
        // if pixels have lower/higher values
        // Streches rest of values
        public static void SaturateRatioHistogram(this HSIImage image, double ratioLow, double ratioHigh)
        {
            SaturateRatioHistogram(image[HSIChannel.Intensity], ratioLow, ratioHigh);
        }
    }

    public class HistogramStretcher
    {
        public void StretchHistogram(Matrix<double> image)
        {
            HistogramOperations.StretchHistogram(image);
        }

        public void StretchHistogram(GrayScaleImage image)
        {
            HistogramOperations.StretchHistogram(image);
        }

        public void StretchHistogram(HSIImage image)
        {
            HistogramOperations.StretchHistogram(image);
        }
    }

    public class HistogramEqualizer
    {
        public void EqualizeHistogram(Matrix<double> image)
        {
            HistogramOperations.EqualizeHistogram(image);
        }

        public void EqualizeHistogram(GrayScaleImage image)
        {
            HistogramOperations.EqualizeHistogram(image);
        }

        public void EqualizeHistogram(HSIImage image)
        {
            HistogramOperations.EqualizeHistogram(image);
        }
    }

    public class HistogramSaturator : IParametrizedProcessor
    {
        private List<ProcessorParameter> _parameters;
        public List<ProcessorParameter> Parameters
        {
            get { return _parameters; }
            protected set { _parameters = value; }
        }

        public void InitParameters()
        {
            Parameters = new List<ProcessorParameter>();
            ProcessorParameter tl = new ProcessorParameter(
                "Saturate Level Low", "SLL",
                "System.Single", 0.2f, 0.0f, 1.0f);

            Parameters.Add(tl);

            ProcessorParameter th = new ProcessorParameter(
                "Saturate Level High", "SLH",
                "System.Single", 0.8f, 0.0f, 1.0f);

            Parameters.Add(th);

            ProcessorParameter rl = new ProcessorParameter(
                "Saturate Ratio Low", "SRL",
                "System.Single", 0.2f, 0.0f, 1.0f);

            Parameters.Add(rl);

            ProcessorParameter rh = new ProcessorParameter(
                "Saturate Ratio High", "SRH",
                "System.Single", 0.8f, 0.0f, 1.0f);

            Parameters.Add(rh);
        }

        public void UpdateParameters()
        {
            SaturateLow = (float)ProcessorParameter.FindValue("SLL", Parameters);
            SaturateHigh = (float)ProcessorParameter.FindValue("SLH", Parameters);
            SaturateRatioLow = (float)ProcessorParameter.FindValue("SRL", Parameters);
            SaturateRatioHigh = (float)ProcessorParameter.FindValue("SRH", Parameters);
        }

        public double SaturateLow { get; set; }
        public double SaturateHigh { get; set; }
        public double SaturateRatioLow { get; set; }
        public double SaturateRatioHigh { get; set; }

        public void SaturateHistogram(Matrix<double> image)
        {
            HistogramOperations.SaturateHistogram(image, SaturateLow, SaturateHigh);
        }

        public void SaturateHistogram(GrayScaleImage image)
        {
            HistogramOperations.SaturateHistogram(image, SaturateLow, SaturateHigh);
        }

        public void SaturateHistogram(HSIImage image)
        {
            HistogramOperations.SaturateHistogram(image, SaturateLow, SaturateHigh);
        }

        public void SaturateRatioHistogram(Matrix<double> image)
        {
            HistogramOperations.SaturateRatioHistogram(image, SaturateRatioLow, SaturateRatioHigh);
        }

        public void SaturateRatioHistogram(GrayScaleImage image)
        {
            HistogramOperations.SaturateRatioHistogram(image, SaturateRatioLow, SaturateRatioHigh);
        }

        public void SaturateRatioHistogram(HSIImage image)
        {
            HistogramOperations.SaturateRatioHistogram(image, SaturateRatioLow, SaturateRatioHigh);
        }
    }
}
