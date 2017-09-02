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
                _textDX.Text = _disp.SubDX.ToString("F2");
                _textDY.Text = _disp.SubDY.ToString("F2");
                _textCost.Text = _disp.Cost.ToString("F3");
                _textConf.Text = _disp.Confidence.ToString("F3");
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
