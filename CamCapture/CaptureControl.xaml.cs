using System.Windows;
using System.Windows.Controls;

namespace CamCapture
{
    public partial class CaptureControl : UserControl
    {
        private bool _singleShot = false;
        private CameraCapture _camera;
        public CameraCapture Camera
        {
            get
            {
                return _camera;
            }
            set
            {
                if(value == null)
                {
                    if (_camera != null)
                    {
                        // Unhook camera
                        _camera.StateChanged -= OnCameraStateChanged;
                        _camera.FrameChanged -= OnCameraFrameChanged;
                        _camera = null;
                    }
                    _butSnapShot.IsEnabled = false;
                    _state = CaptureStates.Uinitilialized;
                    return;
                }

                _camera = value;

                _camera.StateChanged += OnCameraStateChanged;
                _camera.IsSingleShot = _singleShot;
                _camera.FrameChanged += OnCameraFrameChanged;

                if(_camera.State == CaptureStates.Ready)
                    _butStart.IsEnabled = true;

                _state = _camera.State;
            }
        }

        private CaptureStates _state = CaptureStates.Uinitilialized;
        public CaptureStates CaptureState
        {
            get
            {
                return _state;
            }
        }

        public CaptureControl()
        {
            InitializeComponent();

            _butPause.IsEnabled = false;
            _butStart.IsEnabled = false;
            _butStop.IsEnabled = false;
            _butSnapShot.IsEnabled = false;

            IsVisibleChanged += OnIsVisibleChanged;

            //   BindingOperations.SetBinding(_butSnapShot, Button.IsEnabledProperty, new Binding()
            //    {
            //       Path = new PropertyPath(CheckBox.IsCheckedProperty),
            //        Source = _cbSingleShot,
            //        Mode = BindingMode.OneWay
            //    });
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if((bool)e.NewValue == false)
            {
                if(_camera != null && _camera.State == CaptureStates.Running)
                {
                    StopAsync();
                }
            }
        }

        private void OnCameraStateChanged(object sender, CaptureStateChangedEventArgs e)
        {
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.Invoke(() =>
                {
                    OnCameraStateChanged(sender, e);
                });
            }
            else
            {
                _butStart.IsEnabled = e.NewState == CaptureStates.Ready || e.NewState == CaptureStates.Paused;
                _butPause.IsEnabled = e.NewState == CaptureStates.Running;
                _butStop.IsEnabled = e.NewState == CaptureStates.Running;
                if (e.NewState == CaptureStates.Terminated)
                {
                    Camera = null;
                }
                _state = e.NewState;
            }
        }

        private void OnCameraFrameChanged(object sender, CaptureFrameChangedEventArgs e)
        {
            _imageControl.Source = e.NewFrame;
        }

        private void _butStart_Click(object sender, RoutedEventArgs e)
        {
            StartAsync();
        }

        private void _butPause_Click(object sender, RoutedEventArgs e)
        {
            PauseAsync();
        }

        private void _butStop_Click(object sender, RoutedEventArgs e)
        {
            StopAsync();
        }

        // Function below are supposed to be called only if camera is set
        public async void StartAsync()
        {
            await Camera.StartCaptureAsync();
        }

        public async void PauseAsync()
        {
            await Camera.PauseCaptureAsync();
        }

        public async void StopAsync()
        {
            _imageControl.Source = null;
            await Camera.EndCaptureAsync();
        }

        // Called when Control's work is done -> safely stops camera and set it as null
        public async void EndAsync()
        {
            if (Camera != null)
            {
                _imageControl.Source = null;
                await Camera.EndCaptureAsync();
            }
        }

        private void _cbSingleShot_Checked(object sender, RoutedEventArgs e)
        {
            _singleShot = true;
            if (Camera != null)
            {
                _butSnapShot.IsEnabled = _singleShot;
                Camera.IsSingleShot = _singleShot;
            }
        }

        private void _cbSingleShot_Unchecked(object sender, RoutedEventArgs e)
        {
            _singleShot = false;
            if (Camera != null)
            {
                _butSnapShot.IsEnabled = _singleShot;
                Camera.IsSingleShot = _singleShot;
            }
        }

        private void _butSnapShot_Click(object sender, RoutedEventArgs e)
        {
            Camera.NextFrame();
        }
    }
}
