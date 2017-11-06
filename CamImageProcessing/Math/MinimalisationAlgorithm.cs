using MathNet.Numerics.LinearAlgebra;

namespace CamAlgorithms
{
    // Base for algorithms that minimise problem : find P so that ||f(P)-X||^2 is minimal,
    // where P is parameter vector, X is measurement vector and f(P) is arbitrary mapping function
    // Norm is Euqlidean or Mahalanobis if measurments' covariance matrix is supplied
    // Result of minimalisation ( estimated P ) is stored in ResultsVector
    public abstract class MinimalisationAlgorithm
    {
        public Vector<double> MeasurementsVector { get; set; } // May change after processing
        public Vector<double> ParametersVector { get; set; } // May change after processing
        public Vector<double> ResultsVector { get; protected set; }
        public Vector<double> BestResultVector { get; protected set; }
        public ILinearEquationsSolver Solver { get; set; }
        protected Vector<double> _currentErrorVector;

        public bool UseCovarianceMatrix { get; set; } = false;
        public Vector<double> InverseVariancesVector { get; set; }

        public bool DoComputeJacobianNumerically { get; set; } = false;
        public double NumericalDerivativeStep { get; set; } = 1e-6;

        public int MaximumIterations { get; set; } = 100; // End iteration condition : max interations are reached
        public int CurrentIteration { get { return _currentIteration; } }
        protected int _currentIteration;

        public double MaximumResidiual { get; set; } = 0.0; // End iteration condition : residiual ||f(P)-X||^2 is smaller that this
        protected double _currentResidiual;
        protected double _lastResidiual;

        public double MinimumResidiual { get; set; }
        public double BaseResidiual { get; set; }

        public bool Terminate { get; set; } = false; // Set to true to break after next iteration

        // Executes whole algorithm -> MeasurementsVector, ParametersVector and Solver 
        // must be set before calling this
        public virtual void Process()
        {
            _currentIteration = 0;

            Init();

            // Find base residual
            UpdateAll();
            ComputeErrorVector(_currentErrorVector);
            _currentResidiual = ComputeResidiual();
            _lastResidiual = _currentResidiual;
            MinimumResidiual = _currentResidiual;
            BaseResidiual = _currentResidiual;

            while(CheckIterationEndConditions() == false)
            {
                _currentIteration += 1;

                Iterate();
            }
        }

        // Optional initialisation function ( like setting inital parameters )
        public virtual void Init()
        {
            ResultsVector = ParametersVector;
        }

        // Updates algorithm (like recomputing estimated corrected measurments) i.e. after 
        // ResultVector has been changed
        public virtual void UpdateAll()
        {

        }

        // Computes mapping function f(P), where P is paramter vector taken from ParametersVector
        // Assumes mapFuncResult is allocated and of correct size
        public abstract void ComputeMappingFucntion(Vector<double> mapFuncResult);

        // Computes error vector e = f(P) - X, where f is mapping function and X is taken from MeasurementsVector
        // Assumes error is allocated and of correct size
        public virtual void ComputeErrorVector(Vector<double> error)
        {
            ComputeMappingFucntion(error);
            var error1 = MeasurementsVector - error;
            error1.CopyTo(error);
        }

        // Computes ||f(P) - X||^2 = e' * e or e' * E^-1 * e if cov matrix is used
        public virtual double ComputeResidiual()
        {
            if(UseCovarianceMatrix)
            {
                _currentResidiual = _currentErrorVector.
                    PointwiseMultiply(InverseVariancesVector).
                    DotProduct(_currentErrorVector);
            }
            else
            {
                _currentResidiual = _currentErrorVector.
                    DotProduct(_currentErrorVector);
            }

            return _currentResidiual;
        }

        // Computes jacobian J of f(P) over ParametersVector ( might be not needed, so its not abstract )
        // Assumes J is allocated and of correct size
        public virtual void ComputeJacobian(Matrix<double> J) { }

        // Computes delta -> correction to parameter vector
        public abstract void ComputeDelta(Vector<double> delta);

        public virtual bool CheckIterationEndConditions()
        {
            return Terminate == true ||
                _currentIteration > MaximumIterations ||
                _currentResidiual < MaximumResidiual;
        }

        // Performs one iteration of algorithm : computes prameter vector correction and updates it
        // Should also set _currentResidiual to be residiual after correction
        public abstract void Iterate();
    }
}
