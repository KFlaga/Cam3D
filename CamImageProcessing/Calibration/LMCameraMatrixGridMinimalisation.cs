using System;
using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Collections.Generic;

namespace CamAlgorithms
{
    // Parameter vector have form : [p11,p12,...,p34]
    // Result vector have form : [p11,p12,...,p34,eTL1x,eTL1y,eTL1z,...,eTLn,eTRn,eBLn,eBRn]
    // where prc is eP[r,c] and eP is estimated camera matrix and (eTL1,eTR1,eBL1,eBR1) is estimated grid corner points
    // Inital parameters should be P from least-squares solution and supplied grid points
    // Measurment vector have form : [TL1x,TL1y,TL1z,...,TLn,TRn,BLn,BRn,x1,y1,x2,y2,...,xn,yn]
    // Mapping function returns [eTL1x,eTL1y,eTL1z,...,eTLn,eTRn,eBLn,eBRn,eX1,eY1,eZ1,...,exn,eyn]
    public class LMCameraMatrixGridMinimalisation : LevenbergMarquardtBaseAlgorithm
    {
        public List<RealGridData> CalibrationGrids { get; set; }
        public List<CalibrationPoint> CalibrationPoints { get; set; }

        protected List<RealGridData> _grids;
        protected Vector<double> _Lx;
        protected Vector<double> _Ly;
        protected Vector<double> _M;
        protected Vector3[] _reals;
        protected double _gridErrorCoef;

        private int GetGridCornerIdx_Measurments(int grid, int corner)
        {
            return 4 * grid + corner;
        }

        private int GetGridCornerIdx_Parameters(int grid, int corner)
        {
            return 12 + 4 * grid + corner;
        }

        public override void Init()
        {
            ResultsVector = new DenseVector(ParametersVector.Count + CalibrationGrids.Count * 12);
            BestResultVector = new DenseVector(ParametersVector.Count + CalibrationGrids.Count * 12);
            ParametersVector.CopySubVectorTo(ResultsVector, 0, 0, 12);
            for(int i = 0; i < CalibrationGrids.Count; ++i)
            {
                ResultsVector.At(12 + i * 12, CalibrationGrids[i].TopLeft.X);
                ResultsVector.At(12 + i * 12 + 1, CalibrationGrids[i].TopLeft.Y);
                ResultsVector.At(12 + i * 12 + 2, CalibrationGrids[i].TopLeft.Z);
                ResultsVector.At(12 + i * 12 + 3, CalibrationGrids[i].TopRight.X);
                ResultsVector.At(12 + i * 12 + 4, CalibrationGrids[i].TopRight.Y);
                ResultsVector.At(12 + i * 12 + 5, CalibrationGrids[i].TopRight.Z);
                ResultsVector.At(12 + i * 12 + 6, CalibrationGrids[i].BotLeft.X);
                ResultsVector.At(12 + i * 12 + 7, CalibrationGrids[i].BotLeft.Y);
                ResultsVector.At(12 + i * 12 + 8, CalibrationGrids[i].BotLeft.Z);
                ResultsVector.At(12 + i * 12 + 9, CalibrationGrids[i].BotRight.X);
                ResultsVector.At(12 + i * 12 + 10, CalibrationGrids[i].BotRight.Y);
                ResultsVector.At(12 + i * 12 + 11, CalibrationGrids[i].BotRight.Z);
            }
            ResultsVector.CopyTo(BestResultVector);
            _grids = new List<RealGridData>(CalibrationGrids);
            _gridErrorCoef = Math.Sqrt((double)CalibrationPoints.Count / (double)(CalibrationGrids.Count * 12.0));

            _currentErrorVector = new DenseVector(CalibrationGrids.Count * 12 + CalibrationPoints.Count * 2);
            _J = new DenseMatrix(_currentErrorVector.Count, ResultsVector.Count);
            _Jt = new DenseMatrix(ResultsVector.Count, _currentErrorVector.Count);
            _JtJ = new DenseMatrix(ResultsVector.Count, ResultsVector.Count);
            _Jte = new DenseVector(ResultsVector.Count);
            _delta = new DenseVector(ResultsVector.Count);
            _Lx = new DenseVector(CalibrationPoints.Count);
            _Ly = new DenseVector(CalibrationPoints.Count);
            _M = new DenseVector(CalibrationPoints.Count);
            _reals = new Vector3[CalibrationPoints.Count];

            UpdateAll();

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
            Solver = new SvdSolver();
        }

