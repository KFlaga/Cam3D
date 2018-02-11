using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;

namespace CamAlgorithms.Calibration
{
    // Parameter vector have form : [p11,p12,...,p34]
    // prc is eP[r,c] and eP is estimated camera matrix and (eXi,eYi,eZi) is estimated real point
    // Inital parameters should be P from least-squares solution and measured real points
    // Measurment vector have form : [X1,Y1,Z1,X2,Y2,Z2,...,Xn,Yn,Zn,x1,y1,x2,y2,...,xn,yn]
    // Mapping function returns [ex1,ey1...exn,eyn]
    // Error vector have form : [ex1,ey1...exn,eyn,ws] where s is skew and w is weight (adaptative: wi+1 = wi * a, where a is like 1.5, w0 = ||f(P)|| / sqrt(n) )
    // Minimised error is have only algebraic meaning
    public class LMCameraMatrixZeroSkewMinimalisation : LevenbergMarquardtBaseAlgorithm
    {
        protected Vector<double> _Lx;
        protected Vector<double> _Ly;
        protected Vector<double> _M;
        protected double _w;

        public override void Init()
        {
            int measuredPointsCount = (MeasurementsVector.Count) / 5;

            // Allocate matrices
            _J = new DenseMatrix(2 * measuredPointsCount + 1, ParametersVector.Count);
            _Jt = new DenseMatrix(ParametersVector.Count, 2 * measuredPointsCount + 1);
            _JtJ = new DenseMatrix(ParametersVector.Count, ParametersVector.Count);
            _Jte = new DenseVector(ParametersVector.Count);
            _currentErrorVector = new DenseVector(2 * measuredPointsCount + 1);
            _delta = new DenseVector(ParametersVector.Count);
            ResultsVector = new DenseVector(ParametersVector.Count);
            BestResultVector = new DenseVector(ParametersVector.Count);
            ParametersVector.CopyTo(ResultsVector);
            ParametersVector.CopyTo(BestResultVector);
            _Lx = new DenseVector(measuredPointsCount);
            _Ly = new DenseVector(measuredPointsCount);
            _M = new DenseVector(measuredPointsCount);

            UpdateAfterParametersChanged();

            //if(DumpingMethodUsed == DumpingMethod.Additive)
            //{
            //    // Compute initial lambda lam = 10^-3*diag(J'J)/size(J'J)
            //    ComputeJacobian(_J);
            //    _J.TransposeToOther(_Jt);
            //    _Jt.MultiplyToOther(_J, _JtJ);
            //    _lam = 1e-3f * _JtJ.Trace() / (double)_JtJ.ColumnCount;
            //}
            //else 
            if(DumpingMethodUsed == DumpingMethod.Multiplicative)
            {
                _lam = 1e-3f;
            }
            else
                _lam = 0.0;

            _w = 0.0;
            _lastResidiual = _currentResidiual;
        }

        public override void UpdateAfterParametersChanged()
        {
            base.UpdateAfterParametersChanged();
            // Compute Lx,Ly,M
            // exi = (p1Xi+p2Yi+p3Zi+p4)/(p9Xi+p10Yi+p11Zi+p12) = Lxi/Mi
            // eyi = (p5Xi+p6Yi+p7Zi+p8)/(p9Xi+p10Yi+p11Zi+p12) = Lyi/Mi
            int measuredPointsCount = (MeasurementsVector.Count) / 5;
            for(int i = 0; i < measuredPointsCount; ++i)
            {
                _Lx.At(i, ResultsVector.At(0) * MeasurementsVector.At(i * 3) +
                    ResultsVector.At(1) * MeasurementsVector.At(i * 3 + 1) +
                    ResultsVector.At(2) * MeasurementsVector.At(i * 3 + 2) +
                    ResultsVector.At(3));

                _Ly.At(i, ResultsVector.At(4) * MeasurementsVector.At(i * 3) +
                    ResultsVector.At(5) * MeasurementsVector.At(i * 3 + 1) +
                    ResultsVector.At(6) * MeasurementsVector.At(i * 3 + 2) +
                    ResultsVector.At(7));

                _M.At(i, ResultsVector.At(8) * MeasurementsVector.At(i * 3) +
                    ResultsVector.At(9) * MeasurementsVector.At(i * 3 + 1) +
                    ResultsVector.At(10) * MeasurementsVector.At(i * 3 + 2) +
                    ResultsVector.At(11));
            }
        }

        Matrix<double> _cm = new DenseMatrix(3, 3);
        public override void ComputeMappingFucntion(Vector<double> mapFuncResult)
        {
            int measuredPointsCount = (MeasurementsVector.Count) / 5;
            // exi = lxi / mi, eyi = lyi / mi
            for(int i = 0; i < measuredPointsCount; ++i)
            {
                mapFuncResult.At(2 * i, _Lx.At(i) / _M.At(i));
                mapFuncResult.At(2 * i + 1, _Ly.At(i) / _M.At(i));
            }

            // Compute s:
            _cm[0, 0] = ResultsVector[0];
            _cm[0, 1] = ResultsVector[1];
            _cm[0, 2] = ResultsVector[2];
            _cm[1, 0] = ResultsVector[4];
            _cm[1, 1] = ResultsVector[5];
            _cm[1, 2] = ResultsVector[6];
            _cm[2, 0] = ResultsVector[8];
            _cm[2, 0] = ResultsVector[9];
            _cm[2, 0] = ResultsVector[10];

            var RQ = _cm.QR();

            double s = RQ.R[0, 1];// / RQ.R[1, 1];
            mapFuncResult[mapFuncResult.Count - 1] = s;
        }

        public override void ComputeErrorVector(Vector<double> error)
        {
            ComputeMappingFucntion(error);
            int measuredPointsCount = (MeasurementsVector.Count) / 5;
            for(int i = 0; i < measuredPointsCount; ++i)
            {
                error.At(2 * i, Math.Abs(MeasurementsVector[3 * measuredPointsCount + 2 * i] - error[2 * i]));
                error.At(2 * i + 1, Math.Abs(MeasurementsVector[3 * measuredPointsCount + 2 * i + 1] - error[2 * i + 1]));
            }
            error[error.Count - 1] = _w * Math.Abs(error[error.Count - 1]);
        }

        public override void ComputeJacobian(Matrix<double> J)
        {
            J.Clear();
            ComputeJacobian_Numerical(J);
        }
        
        public override void Iterate()
        {
            if(CurrentIteration == 1)
            {
                _cm[0, 0] = ResultsVector[0];
                _cm[0, 1] = ResultsVector[1];
                _cm[0, 2] = ResultsVector[2];
                _cm[1, 0] = ResultsVector[4];
                _cm[1, 1] = ResultsVector[5];
                _cm[1, 2] = ResultsVector[6];
                _cm[2, 0] = ResultsVector[8];
                _cm[2, 0] = ResultsVector[9];
                _cm[2, 0] = ResultsVector[10];

                var RQ = _cm.QR();

                double s = RQ.R[0, 1];// / RQ.R[1, 1];

                _w = _currentResidiual / (s*s);

                ComputeErrorVector(_currentErrorVector);
                _currentResidiual = ComputeResidiual();
                MinimumResidiual = _currentResidiual;
            }

            base.Iterate();
            _w = _w * 1.2;
        }
    }
}
