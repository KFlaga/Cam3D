using CamCore;
using System.Windows.Controls;

namespace CamCapture
{
    public class Module : CamCore.Module
    {
        private CameraCaptureTabs _captureTabs = null;

        public override string Name
        {
            get
            {
                return "Camera capture module";
            }
        }

        public override UserControl MainPanel
        {
            get
            {
                if (_captureTabs == null)
                    _captureTabs = new CameraCaptureTabs();
                return _captureTabs;
            }
        }

        public override bool EndModule()
        {
            _captureTabs.ControlHidden();
            return true;
        }

        public override bool StartModule()
        {
            if (_captureTabs == null)
                _captureTabs = new CameraCaptureTabs(); 
            _captureTabs.Loaded += _captureTabs_Loaded;
            return true;
        }

        private void _captureTabs_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _captureTabs.ControlShown();
            _captureTabs.Loaded -= _captureTabs_Loaded;
        }
    }
}
