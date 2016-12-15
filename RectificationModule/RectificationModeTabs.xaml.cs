using System;
using System.Windows.Controls;

namespace RectificationModule
{
    /// <summary>
    /// Interaction logic for CalibrationModeTabs.xaml
    /// </summary>
    public partial class RectificationModeTabs : UserControl, IDisposable
    {
        public RectificationModeTabs()
        {
            InitializeComponent();

            _tabRect.FeturesDetected += (s, e) =>
            {
                _tabFeatures.FeatureImageLeft = e.FeatureImageLeft;
                _tabFeatures.FeatureImageRight = e.FeatureImageRight;
                _tabFeatures.FeatureListLeft = e.FeatureListLeft;
                _tabFeatures.FeatureListRight = e.FeatureListRight;
                _tabFeatures.ImageLeft = e.ImageLeft;
                _tabFeatures.ImageRight = e.ImageRight;
            };
        }

        public void Dispose()
        {

        }
    }
}
