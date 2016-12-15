using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamImageProcessing.ImageMatching
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

        public int MaxIterations { get; set; } = 10;
        public double KernelCoeff { get; set; }
        public double StepCoeff { get; set; } // Actual coeff used is StepCoeff * (1/dirs)
                                              // public bool UseExtendedMethod { get; set; }
        public bool UseEightDirections { get; set; }
        public bool UseConfidenceWeights { get; set; }

        public bool SmoothDisparityMap { get; set; } // When set instead of interpolating bad disparities
                                                     // from more confident one within color-coherent areas
                                                     // smooth disparities in disparity-coherent areas

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

        public DisparityMap RefineMap(DisparityMap map, IImage img)
        {
            // ∂I(x,y)/∂t = div(c(x,y,t) · ∇I(x,y)) = ∇c*∇I + c*(∇^2)I
            // I(x, y) is image, t is time (iterations)
            // c(||∇I||) = exp(-(||∇I||/K)^2) or 1/(1+(||∇I||/K)^2)
            // we have solution:
            // I(t+1, x, y) = I(t, x, y) + r( sum{dir}(c_dir*∇_dir I(t, x, y)))
            // where c_dir/∇_dir are coeff/gradient in direction
            // r = [0, 1/dirs]
            // c_dir for dir (i,j) = c(||I(x,y) - I(x+i,y+j)||)
            // ∇_dir for dir (i,j) =  I(x,y) - I(x+i,y+j)

            DisparityMap next = (DisparityMap)map.Clone();
            DisparityMap last = (DisparityMap)map.Clone();
            IntVector2[] dirs = UseEightDirections ? _dirs8 : _dirs4;
            double[] dinv = UseEightDirections ? _dinv8 : _dinv4;
            double r = UseEightDirections ? StepCoeff * 0.125 : StepCoeff * 0.25;

            for(int t = 0; t < MaxIterations; ++t)
            {
                for(int x = 1; x < map.ColumnCount - 1; ++x)
                {
                    for(int y = 1; y < map.RowCount - 1; ++y)
                    {
                        double dispX = 0.0;
                        double dispY = 0.0;

                        // Add c_dir*∇_dir I(t, x, y) to val
                        for(int i = 0; i < dirs.Length; ++i)
                        {
                            double gradDispX = last[y + dirs[i].Y, x + dirs[i].X].SubDX - last[y, x].SubDX;
                            double gradDispY = last[y + dirs[i].Y, x + dirs[i].X].SubDY - last[y, x].SubDY;
                            double gradImg = img[y + dirs[i].Y, x + dirs[i].X] - img[y, x];
                            double cx, cy;

                            if(SmoothDisparityMap)
                            {
                                if(last[y + dirs[i].Y, x + dirs[i].X].IsValid() && last[y, x].IsValid())
                                {
                                    cx = 0.5 * _coeff(gradDispX);
                                    cy = 0.5 * _coeff(gradDispY);
                                }
                                else if(last[y + dirs[i].Y, x + dirs[i].X].IsValid())
                                {
                                    cx = _coeff(gradDispX);
                                    cy = _coeff(gradDispY);
                                }
                                else if(last[y, x].IsValid())
                                {
                                    cy = cx = 0.0;
                                }
                                else
                                {
                                    cx = 0.5 * _coeff(gradDispX);
                                    cy = 0.5 * _coeff(gradDispY);
                                }
                            }
                            else
                            {
                                if(last[y + dirs[i].Y, x + dirs[i].X].IsValid() && last[y, x].IsValid())
                                {
                                    cy = cx = 0.5 * _coeff(gradImg);
                                }
                                else if(last[y + dirs[i].Y, x + dirs[i].X].IsValid())
                                {
                                    cy = cx = _coeff(gradImg);
                                }
                                else if(last[y, x].IsValid())
                                {
                                    cy = cx = 0.0;
                                }
                                else
                                {
                                    cy = cx = 0.5 * _coeff(gradImg);
                                }
                            }

                            dispX += gradDispX * cx * dinv[i];
                            dispY += gradDispY * cy * dinv[i];
                        }
                        dispX *= r; // get r( sum{dir}(c_dir*∇_dir I(t, x, y))
                        dispY *= r;
                        dispX += last[y, x].SubDX; // get I(t, x, y) + r( sum{dir}(c_dir*∇_dir I(t, x, y)))
                        dispY += last[y, x].SubDY;

                        if(dispX > 1000)
                        {
                            dispX = 0;
                            dispY = 0;
                        }
                        
                        if(next[y, x].IsInvalid() || SmoothDisparityMap)
                        {
                            next[y, x].SubDX = dispX;
                            next[y, x].SubDY = dispY;
                            next[y, x].DX = dispX.Round();
                            next[y, x].DY = dispY.Round();
                        }
                    }
                }

                // Exchange last and next (I(t) will be overriden by I(t+2), I(t+1) in last)
                var temp = last;
                last = next;
                next = temp;
            }

            for(int x = 1; x < map.ColumnCount - 1; ++x)
            {
                for(int y = 1; y < map.RowCount - 1; ++y)
                {
                    last[y, x].Flags = (int)DisparityFlags.Valid;
                }
            }
            return last;
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

            AlgorithmParameter itersParam = new IntParameter(
                "Max Interations", "ITERS", 10, 1, 1000);
            Parameters.Add(itersParam);

            AlgorithmParameter smoothParam = new BooleanParameter(
                "Smooth Disparity Map", "SMOOTH", false);
            Parameters.Add(smoothParam);

            AlgorithmParameter kerCoeffParam = new DoubleParameter(
                "Kernel Coeff", "KER_COEFF", 0.5, 0.001, 100.0);
            Parameters.Add(kerCoeffParam);

            AlgorithmParameter stepCoeffParam = new DoubleParameter(
                "Step Coeff", "STEP_COEFF", 0.5, -10.0, 10.0);
            Parameters.Add(stepCoeffParam);

            DictionaryParameter kernelTypeParam = new DictionaryParameter(
                "Spatial Kernel Type", "KER_TYPE");

            kernelTypeParam.ValuesMap = new Dictionary<string, object>()
            {
                { "Gaussian", CoeffKernelType.Exponential },
                { "Rational", CoeffKernelType.Rational },
                { "Constant", CoeffKernelType.Constant }
            };

            Parameters.Add(kernelTypeParam);

            AlgorithmParameter dirsParam = new BooleanParameter(
                "Use 8 Gradient Directions", "EIGHT", false);
            Parameters.Add(dirsParam);
        }

        public override void UpdateParameters()
        {
            base.UpdateParameters();
            MaxIterations = AlgorithmParameter.FindValue<int>("ITERS", Parameters);
            KernelType = AlgorithmParameter.FindValue<CoeffKernelType>("KER_TYPE", Parameters);
            KernelCoeff = AlgorithmParameter.FindValue<double>("KER_COEFF", Parameters);
            StepCoeff = AlgorithmParameter.FindValue<double>("STEP_COEFF", Parameters);
            UseEightDirections = AlgorithmParameter.FindValue<bool>("EIGHT", Parameters);
            SmoothDisparityMap = AlgorithmParameter.FindValue<bool>("SMOOTH", Parameters);
        }

        public override string ToString()
        {
            return "Anisotropic Diffusion Smoothing";
        }
    }
}
