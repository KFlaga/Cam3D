using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace CaptureModule
{
    /// <summary>
    /// Interaction logic for CameraCaptureTabs.xaml
    /// </summary>
    public partial class CameraCaptureTabs : UserControl
    {
        private CameraCapture _cameraLeft;
        private CameraCapture _cameraRight;

        private CamCore.InterModularDataSender _leftCameraSaver;
        private CamCore.InterModularDataSender _rightCameraSaver;

        public CameraCaptureTabs()
        {
            InitializeComponent();

            _settingsTab.CameraSettingsChanged = OnCameraSettingsChanged;

            _leftCameraSaver = CamCore.InterModularConnection.RegisterDataSender("LeftCameraCaptureSnapshot");
            _rightCameraSaver = CamCore.InterModularConnection.RegisterDataSender("RightCameraCaptureSnapshot");
        }

        private void OnCameraSettingsChanged()
        {
            _cameraLeft = _settingsTab.CameraLeft;
            _cameraRight = _settingsTab.CameraRight;
            
            _captureLeft.Camera = _cameraLeft;
            _captureRight.Camera = _cameraRight;
        }

        public void ControlShown()
        {
            CameraCaptureManager.Instance.WindowHandle = new System.Windows.Interop.WindowInteropHelper(
                Window.GetWindow(this)).Handle;
            CameraCaptureManager.Instance.BeginMonitorCameras(100);
            
            CameraCaptureManager.Instance.CameraUnplugged += OnCameraUnplugged;
        }

        private void OnCameraUnplugged(object sender, CameraEventArgs e)
        {
            if(e.Camera == _cameraLeft)
            {
                MessageBox.Show("Left camera unplugged");
            }
            if(e.Camera == _cameraRight)
            {
                MessageBox.Show("Right camera unplugged");
            }
        }

        public void ControlHidden()
        {
            CameraCaptureManager.Instance.EndMonitorCameras();

            _captureLeft.EndAsync();
            _captureRight.EndAsync();
        }

        private void _butSaveShotRight_Click(object sender, RoutedEventArgs e)
        {
            SaveFrame(_cameraRight);
        }

        private void _butSaveShotLeft_Click(object sender, RoutedEventArgs e)
        {
            SaveFrame(_cameraLeft);
        }

        private void _butSaveShotRightMemory_Click(object sender, RoutedEventArgs e)
        {
            if (CheckFrameAvailable(_cameraRight))
            {
                _rightCameraSaver.SendData(_cameraRight.CurrentFrame);
            }
        }

        private void _butSaveShotLeftMemory_Click(object sender, RoutedEventArgs e)
        {
            if (CheckFrameAvailable(_cameraLeft))
            {
                _leftCameraSaver.SendData(_cameraLeft.CurrentFrame);
            }
        }

        private bool CheckFrameAvailable(CameraCapture camera)
        {
            return camera != null &&
                camera.CurrentFrame != null &&
                (camera.State == CaptureStates.Running ||
                camera.State == CaptureStates.Paused);
        }

        private void SaveFrame(CameraCapture camera)
        {
            if (CheckFrameAvailable(camera))
            {
                BitmapSource frame = camera.CurrentFrame;

                bool rightRunning = _captureRight.CaptureState == CaptureStates.Running;
                bool leftRunning = _captureLeft.CaptureState == CaptureStates.Running;

                if(rightRunning)
                    _captureRight.PauseAsync();
                if (leftRunning)
                    _captureLeft.PauseAsync();

                SaveFileDialog saveDialog = new SaveFileDialog();
                saveDialog.Filter = "PNG|*.png";
                bool? res = saveDialog.ShowDialog();
                if (res.Value == true)
                {
                    if (!saveDialog.FileName.EndsWith(".png"))
                        MessageBox.Show("Unsupported file format");
                    else
                    {
                        Stream imgFileStream = saveDialog.OpenFile();
                        try
                        {
                            BitmapEncoder encoder = new PngBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(frame));
                            encoder.Save(imgFileStream);
                        }
                        catch (Exception exc)
                        {
                            MessageBox.Show("Failed to save data: " + exc.Message, "Error");
                        }
                        imgFileStream.Close();
                    }
                }
                
                if(rightRunning)
                    _captureRight.StartAsync();
                if(leftRunning)
                    _captureLeft.StartAsync();
            }
        }
    }
}
