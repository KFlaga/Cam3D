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
        object Value { get; }
        IAlgorithmParameter Parameter { get; }

        event EventHandler<ParameterValueChangedEventArgs> InputValueChanged;
    }

    public abstract class ParameterInput<T> : IParameterInput
    {
        public abstract UIElement UIInput { get; }
        public object Value { get { return ActualValue; } }
        public abstract T ActualValue { get; }

        protected AlgorithmParameter<T> _parameter;
        public IAlgorithmParameter Parameter { get { return _parameter; } }

        public abstract event EventHandler<ParameterValueChangedEventArgs> InputValueChanged;

        public ParameterInput(AlgorithmParameter<T> parameter)
        {
            _parameter = parameter;
        }
    }
}
