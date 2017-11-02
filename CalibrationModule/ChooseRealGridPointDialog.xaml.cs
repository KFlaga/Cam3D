using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace CalibrationModule
{
    /// <summary>
    /// Interaction logic for ChooseRealGridPointDialog.xaml
    /// </summary>
    public partial class ChooseRealGridPointDialog : Window
    {
        private int _x;
        public int X
        {
            get
            {
                return _x;
            }
            set
            {
                _x = value;
                _tbX.Text = _x.ToString();
            }
        }

        private int _y;
        public int Y
        {
            get
            {
                return _y;
            }
            set
            {
                _y = value;
                _tbY.Text = _y.ToString();
            }
        }

        private int _gridnum;
        public int GridNum
        {
            get
            {
                return _gridnum;
            }
            set
            {
                _gridnum = value;
                _tbGridNum.Text = _gridnum.ToString();
            }
        }

        public ChooseRealGridPointDialog()
        {
            InitializeComponent();
        }

        private void ValidateIsInteger(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+"); //regex that matches disallowed text
            e.Handled = regex.IsMatch(e.Text);
        }

        private void Accept(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            if (_tbX.Text.Length == 0 || _tbY.Text.Length == 0 || _tbGridNum.Text.Length == 0)
                return;

            X = int.Parse(_tbX.Text);
            Y = int.Parse(_tbY.Text);
            GridNum = int.Parse(_tbGridNum.Text);
            Close();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
