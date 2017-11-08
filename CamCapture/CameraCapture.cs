using System;
using System.Collections.Generic;
using DirectShowLib;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;

namespace CamCapture
{
    public enum CaptureStates
    {
        Running = 0,
        Paused,
        Ready,
        Uinitilialized,
        Terminated
    }

    public class CaptureStateChangedEventArgs : EventArgs
    {
        public CameraCapture CameraCapture { get; set; }
        public CaptureStates OldState { get; set; }
        public CaptureStates NewState { get; set; }
    }

    public class CaptureFrameChangedEventArgs : EventArgs
    {
        public CameraCapture CameraCapture { get; set; }
        public BitmapSource NewFrame { get; set; }
    }

    public class CameraCapture : IDisposable
    {
        // Handle to window camera surface is to be rendered
        IntPtr _windowHandle;

        IntPtr _dwUserIdl = IntPtr.Zero; // ??? required for VMR

        // SurfaceAllocator/ImagePresenter for acquisition of images from camera
        VMR9Capture _vmr9Capture = null;

        // Filter associated with camera
        IBaseFilter _camFilter = null;

        // Some DirectShow classes
        VideoMixingRenderer9 _vmr9 = null;
        IMediaControl _mediaControl = null;
        IGraphBuilder _graphBuilder = null;
        ICaptureGraphBuilder2 _captureGraphBuilder = null;

        // DirectShow device associated with physical camera
        // Contains moniker used to find camera stream filter
        // Property is read-only, and device is set once during construction
        private DsDevice _device = null;
        public DsDevice CameraDsDevice
        {
            get
            {
                return _device;
            }

        }

        // DevicePath copied for compare purposes ( not to call DsDevice's
        // getter - it couses deadlocks sometimes when called if capture is running )
        private string _devicePath;
        public string CameraID
        {
            get
            {
                return _devicePath;
            }
        }

        // Finds camera filter and confifurations available for it
        private void InitCameraFilter()
        {
            object source;
            Guid iid = typeof(IBaseFilter).GUID;
            _device.Mon.BindToObject(null, null, ref iid, out source);
            _camFilter = (IBaseFilter)source;

            FindAvailableConfigurations();
        }

        protected CaptureStates _state;
        public CaptureStates State
        {
            get
            {
                return _state;
            }
            protected set
            {
                var oldstate = _state;
                _state = value;
                if (StateChanged != null)
                    StateChanged(this, new CaptureStateChangedEventArgs()
                    {
                        CameraCapture = this,
                        NewState = value,
                        OldState = oldstate
                    });
            }
        }
        public event EventHandler<CaptureStateChangedEventArgs> StateChanged;

        // List of configurations available for camera
        // First position is default resolution for capture pin
        public List<CameraConfig> AvailableConfigurations { get; protected set; }

        // Current configuration
        // Changing resoultion will result in change of underlaying d3 surfaces
        // so VMR9Capture need to be updated
        // TODO: actually changing configuration do not work - only first choosen works
        private CameraConfig _currentConfiguration = null;
        public CameraConfig CurrentConfiguration
        {
            get
            {
                return _currentConfiguration;
            }
            set
            {
                if (State == CaptureStates.Running)
                {
                    throw new Exception("Cannot change configuration while graph is running");
                }

                if (_currentConfiguration == value || value == null)
                    return;

                State = CaptureStates.Uinitilialized;
                _currentConfiguration = value;

                _vmr9Capture.UpdateResoultion(_currentConfiguration);

                //_camFilter.JoinFilterGraph(null, "");
                CreateGraphs();
                
                State = CaptureStates.Ready;
            }
        }

        // Camera is created using DirectShow device and handle to Window
        // where captured images will be shown
        public CameraCapture(DsDevice camDevice, IntPtr hWnd)
        {
            State = CaptureStates.Uinitilialized;
            _windowHandle = hWnd;
            _device = camDevice;
            _devicePath = _device.DevicePath;
            InitCameraFilter();
            _vmr9Capture = new VMR9Capture(hWnd);

            IsSingleShot = false;
        }

        #region Video/Frame Control

        // Last updated video frame taken from VMR9Capture
        // On frame change an event is fired
        private BitmapSource _lastFrame;
        public BitmapSource CurrentFrame
        {
            get
            {
                return _lastFrame;
            }
            set
            {
                _lastFrame = value;
                if (FrameChanged != null)
                    FrameChanged(this, new CaptureFrameChangedEventArgs()
                    {
                        CameraCapture = this,
                        NewFrame = _lastFrame
                    });
            }
        }
        public event EventHandler<CaptureFrameChangedEventArgs> FrameChanged;

