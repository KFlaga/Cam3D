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

            _tabCam1.Calibrated += (s, e) =>
            {
                CamCore.CalibrationData.Data.CameraLeft = _tabCam1.CameraMatrix;
            };
            
            _tabCam2.Calibrated += (s, e) =>
            {
                CamCore.CalibrationData.Data.CameraRight = _tabCam2.CameraMatrix;
            };
        }

        public void Dispose()
        {
            _tabCam1.Dispose();
        }
    }
}
