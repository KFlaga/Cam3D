using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;

namespace CamCore
{
    // Works like one variable but function takes argument vector X and returns single value
    public class MultivariateFunctionMinimalisation
    {
        public bool UseBFGSMethod { get; set; } = false; // BFGS method do not require hessian

        public Vector<double> InitialParameters { get; set; }

        public double MinimalValue { get; set; }
        public Vector<double> MinimalParameters { get; set; }

        public bool DoComputeDerivativesNumerically { get; set; } = false;
        public double NumericalDerivativeStep { get; set; } = 1e-4;

        public int MaximumIterations { get; set; } = 100; // End iteration condition : max interations are reached
        public int CurrentIteration { get { return _currentIteration; } }
        protected int _currentIteration;

        double _convergenceRate; // ??

        public delegate void InitFunction(Vector<double> paramVector);
        public delegate double FunctionComputer(Vector<double> paramVector);
        public delegate Vector<double> JacobianComputer(Vector<double> paramVector);
        public delegate Matrix<double> HessianComputer(Vector<double> paramVector);
        public InitFunction IterationInit { get; set; } // Optional delegate called before each iteration
        public FunctionComputer Function { get; set; } // Delegate which computes function in interest
        public JacobianComputer Jacobian { get; set; } // Delegate which computes function 1st derivative
        public HessianComputer Hessian { get; set; } // Delegate which computes function 2nd derivative

        Vector<double> _x;
        double _fun;
        Vector<double> _diff1;
        Matrix<double> _diff2;

        public MultivariateFunctionMinimalisation()
        {
            IterationInit = EmptyIt;
        }

        public virtual void Process()
        {
            _currentIteration = 0;

            Init();

            do
            {
                _currentIteration += 1;
                Iterate();
            }
            while(CheckIterationEndConditions() == false);
        }

        protected virtual void Init()
        {
            _x = InitialParameters;
            IterationInit(_x);
            _fun = Function(_x);
            MinimalParameters = _x;
            MinimalValue = _fun;

            _diff1 = new DenseVector(_x.Count);
            _diff2 = new DenseMatrix(_x.Count);
        }

        public void ComputeJacobian_Numerical()
        {
            double x_n, x_p;
            for(int i = 0; i < _x.Count; ++i)
            {
                double x = _x.At(i);
                if(Math.Abs(x) > float.Epsilon)
                {
                    x_n = x * (1 - NumericalDerivativeStep);
                    x_p = x * (1 + NumericalDerivativeStep);
                }
                else
                {
                    x_n = -NumericalDerivativeStep * 0.01;
                    x_p = NumericalDerivativeStep * 0.01;
                }

                _x.At(i, x_n);
                double fun_n = Function(_x);
                _x.At(i, x_p);
                double fun_p = Function(_x);
                double h = (x_p - x_n);
                // f'(x) = f(x+h)-f(x-h)/2h
                _diff1.At(i, (fun_p - fun_n) / (2 * h));

                _x.At(i, x);
            }
            // f''(x) = f(x+h)-2f(x)+f(x-h)/2h^2
          //  _diff2 = (fun_p + fun_n - 2 * _fun) / (h * h);
        }

