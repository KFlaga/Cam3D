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
using static CamControls.ParametrizedProcessorsSelectionPanel;

namespace CamControls
{
    public partial class ParametrizedProcessorsSelectionWindow : Window
    {
        public bool Accepted { get; set; }
        private bool _firstTimeShown = true;

        public ParametrizedProcessorsSelectionWindow()
        {
            InitializeComponent();

            this.IsVisibleChanged += (s, e) =>
            {
                if((bool)e.NewValue == true && _firstTimeShown)
                {
                    _firstTimeShown = false;
                    foreach(var family in _panel._processorFamilies)
                    {
                        family.Value.ComboProcessors.SelectedIndex = 0;
                    }
                }
            };
        }
        
        public void AddProcessorFamily(string familyName)
        {
            _panel.AddProcessorFamily(familyName);
        }

        public void AddToFamily(string familyName, IParameterizable processor)
        {
            _panel.AddToFamily(familyName, processor);
        }

        public IParameterizable GetSelectedProcessor(string family)
        {
            return _panel.GetSelectedProcessor(family);
        }

        public void Accept(object sender, RoutedEventArgs e)
        {
            Accepted = true;
            foreach(var family in _panel._processorFamilies)
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
