using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Diagnostics;
using System.Collections;
using MathNet.Numerics;

namespace CamCore
{
    public static class MatrixExtensions
    {
        // Transposes A and saves it in At
        // Assumes At is allocated with correct size, so one memory storage can be reused
        // in all iterations
        public static void TransposeToOther(this Matrix<double> A, Matrix<double> At)
        {
            for(int r = 0; r < A.RowCount; ++r)
                for(int c = 0; c < A.ColumnCount; ++c)
                    At.At(c, r, A.At(r, c));
        }

        // Computes A*B and stores in C
        // Assumes C is allocated with correct size, so one memory storage can be reused
        // in all iterations
        public static void MultiplyToOther(this Matrix<double> A, Matrix<double> B, Matrix<double> C)
        {
            for(int r = 0; r < C.RowCount; ++r)
            {
                for(int c = 0; c < C.ColumnCount; ++c)
                {
                    C.At(r, c, 0.0f);
                    for(int i = 0; i < A.ColumnCount; ++i)
                    {
                        C.At(r, c, C.At(r, c) + A.At(r, i) * B.At(i, c));
                    }
                }
            }
        }

        // Computes A*b and stores in c, where b and c are column vectors
        // Assumes C is allocated with correct size, so one memory storage can be reused
        // in all iterations
        public static void MultiplyToOther(this Matrix<double> A, Vector<double> b, Vector<double> c)
        {
            for(int r = 0; r < c.Count; ++r)
            {
                c.At(r, 0.0f);
                for(int k = 0; k < A.ColumnCount; ++k)
                {
                    c.At(r, c.At(r) + A.At(r, k) * b.At(k));
                }
            }
        }

        // Changes 0/0 (NaN) to 1 and if B(r,c) = 0 or A(r,c) = 0 then result(r,c) = A(r,c) or B(r,c) 
        public static Matrix<double> PointwiseDivide_NoNaN(this Matrix<double> A, Matrix<double> B)
        {
            Matrix<double> C = new DenseMatrix(A.RowCount, A.ColumnCount);
            for(int c = 0; c < C.ColumnCount; ++c)
            {
                for(int r = 0; r < C.RowCount; ++r)
                {
                    C.At(r, c,
                        B.At(r, c).Equals(0.0) ?
                        (A.At(r, c).Equals(0.0) ? 1.0 : A.At(r, c)) :
                        (A.At(r, c).Equals(0.0) ? 1.0 : A.At(r, c) / B.At(r, c))
                        );
                }
            }
            return C;
        }

        // Computes matrix v1 * v2^T, where v1/v2 are same size column vectors
        public static Matrix<double> FromVectorProduct(Vector<double> v1, Vector<double> v2)
        {
            Matrix<double> mat = new DenseMatrix(v1.Count);
            for(int x = 0; x < mat.ColumnCount; ++x)
            {
                for(int y = 0; y < mat.RowCount; ++y)
                {
                    mat.At(y, x, v1.At(y) * v2.At(x));
                }
            }
            return mat;
        }

        // Computes matrix v1 * v1^T, where v1 is column vectors
        public static Matrix<double> FromVectorProduct(Vector<double> v1)
        {
            Matrix<double> mat = new DenseMatrix(v1.Count);
            for(int x = 0; x < mat.ColumnCount; ++x)
            {
                for(int y = 0; y < mat.RowCount; ++y)
                {
                    mat.At(y, x, v1.At(y) * v1.At(x));
                }
            }
            return mat;
        }

