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

namespace CalibrationModule
{
    /// <summary>
    /// Interaction logic for SetImageSizeWindow.xaml
    /// </summary>
    public partial class SetImageSizeWindow : Window
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

        public SetImageSizeWindow()
        {
            InitializeComponent();
        }

        private void Accept(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            if(_tbX.Text.Length <= 0 || _tbY.Text.Length <= 0)
            {
                MessageBox.Show("Size must be greater than 0 - aborting");
                return;
            }

            X = int.Parse(_tbX.Text);
            Y = int.Parse(_tbY.Text);
            Close();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
