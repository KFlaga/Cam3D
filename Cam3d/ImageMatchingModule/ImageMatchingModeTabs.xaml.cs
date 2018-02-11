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

            _tabRefinement.RequsestImagesUpdate += (s, e) =>
            {
                _tabRefinement.ImageLeft = _tabMatching.ImageLeft;
                _tabRefinement.ImageRight = _tabMatching.ImageRight;
            };
        }

        public void Dispose()
        {

        }
    }
}
