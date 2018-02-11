using CamCore;
using System;

namespace CamAlgorithms.ImageMatching
{
    public class AnisotopicDiffusionRefiner : DisparityRefinement
    {
        public enum CoeffKernelType
        {
            Exponential,
            Rational,
            Constant
        }

        delegate double CoeffFunction(double grad);
        CoeffFunction _coeff;

        CoeffKernelType _kernelType;
        public CoeffKernelType KernelType
        {
            get { return _kernelType; }
            set
            {
                _kernelType = value;
                switch(value)
                {
                    case CoeffKernelType.Exponential: _coeff = GetCoeffValue_Exponental; break;
                    case CoeffKernelType.Rational: _coeff = GetCoeffValue_Rational; break;
                    case CoeffKernelType.Constant:
                    default:
                        _coeff = GetCoeffValue_Constant;
                        break;
                }
            }
        }

        public int Iterations { get; set; } = 10;
        public double KernelCoeff { get; set; }
        public double StepCoeff { get; set; } // Actual coeff used is StepCoeff * (1/_dirs)
                                              // public bool UseExtendedMethod { get; set; }
        public bool UseEightDirections { get; set; }

        public bool SmoothDisparityMap { get; set; } // When set smooths disparities in disparity-coherent areas
        public bool InterpolateDisparityMap { get; set; } // When set interpolates bad disparities
                                                          // from more confident one within color-coherent areas

        double[,] visitCounter; // Counter to find if field could be marked valid ->
                                // invlid field becomes valid if it was visited from valid field
                                // at least 4 / step_coeff times

        IntVector2[] _dirs4 = new IntVector2[4]
        {
            new IntVector2(0, -1),
            new IntVector2(-1, 0),
            new IntVector2(1, 0),
            new IntVector2(0, 1),
        };

        IntVector2[] _dirs8 = new IntVector2[8]
        {
            new IntVector2(-1, -1),
            new IntVector2(0, -1),
            new IntVector2(1, -1),
            new IntVector2(-1, 0),
            new IntVector2(1, 0),
            new IntVector2(-1, 1),
            new IntVector2(0, 1),
            new IntVector2(1, 1),
        };

        double[] _dinv4 = new double[4]
        {
            1.0, 1.0, 1.0, 1.0
        };

        double[] _dinv8 = new double[8]
        {
            0.5, 1.0, 0.5, 1.0, 1.0, 0.5, 1.0, 0.5
        };

        private DisparityMap _next;
        private DisparityMap _last;
        private DisparityMap _initial;
        private IntVector2[] _dirs;
        private double[] _dinv;
        private double _r;

        public DisparityMap RefineMap(DisparityMap map, IImage img)
        {
            // ∂I(x,y)/∂t = div(c(x,y,t) · ∇I(x,y)) = ∇c*∇I + c*(∇^2)I
            // I(x, y) is image, t is time (iterations)
            // c(||∇I||) = exp(-(||∇I||/K)^2) or 1/(1+(||∇I||/K)^2)
            // we have solution:
            // I(t+1, x, y) = I(t, x, y) + _r( sum{dir}(c_dir*∇_dir I(t, x, y)))
            // where c_dir/∇_dir are coeff/gradient in direction
            // _r = [0, 1/_dirs]
            // c_dir for dir (i,j) = c(||I(x,y) - I(x+i,y+j)||)
            // ∇_dir for dir (i,j) =  I(x,y) - I(x+i,y+j)

            _next = (DisparityMap)map.Clone();
            _last = (DisparityMap)map.Clone();
            _initial = (DisparityMap)map.Clone();
            _dirs = UseEightDirections ? _dirs8 : _dirs4;
            _dinv = UseEightDirections ? _dinv8 : _dinv4;

            visitCounter = new double[map.RowCount, map.ColumnCount];
            for(int t = 0; t < Iterations; ++t)
            {
                Iterate(map, img);

                // Exchange _last and _next (I(t) will be overriden by I(t+2), I(t+1) in _last)
                var temp = _last;
                _last = _next;
                _next = temp;

                if(SmoothDisparityMap == false)
                {
                    for(int x = 1; x < map.ColumnCount - 1; ++x)
                    {
                        for(int y = 1; y < map.RowCount - 1; ++y)
                        {
                            if(_last[y, x].IsInvalid() && visitCounter[y, x] > (4 + Iterations * 0.3) / StepCoeff)
                            {
                                _last[y, x].Flags = (int)DisparityFlags.Valid;
                            }
                        }
                    }
                }
            }
            return _last;
        }

        private void Iterate(DisparityMap map, IImage img)
        {
            for(int x = 1; x < map.ColumnCount - 1; ++x)
            {
                for(int y = 1; y < map.RowCount - 1; ++y)
                {
                    double dispX = 0.0;

                    //double cxsum = 0, cysum = 0;

                    double weight = 0;
                    // Add c_dir*∇_dir I(t, x, y) to val
                    for(int i = 0; i < _dirs.Length; ++i)
                    {
                        double w;
                        dispX += GetDirectionalCoeff(img, x, y, i, out w);
                        weight += w;
                    }

                    if(weight > 0)
                    {
                        dispX *= StepCoeff / weight; // get _r( sum{dir}(c_dir*∇_dir I(t, x, y))
                        visitCounter[y, x] += weight;
                    }
                    dispX += _last[y, x].SubDX; // get I(t, x, y) + _r( sum{dir}(c_dir*∇_dir I(t, x, y)))
                    UpdateDisparity(x, y, dispX, weight);
                }
            }
        }

