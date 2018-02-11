using CamControls;
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

namespace ImageMatchingModule
{
    /// <summary>
    /// Interaction logic for RangeSelectionPanel.xaml
    /// </summary>
    public partial class RangeSelectionPanel : Window
    {
        DisparityRange _rangeX;
        public DisparityRange RangeX
        {
            get { return _rangeX; }
            set
            {
                _rangeX = value;
                _maxXActualText.Text = _rangeX.Max.ToString();
                _minXActualText.Text = _rangeX.Min.ToString();
                _maxXSetText.CurrentValue = _rangeX.TempMax;
                _minXSetText.CurrentValue = _rangeX.TempMin;
            }
        }

        DisparityRange _rangeY;
        public DisparityRange RangeY
        {
            get { return _rangeY; }
            set
            {
                _rangeY = value;
                _maxYActualText.Text = _rangeY.Max.ToString();
                _minYActualText.Text = _rangeY.Min.ToString();
                _maxYSetText.CurrentValue = _rangeY.TempMax;
                _minYSetText.CurrentValue = _rangeY.TempMin;
            }
        }

        public bool Accepted { get; set; } = false;

        public RangeSelectionPanel()
        {
            InitializeComponent();
        }

        private void _butAccept_Click(object sender, RoutedEventArgs e)
        {
            _rangeX.TempMax = _maxXSetText.CurrentValue;
            _rangeX.TempMin = _minXSetText.CurrentValue;
            _rangeY.TempMax = _maxYSetText.CurrentValue;
            _rangeY.TempMin = _minYSetText.CurrentValue;

            Accepted = true;
            Close();
        }

        private void _butCancel_Click(object sender, RoutedEventArgs e)
        {
            Accepted = false;
            Close();
        }
    }
}
