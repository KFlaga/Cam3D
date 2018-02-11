using CamCore;
using System;

namespace CamAlgorithms.ImageMatching
{
    public class NormalisedCrossCorrelationCostComputer : MatchingCostComputer
    {
        public int MaskWidth { get; set; } // Actual width is equal to MaskWidth*2 + 1
        public int MaskHeight { get; set; } // Actual height is equal to MaskWidth*2 + 1
        private int _maskSize;

        public bool UseSmoothing { get; set; }
        public double SmoothingSgm { get; set; }

        public override double GetCost(IntVector2 pixelBase, IntVector2 pixelMatched)
        {
            double corr = 0.0f;
            if(UseSmoothing == false)
            {
                double sqLenRef = 0.0f, sqLenTest = 0.0f;
                for(int dx = -MaskWidth; dx <= MaskWidth; ++dx)
                {
                    for(int dy = -MaskHeight; dy <= MaskHeight; ++dx)
                    {
                        if(ImageBase.HaveValueAt(pixelBase.Y + dy, pixelBase.X + dx) &&
                            ImageBase.HaveValueAt(pixelMatched.Y + dy, pixelMatched.X + dx))
                        {
                            double pr = ImageBase[pixelBase.Y + dy, pixelBase.X + dx];
                            double pt = ImageMatched[pixelMatched.Y + dy, pixelMatched.X + dx];
                            corr += pr * pt;
                            sqLenRef += pr * pr;
                            sqLenTest += pt * pt;
                        }
                    }
                }

                corr /= Math.Sqrt(sqLenRef * sqLenTest);
            }
            else
            {
                //% Correlation: c = r / sqrt(dev_r^2 * dev_t^2)
                //% r = sum { G(y, x) * Pr(y +y0/2, x + x0/2) * Pt(y +y0/2, x + x0/2) }
                //% sqdev_r = sum{ G(y, x) * Pr(y + y0/2, x + x0/2)^2 }
                double gauss = 0.0f;
                double sqDevRef = 0.0f, sqDevTest = 0.0f;
                double sgm2 = 2 * SmoothingSgm * SmoothingSgm;
                double norm_coeff = 1 / (SmoothingSgm * (double)Math.Sqrt(2 * Math.PI));
                for(int dx = -MaskWidth; dx <= MaskWidth; ++dx)
                {
                    for(int dy = -MaskHeight; dy <= MaskHeight; ++dx)
                    {

                        if(ImageBase.HaveValueAt(pixelBase.Y + dy, pixelBase.X + dx) &&
                            ImageBase.HaveValueAt(pixelMatched.Y + dy, pixelMatched.X + dx))
                        {
                            double pr = ImageBase[pixelBase.Y + dy, pixelBase.X + dx];
                            double pt = ImageMatched[pixelMatched.Y + dy, pixelMatched.X + dx];
                            gauss = Math.Exp(
                                ((dx - MaskWidth) * (dx - MaskWidth) + (dy - MaskHeight) * (dy - MaskHeight)) / sgm2) * norm_coeff;
                            corr += gauss * pr * pt;
                            sqDevRef += gauss * pr * pr;
                            sqDevTest += gauss * pt * pt;
                        }
                    }
                }

                corr /= Math.Sqrt(sqDevRef * sqDevTest);
            }
            // Return inverse as we need smaller cost for better match
            // And cost = 0 for perfect match, so substract 1.0, as corr = 1 is best fit
            // Also bound value to some big value
            corr = 1.0 / corr;
            corr = corr > _maskSize ? _maskSize : corr - 1.0;
            return corr;
        }

