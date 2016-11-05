using CamCore;
using System.Windows.Controls;

namespace CamControls
{
    /// <summary>
    /// Interaction logic for DisparityBox.xaml
    /// </summary>
    public partial class DisparityBox : UserControl
    {
        private Disparity _disp;
        public Disparity Disparity
        {
            get { return _disp; }
            set
            {
                _disp = value;
                _textDX.Text = _disp.DX.ToString();
                _textDY.Text = _disp.DY.ToString();
                _textCost.Text = _disp.Cost.ToString();
                _textConf.Text = _disp.Confidence.ToString();
            }
        }

        public enum MapDirection
        {
            LeftToRight,
            RightToLeft
        }

        public DisparityBox()
        {
            InitializeComponent();
        }
    }
}
