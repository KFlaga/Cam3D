using CamImageProcessing.ImageMatching;
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