        /// <summary> 
        /// Moore–Penrose pseudoinverse 
        /// If A = U • Σ • VT is the singular value decomposition of A, then A† = V • Σ† • UT. 
        /// For a diagonal matrix such as Σ, we get the pseudoinverse by taking the reciprocal of each non-zero element 
        /// on the diagonal, leaving the zeros in place, and transposing the resulting matrix. 
        /// In numerical computation, only elements larger than some small tolerance are taken to be nonzero,
        /// and the others are replaced by zeros. For example, in the MATLAB or NumPy function pinv, 
        /// the tolerance is taken to be t = ε • max(m,n) • max(Σ), where ε is the machine epsilon. (Wikipedia) 
        /// </summary> 
        /// <param name="M">The matrix to pseudoinverse</param> 
        /// <returns>The pseudoinverse of this Matrix</returns> 
        public static Matrix<double> PseudoInverse(this Matrix<double> M)
        {
            var D = M.Svd(true);
            var W = D.W;
            var s = D.S;

            // The first element of W has the maximum value. 
            double tolerance = Precision.EpsilonOf(2) * Math.Max(M.RowCount, M.ColumnCount) * W[0, 0];

            for(int i = 0; i < s.Count; i++)
            {
                if(s[i] < tolerance)
                    s[i] = 0;
                else
                    s[i] = 1 / s[i];
            }
            W.SetDiagonal(s);

            // (U * W * VT)T is equivalent with V * WT * UT 
            return (D.U * W * D.VT).Transpose();
        }

        public static List<int> FindZeroRows(this Matrix<double> A, double error = double.Epsilon * 100.0f)
        {
            List<int> zeroRows = new List<int>();

            for(int row = 0; row < A.RowCount; ++row)
            {
                bool zeroRow = true;
                for(int col = 0; col < A.ColumnCount; ++col)
                {
                    if(Math.Abs(A.At(row, col)) > error)
                    {
                        zeroRow = false;
                        break;
                    }
                }

                if(zeroRow)
                    zeroRows.Add(row);
            }

            return zeroRows;
        }

        public static List<int> FindZeroColumns(this Matrix<double> A, double error = double.Epsilon * 100.0f)
        {
            List<int> zeroCols = new List<int>();

            for(int col = 0; col < A.ColumnCount; ++col)
            {
                bool zeroCol = true;
                for(int row = 0; row < A.RowCount; ++row)
                {
                    if(Math.Abs(A.At(row, col)) > error)
                    {
                        zeroCol = false;
                        break;
                    }
                }

                if(zeroCol)
                    zeroCols.Add(col);
            }

            return zeroCols;
        }

        // Returns matrix with rows from list removed ( indices in list must be in ascending order )
        // If list is empty initial matix is returned
        public static Matrix<double> RemoveRows(this Matrix<double> A, List<int> toRemove)
        {
            if(toRemove.Count > 0)
            {
                Matrix<double> removedMat = new DenseMatrix(A.RowCount - toRemove.Count, A.ColumnCount);
                int removeIdx = 0;

                // Start copying to new matrix from last row
                // If row is to be removed, do omit it and remove its index from list -> only list index need to
                // be checked as indices in list in are ascending order
                int row = 0;
                for(; row < A.RowCount; ++row)
                {
                    if(row == toRemove[removeIdx])
                    {
                        ++removeIdx;
                        if(removeIdx == toRemove.Count)
                            break;
                    }
                    else
                    {
                        for(int col = 0; col < A.ColumnCount; ++col)
                        {
                            removedMat.At(row - removeIdx, col, A.At(row, col));
                        }
                    }
                }
                ++row;
                // Copy rest of rows ( after all rows to be removed are removed )
                for(; row < A.RowCount; ++row)
                {
                    for(int col = 0; col < A.ColumnCount; ++col)
                    {
                        removedMat.At(row - removeIdx, col, A.At(row, col));
                    }
                }

                return removedMat;
            }
            else
                return A;
        }

        // Returns matrix with columns from list removed ( indices in list must be in ascending order )
        // If list is empty initial matix is returned
        public static Matrix<double> RemoveColumns(this Matrix<double> A, List<int> toRemove)
        {
            if(toRemove.Count > 0)
            {
                Matrix<double> removedMat = new DenseMatrix(A.RowCount, A.ColumnCount - toRemove.Count);
                int removeIdx = 0;

                // Start copying to new matrix from last column
                // If column is to be removed, do omit it and remove its index from list
                int col = 0;
                for(; col < A.ColumnCount; ++col)
                {
                    if(col == toRemove[removeIdx])
                    {
                        ++removeIdx;
                        if(removeIdx == toRemove.Count)
                            break;
                    }
                    else
                    {
                        for(int row = 0; row < A.RowCount; ++row)
                        {
                            removedMat.At(row, col - removeIdx, A.At(row, col));
                        }
                    }
                }
                ++col;
                // Copy rest of columns ( after all columns to be removed are removed )
                for(; col < A.ColumnCount; ++col)
                {
                    for(int row = 0; row < A.RowCount; ++row)
                    {
                        removedMat.At(row, col - removeIdx, A.At(row, col));
                    }
                }

                return removedMat;
            }
            else
                return A;
        }

