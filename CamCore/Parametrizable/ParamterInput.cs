using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace CamCore
{
    public class ParameterValueChangedEventArgs : EventArgs
    {
        public object NewValue { get; set; }
    }

    public interface IParameterInput
    {
        UIElement UIInput { get; }
        object ActualValue { get; }
        AlgorithmParameter Parameter { get; }

        event EventHandler<ParameterValueChangedEventArgs> InputValueChanged;
    }

    public abstract class ParameterInput<T> : IParameterInput
    {
        public abstract UIElement UIInput { get; }
        public object ActualValue { get { return Value; } }
        public abstract T Value { get; }

        protected AlgorithmParameter<T> _parameter;
        public AlgorithmParameter Parameter { get { return _parameter; } }

        public abstract event EventHandler<ParameterValueChangedEventArgs> InputValueChanged;

        public ParameterInput(AlgorithmParameter<T> parameter)
        {
            _parameter = parameter;
        }
    }
}
