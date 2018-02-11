using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;

namespace CamAlgorithms
{
    public class AnisotropicDiffusionFilter : ImageFilter
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

        IntVector2[] _dirs4 = new IntVector2[4]
        {
            new IntVector2(-1, 0),
            new IntVector2(0, -1),
            new IntVector2(0, 1),
            new IntVector2(1, 0),
        };

        IntVector2[] _dirs8 = new IntVector2[8]
        {
            new IntVector2(-1, -1),
            new IntVector2(-1, 0),
            new IntVector2(-1, 1),
            new IntVector2(0, -1),
            new IntVector2(0, 1),
            new IntVector2(1, -1),
            new IntVector2(1, 0),
            new IntVector2(1, 1),
        };

        public override Matrix<double> ApplyFilter()
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

            Matrix<double> next = new DenseMatrix(Image.RowCount, Image.ColumnCount);
            Matrix<double> last = Image.Clone();
            IntVector2[] dirs = UseEightDirections ? _dirs8 : _dirs4;
            double r = UseEightDirections ? StepCoeff * 0.125 : StepCoeff * 0.25;

            for(int t = 0; t < MaxIterations; ++t)
            {
                for(int x = 1; x < Image.ColumnCount - 1; ++x)
                {
                    for(int y = 1; y < Image.RowCount - 1; ++y)
                    {
                        double val = 0.0;

                        // Add c_dir*∇_dir I(t, x, y) to val
                        foreach(var dir in dirs)
                        {
                            double grad = last.At(y + dir.Y, x + dir.X) - last.At(y, x);
                            double c = _coeff(grad);
                            val += grad * c / (dir.X * dir.X + dir.Y * dir.Y);
                        }
                        val *= r; // get r( sum{dir}(c_dir*∇_dir I(t, x, y))
                        val += last.At(y, x); // get I(t, x, y) + r( sum{dir}(c_dir*∇_dir I(t, x, y)))
                        next.At(y, x, val);
                    }
                }

                // Exchange last and next (I(t) will be overriden by I(t+2), I(t+1) in last)
                Matrix<double> temp = last;
                last = next;
                next = temp;
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

        public override Matrix<double> ApplyFilterShrink()
        {
            return ApplyFilter();
        }

        public override string Name { get { return "Anisotropic Diffusion Filter"; } }
        public override void InitParameters()
        {
            base.InitParameters();

            IAlgorithmParameter itersParam = new IntParameter(
                "Max Interations", "ITERS", 10, 1, 1000);
            Parameters.Add(itersParam);

            IAlgorithmParameter kerCoeffParam = new DoubleParameter(
                "Kernel Coeff", "KER_COEFF", 0.5, 0.001, 100.0);
            Parameters.Add(kerCoeffParam);

            IAlgorithmParameter stepCoeffParam = new DoubleParameter(
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

            IAlgorithmParameter dirsParam = new BooleanParameter(
                "Use 8 Gradient Directions", "EIGHT", false);
            Parameters.Add(dirsParam);
        }

        public override void UpdateParameters()
        {
            MaxIterations = IAlgorithmParameter.FindValue<int>("ITERS", Parameters);
            KernelType = IAlgorithmParameter.FindValue<CoeffKernelType>("KER_TYPE", Parameters);
            KernelCoeff = IAlgorithmParameter.FindValue<double>("KER_COEFF", Parameters);
            StepCoeff = IAlgorithmParameter.FindValue<double>("STEP_COEFF", Parameters);
            UseEightDirections = IAlgorithmParameter.FindValue<bool>("EIGHT", Parameters);
        }
    }
}