        // Returns absolute maximum element of matrix
        public static Tuple<int, int, double> AbsoulteMaximum(this Matrix<double> A)
        {
            int maxRow = 0, maxCol = 0;
            double maxVal = 0.0f;

            for(int c = 0; c < A.ColumnCount; ++c)
            {
                for(int r = 0; r < A.RowCount; ++r)
                {
                    double absVal = Math.Abs(A.At(r, c));
                    if(absVal > maxVal)
                    {
                        maxVal = absVal;
                        maxRow = r;
                        maxCol = c;
                    }
                }
            }

            return Tuple.Create(maxRow, maxCol, maxVal);
        }

        // Returns absolute minimum element of matrix, which is not zero
        public static Tuple<int, int, double> AbsoulteMinimum(this Matrix<double> A)
        {
            int minRow = 0, minCol = 0;
            double minVal = double.MaxValue;

            for(int c = 0; c < A.ColumnCount; ++c)
            {
                for(int r = 0; r < A.RowCount; ++r)
                {
                    double absVal = Math.Abs(A.At(r, c));
                    if(absVal < minVal && absVal >= double.Epsilon)
                    {
                        minVal = absVal;
                        minRow = r;
                        minCol = c;
                    }
                }
            }

            return Tuple.Create(minRow, minCol, minVal);
        }

        public static void MultiplyThis(this Matrix<double> A, double scalar)
        {
            for(int c = 0; c < A.ColumnCount; ++c)
            {
                for(int r = 0; r < A.RowCount; ++r)
                {
                    A.At(r, c, A.At(r, c) * scalar);
                }
            }
        }

        public static void PointwiseMultiplyThis(this Matrix<double> A, Matrix<double> other)
        {
            for(int c = 0; c < A.ColumnCount; ++c)
            {
                for(int r = 0; r < A.RowCount; ++r)
                {
                    A.At(r, c, A.At(r, c) * other.At(r, c));
                }
            }
        }

        public static void PointwiseAddThis(this Matrix<double> A, Matrix<double> other)
        {
            for(int c = 0; c < A.ColumnCount; ++c)
            {
                for(int r = 0; r < A.RowCount; ++r)
                {
                    A.At(r, c, A.At(r, c) + other.At(r, c));
                }
            }
        }

        public class DoubleMatrixVisualiser
        {
            Matrix<double> _matrix;

            public DoubleMatrixVisualiser(Matrix<double> matrix)
            {
                _matrix = matrix;
            }

            public HunderdList<HunderdList<double>> Data
            {
                get
                {
                    double[][] matrix = _matrix.ToRowArrays();
                    HunderdList<double>[] columns100 = new HunderdList<double>[_matrix.RowCount];
                    for(int i = 0; i < _matrix.RowCount; ++i)
                    {
                        columns100[i] = new HunderdList<double>(matrix[i]);
                    }

                    return new HunderdList<HunderdList<double>>(columns100);
                }
            }
        }


        public class HunderdList<T>
        {
            public List<T[]> List { get; private set; }

            public HunderdList(T[] data)
            {
                int hundretsRows = data.Length / 100;
                int remaining = data.Length - hundretsRows * 100;

                List = new List<T[]>(hundretsRows);
                for(int i = 0; i < hundretsRows; ++i)
                {
                    var data100 = new T[100];
                    Array.Copy(data, i * 100, data100, 0, 100);
                    List.Add(data100);
                }

                if(remaining > 0)
                {
                    var dataRem = new T[remaining];
                    Array.Copy(data, hundretsRows * 100, dataRem, 0, remaining);
                    List.Add(dataRem);
                }
            }
        }
    }
}
