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
    public partial class ParametersSelectionPanel : StackPanel
    {
        public delegate IParameterInput InputCreator(AlgorithmParameter parameter);
        public static Dictionary<string, InputCreator> InputCreators = new Dictionary<string, InputCreator>()
        {
             { typeof(int).Name, (p) => { return new IntParameterInput(p as AlgorithmParameter<int>); } },
             { typeof(float).Name, (p) => { return new FloatParameterInput(p as AlgorithmParameter<float>); } },
             { typeof(double).Name, (p) => { return new DoubleParameterInput(p as AlgorithmParameter<double>); }},
             { typeof(bool).Name, (p) => { return new BooleanParameterInput(p as AlgorithmParameter<bool>); }},
             { typeof(string).Name, (p) => { return new StringParameterInput(p as AlgorithmParameter<string>); }},
             { typeof(Vector2).Name, (p) => { return new Vector2ParameterInput(p as AlgorithmParameter<Vector2>); }},
             { typeof(Vector3).Name, (p) => { return new Vector3ParameterInput(p as AlgorithmParameter<Vector3>); }},
             { "Dictionary", (p) => { return new ComboParameterInput(p as DictionaryParameter); }},
             { "IParameterizable",  (p) => { return new ParametrizableParameterInput(p as ParametrizedObjectParameter); }}
        };

        public ParametersSelectionPanel()
        {
            InitializeComponent();
        }

        public void SetParameters(List<AlgorithmParameter> paramters)
        {
            this.Children.Clear();
            foreach(var parameter in paramters)
            {
                var input = InputCreators[parameter.TypeName](parameter);
                parameter.Input = input;
                this.Children.Add(input.UIInput);
            }
        }
    }
}
