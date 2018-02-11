using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace CamCore
{
    // Basic Gauss method solver with choosing of primary element
    // Assumes Equations matrix is square and of same size as left size vector
    public class GaussSolver : ILinearEquationsSolver
    {
        Matrix<double> _A;
        public Matrix<double> EquationsMatrix // Matrix is changed after solving, so it should be cloned if needed later
        {
            set
            {
                _A = value;
            }
        }

        Vector<double> _b;
        public Vector<double> RightSideVector // Vector is changed after solving, so it should be cloned if needed later
        {
            set
            {
                _b = value;
            }
        }

        Vector<double> _x;
        public Vector<double> ResultVector
        {
            get
            {
                return _x;
            }
        }

        List<int> _permutation = new List<int>();

        public void Solve()
        {
            _x = new DenseVector(_A.ColumnCount);
            _permutation.Clear();
            // Fill permutation with base order
            for(int i = 0; i < _b.Count; ++i)
            {
                _permutation.Add(i);
            }

            // Perform standard gauss elimination but with changed order
            for(int col = 0; col < _b.Count; col++)
            {
                // For each step find greatest value in column
                int maxRow = col;
                double maxValue = Math.Abs(_A.At(_permutation[col], col));
                for(int row = col + 1; row < _permutation.Count; ++row)
                {
                    double curVal = Math.Abs(_A.At(_permutation[row], col));
                    if(maxValue < curVal)
                    {
                        maxRow = row;
                        maxValue = curVal;
                    }
                }

                // Swap permuatation, so that greatest value is next primary element
                int temp = _permutation[col];
                _permutation[col] = _permutation[maxRow];
                _permutation[maxRow] = temp;

                // Primary row for next column = p[col], primary element (p[col], col)
                for(int row = col + 1; row < _b.Count; ++row)
                {
                    // For each row below primary row (according to permutation)
                    // sub multiplied primary row to zero elements
                    double mult = _A.At(_permutation[row], col) / _A.At(_permutation[col], col);
                    // sub all elements ( start from col, as previous should be zeroed already
                    for(int elem = col; elem < _b.Count; ++elem)
                    {
                        _A.At(_permutation[row], elem,
                            _A.At(_permutation[row], elem) - _A.At(_permutation[col], elem) * mult);
                    }
                    // Also change left side
                    _b.At(_permutation[row],
                        _b.At(_permutation[row]) - _b.At(_permutation[col]) * mult);
                }
            }

            // This point we have U * x = h, where U would be upper-right-triangular of
            // columns are swaped in permutation order

            for(int col = _b.Count - 1; col >= 0; --col)
            {
                // Compute x-es in reversed order
                _x.At(col, _b.At(_permutation[col])); // Starting value (correct if row is [0 ... 1 ... 0]) 
                for(int i = col + 1; i < _x.Count; ++i)
                {
                    // Substract from starting value all other x-es 
                    // ( solving a[perm,col]*x[col] = b[perm] - a[perm,col+1]*x[col+1] ... - a[perm,n]*x[n] )
                    _x.At(col, _x.At(col) - _A.At(_permutation[col], i) * _x.At(i));
                }
                _x.At(col, _x.At(col) / _A.At(_permutation[col], col));
            }
        }
    }
}
