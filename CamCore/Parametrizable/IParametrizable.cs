using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace CamCore
{
    // Interface for arbitrary algorithm etc. that can be parametrized using list of ProcessorParameters
    public interface IParameterizable
    {
        List<AlgorithmParameter> Parameters { get; }
        // Creates and fills with default values list of parameters
        void InitParameters();
        // Sets new parameters 
        void UpdateParameters();
    }
}