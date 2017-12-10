using System;
using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Collections.Generic;

namespace CamAlgorithms.Calibration
{
    public class CameraMatrixGridExplicitMinimalisation : CameraMatrixGridMinimalisation
    {
        public static int fxIdx => 0;
        public static int fyIdx => 1;
        public static int sIdx => 2;
        public static int pxIdx => 3;
        public static int pyIdx => 4;
        public static int rxIdx => 5;
        public static int ryIdx => 6;
        public static int rzIdx => 7;
        public static int cxIdx => 8;
        public static int cyIdx => 9;
        public static int czIdx => 10;

        public override int CameraParametersCount => 11;

        protected override void UpdateLxLyM()
        {
            var K = DenseMatrix.OfRowArrays(new double[][]
            {
                new double[] { ResultsVector[fxIdx], ResultsVector[sIdx], ResultsVector[pxIdx] },
                new double[] { 0.0, ResultsVector[fyIdx], ResultsVector[pyIdx] },
                new double[] { 0.0,  0.0,   1.0 }
            });
            var R = RotationConverter.EulerToMatrix(new double[3] { ResultsVector[rxIdx], ResultsVector[ryIdx], ResultsVector[rzIdx] });
            var C = new DenseVector(new double[] { ResultsVector[cxIdx], ResultsVector[cyIdx], ResultsVector[czIdx] });
            Matrix<double> P = Camera.FromDecomposition(K, R, C).Matrix;

            for(int i = 0; i < CalibrationPoints.Count; ++i)
            {
                _Lx.At(i, P.At(0, 0) * _reals[i].X +
                    P.At(0, 1) * _reals[i].Y +
                    P.At(0, 2) * _reals[i].Z +
                    P.At(0, 3));

                _Ly.At(i, P.At(2, 0) * _reals[i].X +
                    P.At(2, 1) * _reals[i].Y +
                    P.At(2, 2) * _reals[i].Z +
                    P.At(2, 3));

                _M.At(i, P.At(2, 0) * _reals[i].X +
                    P.At(2, 1) * _reals[i].Y +
                    P.At(2, 2) * _reals[i].Z +
                    P.At(2, 3));
            }
        }

        protected override double GetSkew()
        {
            return ResultsVector[sIdx];
        }

        protected override double GetFy()
        {
            return ResultsVector[fyIdx];
        }
    }
}