        private double GetDirectionalCoeff(IImage img, int x, int y, int i, out double weight)
        {
            double gradDispX = _last[y + _dirs[i].Y, x + _dirs[i].X].SubDX - _last[y, x].SubDX;
            double gradImg = img[y + _dirs[i].Y, x + _dirs[i].X] - img[y, x];
            double cx;

            if(SmoothDisparityMap)
            {
                cx = _coeff(gradDispX);
                weight = GetWeightForSmoothing(_dirs[i], x, y);
            }
            else if(InterpolateDisparityMap)
            {
                cx = _coeff(gradImg);
                weight = GetWeightForInterpolation(_dirs[i], x, y);
            }
            else
            {
                cx = _coeff(gradImg);
                weight = GetWeightForDiffusion(_dirs[i], x, y);
            }
            return gradDispX * cx * weight * _dinv[i];
        }

        private void UpdateDisparity(int x, int y, double dispX, double weight)
        {
            if(Math.Abs(dispX) > 1000)
            {
                // Detect invalid disparitiess
                _next[y, x].SubDX = 0;
                _next[y, x].DX = 0;
                _next[y, x].Flags = (int)DisparityFlags.Invalid;
            }
            else if(_next[y, x].IsInvalid() || SmoothDisparityMap)
            {
                _next[y, x].SubDX = dispX;
                _next[y, x].DX = dispX.Round();
            }
        }

        private double GetWeightForDiffusion(IntVector2 dir, int x, int y)
        {
            if(_initial[y + dir.Y, x + dir.X].IsValid()) { return 1.0; }
            else if(_last[y + dir.Y, x + dir.X].IsValid()) { return 0.5; }
            else if(_last[y, x].IsInvalid() && _last[y + dir.Y, x + dir.X].IsInvalid()) { return 0.25; }
            return 0.0;
        }

        private double GetWeightForInterpolation(IntVector2 dir, int x, int y)
        {
            if(_initial[y, x].IsInvalid() && _last[y + dir.Y, x + dir.X].IsValid())
            {
                return 1.0;
            }
            else
            {
                return 0.0;
            }
        }

        private double GetWeightForSmoothing(IntVector2 dir, int x, int y)
        {
            if(_last[y + dir.Y, x + dir.X].IsValid() && _last[y, x].IsValid())
            {
                return 1.0;
            }
            else
            {
                return 0.0;
            }
        }

        double GetCoeffValue_Exponental(double grad)
        {
            return Math.Exp(-((grad * grad) / (KernelCoeff * KernelCoeff)));
        }

        double GetCoeffValue_Rational(double grad)
        {
            return 1.0 / (1.0 + ((grad * grad) / (KernelCoeff * KernelCoeff)));
        }

        double GetCoeffValue_Constant(double grad)
        {
            return 1.0;
        }

        public override void RefineMaps()
        {
            if(MapLeft != null && ImageLeft != null)
            {
                MapLeft = RefineMap(MapLeft, ImageLeft);
            }

            if(MapRight != null && ImageRight != null)
            {
                MapRight = RefineMap(MapRight, ImageRight);
            }
        }

        public override string Name
        {
            get
            {
                return "Anisotropic Diffusion";
            }
        }

        public override void InitParameters()
        {
            base.InitParameters();

            Parameters.Add(new IntParameter(
                "Interations Count", "Iterations", 10, 1, 1000));
            Parameters.Add(new BooleanParameter(
                "Smooth Disparity Map", "SmoothDisparityMap", false));
            Parameters.Add(new BooleanParameter(
                "Interpolate Disparity Map", "InterpolateDisparityMap", false));
            Parameters.Add(new DoubleParameter(
                "Kernel Coeff", "KernelCoeff", 0.5, 0.001, 100.0));
            Parameters.Add(new DoubleParameter(
                "Step Coeff", "StepCoeff", 0.5, -10.0, 10.0));
            Parameters.Add(new DictionaryParameter("Spatial Kernel Type", "KernelType")
            {
                ValuesMap =
                {
                    { "Gaussian", CoeffKernelType.Exponential },
                    { "Rational", CoeffKernelType.Rational },
                    { "Constant", CoeffKernelType.Constant }
                }
            });
            Parameters.Add(new BooleanParameter(
                "Use 8 Gradient Directions", "UseEightDirections", false));
        }

        public override void UpdateParameters()
        {
            base.UpdateParameters();
            Iterations = IAlgorithmParameter.FindValue<int>("Iterations", Parameters);
            SmoothDisparityMap = IAlgorithmParameter.FindValue<bool>("SmoothDisparityMap", Parameters);
            InterpolateDisparityMap = IAlgorithmParameter.FindValue<bool>("InterpolateDisparityMap", Parameters);
            KernelCoeff = IAlgorithmParameter.FindValue<double>("KernelCoeff", Parameters);
            StepCoeff = IAlgorithmParameter.FindValue<double>("StepCoeff", Parameters);
            KernelType = IAlgorithmParameter.FindValue<CoeffKernelType>("KernelType", Parameters);
            UseEightDirections = IAlgorithmParameter.FindValue<bool>("UseEightDirections", Parameters);
        }

        public override string ToString()
        {
            return "Anisotropic Diffusion";
        }
    }
}
