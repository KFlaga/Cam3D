using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;

namespace CamCore
{
    // TODO: ADD XmlSerizalizable interface
    
    // Interface for arbitrary algorithm etc. that can be parametrized using list of ProcessorParameters
    public interface IParameterizable : INamed
    {
        [XmlIgnore]
        List<IAlgorithmParameter> Parameters { get; }
        // Creates and fills with default values list of parameters
        void InitParameters();
        // Sets new parameters 
        void UpdateParameters();
    }
}