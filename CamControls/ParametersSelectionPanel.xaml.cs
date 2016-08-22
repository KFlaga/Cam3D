using CamCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CamControls
{
    public partial class ParametersSelectionPanel : UserControl
    {
        public ParametersSelectionPanel()
        {
            InitializeComponent();
        }

        public void SetParameters(List<ProcessorParameter> paramters)
        {
            _mainPanel.Children.Clear();
            foreach(var parameter in paramters)
            {
                _mainPanel.Children.Add(CreateOptionSelector(parameter));
            }
        }

        public UIElement CreateOptionSelector(ProcessorParameter parameter)
        {
            DockPanel optPanel = new DockPanel();
            optPanel.Height = 25;

            Label name = new Label();
            name.Content = parameter.Name;
            DockPanel.SetDock(name, Dock.Left);

            if(parameter.TypeName.Contains("Boolean"))
            {
                CheckBox checkBox = new CheckBox();
                checkBox.IsChecked = (bool)parameter.DefaultValue;
                checkBox.Checked += (s, e) => { parameter.ActualValue = true; };
                checkBox.Unchecked += (s, e) => { parameter.ActualValue = false; };
                checkBox.HorizontalAlignment = HorizontalAlignment.Center;
                checkBox.HorizontalContentAlignment = HorizontalAlignment.Center;
                checkBox.VerticalAlignment = VerticalAlignment.Center;

                DockPanel.SetDock(checkBox, Dock.Right);
                optPanel.Children.Add(checkBox);
            }
            else
            {
                NumberTextBox textBox = null;

                if(parameter.TypeName.Contains("UInt"))
                {
                    textBox = new UnsignedIntegerTextBox();
                }
                else if(parameter.TypeName.Contains("Int"))
                {
                    textBox = new IntegerTextBox();
                }
                else if(parameter.TypeName.Contains("Single"))
                {
                    textBox = new SingleTextBox();
                }
                else if(parameter.TypeName.Contains("Double"))
                {
                    textBox = new DoubleTextBox();
                }

                textBox.SetNumber(parameter.DefaultValue);
                textBox.SetMinMaxValues(parameter.MinValue, parameter.MaxValue);
                textBox.MinWidth = 100;
                textBox.HorizontalAlignment = HorizontalAlignment.Center;
                textBox.HorizontalContentAlignment = HorizontalAlignment.Center;
                textBox.VerticalAlignment = VerticalAlignment.Center;
                DockPanel.SetDock(textBox, Dock.Right);
                optPanel.Children.Add(textBox);

                textBox.TextChanged += (s, e) =>
                {
                    parameter.ActualValue = textBox.GetNumber();
                };
            }

            optPanel.Children.Add(name);

            return optPanel;
        }
    }
}