        public void ComputeHessian_Numerical()
        {
            double xi_n, xi_p, xk_n, xk_p, xi, xk, fun_p, fun_n, hi, hk;
            for(int i = 0; i < _x.Count; ++i)
            {
                xi = _x.At(i);
                if(Math.Abs(xi) > float.Epsilon)
                {
                    xi_n = xi * (1 - NumericalDerivativeStep);
                    xi_p = xi * (1 + NumericalDerivativeStep);
                }
                else
                {
                    xi_n = -NumericalDerivativeStep * 0.01;
                    xi_p = NumericalDerivativeStep * 0.01;
                }
                hi = (xi_p - xi_n);

                for(int k = 0; k < _x.Count; ++k)
                {
                    xk = _x.At(k);
                    if(i == k)
                    {
                        // ∂^2f/∂xi^2 = f(X+hi) - 2f(X) + f(X-hi) / hi^2
                        _x.At(i, xi_n);
                        fun_n = Function(_x);
                        _x.At(i, xi_p);
                        fun_p = Function(_x);
                        _diff2.At(i, k, (fun_p + fun_n - 2 * _fun) / (2 * hi));
                    }
                    else
                    {
                        // X = [x1 ... xn]
                        // X + hk = [x1 ... xk+hk ... xn]
                        // let gi(xk) = ∂f/∂xi (X)
                        // ∂gi/∂xk (xk) ≈ (g(xk + h1) − g(xk − h1)) / 2*hk
                        // ∂^2f/∂xi∂xk (X) ≈ (∂f/∂xi (X+hk) − ∂f/∂xi (X-hk)) / 2*hk
                        // ∂f/∂xi (X+hk) ≈ f(X+[hi,hk]) − f(X+[-hi,hk]) / 2*hi
                        // ∂f/∂xi (X-hk) ≈ f(X-[-hi,hk]) − f(X-[hi,hk]) / 2*hi
                        xk = _x.At(k);
                        if(Math.Abs(xk) > float.Epsilon)
                        {
                            xk_n = xk * (1 - NumericalDerivativeStep);
                            xk_p = xk * (1 + NumericalDerivativeStep);
                        }
                        else
                        {
                            xk_n = -NumericalDerivativeStep * 0.01;
                            xk_p = NumericalDerivativeStep * 0.01;
                        }

                        _x.At(k, xk_n);
                        _x.At(i, xi_n);
                        fun_n = Function(_x);
                        _x.At(i, xi_p);
                        fun_p = Function(_x);                   
                        double dfdxi_n = (fun_p - fun_n) / (2 * hi);

                        _x.At(k, xk_p);
                        _x.At(i, xi_n);
                        fun_n = Function(_x);
                        _x.At(i, xi_p);
                        fun_p = Function(_x);                 
                        double dfdxi_p = (fun_p - fun_n) / (2 * hi);

                        hk = (xk_p - xk_n);
                        _diff2.At(i, k, (dfdxi_p - dfdxi_n) / (2 * hk));
                    }
                    _x.At(k, xk);
                }
                _x.At(i, xi);
            }
        }

        protected virtual bool CheckIterationEndConditions()
        {
            return _currentIteration > MaximumIterations ||
                _diff1.L1Norm() < float.Epsilon;
        }
        
        ILinearEquationsSolver _solver = new GaussSolver();
        protected void Iterate()
        {
            IterationInit(_x);

            Vector<double> dx;
            if(UseBFGSMethod)
                dx = IterateBFGS();
            else
                dx = IterateNewton();
            
            _x = _x - dx;
            _fun = Function(_x);

            if(_fun < MinimalValue)
            {
                MinimalValue = _fun;
                MinimalParameters = _x;
            }

            //TODO :
            // things to consider :
            // === DONE === if diff1 ~= 0 : algorithms stops -> done in check conditions
            // if diff2 ~= 0 : we have to use gradient descent with some small step
        }

        protected Vector<double> IterateNewton()
        {
            if(DoComputeDerivativesNumerically)
            {
                ComputeJacobian_Numerical();
                ComputeHessian_Numerical();
            }
            else
            {
                _diff1 = Jacobian(_x);
                _diff2 = Hessian(_x);
            }

            Vector<double> dx;
            if(_diff1.Count <= 3)
            {
                dx = _diff2.Inverse() * _diff1;
            }
            else
            {
                _solver.EquationsMatrix = _diff2;
                _solver.RightSideVector = _diff1;
                _solver.Solve();
                dx = _solver.ResultVector;
            }

            return dx;
        }

        protected Vector<double> IterateBFGS()
        {
            if(DoComputeDerivativesNumerically)
            {
                ComputeJacobian_Numerical();
                ComputeHessian_Numerical();
            }
            else
            {
                _diff1 = Jacobian(_x);
                _diff2 = Hessian(_x);
            }

            Vector<double> dx;
            if(_diff1.Count <= 3)
            {
                dx = _diff2.Inverse() * _diff1;
            }
            else
            {
                _solver.EquationsMatrix = _diff2;
                _solver.RightSideVector = _diff1;
                _solver.Solve();
                dx = _solver.ResultVector;
            }

            return dx;
        }

        private void EmptyIt(Vector<double> p)
        {

        }
    }
}
