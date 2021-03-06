﻿using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Diagnostics;
using System.Collections;
using System.Text;

namespace CamCore
{
    public static class VectorExtensions
    {
        // Returns vector with rows from list removed ( indices in list must be in ascending order )
        // If list is empty initial vector is returned
        public static Vector<double> RemoveElements(this Vector<double> v, List<int> toRemove)
        {
            if(toRemove.Count > 0)
            {
                Vector<double> removedVec = new DenseVector(v.Count - toRemove.Count);
                int removeIdx = 0;

                // Start copying to new matrix from last row
                // If row is to be removed, do omit it and remove its index from list -> only list index need to
                // be checked as indices in list in are ascending order
                int idx = 0;
                for(; idx < v.Count; ++idx)
                {
                    if(idx == toRemove[removeIdx])
                    {
                        ++removeIdx;
                        if(removeIdx == toRemove.Count)
                            break;
                    }
                    else
                    {
                        removedVec.At(idx - removeIdx, v.At(idx));
                    }
                }
                ++idx;
                // Copy rest of rows ( after all rows to be removed are removed )
                for(; idx < v.Count; ++idx)
                {
                    removedVec.At(idx - removeIdx, v.At(idx));
                }

                return removedVec;
            }
            else
                return v;
        }

        // Returns absolute maximum element of vector
        public static Tuple<int, double> AbsoulteMaximum(this Vector<double> v)
        {
            int maxIdx = 0;
            double maxVal = 0.0f;

            for(int i = 0; i < v.Count; ++i)
            {
                double absVal = Math.Abs(v.At(i));
                if(absVal > maxVal)
                {
                    maxVal = absVal;
                    maxIdx = i;
                }
            }

            return Tuple.Create(maxIdx, maxVal);
        }

        // Returns absolute minimum element of vector, which is not zero
        public static Tuple<int, double> AbsoulteMinimum(this Vector<double> v)
        {
            int minIdx = 0;
            double minVal = double.MaxValue;

            for(int i = 0; i < v.Count; ++i)
            {
                double absVal = Math.Abs(v.At(i));
                if(absVal < minVal && absVal >= double.Epsilon)
                {
                    minVal = absVal;
                    minIdx = i;
                }
            }

            return Tuple.Create(minIdx, minVal);
        }
        
        // Changes 0/0 (NaN) to 1 and if B(i) = 0 or A(i) = 0 then result(i) = A(i) or B(i) 
        public static Vector<double> PointwiseDivide_NoNaN(this Vector<double> a, Vector<double> b)
        {
            Vector<double> c = new DenseVector(a.Count);
            for(int i = 0; i < c.Count; ++i)
            {
                    c.At(i,
                        b.At(i).Equals(0.0) ?
                        (a.At(i).Equals(0.0) ? 1.0 : a.At(i)) :
                        (a.At(i).Equals(0.0) ? 1.0 : a.At(i) / b.At(i))
                        );
            }
            return c;
        }

        public static void MultiplyThis(this Vector<double> v, double scalar)
        {
            for(int i = 0; i < v.Count; ++i)
            {
                v.At(i, v.At(i) * scalar);
            }
        }

        public static void DivideThis(this Vector<double> v, double scalar)
        {
            for(int i = 0; i < v.Count; ++i)
            {
                v.At(i, v.At(i) / scalar);
            }
        }

        public static void PointwiseMultiplyThis(this Vector<double> v1, Vector<double> v2)
        {
            for(int i = 0; i < v1.Count; ++i)
            {
                v1.At(i, v1.At(i) * v2.At(i));
            }
        }

        public static void PointwiseDivideThis(this Vector<double> v1, Vector<double> v2)
        {
            for(int i = 0; i < v1.Count; ++i)
            {
                v1.At(i, v1.At(i) / v2.At(i));
            }
        }

        public static void PointwiseAddThis(this Vector<double> v1, Vector<double> v2)
        {
            for(int i = 0; i < v1.Count; ++i)
            {
                v1.At(i, v1.At(i) + v2.At(i));
            }
        }

        public static Vector<double> Cross(this Vector<double> left, Vector<double> right)
        {
            if((left.Count != 3 || right.Count != 3))
            {
                string message = "Vectors must have a length of 3.";
                throw new Exception(message);
            }

            Vector<double> result = new DenseVector(3);
            result[0] = left[1] * right[2] - left[2] * right[1];
            result[1] = -left[0] * right[2] + left[2] * right[0];
            result[2] = left[0] * right[1] - left[1] * right[0];

            return result;
        }
        
        public static void CopyFromMatrix(this Vector<double> v, Matrix<double> m, bool rowWise = true)
        {
            if(rowWise)
            {
                for(int c = 0; c < m.ColumnCount; ++c)
                {
                    for(int r = 0; r < m.RowCount; ++r)
                    {
                        v.At(r * m.ColumnCount + c, m.At(r, c));
                    }
                }
            }
            else
            {
                for(int c = 0; c < m.ColumnCount; ++c)
                {
                    for(int r = 0; r < m.RowCount; ++r)
                    {
                        v.At(c * m.RowCount + r, m.At(r, c));
                    }
                }
            }
        }

        public static string ToColumnVectorString(this Vector<double> m, string format = "F3")
        {
            StringBuilder result = new StringBuilder();
            for(int r = 0; r < m.Count; ++r)
            {
                result.Append("|" + m[r].ToString(format));
                result.AppendLine("|");
            }
            return result.ToString();
        }

        public static string ToRowVectorString(this Vector<double> m, string format = "F3")
        {
            StringBuilder result = new StringBuilder();
            result.Append("|");
            for(int r = 0; r < m.Count; ++r)
            {
                result.Append(m[r].ToString(format) + ", ");
            }
            result.Remove(result.Length - 2, 2);
            result.Append("|");
            return result.ToString();
        }

        public class DoubleVectorVisualiser
        {
            Vector<double> _vector;

            public DoubleVectorVisualiser(Vector<double> v)
            {
                _vector = v;
            }

            public HunderdList<double> Data
            {
                get
                {
                    double[] vector = _vector.ToArray();

                    return new HunderdList<double>(vector);
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
