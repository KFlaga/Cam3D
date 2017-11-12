using System;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace CamAlgorithms.Calibration
{
    // Parameter vector have form : [p11,p12,...,p34,eX1,eY1,eZ1,...,eXn,eZn,eYn]
    // where prc is eP[r,c] and eP is estimated camera matrix and (eXi,eYi,eZi) is estimated real point
    // Inital parameters should be P from least-squares solution and measured real points
    // Measurment vector have form : [X1,Y1,Z1,X2,Y2,Z2,...,Xn,Yn,Zn,x1,y1,x2,y2,...,xn,yn]
    // Mapping function returns [eX1,eY1,eZ1,...,ex1,ey1]
    // Algorithm doesn't make use of form of J matrix or other matrices ( so its simple ) 
    class LMCameraMatrixSimpleMinimalisation : LevenbergMarquardtBaseAlgorithm
    {
        protected Vector<double> _Lx;
        protected Vector<double> _Ly;
        protected Vector<double> _M;

        public Vector<double> RealPoints { get; set; }

        public override void Init()
        {
            int measuredPointsCount = (ParametersVector.Count - 12) / 3;
            //int measuredPointsCount = (RealPoints.Count) / 3;

            // Allocate matrices
            _J = new DenseMatrix(MeasurementsVector.Count, ParametersVector.Count);
            _Jt = new DenseMatrix(ParametersVector.Count, MeasurementsVector.Count);
            _JtJ = new DenseMatrix(ParametersVector.Count, ParametersVector.Count);
            _Jte = new DenseVector(ParametersVector.Count);
            _currentErrorVector = new DenseVector(MeasurementsVector.Count);
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

            _lastResidiual = _currentResidiual;
        }
        
        public override void UpdateAfterParametersChanged()
        {
            base.UpdateAfterParametersChanged();
            // Compute Lx,Ly,M
            // exi = (p1Xi+p2Yi+p3Zi+p4)/(p9Xi+p10Yi+p11Zi+p12) = Lxi/Mi
            // eyi = (p5Xi+p6Yi+p7Zi+p8)/(p9Xi+p10Yi+p11Zi+p12) = Lyi/Mi
            int measuredPointsCount = (ParametersVector.Count - 12) / 3;
            for(int i = 0; i < measuredPointsCount; ++i)
            {
                _Lx.At(i, ResultsVector.At(0) * ResultsVector.At(i * 3 + 12) +
                    ResultsVector.At(1) * ResultsVector.At(i * 3 + 13) +
                    ResultsVector.At(2) * ResultsVector.At(i * 3 + 14) +
                    ResultsVector.At(3));

                _Ly.At(i, ResultsVector.At(4) * ResultsVector.At(i * 3 + 12) +
                    ResultsVector.At(5) * ResultsVector.At(i * 3 + 13) +
                    ResultsVector.At(6) * ResultsVector.At(i * 3 + 14) +
                    ResultsVector.At(7));

                _M.At(i, ResultsVector.At(8) * ResultsVector.At(i * 3 + 12) +
                    ResultsVector.At(9) * ResultsVector.At(i * 3 + 13) +
                    ResultsVector.At(10) * ResultsVector.At(i * 3 + 14) +
                    ResultsVector.At(11));
            }
            //int measuredPointsCount = (RealPoints.Count) / 3;
            //for(int i = 0; i < measuredPointsCount; ++i)
            //{
            //    _Lx.At(i, ResultsVector.At(0) * RealPoints.At(i * 3) +
            //        ResultsVector.At(1) * RealPoints.At(i * 3 + 1) +
            //        ResultsVector.At(2) * RealPoints.At(i * 3 + 2) +
            //        ResultsVector.At(3));

            //    _Ly.At(i, ResultsVector.At(4) * RealPoints.At(i * 3) +
            //        ResultsVector.At(5) * RealPoints.At(i * 3 + 1) +
            //        ResultsVector.At(6) * RealPoints.At(i * 3 + 2) +
            //        ResultsVector.At(7));

            //    _M.At(i, ResultsVector.At(8) * RealPoints.At(i * 3) +
            //        ResultsVector.At(9) * RealPoints.At(i * 3 + 1) +
            //        ResultsVector.At(10) * RealPoints.At(i * 3 + 2) +
            //        ResultsVector.At(11));
            //}
        }

        public override void ComputeMappingFucntion(Vector<double> mapFuncResult)
        {
            for(int i = 0; i < ParametersVector.Count - 12; ++i)
            {
                mapFuncResult.At(i, ResultsVector.At(i + 12));
            }

            // exi = Lxi/Mi, eyi = Lyi/Mi
            int measuredPointsCount = (ParametersVector.Count - 12) / 3;
            for(int i = 0; i < measuredPointsCount; ++i)
            {
                mapFuncResult.At(measuredPointsCount * 3 + 2 * i, _Lx.At(i) / _M.At(i));
                mapFuncResult.At(measuredPointsCount * 3 + 2 * i + 1, _Ly.At(i) / _M.At(i));
            }

            // exi = Lxi/Mi, eyi = Lyi/Mi
            //int measuredPointsCount = (RealPoints.Count) / 3;
            //for(int i = 0; i < measuredPointsCount; ++i)
            //{
            //    mapFuncResult.At(2 * i, _Lx.At(i) / _M.At(i));
            //    mapFuncResult.At(2 * i + 1, _Ly.At(i) / _M.At(i));
            //}
        }


        public override void ComputeJacobian(Matrix<double> J)
        {
            J.Clear();

            if(DoComputeJacobianNumerically)
            {
                ComputeJacobian_Numerical(J);
            }
            else
            {
                ComputeJacobian_Analitic(J);
            }
        }

        public void ComputeJacobian_Analitic(Matrix<double> J)
        {
            //                              | 0     Jrr |
            // Jacobian have block form J = |           |
            //                              | Jip   Jir |
            // Where:
            // 0 is 3n x 12 matrix of zeros
            // Jrr is d(||eXi-Xi||^2)/d(eXi) 3n x 3n identity matrix
            // Jip is d(||exi-xi||^2)/d(pi) 2n x 12 matrix, mostly filled
            // Jir is d(||exi-xi||^2)/d(eXi) 2n x 3n matrix, non-zero only for real point i

            int N = (ParametersVector.Count - 12) / 3; // measuredPointsCount
            // First set Jrr -> put 1 on diagonal
            for(int r = 0; r < N * 3; ++r)
            {
                J.At(r, r + 12, 1.0f);
            }

            double Mi, Mi_1, Mi2_1, Lxi, Lyi, Xi, Yi, Zi;
            int posx, posy;
            for(int i = 0; i < N; ++i)
            {
                Mi = _M.At(i);
                Mi_1 = Math.Abs(Mi) < 1e-12f ? 0.0f : 1.0f / Mi;
                Mi2_1 = Mi_1 * Mi_1;
                Lxi = _Lx.At(i);
                Lyi = _Ly.At(i);
                Xi = ResultsVector.At(i * 3 + 12);
                Yi = ResultsVector.At(i * 3 + 13);
                Zi = ResultsVector.At(i * 3 + 14);
                posx = N * 3 + i;
                posy = posx + 1;
                // Compute Jip for each exi,eyi
                J.At(posx, 0, Xi * Mi_1); // d(exi)/d(p1) = eXi / Mi
                J.At(posx, 1, Yi * Mi_1); // d(exi)/d(p2) = eYi / Mi
                J.At(posx, 2, Zi * Mi_1); // d(exi)/d(p3) = eZi / Mi
                J.At(posx, 3, Mi_1);      // d(exi)/d(p4) = 1 / Mi
                // d(exi)/d(p5-8) = 0
                J.At(posx, 8, -Xi * Lxi * Mi2_1); // d(exi)/d(p9) = -eXi*Lxi / Mi^2
                J.At(posx, 9, -Yi * Lxi * Mi2_1); // d(exi)/d(p10) = -eYi*Lxi / Mi^2
                J.At(posx, 10, -Zi * Lxi * Mi2_1);// d(exi)/d(p11) = -eZi*Lxi / Mi^2
                J.At(posx, 11, -Lxi * Mi2_1);    // d(exi)/d(p12) = -Lxi / Mi^2

                // d(eyi)/d(p1-4) = 0
                J.At(posy, 4, Xi * Mi_1); // d(eyi)/d(p5) = eXi / Mi
                J.At(posy, 5, Yi * Mi_1); // d(eyi)/d(p6) = eYi / Mi
                J.At(posy, 6, Zi * Mi_1); // d(eyi)/d(p7) = eZi / Mi
                J.At(posy, 7, 1 * Mi_1);  // d(eyi)/d(p8) = 1 / Mi

                J.At(posy, 8, -Xi * Lyi * Mi2_1); // d(eyi)/d(p9) = -eXi*Lyi / Mi^2
                J.At(posy, 9, -Yi * Lyi * Mi2_1); // d(eyi)/d(p10) = -eYi*Lyi / Mi^2
                J.At(posy, 10, -Zi * Lyi * Mi2_1);// d(eyi)/d(p11) = -eZi*Lyi / Mi^2
                J.At(posy, 11, -Lyi * Mi2_1);     // d(eyi)/d(p12) = -Lyi / Mi^2

                // Compute Jir for each exi,eyi 
                J.At(posx, i * 3 + 12, // d(exi)/d(Xi) = p1Mi - p9Lxi / Mi^2
                    (ResultsVector.At(0) * Mi - ResultsVector.At(8) * Lxi) * Mi2_1);
                J.At(posx, i * 3 + 13, // d(exi)/d(Yi) = p2Mi - p10Lxi / Mi^2
                    (ResultsVector.At(1) * Mi - ResultsVector.At(9) * Lxi) * Mi2_1);
                J.At(posx, i * 3 + 14, // d(exi)/d(Zi) = p3Mi - p11Lxi / Mi^2
                    (ResultsVector.At(2) * Mi - ResultsVector.At(10) * Lxi) * Mi2_1);
                J.At(posy, i * 3 + 12, // d(eyi)/d(Xi) = p5Mi - p9Lyi / Mi^2
                    (ResultsVector.At(4) * Mi - ResultsVector.At(8) * Lyi) * Mi2_1);
                J.At(posy, i * 3 + 13, // d(eyi)/d(Yi) = p6Mi - p10Lyi / Mi^2
                    (ResultsVector.At(5) * Mi - ResultsVector.At(9) * Lyi) * Mi2_1);
                J.At(posy, i * 3 + 14, // d(eyi)/d(Zi) = p7Mi - p11Lyi / Mi^2
                    (ResultsVector.At(6) * Mi - ResultsVector.At(10) * Lyi) * Mi2_1);
            }
        }

        
    }
}
