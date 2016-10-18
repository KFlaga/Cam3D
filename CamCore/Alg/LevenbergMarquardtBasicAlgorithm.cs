using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace CamCore
{
    // Base LM algorithm -> jacobian / mapping function must still be supplied
    public abstract class LevenbergMarquardtBaseAlgorithm : MinimalisationAlgorithm
    {
        protected double _lam;

        protected Matrix<double> _J;
        protected Matrix<double> _Jt;
        protected Matrix<double> _JtJ;
        protected Vector<double> _Jte;
        protected Vector<double> _delta;

        public enum DumpingMethod
        {
            Multiplicative, // Diagonal of J'J is scaled by (1+lam)
            Additive, // lam * trace(JtJ)/ParamsCount is added to diagonal
            None
        }

        public DumpingMethod DumpingMethodUsed { get; set; } = DumpingMethod.Multiplicative;

        public override void Init()
        {
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

            if(DumpingMethodUsed == DumpingMethod.Additive)
            {
                // Compute initial lambda lam = 10^-3*diag(J'J)/size(J'J)
                ComputeJacobian(_J);
                _J.TransposeToOther(_Jt);
                _Jt.MultiplyToOther(_J, _JtJ);
                _lam = 1e-3f * _JtJ.Trace() / (double)_JtJ.ColumnCount;
            }
            else if(DumpingMethodUsed == DumpingMethod.Multiplicative)
            {
                _lam = 1e-3f;
            }
            else
                _lam = 0.0;

            Solver = new SvdSolver();
        }

        public override void ComputeDelta(Vector<double> delta)
        {
            // (J'J + lam*diag(JtJ))d = -J'e or (J'E^-1J + lamI)d = -J'E^-1e
            // We have (J'J)* with diagonal scaled with (1+lam)

            // 1) Get jacobian and J', J'e, J'J
            ComputeJacobian(_J);
            _J.TransposeToOther(_Jt);

            if(UseCovarianceMatrix)
            {
                for(int r = 0; r < _Jt.RowCount; ++r)
                {
                    for(int c = 0; c < _Jt.ColumnCount; ++c)
                    {
                        _Jt.At(r, c, _Jt.At(r, c) * InverseVariancesVector.At(c));
                    }
                }
            }

            _Jt.MultiplyToOther(_J, _JtJ);
            _Jt.MultiplyToOther(_currentErrorVector, _Jte);

            if(DumpingMethodUsed == DumpingMethod.Additive)
            {
                for(int i = 0; i < _JtJ.ColumnCount; ++i)
                {
                    _JtJ.At(i, i, _JtJ.At(i, i) + _lam);
                }
            }
            else if(DumpingMethodUsed == DumpingMethod.Multiplicative)
            {
                for(int i = 0; i < _JtJ.ColumnCount; ++i)
                {
                    _JtJ.At(i, i, _JtJ.At(i, i) * (_lam + 1));
                }
            }

            // 2) Solve for delta
            // 2.1) Remove zero colums/rows
            Matrix<double> jtj = _JtJ;

            List<int> zeroColumns = _JtJ.FindZeroColumns();
            if(zeroColumns.Count > 0)
            {
                jtj = jtj.RemoveColumns(zeroColumns);
            }

            List<int> zeroRows = jtj.FindZeroRows();
            Vector<double> rightSideVec = _Jte;
            if(zeroRows.Count > 0)
            {
                jtj = jtj.RemoveRows(zeroRows);
                rightSideVec = _Jte.RemoveElements(zeroRows);
            }

            // 2.2) Use svd to solve equations ( handles rank-deficient as well )
            Solver.EquationsMatrix = jtj;
            Solver.RightSideVector = rightSideVec.Negate();
            Solver.Solve();

            // 2.3) Copy results to delta, varaibles corresponding to zeroed colums set to 0
            if(zeroColumns.Count > 0)
            {
                int zeroIdx = 0;
                int resultIdx = 0;
                int deltaIdx = 0;
                for(; deltaIdx < delta.Count; ++deltaIdx)
                {
                    if(deltaIdx == zeroColumns[zeroIdx])
                    {
                        delta[deltaIdx] = 0.0f;
                        ++zeroIdx;
                        if(zeroIdx == zeroColumns.Count)
                            break;
                    }
                    else
                    {
                        delta[deltaIdx] = Solver.ResultVector[resultIdx];
                        ++resultIdx;
                    }
                }
                ++deltaIdx;

                for(; deltaIdx < delta.Count; ++deltaIdx)
                {
                    delta[deltaIdx] = Solver.ResultVector[resultIdx];
                    ++resultIdx;
                }
            }
            else
                Solver.ResultVector.CopyTo(delta);
        }

        public override void Iterate()
        {
            // Start with computing error
            ComputeErrorVector(_currentErrorVector);
            // Update paramter vector (using (JtJ+IL)D = Jte)
            ComputeDelta(_delta);
            var oldRes = ResultsVector.Clone();
            ResultsVector += _delta;
            UpdateAll();

            _lastResidiual = _currentResidiual;
            // Compute new residiual
            ComputeErrorVector(_currentErrorVector);
            _currentResidiual = ComputeResidiual();
            if(MinimumResidiual > _currentResidiual)
            {
                MinimumResidiual = _currentResidiual;
                ResultsVector.CopyTo(BestResultVector);
            }
            else
            {
                oldRes.CopyTo(ResultsVector);
            }
            
            if(_currentResidiual < MinimumResidiual)
            {
                // Update lambda -> lower only if new residiual is good enough
                // (1% difference as it tends to stuck with high lambda and almost no change in results)
                _lam *= 0.1;
            }
            else if(_currentResidiual < MinimumResidiual * 1.01)
            {
                // Update lambda -> lower only if new residiual is good enough
                // (1% difference as it tends to stuck with high lambda and almost no change in results)
                _lam *= 0.1;
            }
            else if(_currentResidiual >= _lastResidiual)
            {
                _lam *= 10.0;
            }
        }

    }
}
