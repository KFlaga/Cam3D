using CamCore;
using System;
using System.Windows.Controls;

namespace CalibrationModule
{
    /// <summary>
    /// Interaction logic for CalibrationModeTabs.xaml
    /// </summary>
    public partial class CalibrationModeTabs : UserControl, IDisposable
    {
        public CalibrationModeTabs()
        {
            InitializeComponent();

            _tabCam1.CameraIndex = SideIndex.Left;
            _tabCam2.CameraIndex = SideIndex.Right;
        }

        public void Dispose()
        {
            _tabCam1.Dispose();
            _tabCam2.Dispose();
        }
    }
}
