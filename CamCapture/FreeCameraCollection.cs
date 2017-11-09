using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CamCapture
{
    // List of free cameras to choose. Updates itself when camera is plugged/unplugged
    // or reserved/freed.
    public class FreeCameraCollection : ObservableCollection<CameraCapture>
    {
        private CameraCapture _selectedCamera;
        public CameraCapture SelectedCamera
        {
            get
            {
                return _selectedCamera;
            }
            set
            {
                if (_selectedCamera != null)
                    CameraCaptureManager.Instance.FreeCamera(_selectedCamera);
                _selectedCamera = value;
                if (_selectedCamera != null)
                    CameraCaptureManager.Instance.ReserveCamera(_selectedCamera);
            }
        }

        public FreeCameraCollection( )
        {
            CameraCaptureManager.Instance.CameraFreed += OnCameraFreed;
            CameraCaptureManager.Instance.CameraReserved += OnCameraReserved;
            CameraCaptureManager.Instance.CameraPlugged += OnCameraPlugged;
            CameraCaptureManager.Instance.CameraUnplugged += OnCameraUnplugged;
        }

        public FreeCameraCollection(List<CameraCapture> cameras)
        {
            foreach(var cam in  cameras)
            {
                this.Add(cam);
            }

            CameraCaptureManager.Instance.CameraFreed += OnCameraFreed;
            CameraCaptureManager.Instance.CameraReserved += OnCameraReserved;
            CameraCaptureManager.Instance.CameraPlugged += OnCameraPlugged;
            CameraCaptureManager.Instance.CameraUnplugged += OnCameraUnplugged;
        }

        private void OnCameraUnplugged(object sender, CameraEventArgs e)
        {
            try
            {
                if(e.Camera == SelectedCamera)
                {
                    if(SelectedCameraUnplugged != null)
                        SelectedCameraUnplugged(this, e);
                }
                this.Remove(e.Camera);
            }
            catch(Exception)
            {
                
            }
        }

        private void OnCameraPlugged(object sender, CameraEventArgs e)
        {
            this.Add(e.Camera);
        }

        private void OnCameraReserved(object sender, CameraEventArgs e)
        {
            // selected camera is reserved means that it is selected in this collection, so
            // it shouldn't be removed from it
            if (e.Camera == SelectedCamera)
                return;

            this.Remove(e.Camera);
        }

        private void OnCameraFreed(object sender, CameraEventArgs e)
        {
            // selected camera is freed means that it was selected by this collection, so
            // it is already in it - no need to add it again
            if (e.Camera == SelectedCamera)
                return;

            this.Add(e.Camera);
        }

        public event EventHandler<CameraEventArgs> SelectedCameraUnplugged;
    }
}
