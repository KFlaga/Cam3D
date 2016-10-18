using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CamCore;
using System.Windows.Controls;
using System.Windows.Media;

namespace CamControls
{
    public class IntParameterInput : ParameterInput<int>
    {
        private IntegerTextBox _inputTextBox = new IntegerTextBox();
        private Label _parameterLabel = new Label();
        private DockPanel _panel = new DockPanel();
        public override UIElement UIInput
        {
            get
            {
                return _panel;
            }
        }

        public override int Value
        {
            get
            {
                return _inputTextBox.CurrentValue;
            }
        }

        public override event EventHandler<ParameterValueChangedEventArgs> InputValueChanged;

        public IntParameterInput(AlgorithmParameter<int> parameter) : base(parameter)
        {
            if(parameter.MaxValue == parameter.MinValue)
            {
                // Parameter is not limiter
                _inputTextBox.LimitValue = false;
            }
            else
            {
                _inputTextBox.LimitValue = true;
                _inputTextBox.MinValue = parameter.MinValue;
                _inputTextBox.MaxValue = parameter.MaxValue;
            }

            _inputTextBox.CurrentValue = parameter.Value;
            _inputTextBox.ValueChanged += _inputTextBox_ValueChanged;

            _panel.Height = 25.0;

            _inputTextBox.MinWidth = 100.0;
            _inputTextBox.HorizontalAlignment = HorizontalAlignment.Center;
            _inputTextBox.HorizontalContentAlignment = HorizontalAlignment.Center;
            _inputTextBox.VerticalAlignment = VerticalAlignment.Center;
            DockPanel.SetDock(_inputTextBox, Dock.Right);

            _parameterLabel.Content = parameter.Name;
            DockPanel.SetDock(_parameterLabel, Dock.Left);

            _panel.Children.Add(_parameterLabel);
            _panel.Children.Add(_inputTextBox);
        }

        private void _inputTextBox_ValueChanged(object sender, NumberTextBoxValueChangedEventArgs<int> e)
        {
            InputValueChanged?.Invoke(this, new ParameterValueChangedEventArgs()
            {
                NewValue = e.NewValue
            });
        }
    }

    public class FloatParameterInput : ParameterInput<float>
    {
        private SingleTextBox _inputTextBox = new SingleTextBox();
        private Label _parameterLabel = new Label();
        private DockPanel _panel = new DockPanel();
        public override UIElement UIInput
        {
            get
            {
                return _panel;
            }
        }

        public override float Value
        {
            get
            {
                return _inputTextBox.CurrentValue;
            }
        }

        public override event EventHandler<ParameterValueChangedEventArgs> InputValueChanged;

        public FloatParameterInput(AlgorithmParameter<float> parameter) : base(parameter)
        {
            if(parameter.MaxValue == parameter.MinValue)
            {
                // Parameter is not limiter
                _inputTextBox.LimitValue = false;
            }
            else
            {
                _inputTextBox.LimitValue = true;
                _inputTextBox.MinValue = parameter.MinValue;
                _inputTextBox.MaxValue = parameter.MaxValue;
            }

            _inputTextBox.CurrentValue = parameter.Value;
            _inputTextBox.ValueChanged += _inputTextBox_ValueChanged;

            _panel.Height = 25.0;

            _inputTextBox.MinWidth = 100.0;
            _inputTextBox.HorizontalAlignment = HorizontalAlignment.Center;
            _inputTextBox.HorizontalContentAlignment = HorizontalAlignment.Center;
            _inputTextBox.VerticalAlignment = VerticalAlignment.Center;
            DockPanel.SetDock(_inputTextBox, Dock.Right);

            _parameterLabel.Content = parameter.Name;
            DockPanel.SetDock(_parameterLabel, Dock.Left);

            _panel.Children.Add(_parameterLabel);
            _panel.Children.Add(_inputTextBox);
        }

        private void _inputTextBox_ValueChanged(object sender, NumberTextBoxValueChangedEventArgs<float> e)
        {
            InputValueChanged?.Invoke(this, new ParameterValueChangedEventArgs()
            {
                NewValue = e.NewValue
            });
        }
    }

    public class DoubleParameterInput : ParameterInput<double>
    {
        private DoubleTextBox _inputTextBox = new DoubleTextBox();
        private Label _parameterLabel = new Label();
        private DockPanel _panel = new DockPanel();
        public override UIElement UIInput
        {
            get
            {
                return _panel;
            }
        }

