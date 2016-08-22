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
    public partial class ParametrizedProcessorsSelectionWindow : Window
    {
        class ProcessorFamily
        {
            public int Index { get; set; }
            public string Name { get; set; }
            public ComboBox ComboProcessors { get; set; }
            public Label LabelName { get; set; }
            public ParametersSelectionPanel ParametersPanel { get; set; }

            public IParametrizedProcessor SelectedProcessor { get; set; }

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
                SelectedProcessor = (IParametrizedProcessor)e.AddedItems[0];
                ParametersPanel.SetParameters(SelectedProcessor.Parameters);
            }
        }

        public bool Accepted { get; set; }
        private Dictionary<string, ProcessorFamily> _processorFamilies = new Dictionary<string, ProcessorFamily>();
        private bool _firstTimeShown = true;

        public ParametrizedProcessorsSelectionWindow()
        {
            InitializeComponent();

            this.IsVisibleChanged += (s, e) =>
            {
                if((bool)e.NewValue == true && _firstTimeShown)
                {
                    _firstTimeShown = false;
                    foreach(var family in _processorFamilies)
                    {
                        family.Value.ComboProcessors.SelectedIndex = 0;
                    }
                }
            };
        }

        public void AddProcessorFamily(string familyName)
        {
            var family = new ProcessorFamily(_mainPanel, _processorFamilies.Count, familyName);
            _processorFamilies.Add(familyName, family);
        }

        public void AddToFamily(string familyName, IParametrizedProcessor processor)
        {
            processor.InitParameters();
            var family = _processorFamilies[familyName];
            family.ComboProcessors.Items.Add(processor);
        }

        public IParametrizedProcessor GetSelectedProcessor(string family)
        {
            return _processorFamilies[family].SelectedProcessor;
        }

        public void Accept(object sender, RoutedEventArgs e)
        {
            Accepted = true;
            foreach(var family in _processorFamilies)
            {
                if(family.Value.SelectedProcessor != null)
                {
                    family.Value.SelectedProcessor.UpdateParameters();
                }
                else
                {
                    MessageBox.Show("Processor from family " + family.Value.Name + " not selected - aborting.");
                    Accepted = false;
                    break;
                }
            }
            Hide();
        }

        public void Cancel(object sender, RoutedEventArgs e)
        {
            Accepted = false;
            Hide();
        }
    }
}
