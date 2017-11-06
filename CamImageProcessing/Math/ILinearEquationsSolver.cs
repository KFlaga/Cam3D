using MathNet.Numerics.LinearAlgebra;

namespace CamAlgorithms
{
    // Linear equation solver interface for equations : Ax = b 
    public interface ILinearEquationsSolver
    {
        Matrix<double> EquationsMatrix { set; } // May change after solving, so it should be cloned if needed later (depends on subclass)
        Vector<double> RightSideVector { set; } // May change after solving, so it should be cloned if needed later (depends on subclass)
        Vector<double> ResultVector { get; }

        void Solve();
    }
}
