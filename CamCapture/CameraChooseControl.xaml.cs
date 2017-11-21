using System.Windows.Controls;

namespace CaptureModule
{
    // User control containg 2 comboboxes to choose used camera device and one
    // of availiable configuration. Uses FreeCameraCollection for storing cameras,
    // so all controls will share same pool of cameras and each camera could be selected
    // only in one control. After saving selected camera can be accesed by 'Camera' DP.
    public partial class CameraChooseControl : UserControl
    {
        private CameraCapture _camera;
        public CameraCapture Camera
        {
            get
            {
                return _cameras.SelectedCamera;
            }
        }

        private CameraConfig _config;
        private CameraCapture _oldCamera = null;
        private CameraConfig _oldConfig;

        private FreeCameraCollection _cameras;

        public CameraChooseControl()
        {
            InitializeComponent();

            _cameras = new FreeCameraCollection(CameraCaptureManager.Instance.FreeCameras);
            _cbCameras.ItemsSource = _cameras;
            _cameras.SelectedCameraUnplugged += OnSelectedCameraUnplugged;
        }

        public void Save()
        {
            if (Camera != null && _config != null)
            {
                Camera.CurrentConfiguration = _config;
                _oldCamera = Camera;
                _oldConfig = _config;
            }
        }
        
        public void Undo()
        {
            _cbCameras.SelectedItem = _oldCamera;
            _cbConfigs.SelectedItem = _oldConfig;
        }
        
        // If currently choosen camera is unplugged, unselect camera
        // Becouse plug detection occurs on separate thread, invoke it on UI thread
        private void OnSelectedCameraUnplugged(object sender, CameraEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (_cbCameras.SelectedItem == e.Camera)
                {
                    _cbCameras.SelectedIndex = -1;
                    _cbConfigs.Items.Clear();
                }
            });
        }

        private void _cbCameras_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1)
            {
                _cameras.SelectedCamera = (CameraCapture)e.AddedItems[0];
                foreach (var conf in _cameras.SelectedCamera.AvailableConfigurations)
                {
                    _cbConfigs.Items.Add(conf);
                }
            }
        }

        private void _cbConfigs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1)
            {
                _config = (CameraConfig)e.AddedItems[0];
            }
        }
    }
}
