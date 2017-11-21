using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamCore
{
    public static class RotationConverter
    {
        // Converts XYZ euler angles to 3x3 rotation matrix (assumes matrix is correctly allocated)
        public static void EulerToMatrix(Vector<double> euler, Matrix<double> matrix)
        {
            // | cycz           −cysz           sy    |
            // | czsxsy + cxsz  cxcz − sxsysz   −cysx |
            // | −cxczsy + sxsz czsx + cxsysz   cxcy  |
            //
            double sx = Math.Sin(euler.At(0));
            double cx = Math.Cos(euler.At(0));
            double sy = Math.Sin(euler.At(1));
            double cy = Math.Cos(euler.At(1));
            double sz = Math.Sin(euler.At(2));
            double cz = Math.Cos(euler.At(2));
            matrix.At(0, 0, cy * cz);
            matrix.At(0, 1, -cy * sz);
            matrix.At(0, 2, sy);
            matrix.At(1, 0, cz * sx * sy + cx * sz);
            matrix.At(1, 1, cx * cz - sx * sy * sz);
            matrix.At(1, 2, -cy * sx);
            matrix.At(2, 0, -cx * cz * sy + sz * sz);
            matrix.At(2, 1, cz * sx + cx * sy * sz);
            matrix.At(2, 2, cx * cy);
        }

        // Converts XYZ euler angles to 3x3 rotation matrix (assumes matrix is correctly allocated)
        public static void EulerToMatrix(double[] euler, Matrix<double> matrix)
        {
            // | cycz           −cysz           sy    |
            // | czsxsy + cxsz  cxcz − sxsysz   −cysx |
            // | −cxczsy + sxsz czsx + cxsysz   cxcy  |
            //
            double sx = Math.Sin(euler[0]);
            double cx = Math.Cos(euler[0]);
            double sy = Math.Sin(euler[1]);
            double cy = Math.Cos(euler[1]);
            double sz = Math.Sin(euler[2]);
            double cz = Math.Cos(euler[2]);
            matrix.At(0, 0, cy * cz);
            matrix.At(0, 1, -cy * sz);
            matrix.At(0, 2, sy);
            matrix.At(1, 0, cz * sx * sy + cx * sz);
            matrix.At(1, 1, cx * cz - sx * sy * sz);
            matrix.At(1, 2, -cy * sx);
            matrix.At(2, 0, -cx * cz * sy + sz * sz);
            matrix.At(2, 1, cz * sx + cx * sy * sz);
            matrix.At(2, 2, cx * cy);
        }

        // Converts 3x3 rotation matrix to XYZ euler angles (assumes vector is correctly allocated)
        public static void MatrixToEuler(Vector<double> euler, Matrix<double> matrix)
        {
            if(matrix.At(0, 2) < 1.0 - 1e-9)
            {
                if(matrix.At(0, 2) > -1.0 + 1e-9)
                {
                    //      t he t aY = a s i n(r 0 2);
                    //     t he t aX = a t a n 2(−r12, r 2 2);
                    //    t h e t aZ = a t a n 2(−r01, r 0 0);
                    euler.At(1, Math.Asin(matrix.At(0, 2)));
                    euler.At(0, Math.Atan2(-matrix.At(1, 2), matrix.At(2, 2)));
                    euler.At(2, Math.Atan2(-matrix.At(0, 1), matrix.At(0, 0)));
                }
                else // r 0 2 = −1
                {
                    // Not a u n i q u e s o l u t i o n : t h e t aZ − t he t aX = a t a n 2 ( r10 , r 1 1 )
                    //  t he t aY = −PI / 2;
                    // t he t aX = −a t a n 2(r10, r 1 1);
                    // t h e t aZ = 0;
                    euler.At(1, -Math.PI * 0.5);
                    euler.At(0, -Math.Atan2(-matrix.At(1, 0), matrix.At(1, 1)));
                    euler.At(2, 0.0);
                }
            }
            else // r 0 2 = +1
            {
                // Not a u n i q u e s o l u t i o n : t h e t aZ + t he t aX = a t a n 2 ( r10 , r 1 1 )
                // t he t aY = +PI / 2;
                // t he t aX = a t a n 2(r10, r 1 1);
                // t h e t aZ = 0;
                euler.At(1, Math.PI * 0.5);
                euler.At(0, -Math.Atan2(-matrix.At(1, 0), matrix.At(1, 1)));
                euler.At(2, 0.0);
            }
        }
    }
}
