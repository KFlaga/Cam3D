using System.Windows;

namespace CamControls
{
    /// <summary>
    /// Interaction logic for RangeSelectionPanel.xaml
    /// </summary>
    public partial class RangeSelectionPanel : Window
    {
        DisparityRange _range;
        public DisparityRange Range
        {
            get { return _range; }
            set
            {
                _range = value;
                _maxXActualText.Text = _range.Max.ToString();
                _minXActualText.Text = _range.Min.ToString();
                _maxXSetText.CurrentValue = _range.TempMax;
                _minXSetText.CurrentValue = _range.TempMin;
            }
        }

        public bool Accepted { get; set; } = false;

        public RangeSelectionPanel()
        {
            InitializeComponent();
        }

        private void _butAccept_Click(object sender, RoutedEventArgs e)
        {
            _range.TempMax = _maxXSetText.CurrentValue;
            _range.TempMin = _minXSetText.CurrentValue;

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
