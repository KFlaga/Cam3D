using CamCore;
using System.Windows;

namespace CamControls
{
    public partial class ParametrizableSelectionWindow : Window
    {
        public bool Accepted { get; set; }
        public IParameterizable Selected { get { return _panel.Selected; } }
        private bool _firstTimeShown = true;

        public ParametrizableSelectionWindow(string name = "")
        {
            InitializeComponent();
            _panel.NameOfParametrizable = name;

            this.IsVisibleChanged += (s, e) =>
            {
                if((bool)e.NewValue == true && _firstTimeShown)
                {
                    _firstTimeShown = false;
                    _panel.ParametrizablesCombo.SelectedIndex = 0;
                }
            };
        }

        public void AddParametrizable(IParameterizable processor)
        {
            _panel.AddParametrizable(processor);
        }

        public void Accept(object sender, RoutedEventArgs e)
        {
            Accepted = true;
            if(_panel.Selected != null)
            {
                _panel.Selected.UpdateParameters();
            }
            else
            {
                MessageBox.Show("Parametrizable not selected - aborting.");
                Accepted = false;
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