        public override void UpdateAll()
        {
            base.UpdateAll();
            // 1) Update grids
            for(int i = 0; i < CalibrationGrids.Count; ++i)
            {
                int gridPos = 12 * i + 12;
                _grids[i].TopLeft.X = ResultsVector.At(gridPos);
                _grids[i].TopLeft.Y = ResultsVector.At(gridPos + 1);
                _grids[i].TopLeft.Z = ResultsVector.At(gridPos + 2);
                _grids[i].TopRight.X = ResultsVector.At(gridPos + 3);
                _grids[i].TopRight.Y = ResultsVector.At(gridPos + 4);
                _grids[i].TopRight.Z = ResultsVector.At(gridPos + 5);
                _grids[i].BotLeft.X = ResultsVector.At(gridPos + 6);
                _grids[i].BotLeft.Y = ResultsVector.At(gridPos + 7);
                _grids[i].BotLeft.Z = ResultsVector.At(gridPos + 8);
                _grids[i].BotRight.X = ResultsVector.At(gridPos + 9);
                _grids[i].BotRight.Y = ResultsVector.At(gridPos + 10);
                _grids[i].BotRight.Z = ResultsVector.At(gridPos + 11);
            }
            
            // 2) Update real points
            for(int i = 0; i < CalibrationPoints.Count; ++i)
            {
                var cp = CalibrationPoints[i];
                var grid = _grids[cp.GridNum];
                var real = grid.GetRealFromCell(cp.RealRow, cp.RealCol);
                _reals[i] = grid.GetRealFromCell(cp.RealRow, cp.RealCol);
            }

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

        public override void ComputeMappingFucntion(Vector<double> mapFuncResult)
        {
            // exi = Lxi/Mi, eyi = Lyi/Mi
            //for(int i = 0; i < CalibrationPoints.Count; ++i)
            //{
            //    mapFuncResult.At(2 * i, _Lx.At(i) / _M.At(i));
            //    mapFuncResult.At(2 * i + 1, _Ly.At(i) / _M.At(i));
            //}
        }

        public override void ComputeErrorVector(Vector<double> error)
        {
            int N2 = CalibrationPoints.Count * 2;
            for(int i = 0; i < CalibrationPoints.Count; ++i)
            {
                error.At(2 * i, MeasurementsVector.At(2 * i) - _Lx.At(i) / _M.At(i));
                error.At(2 * i + 1, MeasurementsVector.At(2 * i + 1) - _Ly.At(i) / _M.At(i));
            }

            for(int i = 0; i < CalibrationGrids.Count; ++i)
            {
                var grid = _grids[i];
                error.At(N2 + 12 * i, _gridErrorCoef * (grid.TopLeft.X - MeasurementsVector.At(N2 + 12 * i)));
                error.At(N2 + 12 * i + 1, _gridErrorCoef * (grid.TopLeft.Y - MeasurementsVector.At(N2 + 12 * i + 1)));
                error.At(N2 + 12 * i + 2, _gridErrorCoef * (grid.TopLeft.Z - MeasurementsVector.At(N2 + 12 * i + 2)));
                error.At(N2 + 12 * i + 3, _gridErrorCoef * (grid.TopRight.X - MeasurementsVector.At(N2 + 12 * i + 3)));
                error.At(N2 + 12 * i + 4, _gridErrorCoef * (grid.TopRight.Y - MeasurementsVector.At(N2 + 12 * i + 4)));
                error.At(N2 + 12 * i + 5, _gridErrorCoef * (grid.TopRight.Z - MeasurementsVector.At(N2 + 12 * i + 5)));
                error.At(N2 + 12 * i + 6, _gridErrorCoef * (grid.BotLeft.X - MeasurementsVector.At(N2 + 12 * i + 6)));
                error.At(N2 + 12 * i + 7, _gridErrorCoef * (grid.BotLeft.Y - MeasurementsVector.At(N2 + 12 * i + 7)));
                error.At(N2 + 12 * i + 8, _gridErrorCoef * (grid.BotLeft.Z - MeasurementsVector.At(N2 + 12 * i + 8)));
                error.At(N2 + 12 * i + 9, _gridErrorCoef * (grid.BotRight.X - MeasurementsVector.At(N2 + 12 * i + 9)));
                error.At(N2 + 12 * i + 10, _gridErrorCoef * (grid.BotRight.Y - MeasurementsVector.At(N2 + 12 * i + 10)));
                error.At(N2 + 12 * i + 11, _gridErrorCoef * (grid.BotRight.Z - MeasurementsVector.At(N2 + 12 * i + 11)));
            }
        }

        public override void ComputeJacobian(Matrix<double> J)
        {
            J.Clear();
            ComputeJacobian_Numerical(J);
        }
        
    }
}
