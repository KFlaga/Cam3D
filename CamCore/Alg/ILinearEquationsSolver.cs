using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace CamCore
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