        public override double Value
        {
            get
            {
                return _inputTextBox.CurrentValue;
            }
        }

        public override event EventHandler<ParameterValueChangedEventArgs> InputValueChanged;

        public DoubleParameterInput(AlgorithmParameter<double> parameter) : base(parameter)
        {
            if(parameter.MaxValue == parameter.MinValue)
            {
                // Parameter is not limiter
                _inputTextBox.LimitValue = false;
            }
            else
            {
                _inputTextBox.LimitValue = true;
                _inputTextBox.MinValue = parameter.MinValue;
                _inputTextBox.MaxValue = parameter.MaxValue;
            }

            _inputTextBox.CurrentValue = parameter.Value;
            _inputTextBox.ValueChanged += _inputTextBox_ValueChanged;

            _panel.Height = 25.0;

            _inputTextBox.MinWidth = 100.0;
            _inputTextBox.HorizontalAlignment = HorizontalAlignment.Center;
            _inputTextBox.HorizontalContentAlignment = HorizontalAlignment.Center;
            _inputTextBox.VerticalAlignment = VerticalAlignment.Center;
            DockPanel.SetDock(_inputTextBox, Dock.Right);

            _parameterLabel.Content = parameter.Name;
            DockPanel.SetDock(_parameterLabel, Dock.Left);

            _panel.Children.Add(_parameterLabel);
            _panel.Children.Add(_inputTextBox);
        }

        private void _inputTextBox_ValueChanged(object sender, NumberTextBoxValueChangedEventArgs<double> e)
        {
            InputValueChanged?.Invoke(this, new ParameterValueChangedEventArgs()
            {
                NewValue = e.NewValue
            });
        }
    }

    public class BooleanParameterInput : ParameterInput<bool>
    {
        private CheckBox _inputCheckBox = new CheckBox();
        private Label _parameterLabel = new Label();
        private DockPanel _panel = new DockPanel();
        public override UIElement UIInput
        {
            get
            {
                return _panel;
            }
        }

        public override bool Value
        {
            get
            {
                return _inputCheckBox.IsChecked.HasValue && _inputCheckBox.IsChecked.Value;
            }
        }

        public override event EventHandler<ParameterValueChangedEventArgs> InputValueChanged;

        public BooleanParameterInput(AlgorithmParameter<bool> parameter) : base(parameter)
        {
            _inputCheckBox.IsChecked = parameter.Value;
            _inputCheckBox.Checked += _inputCheckBox_Checked;
            _inputCheckBox.Unchecked += _inputCheckBox_Unchecked;

            _panel.Height = 25.0;

            _inputCheckBox.HorizontalAlignment = HorizontalAlignment.Center;
            _inputCheckBox.HorizontalContentAlignment = HorizontalAlignment.Center;
            _inputCheckBox.VerticalAlignment = VerticalAlignment.Center;
            DockPanel.SetDock(_inputCheckBox, Dock.Right);

            _parameterLabel.Content = parameter.Name;
            DockPanel.SetDock(_parameterLabel, Dock.Left);

            _panel.Children.Add(_parameterLabel);
            _panel.Children.Add(_inputCheckBox);
        }

        private void _inputCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            InputValueChanged?.Invoke(this, new ParameterValueChangedEventArgs()
            {
                NewValue = true
            });
        }

