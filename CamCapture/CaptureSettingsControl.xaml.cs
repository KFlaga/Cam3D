using System.Windows;
using System.Windows.Controls;

namespace CamCapture
{
    /// <summary>
    /// Control used to select cameras for further video capture
    /// and available configurations 
    /// </summary>
    public partial class CaptureSettingsControl : UserControl
    {
        public CameraCapture CameraLeft
        {
            get
            {
                return _camChooseLeft.Camera;
            }
        }
        public CameraCapture CameraRight
        {
            get
            {
                return _camChooseRight.Camera;
            }
        }
        
        public CameraSettingsChangedCallback CameraSettingsChanged;
        public delegate void CameraSettingsChangedCallback();

        public CaptureSettingsControl()
        {
            InitializeComponent();
        }
        
        private void _butSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            _camChooseLeft.Save();
            _camChooseRight.Save();
            if (CameraSettingsChanged != null)
                CameraSettingsChanged();
        }

        private void _butUndo_Click(object sender, RoutedEventArgs e)
        {
            _camChooseLeft.Undo();
            _camChooseRight.Undo();
        }
    }
}
