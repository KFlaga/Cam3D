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
        public delegate IParameterInput InputCreator(AlgorithmParameter parameter);
        public static Dictionary<string, InputCreator> InputCreators = new Dictionary<string, InputCreator>()
        {
             { typeof(int).Name, CreateInput_Int},
             { typeof(float).Name, CreateInput_Float},
             { typeof(double).Name, CreateInput_Double},
             { typeof(bool).Name, CreateInput_Boolean},
             { typeof(string).Name, CreateInput_String},
             { "Dictionary", CreateInput_Combo},
             { "IParameterizable", CreateInput_Parametrizable}
        };

        public static IParameterInput CreateInput_Int(AlgorithmParameter parameter)
        {
            return new IntParameterInput(parameter as AlgorithmParameter<int>);
        }

        public static IParameterInput CreateInput_Float(AlgorithmParameter parameter)
        {
            return new FloatParameterInput(parameter as AlgorithmParameter<float>);
        }

        public static IParameterInput CreateInput_Double(AlgorithmParameter parameter)
        {
            return new DoubleParameterInput(parameter as AlgorithmParameter<double>);
        }

        public static IParameterInput CreateInput_Boolean(AlgorithmParameter parameter)
        {
            return new BooleanParameterInput(parameter as AlgorithmParameter<bool>);
        }

        public static IParameterInput CreateInput_String(AlgorithmParameter parameter)
        {
            return new StringParameterInput(parameter as AlgorithmParameter<string>);
        }

        public static IParameterInput CreateInput_Combo(AlgorithmParameter parameter)
        {
            return new ComboParameterInput(parameter as DictionaryParameter);
        }

        public static IParameterInput CreateInput_Parametrizable(AlgorithmParameter parameter)
        {
            return new ParametrizableParameterInput(parameter as ParametrizedObjectParameter);
        }

        public ParametersSelectionPanel()
        {
            InitializeComponent();
        }

        public void SetParameters(List<AlgorithmParameter> paramters)
        {
            _mainPanel.Children.Clear();
            foreach(var parameter in paramters)
            {
                var input = InputCreators[parameter.TypeName](parameter);
                parameter.Input = input;
                _mainPanel.Children.Add(input.UIInput);
            }
        }
    }
}
