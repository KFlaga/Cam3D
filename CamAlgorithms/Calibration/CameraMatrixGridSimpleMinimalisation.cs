using System;
using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Collections.Generic;

namespace CamAlgorithms.Calibration
{
    public class CameraMatrixGridSimpleMinimalisation : CameraMatrixGridMinimalisation
    {
        public override int CameraParametersCount => 12;

        protected override void UpdateLxLyM()
        {
            // Compute Lx,Ly,M
            // exi = (p1Xi+p2Yi+p3Zi+p4)/(p9Xi+p10Yi+p11Zi+p12) = Lxi/Mi
            // eyi = (p5Xi+p6Yi+p7Zi+p8)/(p9Xi+p10Yi+p11Zi+p12) = Lyi/Mi
            for(int i = 0; i < CalibrationPoints.Count; ++i)
            {
                _Lx.At(i, ResultsVector.At(0) * _reals[i].X +
                    ResultsVector.At(1) * _reals[i].Y +
                    ResultsVector.At(2) * _reals[i].Z +
                    ResultsVector.At(3));

                _Ly.At(i, ResultsVector.At(4) * _reals[i].X +
                    ResultsVector.At(5) * _reals[i].Y +
                    ResultsVector.At(6) * _reals[i].Z +
                    ResultsVector.At(7));

                _M.At(i, ResultsVector.At(8) * _reals[i].X +
                    ResultsVector.At(9) * _reals[i].Y +
                    ResultsVector.At(10) * _reals[i].Z +
                    ResultsVector.At(11));
            }
        }

        protected override double GetSkew()
        {
            return GetInternals()[0, 1];
        }

        protected override double GetFy()
        {
            return GetInternals()[1, 1];
        }
        
        protected Matrix<double> GetInternals()
        {
            Matrix<double> cm = new DenseMatrix(3, 3);
            cm[0, 0] = ResultsVector[0];
            cm[0, 1] = ResultsVector[1];
            cm[0, 2] = ResultsVector[2];
            cm[1, 0] = ResultsVector[4];
            cm[1, 1] = ResultsVector[5];
            cm[1, 2] = ResultsVector[6];
            cm[2, 0] = ResultsVector[8];
            cm[2, 0] = ResultsVector[9];
            cm[2, 0] = ResultsVector[10];

            var RQ = cm.QR();
            return RQ.R;
        }
    }
}