        // If capture is single shot, frame will be updated one time after calling StartCapture()
        // and remain unchanged until call to NextFrame() or StartCapture() again
        // Effect takes place immedietly after setting variable even if running
        private bool _isSingleShot;
        public bool IsSingleShot
        {
            get { return _isSingleShot; }
            set
            {
                _isSingleShot = value;
                // On switching to continous mode get next frame immeditely
                if (_isSingleShot == false && State == CaptureStates.Running)
                    NextFrame();
            }
        }

        // Start/Pause/Stop capturing new frames
        // If capture is single shot, capture still should be running all the time
        public async Task StartCaptureAsync()
        {
            if (State != CaptureStates.Uinitilialized && State != CaptureStates.Running)
            {
                int hr = await Task.Run((Func<int>)_mediaControl.Run);
                DsError.ThrowExceptionForHR(hr);
                State = CaptureStates.Running;

                NextFrame();
            }
        }

        // Pause and stop should not be called if VMR9Capture
        // invoked event on frame change, as pausing is synchronous and filter thread
        // will wait for UI to process event before stopping, and UI will wait for stop
        // Therefore methods are Async
        public async Task PauseCaptureAsync()
        {
            if (State == CaptureStates.Running)
            {
                _vmr9Capture.NewFrameAvailable = null;
                State = CaptureStates.Paused;
                int hr = await Task.Run((Func<int>)_mediaControl.Pause);
                DsError.ThrowExceptionForHR(hr);
            }
        }

        public async Task EndCaptureAsync()
        {
            if (State == CaptureStates.Running || State == CaptureStates.Paused)
            {
                _vmr9Capture.NewFrameAvailable = null;
                State = CaptureStates.Ready;
                int hr = await Task.Run((Func<int>)_mediaControl.Stop);
                DsError.ThrowExceptionForHR(hr);
            }
        }

        // Sets callback which updates current frame on next frame available
        public void NextFrame()
        {
            _vmr9Capture.NewFrameAvailable = OnNextFrameAvailable;
        }

        // Called when new frame arives
        // if capture is single shot reset callback, so it won't
        // be called again
        private void OnNextFrameAvailable()
        {
            if (IsSingleShot)
            {
                _vmr9Capture.NewFrameAvailable = null;
            }
            CurrentFrame = _vmr9Capture.CurrentFrame;
        }

        // Terminates filters and VMR9Capture -> to be called
        // when camera is unplugged
        public async void TerminateAsync()
        {
            _vmr9Capture.NewFrameAvailable = null;
            State = CaptureStates.Terminated;
            await Task.Run((Func<int>)_mediaControl.Stop);
            //try
            //{
            //    _graphBuilder.RemoveFilter(_camFilter);
            //}
            //catch(Exception) { }
            _vmr9Capture.Terminate();
        }


        #endregion

        #region InternalOperations

        // Creates, cofigurates and connects DirectShow filter graphs
        private void CreateGraphs()
        {
            int hr;

            ReleaseOldGraphs();

            _graphBuilder = (IGraphBuilder)new FilterGraph();
            _captureGraphBuilder = (ICaptureGraphBuilder2)new CaptureGraphBuilder2();
            hr = _captureGraphBuilder.SetFiltergraph(_graphBuilder);
            DsError.ThrowExceptionForHR(hr);

            _mediaControl = (IMediaControl)_graphBuilder;

            hr = _graphBuilder.AddFilter(_camFilter, "Video Capture");
            DsError.ThrowExceptionForHR(hr);
            
            SetCameraConfiguration();

            CreateVMR();

            hr = _captureGraphBuilder.RenderStream(PinCategory.Capture, MediaType.Video, _camFilter, null, (IBaseFilter)_vmr9);
            DsError.ThrowExceptionForHR(hr);
        }

        private void ReleaseOldGraphs()
        {
            if (_vmr9 != null)
                Marshal.ReleaseComObject(_vmr9);
            if (_captureGraphBuilder != null)
                Marshal.ReleaseComObject(_captureGraphBuilder);
            if (_graphBuilder != null)
                Marshal.ReleaseComObject(_graphBuilder);
        }

