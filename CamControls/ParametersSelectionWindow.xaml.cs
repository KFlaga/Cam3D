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
    public partial class ParametersSelectionWindow : Window
    {
        public bool Accepted { get; set; }

        IParametrizedProcessor _processor;
        public IParametrizedProcessor Processor
        {
            get { return _processor; }
            set
            {
                _processor = value;
                _processor.InitParameters();
                _parametersPanel.SetParameters(_processor.Parameters);

                _labelTitle.Content = "Set parameters for: " + _processor.ToString();
            }
        }

        public ParametersSelectionWindow()
        {
            InitializeComponent();
        }

        public void Accept(object sender, RoutedEventArgs e)
        {
            Accepted = true;

            if(_processor == null)
            {
                MessageBox.Show("Processor not set - aborting.");
                Accepted = false;
            }
            else
                _processor.UpdateParameters();

            Hide();
        }

        public void Cancel(object sender, RoutedEventArgs e)
        {
            Accepted = false;
            Hide();
        }
    }
}