        private void _inputCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            InputValueChanged?.Invoke(this, new ParameterValueChangedEventArgs()
            {
                NewValue = false
            });
        }
    }
    
    public class StringParameterInput : ParameterInput<string>
    {
        private TextBox _inputTextBox = new TextBox();
        private Label _parameterLabel = new Label();
        private DockPanel _panel = new DockPanel();
        public override UIElement UIInput
        {
            get
            {
                return _panel;
            }
        }

        public override string Value
        {
            get
            {
                return _inputTextBox.Text;
            }
        }

        public override event EventHandler<ParameterValueChangedEventArgs> InputValueChanged;

        public StringParameterInput(AlgorithmParameter<string> parameter) : base(parameter)
        {
            _inputTextBox.Text = parameter.Value;
            _inputTextBox.TextChanged += _inputTextBox_TextChanged;

            _panel.Height = 25.0;

            _inputTextBox.MinWidth = 100.0;
            _inputTextBox.HorizontalAlignment = HorizontalAlignment.Center;
            _inputTextBox.HorizontalContentAlignment = HorizontalAlignment.Center;
            _inputTextBox.VerticalAlignment = VerticalAlignment.Center;
            DockPanel.SetDock(_inputTextBox, Dock.Right);

            _parameterLabel.Content = parameter.Name;
            DockPanel.SetDock(_parameterLabel, Dock.Left);

            _panel.Children.Add(_parameterLabel);
            _panel.Children.Add(_inputTextBox);
        }

        private void _inputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            InputValueChanged?.Invoke(this, new ParameterValueChangedEventArgs()
            {
                NewValue = _inputTextBox.Text
            });
        }
    }
    
    public class ComboParameterInput : ParameterInput<object>
    {
        private ComboBox _inputComboBox = new ComboBox();
        private Label _parameterLabel = new Label();
        private StackPanel _panel = new StackPanel();
        public override UIElement UIInput
        {
            get
            {
                return _panel;
            }
        }

        public override object Value
        {
            get
            {
                return ((DictionaryParameter)_parameter).
                    ValuesMap[(string)_inputComboBox.SelectedItem];
            }
        }

        public override event EventHandler<ParameterValueChangedEventArgs> InputValueChanged;

        public ComboParameterInput(DictionaryParameter parameter) : base(parameter)
        {
            foreach(var item in parameter.ValuesMap)
            {
                _inputComboBox.Items.Add(item.Key);
            }
            _inputComboBox.SelectedIndex = 0;
            parameter.Value = Value;
            _inputComboBox.SelectionChanged += _inputComboBox_SelectionChanged;

            _panel.Height = 55.0;
            _panel.MinWidth = 120.0;
            _panel.Orientation = Orientation.Vertical;

            _inputComboBox.Height = 30.0;
            _inputComboBox.HorizontalAlignment = HorizontalAlignment.Center;
            _inputComboBox.HorizontalContentAlignment = HorizontalAlignment.Center;
            _inputComboBox.VerticalAlignment = VerticalAlignment.Center;

            _parameterLabel.Content = parameter.Name;

            _panel.Children.Add(_parameterLabel);
            _panel.Children.Add(_inputComboBox);
        }

        private void _inputComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            InputValueChanged?.Invoke(this, new ParameterValueChangedEventArgs()
            {
                NewValue = Value
            });
        }
    }
    
    public class ParametrizableParameterInput : ParameterInput<IParameterizable>
    {
        private ComboBox _inputComboBox = new ComboBox();
        private Label _parameterLabel = new Label();
        private StackPanel _panel = new StackPanel();
        private ParametersSelectionPanel _parametersPanel = new ParametersSelectionPanel();
        public override UIElement UIInput
        {
            get
            {
                return _panel;
            }
        }

        public override IParameterizable Value
        {
            get
            {
                return (IParameterizable)_inputComboBox.SelectedItem;
            }
        }

        public override event EventHandler<ParameterValueChangedEventArgs> InputValueChanged;

        public ParametrizableParameterInput(ParametrizedObjectParameter parameter) : base(parameter)
        {
            foreach(var item in parameter.Parameterizables)
            {
                _inputComboBox.Items.Add(item);
            }

            _panel.MinHeight = 65.0;
            _panel.MinWidth = 120.0;
            _panel.Orientation = Orientation.Vertical;

            _inputComboBox.Height = 30.0;
            _inputComboBox.HorizontalAlignment = HorizontalAlignment.Center;
            _inputComboBox.HorizontalContentAlignment = HorizontalAlignment.Center;
            _inputComboBox.VerticalAlignment = VerticalAlignment.Center;

            _parameterLabel.MinHeight = 25.0;
            _parameterLabel.Content = parameter.Name;
            _parameterLabel.HorizontalContentAlignment = HorizontalAlignment.Center;

            _parametersPanel.MinHeight = 10.0;
            _parametersPanel.HorizontalAlignment = HorizontalAlignment.Center;
            _parametersPanel.Background = new SolidColorBrush(Colors.AliceBlue);

            _panel.Children.Add(_parameterLabel);
            _panel.Children.Add(_inputComboBox);
            _panel.Children.Add(_parametersPanel);

            _inputComboBox.SelectedIndex = 0;
            _parametersPanel.SetParameters(Value.Parameters);
            _parameter.Value = Value;

            _inputComboBox.SelectionChanged += _inputComboBox_SelectionChanged;
        }

        private void _inputComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _parametersPanel.SetParameters(Value.Parameters);
            InputValueChanged?.Invoke(this, new ParameterValueChangedEventArgs()
            {
                NewValue = Value
            });
        }
    }
}