        private void CreateVMR()
        {
            _vmr9 = new VideoMixingRenderer9();
            IBaseFilter vmrFilter = (IBaseFilter)_vmr9;
            int hr = _graphBuilder.AddFilter(vmrFilter, "VMR9");
            DsError.ThrowExceptionForHR(hr);

            IVMRFilterConfig9 vmr9Config = (IVMRFilterConfig9)_vmr9;
            vmr9Config.SetNumberOfStreams(1);
            vmr9Config.SetRenderingMode(VMR9Mode.Renderless);
            vmr9Config.SetRenderingPrefs(VMR9RenderPrefs.None);

            IVMRSurfaceAllocatorNotify9 vmr9AllocNotify = (IVMRSurfaceAllocatorNotify9)_vmr9;
            hr = vmr9AllocNotify.AdviseSurfaceAllocator(_dwUserIdl, _vmr9Capture);
            DsError.ThrowExceptionForHR(hr);
            hr = _vmr9Capture.AdviseNotify(vmr9AllocNotify);
            DsError.ThrowExceptionForHR(hr);
        }

        #region _camFilter Configurations 

        protected void FindAvailableConfigurations()
        {
            AvailableConfigurations = new List<CameraConfig>();

            if (CameraDsDevice == null || _camFilter == null)
                return;

            IPin pin = DsFindPin.ByCategory(_camFilter, PinCategory.Capture, 0);
            
            if (pin != null)
               FindConfigurationsForPin(pin);
        }

        // Enumerares all media types for this pin and iterates through, 
        // extracts VideoInfoHeaders and creates CameraConfigs from them
        protected void FindConfigurationsForPin(IPin pin)
        {
            IEnumMediaTypes mediaTypeEnum;
            int hr = pin.EnumMediaTypes(out mediaTypeEnum);
            DsError.ThrowExceptionForHR(hr);

            AMMediaType[] mediaTypes = new AMMediaType[1];
            IntPtr fetched = IntPtr.Zero;
            hr = mediaTypeEnum.Next(1, mediaTypes, fetched);
            DsError.ThrowExceptionForHR(hr);

            while (fetched != null && mediaTypes[0] != null)
            {
                VideoInfoHeader videoInfo = new VideoInfoHeader();
                // Copy unmanaged structure into managed one
                Marshal.PtrToStructure(mediaTypes[0].formatPtr, videoInfo);
                if (videoInfo.BmiHeader.Size != 0 && videoInfo.BmiHeader.BitCount != 0)
                {
                    AvailableConfigurations.Add(new CameraConfig(videoInfo));
                }
                hr = mediaTypeEnum.Next(1, mediaTypes, fetched);
                DsError.ThrowExceptionForHR(hr);
            }
        }

        // Configures camera filter to work with CurrentConfiguration
        protected void SetCameraConfiguration()
        {
            // Find camera pin
            IPin pin = DsFindPin.ByCategory(_camFilter, PinCategory.Capture, 0);
            if (pin == null)
                return;
            // Extract config interface from pin
            IAMStreamConfig config = (IAMStreamConfig)pin;
            
            // Get pin capabilities count
            int capsCount, capsSize;
            int hr = config.GetNumberOfCapabilities(out capsCount, out capsSize);
            DsError.ThrowExceptionForHR(hr);

            AMMediaType mediaType;
            // Allocate vscc for GetStreamCaps() - neccessary even if its unused
            VideoStreamConfigCaps vscc = new VideoStreamConfigCaps();
            IntPtr vsccPtr = Marshal.AllocHGlobal(Marshal.SizeOf(vscc));

            // For each format get mediaType and check if its correct one
            for (int format = 0; format < capsCount; format++)
            {
                hr = config.GetStreamCaps(format, out mediaType, vsccPtr);
                DsError.ThrowExceptionForHR(hr);

                VideoInfoHeader videoInfo = new VideoInfoHeader();

                // Examine the format, and possibly use it. 
                if (mediaType.formatType == FormatType.VideoInfo)
                {
                    Marshal.PtrToStructure(mediaType.formatPtr, videoInfo);
                    if(_currentConfiguration.Equals(videoInfo))
                    {
                        hr = config.SetFormat(mediaType);
                        DsError.ThrowExceptionForHR(hr);
                    }
                }
            }
            // Free allocated vscc
            Marshal.FreeHGlobal(vsccPtr);
        }

        #endregion

        #endregion

        public override string ToString()
        {
            return CameraDsDevice.Name;
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_vmr9Capture != null)
                        _vmr9Capture.Dispose();
                    TerminateAsync();
                }

                disposedValue = true;
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
