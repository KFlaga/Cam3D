using Complex = MathNet.Numerics.Complex32;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;

namespace CamAlgorithms
{
    public class Polynomial
    {
        public int Rank { get; set; }
        public Vector<float> Coefficents { get; set; }

        public float At(float x)
        {
            float val = Coefficents.At(Rank);
            float xn = x;
            for(int c = Rank - 1; c >= 0; --c)
            {
                val += xn * Coefficents.At(c);
                xn *= x;
            }
            return val;
        }

        public Complex At(Complex x)
        {
            Complex val = new Complex(Coefficents.At(Rank), 0.0f);
            Complex xn = x;
            for(int c = Rank - 1; c >= 0; --c)
            {
                val = new Complex(val.Real + Coefficents.At(c) * xn.Real,
                    val.Imaginary + Coefficents.At(c) * xn.Imaginary);
                xn *= x;
            }
            return val;
        }

        // Computes coefficients of real polynomial (or estimates if values are noised) using Svd
        // Each row of matrix should contain [x, P(x)], at least rank+1 rows
        // Supplied x-es best have magintude in range [1-2], so resulting coefficient matrix is well conditioned
        public static Polynomial EstimatePolynomial(Matrix<float> values, int rank)
        {
            // 1) Create equation Xa = b
            // | x1^n x1^n-1 ... x1 1 | | a0 |   | P(x1) |
            // |                      | | ...| = |       |
            // | xk^n xk^n-1 ... xk 1 | | an |   | P(xk) |
            Matrix<float> X = new DenseMatrix(values.RowCount, rank + 1);
            Vector<float> P = new DenseVector(values.RowCount);

            for(int i = 0; i < values.RowCount; ++i)
            {
                X[i, rank] = 1.0f;
                for(int c = rank - 1; c >= 0; --c)
                {
                    X[i, c] = X[i, c + 1] * values.At(i, 0);
                }
                P[i] = values[i, 1];
            }

            return new Polynomial()
            {
                Coefficents = SvdSolver.Solve(X, P),
                Rank = rank
            };
        }

        // Computes coefficients of real monic (highest-degree coeff = 1) polynomial (or estimates if values are noised) using Svd
        // Each row of matrix should contain [x, P(x)], at least rank rows
        // Supplied x-es best have magintude in range [1-2], so resulting coefficient matrix is well conditioned
        public static Polynomial EstimatePolynomial_Monic(Matrix<float> values, int rank)
        {
            // 1) Create equation Xa = b
            // | x1^n-1 ... x1 1 | | a1 |   | P(x1) - x1^n |
            // |                 | | ...| = |              |
            // | xk^n-1 ... xk 1 | | an |   | P(xk) - xk^n |
            Matrix<float> X = new DenseMatrix(values.RowCount, rank);
            Vector<float> P = new DenseVector(values.RowCount);

            for(int i = 0; i < values.RowCount; ++i)
            {
                X[i, rank - 1] = 1.0f;
                for(int c = rank - 2; c >= 0; --c)
                {
                    X[i, c] = X[i, c + 1] * values.At(i, 0);
                }
                P[i] = values[i, 1] - X[i, 0] * values.At(i, 0);
            }

            Vector<float> coeffs = new DenseVector(rank + 1);
            var res = SvdSolver.Solve(X, P);
            res.CopySubVectorTo(coeffs, 0, 1, rank);
            coeffs[0] = 1.0f;

            return new Polynomial()
            {
                Coefficents = coeffs,
                Rank = rank
            };
        }

        public static double ComputeAt(double x, Vector<float> coeffs)
        {
            double val = coeffs.At(coeffs.Count - 1);
            double xn = x;
            for(int c = coeffs.Count - 2; c >= 0; --c)
            {
                val += xn * coeffs.At(c);
                xn *= x;
            }
            return val;
        }

        public static Complex ComputeAt(Complex x, Vector<float> coeffs)
        {
            Complex val = new Complex(coeffs[coeffs.Count - 1], 0.0f);
            Complex xn = x;
            for(int c = coeffs.Count - 2; c >= 0; --c)
            {
                val = new Complex(val.Real + coeffs.At(c) * xn.Real,
                    val.Imaginary + coeffs.At(c) * xn.Imaginary);
                xn *= x;
            }
            return val;
        }
    }
}
