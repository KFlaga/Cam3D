using System;
using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Collections.Generic;

namespace CamAlgorithms.Calibration
{
    // Parameter vector have form : [p11,p12,...,p34]
    // Result vector have form : [p11,p12,...,p34,eTL1x,eTL1y,eTL1z,...,eTLn,eTRn,eBLn,eBRn]
    // where prc is eP[r,c] and eP is estimated camera matrix and (eTL1,eTR1,eBL1,eBR1) is estimated grid corner points
    // Inital parameters should be P from least-squares solution and supplied grid points
    // Measurment vector have form : [TL1x,TL1y,TL1z,...,TLn,TRn,BLn,BRn,x1,y1,x2,y2,...,xn,yn]
    // Mapping function returns [eTL1x,eTL1y,eTL1z,...,eTLn,eTRn,eBLn,eBRn,eX1,eY1,eZ1,...,exn,eyn]
    public abstract class CameraMatrixGridMinimalisation : LevenbergMarquardtBaseAlgorithm
    {
        public List<RealGridData> CalibrationGrids { get; set; }
        public List<CalibrationPoint> CalibrationPoints { get; set; }
        public bool MinimalizeSkew { get; set; }
        
        protected List<RealGridData> _grids;
        protected Vector<double> _Lx;
        protected Vector<double> _Ly;
        protected Vector<double> _M;
        protected Vector3[] _reals;
        protected double _gridErrorCoef;
        protected double _skewCoeff;

        public abstract int CameraParametersCount { get; }

        public override void Init()
        {
            Vector<double> extenedMesaurementVector = new DenseVector(MeasurementsVector.Count + 1);
            MeasurementsVector.CopySubVectorTo(extenedMesaurementVector, 0, 0, MeasurementsVector.Count);
            MeasurementsVector = extenedMesaurementVector;
            Vector<double> extenedVariancesVector = new DenseVector(InverseVariancesVector.Count + 1);
            InverseVariancesVector.CopySubVectorTo(extenedVariancesVector, 0, 0, InverseVariancesVector.Count);
            InverseVariancesVector = extenedVariancesVector;
            InverseVariancesVector[InverseVariancesVector.Count - 1] = 1.0;
            base.Init();
            
            _grids = new List<RealGridData>(CalibrationGrids);
            _gridErrorCoef = Math.Sqrt(2.0 * CalibrationPoints.Count / (CalibrationGrids.Count * 12.0));
            
            _Lx = new DenseVector(CalibrationPoints.Count);
            _Ly = new DenseVector(CalibrationPoints.Count);
            _M = new DenseVector(CalibrationPoints.Count);
            _reals = new Vector3[CalibrationPoints.Count];

            _skewCoeff = 0;

            UpdateAfterParametersChanged();
        }

        public override void UpdateAfterParametersChanged()
        {
            UpdateGrids();
            UpdateRealPoints();
            UpdateLxLyM();
        }

        protected void UpdateGrids()
        {
            for(int i = 0; i < CalibrationGrids.Count; ++i)
            {
                int gridPos = 12 * i + CameraParametersCount;
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
        }

        protected void UpdateRealPoints()
        {
            for(int i = 0; i < CalibrationPoints.Count; ++i)
            {
                var cp = CalibrationPoints[i];
                var grid = _grids[cp.GridNum];
                var real = grid.GetRealFromCell(cp.RealRow, cp.RealCol);
                _reals[i] = real;
            }
        }

        protected abstract void UpdateLxLyM();

        public override void ComputeMappingFucntion(Vector<double> mapFuncResult)
        {
            // exi = Lxi/Mi, eyi = Lyi/Mi
            for(int i = 0; i < CalibrationPoints.Count; ++i)
            {
                mapFuncResult.At(2 * i, _Lx.At(i) / _M.At(i));
                mapFuncResult.At(2 * i + 1, _Ly.At(i) / _M.At(i));
            }

            int N2 = CalibrationPoints.Count * 2;
            for(int i = 0; i < CalibrationGrids.Count * 12; ++i)
            {
                mapFuncResult.At(N2 + i, ResultsVector.At(CameraParametersCount + i));
            }

            if(MinimalizeSkew)
            {
                mapFuncResult[mapFuncResult.Count - 1] = Math.Abs(GetSkew());
            }
        }

        public override void ComputeErrorVector(Vector<double> error)
        {
            ComputeMappingFucntion(error);

            int N2 = CalibrationPoints.Count * 2;
            for(int i = 0; i < N2; ++i)
            {
                error.At(i, MeasurementsVector.At(i) - error.At(i));
            }

            int N2G = N2 + CalibrationGrids.Count * 12;
            for(int i = N2; i < N2G; ++i)
            {
                error.At(i, _gridErrorCoef * (MeasurementsVector.At(i) - error.At(i)));
            }

            error[error.Count - 1] = _skewCoeff * error[error.Count - 1];
        }

        public override void ComputeJacobian(Matrix<double> J)
        {
            J.Clear();
            ComputeJacobian_Numerical(J);
        }

        public override void Iterate()
        {
            if(CurrentIteration == 1 && MinimalizeSkew)
            {
                _skewCoeff = Math.Sqrt(_currentResidiual) / Math.Abs(GetFy());

                ComputeErrorVector(_currentErrorVector);
                _currentResidiual = ComputeResidiual();
                MinimumResidiual = _currentResidiual;
            }

            base.Iterate();

            if(CurrentIteration < 5)
            {
                double s = GetSkew();
                double oldSkewError = _skewCoeff * _skewCoeff * s * s;
                _skewCoeff = _skewCoeff * 1.2;

                double newSkewError = _skewCoeff * _skewCoeff * s * s;
                double residiualCorrection = newSkewError - oldSkewError;

                _lastResidiual = _lastResidiual + residiualCorrection;
                MinimumResidiual = MinimumResidiual + residiualCorrection;
            }
        }

        protected abstract double GetSkew();
        protected abstract double GetFy();
    }
}
