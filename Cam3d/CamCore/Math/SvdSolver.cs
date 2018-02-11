using System;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace CamCore
{
    // Uses SVD decomposition to find least-squares solution of problem Ax = b 
    // Equation may have exact or either over- or under-determined solution
    // b should not be zero vector, as it would lead to x = 0 solution
    public class SvdSolver : ILinearEquationsSolver
    {
        Matrix<double> _A;
        public Matrix<double> EquationsMatrix { set { _A = value; } }

        Vector<double> _b;
        public Vector<double> RightSideVector { set { _b = value; } }

        Vector<double> _x;
        public Vector<double> ResultVector { get { return _x; } }

        public void Solve()
        {
            _x = Solve(_A, _b);
        }

        public static Vector<double> Solve(Matrix<double> A, Vector<double> b)
        {
            MathNet.Numerics.LinearAlgebra.Factorization.Svd<double> svd = A.Svd();

            double dmax = svd.S[0];
            Vector<double> y = new DenseVector(A.ColumnCount);
            Vector<double> bp = svd.U.Transpose() * b;

            // TODO: use the fact that S is sorted ( while S[i]/dmax < e then assume its greater )
            int minSize = Math.Min(A.ColumnCount, A.RowCount);
            for(int i = 0; i < minSize; ++i)
            {
                if(svd.S[i] / dmax < 1e-16)
                {
                    y[i] = 0.0f;
                }
                else
                {
                    y[i] = bp[i] / svd.S[i];
                }
            }

            return svd.VT.Transpose() * y;
        }


        public static Vector<float> Solve(Matrix<float> A, Vector<float> b)
        {
            MathNet.Numerics.LinearAlgebra.Factorization.Svd<float> svd = A.Svd();

            double dmax = svd.S[0];
            Vector<float> y = new MathNet.Numerics.LinearAlgebra.Single.DenseVector(A.ColumnCount);
            Vector<float> bp = svd.U.Transpose() * b;

            // TODO: use the fact that S is sorted ( while S[i]/dmax < e then assume its greater )
            int minSize = Math.Min(A.ColumnCount, A.RowCount);
            for(int i = 0; i < minSize; ++i)
            {
                if(svd.S[i] / dmax < 1e-16)
                {
                    y[i] = 0.0f;
                }
                else
                {
                    y[i] = bp[i] / svd.S[i];
                }
            }

            return svd.VT.Transpose() * y;
        }
    }

    // Uses SVD decomposition to find least-squares solution of problem Ax = 0 subject to ||x|| = 1
    // Matrix A should have at least A.ColumnCount rows and be of full column rank
    public class SvdZeroFullrankSolver : ILinearEquationsSolver
    {
        Matrix<double> _A;
        public Matrix<double> EquationsMatrix { set { _A = value; } }

        public Vector<double> RightSideVector { set { } }

        Vector<double> _x;
        public Vector<double> ResultVector { get { return _x; } }

        public void Solve()
        {
            _x = Solve(_A);
        }

        public static Vector<double> Solve(Matrix<double> A)
        {
            MathNet.Numerics.LinearAlgebra.Factorization.Svd<double> svd = A.Svd();
            return svd.VT.Row(svd.VT.RowCount - 1);
        }
    }
}

