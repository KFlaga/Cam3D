using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using CamCore;

namespace CamAlgorithms
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
            // Additive, // lam * trace(JtJ)/ParamsCount is added to diagonal
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
            
            _lam = DumpingMethodUsed == DumpingMethod.Multiplicative ? 1e-3 : 0.0;
        }

        public override void ComputeDelta(Vector<double> delta)
        {
            // (J'J + lam*diag(JtJ))d = -J'e or (J'E^-1J + lamI)d = -J'E^-1e
            // We have (J'J)* with diagonal scaled with (1+lam)

            // 1) Get jacobian and J', J'e, J'J
            ComputeJacobian(_J);
            if(EndIterationIfJacobianIsZero(delta)) { return; }

            _J.TransposeToOther(_Jt);
            MultiplyJacobianByConvarianceMatrix(_Jt);

            _Jt.MultiplyToOther(_J, _JtJ);
            _Jt.MultiplyToOther(_currentErrorVector, _Jte);

            for(int i = 0; i < _JtJ.ColumnCount; ++i)
            {
                _JtJ.At(i, i, _JtJ.At(i, i) * (_lam + 1.0));
            }

            // 2) Solve for delta
            // 2.1) Remove zero colums/rows
            List<int> zeroColumns;
            Vector<double> jte;
            Matrix<double> jtj = RemoveZeroRowsAndColumnsFromJacobian(out zeroColumns, out jte);

            // 2.2) Use svd to solve equations ( handles rank-deficient as well )
            _linearSolver.EquationsMatrix = jtj;
            _linearSolver.RightSideVector = jte.Negate();
            _linearSolver.Solve();

            // 2.3) Copy results to delta, varaibles corresponding to zeroed colums set to 0
            if(zeroColumns.Count > 0)
            {
                ZeroDeltaForCorrespondingZeroedJacobianColumn(delta, zeroColumns);
            }
            else
            {
                _linearSolver.ResultVector.CopyTo(delta);
            }
        }
        
        private void MultiplyJacobianByConvarianceMatrix(Matrix<double> jt)
        {
            if(UseCovarianceMatrix)
            {
                for(int r = 0; r < jt.RowCount; ++r)
                {
                    for(int c = 0; c < jt.ColumnCount; ++c)
                    {
                        jt.At(r, c, jt.At(r, c) * InverseVariancesVector.At(c));
                    }
                }
            }
        }

        private bool EndIterationIfJacobianIsZero(Vector<double> delta)
        {
            if(_J.AbsoulteMaximum().Item3 < float.Epsilon)
            {
                delta.MultiplyThis(0.0);
                CurrentIteration = MaximumIterations + 1;
                return true;
            }
            return false;
        }

        private Matrix<double> RemoveZeroRowsAndColumnsFromJacobian(out List<int> zeroColumns, out Vector<double> jte)
        {
            Matrix<double> jtj = _JtJ;
            zeroColumns = jtj.FindZeroColumns();
            if(zeroColumns.Count > 0)
            {
                jtj = jtj.RemoveColumns(zeroColumns);
            }

            List<int> zeroRows = jtj.FindZeroRows();
            jte = _Jte;
            if(zeroRows.Count > 0)
            {
                jtj = jtj.RemoveRows(zeroRows);
                jte = _Jte.RemoveElements(zeroRows);
            }

            return jtj;
        }

        private void ZeroDeltaForCorrespondingZeroedJacobianColumn(Vector<double> delta, List<int> zeroColumns)
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
                    if(zeroIdx == zeroColumns.Count) { break; }
                }
                else
                {
                    delta[deltaIdx] = _linearSolver.ResultVector[resultIdx];
                    ++resultIdx;
                }
            }
            ++deltaIdx;

            for(; deltaIdx < delta.Count; ++deltaIdx)
            {
                delta[deltaIdx] = _linearSolver.ResultVector[resultIdx];
                ++resultIdx;
            }
        }

        public override void Iterate()
        {
            // Start with computing error
            ComputeErrorVector(_currentErrorVector);
            // Update paramter vector (using (JtJ+IL)D = Jte)
            ComputeDelta(_delta);
            var oldRes = ResultsVector.Clone();
            ResultsVector += _delta;
            UpdateAfterParametersChanged();

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

            UpdateLambda();
        }

        protected virtual void UpdateLambda()
        {
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
                _lam *= 0.5;
            }
            else if(_currentResidiual >= _lastResidiual)
            {
                _lam *= 10.0;
            }
        }

        public override void ComputeJacobian(Matrix<double> J)
        {
            ComputeJacobian_Numerical(J);
            ComputeErrorVector(_currentErrorVector);
        }
        
        public void ComputeJacobian_Numerical(Matrix<double> J)
        {
            Vector<double> error_n = new DenseVector(_currentErrorVector.Count);
            Vector<double> error_p = new DenseVector(_currentErrorVector.Count);
            for(int k = 0; k < ParametersVector.Count; ++k)
            {
                double oldK = ResultsVector[k];
                double k_n = Math.Abs(oldK) > float.Epsilon ? oldK * (1 - NumericalDerivativeStep) : -NumericalDerivativeStep * 0.01;
                double k_p = Math.Abs(oldK) > float.Epsilon ? oldK * (1 + NumericalDerivativeStep) : NumericalDerivativeStep * 0.01;

                ResultsVector[k] = k_n;
                UpdateAfterParametersChanged();
                ComputeErrorVector(error_n);

                ResultsVector[k] = k_p;
                UpdateAfterParametersChanged();
                ComputeErrorVector(error_p);

                Vector<double> diff_e = 1.0 / (k_p - k_n) * (error_p - error_n);
                J.SetColumn(k, diff_e);

                ResultsVector[k] = oldK;

                // Throw if NaN found in jacobian
                bool nanInfFound = diff_e.Exists((e) => { return double.IsNaN(e) || double.IsInfinity(e); });
                if(nanInfFound)
                {
                    throw new DivideByZeroException("NaN or Infinity found on jacobian");
                }
            }

            UpdateAfterParametersChanged();
        }
    }
}