        public override double GetCost_Border(IntVector2 pixelBase, IntVector2 pixelMatched)
        {
            int px_b, px_m, py_b, py_m;
            double corr = 0.0f;
            if(UseSmoothing == false)
            {
                double sqLenRef = 0.0f, sqLenTest = 0.0f;
                for(int dx = -MaskWidth; dx <= MaskWidth; ++dx)
                {
                    for(int dy = -MaskHeight; dy <= MaskHeight; ++dx)
                    {
                        px_b = Math.Max(0, Math.Min(ImageBase.ColumnCount - 1, pixelBase.X + dx));
                        py_b = Math.Max(0, Math.Min(ImageBase.RowCount - 1, pixelMatched.Y + dy));
                        px_m = Math.Max(0, Math.Min(ImageMatched.ColumnCount - 1, pixelBase.X + dx));
                        py_m = Math.Max(0, Math.Min(ImageMatched.RowCount - 1, pixelMatched.Y + dy));

                        if(ImageBase.HaveValueAt(py_b, px_b) &&
                            ImageBase.HaveValueAt(py_m, px_m))
                        {
                            double pr = ImageBase[py_b, px_b];
                            double pt = ImageMatched[py_m, px_m];
                            corr += pr * pt;
                            sqLenRef += pr * pr;
                            sqLenTest += pt * pt;
                        }
                    }
                }

                corr /= Math.Sqrt(sqLenRef * sqLenTest);
            }
            else
            {
                //% Correlation: c = r / sqrt(dev_r^2 * dev_t^2)
                //% r = sum { G(y, x) * Pr(y +y0/2, x + x0/2) * Pt(y +y0/2, x + x0/2) }
                //% sqdev_r = sum{ G(y, x) * Pr(y + y0/2, x + x0/2)^2 }
                double gauss = 0.0f;
                double sqDevRef = 0.0f, sqDevTest = 0.0f;
                double sgm2 = 2 * SmoothingSgm * SmoothingSgm;
                double norm_coeff = 1 / (SmoothingSgm * (double)Math.Sqrt(2 * Math.PI));
                for(int dx = -MaskWidth; dx <= MaskWidth; ++dx)
                {
                    for(int dy = -MaskHeight; dy <= MaskHeight; ++dx)
                    {
                        px_b = Math.Max(0, Math.Min(ImageBase.ColumnCount - 1, pixelBase.X + dx));
                        py_b = Math.Max(0, Math.Min(ImageBase.RowCount - 1, pixelMatched.Y + dy));
                        px_m = Math.Max(0, Math.Min(ImageMatched.ColumnCount - 1, pixelBase.X + dx));
                        py_m = Math.Max(0, Math.Min(ImageMatched.RowCount - 1, pixelMatched.Y + dy));

                        if(ImageBase.HaveValueAt(py_b, px_b) &&
                            ImageBase.HaveValueAt(py_m, px_m))
                        {
                            double pr = ImageBase[py_b, px_b];
                            double pt = ImageMatched[py_m, px_m];
                            gauss = Math.Exp(
                                ((dx - MaskWidth) * (dx - MaskWidth) + (dy - MaskHeight) * (dy - MaskHeight)) / sgm2) * norm_coeff;
                            corr += gauss * pr * pt;
                            sqDevRef += gauss * pr * pr;
                            sqDevTest += gauss * pt * pt;
                        }
                    }
                }

                corr /= Math.Sqrt(sqDevRef * sqDevTest);
            }
            return corr;
        }

        public override void Init()
        {
            _maskSize = (2 * MaskHeight + 1) * (2 * MaskWidth + 1);
            // Cost is equal to 1/corr - 1
            // Max corr is 1, so min cost is 0
            // As min corr is 0, then max cost is inf, but limit it to mask size (which is quite
            MaxCost = _maskSize;
        }

        public override void Update()
        {
            // Correlation needs no updates
        }


        public override string Name
        {
            get
            {
                return "CrossCorelation Cost Computer";
            }
        }

        public override void InitParameters()
        {
            base.InitParameters();
            IAlgorithmParameter maskW = new IntParameter(
                "Mask Width Radius", "MWR", 3, 1, 6);
            _parameters.Add(maskW);

            IAlgorithmParameter maskH = new IntParameter(
                "Mask Height Radius", "MHR", 3, 1, 6);
            _parameters.Add(maskH);

            IAlgorithmParameter useSmooth = new BooleanParameter(
                "Use gaussian smoothing", "UGS", false);
            _parameters.Add(useSmooth);

            IAlgorithmParameter smootSgm = new DoubleParameter(
                "Smoothing Deviation", "SD", 1.2, 0.01, 10.0);
            _parameters.Add(smootSgm);
        }

        public override void UpdateParameters()
        {
            base.UpdateParameters();
            MaskWidth = IAlgorithmParameter.FindValue<int>("MWR", Parameters);
            MaskHeight = IAlgorithmParameter.FindValue<int>("MHR", Parameters);
            UseSmoothing = IAlgorithmParameter.FindValue<bool>("UGS", Parameters);
            SmoothingSgm = IAlgorithmParameter.FindValue<double>("SD", Parameters);
        }
    }
}
