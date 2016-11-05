using System;
using System.Windows.Controls;

namespace ImageMatchingModule
{
    /// <summary>
    /// Interaction logic for CalibrationModeTabs.xaml
    /// </summary>
    public partial class ImageMatchingModeTabs : UserControl, IDisposable
    {
        public ImageMatchingModeTabs()
        {
            InitializeComponent();

            _tabMatching.LeftMapChanged += (s, e) => { if(e.NewMap != null) _tabDisparity.DisparityMapLeft = e.NewMap; };
            _tabMatching.RightMapChanged += (s, e) => { if(e.NewMap != null) _tabDisparity.DisparityMapRight = e.NewMap; };

            _tabRefinement.RequsestDisparityMapsUpdate += (s, e) =>
            {
                _tabRefinement.MapLeftBase = _tabDisparity.DisparityMapLeft;
                _tabRefinement.MapRightBase = _tabDisparity.DisparityMapRight;
            };
        }

        public void Dispose()
        {

        }
    }
}
