﻿using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamCore
{
    public static class PointNormalization
    {
        // Returns normalisation matrix : xn = Mx
        // Uses list of points in matrix : each point is column 3-vector (so xi is [0,i])
        // Assusmes that weight of each point is 1
        public static Matrix<double> FindNormalizationMatrix2d(Matrix<double> points)
        {
            Matrix<double> norm = new DenseMatrix(3, 3);
            int n = points.ColumnCount;
            // Compute center of image points
            double xc = 0, yc = 0;
            for(int c = 0; c < n; ++c)
            {
                xc += points.At(0, c);
                yc += points.At(1, c);
            }
            xc /= n;
            yc /= n;
            // Get mean distance of points from center
            double dist = 0;
            for(int c = 0; c < n; ++c)
            {
                dist += (points.At(0, c) - xc) * (points.At(0, c) - xc) + 
                    (points.At(1, c) - yc) * (points.At(1, c) - yc);
            }
            dist /= n;
            // Normalize in a way that mean dist = sqrt(2)
            double ratio = Math.Sqrt(2) / Math.Sqrt(dist);
            // Noramlize matrix - homonogeus point must be multiplied by it
            norm[0, 0] = ratio;
            norm[1, 1] = ratio;
            norm[0, 2] = -ratio * xc;
            norm[1, 2] = -ratio * yc;
            norm[2, 2] = 1.0;

            return norm;
        }

        // Returns normalisation matrix : xn = Mx
        // Uses list of points : each point is 3-vector
        // Assusmes that weight of each point is 1
        public static Matrix<double> FindNormalizationMatrix2d(List<Vector<double>> points)
        {
            Matrix<double> norm = new DenseMatrix(3, 3);
            int n = points.Count;
            // Compute center of image points
            double xc = 0, yc = 0;
            for(int c = 0; c < n; ++c)
            {
                xc += points[c].At(0);
                yc += points[c].At(1);
            }
            xc /= n;
            yc /= n;
            // Get mean distance of points from center
            double dist = 0;
            for(int c = 0; c < n; ++c)
            {
                dist += (points[c].At(0) - xc) * (points[c].At(0) - xc) +
                    (points[c].At(1) - yc) * (points[c].At(1) - yc);
            }
            dist /= n;
            // Normalize in a way that mean dist = sqrt(2)
            double ratio = Math.Sqrt(2) / Math.Sqrt(dist);
            // Noramlize matrix - homonogeus point must be multiplied by it
            norm[0, 0] = ratio;
            norm[1, 1] = ratio;
            norm[0, 2] = -ratio * xc;
            norm[1, 2] = -ratio * yc;
            norm[2, 2] = 1.0;

            return norm;
        }


        // Returns normalisation matrix : xn = Mx
        // Uses list of points in matrix : each point is column 3-vector (so xi is [0,i])
        // Assusmes that weight of each point is 1
        public static Matrix<double> FindNormalizationMatrix3d(Matrix<double> points)
        {
            Matrix<double> norm = new DenseMatrix(4, 4);
            int n = points.ColumnCount;
            // Compute center of image points
            double xc = 0.0, yc = 0.0, zc = 0.0;
            for(int c = 0; c < n; ++c)
            {
                xc += points.At(0, c);
                yc += points.At(1, c);
                zc += points.At(2, c);
            }
            xc /= n;
            yc /= n;
            zc /= n;
            // Get mean distance of points from center
            double dist = 0;
            for(int c = 0; c < n; ++c)
            {
                dist += (points.At(0, c) - xc) * (points.At(0, c) - xc) +
                    (points.At(1, c) - yc) * (points.At(1, c) - yc) +
                    (points.At(2, c) - zc) * (points.At(2, c) - zc);
            }
            dist /= n;
            // Normalize in a way that mean dist = sqrt(3)
            double ratio = Math.Sqrt(3) / Math.Sqrt(dist);
            // Noramlize matrix - homonogeus point must be multiplied by it
            norm[0, 0] = ratio;
            norm[1, 1] = ratio;
            norm[2, 2] = ratio;
            norm[0, 3] = -ratio * xc;
            norm[1, 3] = -ratio * yc;
            norm[2, 3] = -ratio * zc;
            norm[3, 3] = 1.0;

            return norm;
        }

        // Returns normalisation matrix : xn = Mx
        // Uses list of points : each point is 3-vector
        // Assusmes that weight of each point is 1
        public static Matrix<double> FindNormalizationMatrix3d(List<Vector<double>> points)
        {
            Matrix<double> norm = new DenseMatrix(4, 4);
            int n = points.Count;
            // Compute center of image points
            double xc = 0, yc = 0, zc = 0;
            for(int c = 0; c < n; ++c)
            {
                xc += points[c].At(0);
                yc += points[c].At(1);
                zc += points[c].At(2);
            }
            xc /= n;
            yc /= n;
            zc /= n;
            // Get mean distance of points from center
            double dist = 0;
            for(int c = 0; c < n; ++c)
            {
                dist += (points[c].At(0) - xc) * (points[c].At(0) - xc) +
                    (points[c].At(1) - yc) * (points[c].At(1) - yc) +
                    (points[c].At(2) - zc) * (points[c].At(2) - zc);
            }
            dist /= n;
            // Normalize in a way that mean dist = sqrt(3)
            double ratio = Math.Sqrt(3) / Math.Sqrt(dist);
            // Noramlize matrix - homonogeus point must be multiplied by it
            norm[0, 0] = ratio;
            norm[1, 1] = ratio;
            norm[2, 2] = ratio;
            norm[0, 3] = -ratio * xc;
            norm[1, 3] = -ratio * yc;
            norm[2, 3] = -ratio * zc;
            norm[3, 3] = 1.0;

            return norm;
        }

        public static Matrix<double> NormalizePoints(Matrix<double> points, out Matrix<double> normalisationMatrix)
        {
            normalisationMatrix = points.RowCount == 3 ? FindNormalizationMatrix2d(points) : FindNormalizationMatrix3d(points);

            points = normalisationMatrix.Multiply(points);
            for(int p = 0; p < points.ColumnCount; p++)
            {
                for(int d = 0; d < points.RowCount; ++d)
                {
                    points[d, p] /= points[points.RowCount - 1, p];
                }
            }
            return points;
        }

        public static Matrix<double> NormalizePoints(Matrix<double> points, Matrix<double> normalisationMatrix)
        {
            points = normalisationMatrix.Multiply(points);
            for(int p = 0; p < points.ColumnCount; p++)
            {
                for(int d = 0; d < points.RowCount; ++d)
                {
                    points[d, p] /= points[points.RowCount - 1, p];
                }
            }
            return points;
        }

        public static List<Vector<double>> NormalizePoints(List<Vector<double>> points, Matrix<double> normalisationMatrix)
        {
            var pointsNormalized = new List<Vector<double>>();
            for(int p = 0; p < points.Count; p++)
            {
                pointsNormalized.Add(normalisationMatrix * points[p]);
            }
            return pointsNormalized;
        }

        // Returns normalisation matrix : xn = Mx
        // Uses list of points in matrix : each point is column 3-vector (so xi is [0,i])
        // Assusmes that weight of each point is 1
        // Scales each dimension separately
        public static Matrix<double> FindNonIsotropicNormalizationMatrix2d(Matrix<double> points)
        {
            Matrix<double> norm = new DenseMatrix(3, 3);
            int n = points.ColumnCount;
            // Compute center of image points
            double xc = 0, yc = 0;
            for(int c = 0; c < n; ++c)
            {
                xc += points.At(0, c);
                yc += points.At(1, c);
            }
            xc /= n;
            yc /= n;
            // Get mean distance of points from center
            double distX = 0;
            double distY = 0;
            for(int c = 0; c < n; ++c)
            {
                distX += (points.At(0, c) - xc) * (points.At(0, c) - xc);
                distY += (points.At(1, c) - yc) * (points.At(1, c) - yc);
            }
            distX /= n;
            distY /= n;
            // Normalize in a way that mean dist = sqrt(2)
            double ratiox = Math.Sqrt(2) / Math.Sqrt(distX);
            double ratioy = Math.Sqrt(2) / Math.Sqrt(distY);
            // Noramlize matrix - homonogeus point must be multiplied by it
            norm[0, 0] = ratiox;
            norm[1, 1] = ratioy;
            norm[0, 2] = -ratiox * xc;
            norm[1, 2] = -ratioy * yc;
            norm[2, 2] = 1.0;

            return norm;
        }

        // Returns normalisation matrix : xn = Mx
        // Uses list of points : each point is 3-vector
        // Assusmes that weight of each point is 1
        public static Matrix<double> FindNonIsotropicNormalizationMatrix2d(List<Vector<double>> points)
        {
            Matrix<double> norm = new DenseMatrix(3, 3);
            int n = points.Count;
            // Compute center of image points
            double xc = 0, yc = 0;
            for(int c = 0; c < n; ++c)
            {
                xc += points[c].At(0);
                yc += points[c].At(1);
            }
            xc /= n;
            yc /= n;
            // Get mean distance of points from center
            double distX = 0, distY = 0;
            for(int c = 0; c < n; ++c)
            {
                distX += Math.Sqrt((points[c].At(0) - xc) * (points[c].At(0) - xc));
                distY += Math.Sqrt((points[c].At(1) - yc) * (points[c].At(1) - yc));
            }
            distX /= n;
            distY /= n;
            // Normalize in a way that mean dist = sqrt(2)
            double ratiox = Math.Sqrt(2) / distX;
            double ratioy = Math.Sqrt(2) / distY;
            // Noramlize matrix - homonogeus point must be multiplied by it
            norm[0, 0] = ratiox;
            norm[1, 1] = ratioy;
            norm[0, 2] = -ratiox * xc;
            norm[1, 2] = -ratioy * yc;
            norm[2, 2] = 1.0;

            return norm;
        }
    }
}
