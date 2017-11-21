using DirectShowLib;
using System;
using System.Collections.Generic;
using System.Timers;

namespace CaptureModule
{
    public class CameraEventArgs : EventArgs
    {
        public CameraCapture Camera { get; set; }
        public bool Available { get; set; } // True if camera is plugged / freed
    }

    public class CameraViewItem
    {
        public CameraCapture Camera { get; private set; }

        public CameraViewItem(CameraCapture cam)
        {
            Camera = cam;
        }

        public override string ToString()
        {
            return Camera.ToString();
        }
    }

    // Detects camera plugging / unplugging
    // Lists available cameras and notifies on changes
    // Becouse every camera can be used only for one CameraCapture
    // stores not used yet cameras ( when more cameras are used in same time )
    // Manager is singleton avalable through CameraCaptureManager.Instance
    public class CameraCaptureManager
    {
        static CameraCaptureManager _instance;
        public static CameraCaptureManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new CameraCaptureManager();
                return _instance;
            }
        }

        Timer _refreshTimer;
        
        List<CameraCapture> _camerasAll;
        public List<CameraCapture> AvailableCameras
        {
            get
            {
                return _camerasAll;
            }
        }
        
        // Handle to Window where cameras images will be rendered
        // Must be set prior to monitoring cameras
        public IntPtr WindowHandle { get; set; }

        public CameraCaptureManager()
        {
            _refreshTimer = new Timer();
            _refreshTimer.Interval = 100;
            _refreshTimer.Enabled = true;
            _refreshTimer.AutoReset = true;

            _camerasAll = new List<CameraCapture>();
            _freeCameras = new List<CameraCapture>();
        }

        private void _checkTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _refreshTimer.Stop();
            UdpateAvailableCameras();
        }

        // Start/end refreshing camera info
        public void BeginMonitorCameras(double interval_ms)
        {
            _refreshTimer.Elapsed += _checkTimer_Elapsed;
            _refreshTimer.Interval = interval_ms;
            _refreshTimer.Stop();
            UdpateAvailableCameras();
        }

        public void EndMonitorCameras()
        {
            _refreshTimer.Elapsed -= _checkTimer_Elapsed;
            _refreshTimer.Stop();
        }

        #region CameraView

        // Functions/Property provided for easy camera selector UI
        // Collection viewers could use FreeCameras as items source for showing cameras
        // that can be used ( plugged but not used yet )
        List<CameraCapture> _freeCameras;
        public List<CameraCapture> FreeCameras
        {
            get
            {
                return _freeCameras;
            }
        }

        public void ReserveCamera(CameraCapture camera)
        {
            _freeCameras.Remove(camera);
            if (CameraReserved != null)
                CameraReserved(this, new CameraEventArgs()
                {
                    Camera = camera,
                    Available = false
                });
        }

        public void FreeCamera(CameraCapture camera)
        {
            _freeCameras.Add(camera);
            if (CameraFreed != null)
                CameraFreed(this, new CameraEventArgs()
                {
                    Camera = camera,
                    Available = true
                });
        }

        #endregion

        public void UdpateAvailableCameras()
        {
            // Find all camera devices
            DsDevice[] availableDevices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            // Copy list of cameras -> each plugged camera will be removed from list, so unplugged will remain
            List<CameraCapture> unpluggedCameras = new List<CameraCapture>(_camerasAll);
            // Detect plugs/unplugs -> compare available devices with cameras
            foreach(var device in availableDevices)
            {
                bool newDevice = true;
                string devCode = device.DevicePath;
                for(int cam = 0; cam < unpluggedCameras.Count; cam++)
                {
                    if (unpluggedCameras[cam].CameraID == devCode)
                    {
                        newDevice = false;
                        unpluggedCameras.RemoveAt(cam);
                        break;
                    }
                }
                // Device not found -> add it to available list
                if (newDevice)
                    OnCameraPlugged(device);
            }

            // All found cameras are removed from unplugged list, so for each remaining
            // one call such event handler and remove from list of available cameras
            for (int cam = 0; cam < unpluggedCameras.Count; cam++)
            {
                OnCameraUnplugged(unpluggedCameras[cam]);
                _camerasAll.Remove(unpluggedCameras[cam]);
                _freeCameras.Remove(unpluggedCameras[cam]);
            }

            _refreshTimer.Start();
        }

        private void OnCameraPlugged(DsDevice device)
        {
            CameraCapture camera = new CameraCapture(device, WindowHandle);
            _camerasAll.Add(camera);
            _freeCameras.Add(camera);
            if (CameraPlugged != null)
                CameraPlugged(this, new CameraEventArgs()
                {
                    Camera = camera,
                    Available = true
                });
        }

        private void OnCameraUnplugged(CameraCapture camera)
        {
            camera.TerminateAsync();
            if (CameraUnplugged != null)
                CameraUnplugged(this, new CameraEventArgs()
                {
                    Camera = camera,
                    Available = false
                });
        }

        public event EventHandler<CameraEventArgs> CameraUnplugged;
        public event EventHandler<CameraEventArgs> CameraPlugged;
        public event EventHandler<CameraEventArgs> CameraFreed;
        public event EventHandler<CameraEventArgs> CameraReserved;
    }
}
