using Matrix = MathNet.Numerics.LinearAlgebra.Matrix<float>;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra.Single;

namespace CamImageProcessing
{
    public static class MatrixConvolution
    {
        // Return convolution A * B
        // Size of returned matrix = size of A, but
        // elements on boundaries ( of length rows(cols) of B / 2 )
        // are not computed and set to 0
        // Size of B must be odd
        public static Matrix Convolve(Matrix A, Matrix B)
        {
            Matrix conv = new DenseMatrix(A.RowCount, A.ColumnCount);

            int row2 = B.RowCount / 2;
            int col2 = B.ColumnCount / 2;
            int xmax = A.ColumnCount - col2;
            int ymax = A.RowCount - row2;

            int y, x, dx, dy;
            float maskSum;

            for ( y = row2; y < ymax; ++y)
                for ( x = col2; x < xmax; ++x)
                {
                    maskSum = 0.0f;
                    for ( dy = -row2; dy <= row2; ++dy)
                        for ( dx = -col2; dx <= col2; ++dx)
                        {
                            maskSum += A[y + dy, x + dx] * B[row2 + dy, col2 + dx];
                        }
                    conv[y, x] = maskSum;
                }

            return conv;
        }

        // Convolution A * B
        // Size of returned matrix is rows(cols) of A - rows(cols) of B
        // (convolution not computed for boundary elements of A )
        // Size of B must be odd
        public static Matrix ConvolveShrink(Matrix A, Matrix B)
        {
            Matrix conv = new DenseMatrix(A.RowCount - (B.RowCount / 2) * 2, 
                                            A.ColumnCount - (B.ColumnCount / 2) * 2);

            int row2 = B.RowCount / 2;
            int col2 = B.ColumnCount / 2;
            int xmax = A.ColumnCount - col2;
            int ymax = A.RowCount - row2;

            for (int y = row2; y < ymax; ++y)
                for (int x = col2; x < xmax; ++x)
                {
                    float maskSum = 0;
                    for (int dy = -row2; dy <= row2; dy++)
                        for (int dx = -col2; dx <= col2; dx++)
                        {
                            maskSum += A[y + dy, x + dx] * B[row2 + dy, col2 + dx];
                        }
                    conv[y - row2, x - col2] = maskSum;
                }

            return conv;
        }

        public static Matrix ConvolveHorizontalMask(Matrix A, Vector B)
        {
            Matrix conv = new DenseMatrix(A.RowCount, A.ColumnCount);

            int len2 = B.Count / 2;
            int xmax = A.ColumnCount - len2;
            int ymax = A.RowCount;

            for (int y = 0; y < ymax; ++y)
                for (int x = len2; x < xmax; ++x)
                {
                    float maskSum = 0;
                    for (int dx = -len2; dx <= len2; dx++)
                    {
                       maskSum += A[y, x + dx] * B[len2 + dx];
                    }
                    conv[y, x] = maskSum;
                }

            return conv;
        }

        public static Matrix ConvolveHorizontalMaskShrink(Matrix A, Vector B)
        {
            Matrix conv = new DenseMatrix(A.RowCount, A.ColumnCount - (B.Count / 2) * 2);

            int len2 = B.Count / 2;
            int xmax = A.ColumnCount - len2;
            int ymax = A.RowCount;

            for (int y = 0; y < ymax; ++y)
                for (int x = len2; x < xmax; ++x)
                {
                    float maskSum = 0;
                    for (int dx = -len2; dx <= len2; dx++)
                    {
                        maskSum += A[y, x + dx] * B[len2 + dx];
                    }
                    conv[y, x - len2] = maskSum;
                }

            return conv;
        }

        public static Matrix ConvolveVerticalMask(Matrix A, Vector B)
        {
            Matrix conv = new DenseMatrix(A.RowCount, A.ColumnCount);

            int len2 = B.Count / 2;
            int xmax = A.ColumnCount;
            int ymax = A.RowCount - len2;

            for (int y = len2; y < ymax; ++y)
                for (int x = 0; x < xmax; ++x)
                {
                    float maskSum = 0;
                    for (int dy = -len2; dy <= len2; dy++)
                    {
                        maskSum += A[y + dy, x] * B[len2 + dy];
                    }
                    conv[y, x] = maskSum;
                }

            return conv;
        }

        public static Matrix ConvolveVerticalMaskShrink(Matrix A, Vector B)
        {
            Matrix conv = new DenseMatrix(A.RowCount, A.ColumnCount - (B.Count / 2) * 2);

            int len2 = B.Count / 2;
            int xmax = A.ColumnCount;
            int ymax = A.RowCount - len2;

            for (int y = len2; y < ymax; ++y)
                for (int x = 0; x < xmax; ++x)
                {
                    float maskSum = 0;
                    for (int dy = -len2; dy <= len2; dy++)
                    {
                        maskSum += A[y + dy, x] * B[len2 + dy];
                    }
                    conv[y, x - len2] = maskSum;
                }

            return conv;
        }
    }
    

}
