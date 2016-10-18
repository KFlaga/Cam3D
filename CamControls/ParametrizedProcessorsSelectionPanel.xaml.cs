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
    public partial class ParametrizedProcessorsSelectionPanel : UserControl
    {
        internal class ProcessorFamily
        {
            public int Index { get; set; }
            public string Name { get; set; }
            public ComboBox ComboProcessors { get; set; }
            public Label LabelName { get; set; }
            public ParametersSelectionPanel ParametersPanel { get; set; }

            public IParameterizable SelectedProcessor { get; set; }

            public ProcessorFamily(Panel parentPanel, int idx, string name)
            {
                Index = idx;
                Name = name;
                SelectedProcessor = null;

                LabelName = new Label();
                LabelName.Content = "Choose " + name;
                DockPanel.SetDock(LabelName, Dock.Top);
                LabelName.Height = 30.0;
                LabelName.HorizontalContentAlignment = HorizontalAlignment.Center;
                parentPanel.Children.Add(LabelName);

                ComboProcessors = new ComboBox();
                DockPanel.SetDock(ComboProcessors, Dock.Top);
                ComboProcessors.Height = 25.0;
                ComboProcessors.Margin = new Thickness(30.0, 5.0, 30.0, 5.0);
                ComboProcessors.HorizontalContentAlignment = HorizontalAlignment.Center;
                parentPanel.Children.Add(ComboProcessors);

                ComboProcessors.SelectionChanged += ComboProcessors_SelectionChanged;

                ParametersPanel = new ParametersSelectionPanel();
                ParametersPanel.MinHeight = 20.0;
                DockPanel.SetDock(ParametersPanel, Dock.Top);
                parentPanel.Children.Add(ParametersPanel);
            }

            private void ComboProcessors_SelectionChanged(object sender, SelectionChangedEventArgs e)
            {
                SelectedProcessor = (IParameterizable)e.AddedItems[0];
                ParametersPanel.SetParameters(SelectedProcessor.Parameters);
            }
        }

        internal Dictionary<string, ProcessorFamily> _processorFamilies = new Dictionary<string, ProcessorFamily>();

        public ParametrizedProcessorsSelectionPanel()
        {
            InitializeComponent();
        }

        public void AddProcessorFamily(string familyName)
        {
            var family = new ProcessorFamily(_mainPanel, _processorFamilies.Count, familyName);
            _processorFamilies.Add(familyName, family);
        }

        public void AddToFamily(string familyName, IParameterizable processor)
        {
            processor.InitParameters();
            var family = _processorFamilies[familyName];
            family.ComboProcessors.Items.Add(processor);
        }

        public IParameterizable GetSelectedProcessor(string family)
        {
            return _processorFamilies[family].SelectedProcessor;
        }

    }
}
