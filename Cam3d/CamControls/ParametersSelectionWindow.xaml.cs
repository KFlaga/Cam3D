using CamCore;
using System.Windows;

namespace CamControls
{
    // TODO: add auto size adjust
    public partial class ParametersSelectionWindow : Window
    {
        public bool Accepted { get; set; }

        IParameterizable _processor;
        public IParameterizable Processor
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
            {
                _processor.UpdateParameters();
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
