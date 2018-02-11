using System;

namespace CamCore
{
    // Class that minimises function f(x) over one parameter x
    // Base class uses basic Newton 2nd derivative algorithm
    //
    public class OneVariableMinimisation
    {
        public double InitialParameter { get; set; }

        public double MinimalValue { get; set; }
        public double MinimalParameter { get; set; }

        public bool DoComputeDerivativesNumerically { get; set; } = false;
        public double NumericalDerivativeStep { get; set; } = 1e-4;

        public int MaximumIterations { get; set; } = 100; // End iteration condition : max interations are reached
        public int CurrentIteration { get { return _currentIteration; } }
        protected int _currentIteration;

        double _convergenceRate; // ??

        public delegate double FunctionComputer(double paramValue);
        public FunctionComputer Function { get; set; } // Delegate which computes function in interest
        public FunctionComputer Derivative_1st { get; set; } // Delegate which computes function 1st derivative
        public FunctionComputer Derivative_2nd { get; set; } // Delegate which computes function 2nd derivative

        double _x;
        double _fun;
        double _diff1;
        double _diff2;

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

        public virtual void Init()
        {
            _x = InitialParameter;
            _fun = Function(_x);
            MinimalParameter = _x;
            MinimalValue = _fun;
        }

        public void ComputeDerivatives_Numerical()
        {
            double x_n, x_p;
            if(Math.Abs(_x) > float.Epsilon)
            {
                x_n = _x * (1 - NumericalDerivativeStep);
                x_p = _x * (1 + NumericalDerivativeStep);
            }
            else
            {
                x_n = -NumericalDerivativeStep * 0.01;
                x_p = NumericalDerivativeStep * 0.01;
            }
            
            double fun_n = Function(x_n);
            double fun_p = Function(x_p);

            double h = (x_p - x_n);
            // f'(x) = f(x+h)-f(x-h)/2h
            _diff1 = (fun_p - fun_n) / (2*h);
            // f''(x) = f(x+h)-2f(x)+f(x-h)/h^2
            _diff2 = (fun_p + fun_n - 2 * _fun) / (h * h);
        }

        public virtual bool CheckIterationEndConditions()
        {
            return _currentIteration > MaximumIterations ||
                Math.Abs(_diff1) < float.Epsilon;
        }

        public void Iterate()
        {
            if(DoComputeDerivativesNumerically)
            {
                ComputeDerivatives_Numerical();
            }
            else
            {
                _diff1 = Derivative_1st(_x);
                _diff2 = Derivative_2nd(_x);
            }

            double dx = -_diff1 / _diff2;
            _x = _x + dx;
            _fun = Function(_x);

            if(_fun < MinimalValue)
            {
                MinimalValue = _fun;
                MinimalParameter = _x;
            }

            //TODO :
            // things to consider :
            // === DONE === if diff1 ~= 0 : algorithms stops -> done in check conditions
            // if diff2 ~= 0 : we have to use gradient descent with some small step
        }
    }
}
