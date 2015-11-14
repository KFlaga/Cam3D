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
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class ColorPicker : Window
    {
        private Color _pickedColor;
        public Color PickedColor
        {
            get
            {
                return _pickedColor;
            }
            set
            {
                _pickedColor = value;
                UpdateColor();
            }
        } 

        public ColorPicker()
        {
            _pickedColor = Colors.White;
            InitializeComponent();
            UpdateColor();
        }

        private void UpdateColor()
        {
            _tbAlpha.SetNumber((uint)_pickedColor.A);
            _tbBlue.SetNumber((uint)_pickedColor.B);
            _tbGreen.SetNumber((uint)_pickedColor.G);
            _tbRed.SetNumber((uint)_pickedColor.R);
        }

        private void _tbAlpha_TextChanged(object sender, TextChangedEventArgs e)
        {
            _pickedColor.A = (byte)_tbAlpha.CurrentValue;
        }

        private void _tbBlue_TextChanged(object sender, TextChangedEventArgs e)
        {
            _pickedColor.B = (byte)_tbBlue.CurrentValue;
        }

        private void _tbGreen_TextChanged(object sender, TextChangedEventArgs e)
        {
            _pickedColor.G = (byte)_tbGreen.CurrentValue;
        }

        private void _tbRed_TextChanged(object sender, TextChangedEventArgs e)
        {
            _pickedColor.R = (byte)_tbRed.CurrentValue;
        }

        private void Accept(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
    }
}
