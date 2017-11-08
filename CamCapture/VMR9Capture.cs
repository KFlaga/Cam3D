using CamAlgorithms;
using DirectShowLib;
using SharpDX.Direct3D9;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace CamCapture
{
    // Video Mixed Renderer custom Allocator/Presenter
    // Creates DirectX Devices and surfaces, which then exposes
    // as BitmapSource for easy drawing/processing
    public class VMR9Capture : IVMRSurfaceAllocator9, IVMRImagePresenter9, IDisposable
    {
        private IVMRSurfaceAllocatorNotify9 _surfAllocatorNotify;

        // Direct3D devices
        private Direct3D _d3d;
        private Device _d3Device;

        // Handle to Window where images from camera will be shown
        private IntPtr _windowHandle;

        // Current camera configuration - for width/height of surfaces
        private CameraConfig _resolution;

        // Surfaces for VMR
        private Surface[] _surfaces;

        // Set to true if new surface arrived since last call to CurrentFrame
        private bool _frameChanged = true;

        // Pointer to surface recived in PresentImage
        private IntPtr _currentFrameSurface;

        // Saves surface in backbuffer and then copies it to BitmapSource
        private D3DImageConverter _imageConverter = new D3DImageConverter();

        // Current frame is surface get from DirectShow converted to BitmapSource
        // Convertion occurs only on get request if frame changed since last call
        // (ie. if camera in singleshot mode there may be more such request for same surface)
        public BitmapSource CurrentFrame
        {
            get
            {
                if (_frameChanged)
                {
                    _imageConverter.SetD3DSurfaceSource(_currentFrameSurface);
                    _frameChanged = false;
                }
                return _imageConverter.BitmapSource;
            }
        }

        // Object created for specific window
        public VMR9Capture(IntPtr hWnd)
        {
            _windowHandle = hWnd;

            _d3d = new Direct3DEx();
        }

        // On resolution change recreate direct3d device with new size
        public void UpdateResoultion(CameraConfig newResolution)
        {
            _resolution = newResolution;
            CreateD3Device();
        }
        
        public void Terminate()
        {
            try
            {
                _surfAllocatorNotify.ChangeD3DDevice(IntPtr.Zero, IntPtr.Zero);
            }
            catch(Exception)
            {

            }
        }

        #region IVMRSurfaceAllocator9 Implementation

        public int InitializeDevice(IntPtr dwUserID, ref VMR9AllocationInfo lpAllocInfo, ref int lpNumBuffers)
        {
            _surfaces = new Surface[lpNumBuffers];
            for (int s = 0; s < lpNumBuffers; s++)
            {
                _surfaces[s] = Surface.CreateRenderTarget(_d3Device, lpAllocInfo.dwWidth, lpAllocInfo.dwHeight, Format.X8R8G8B8,
                    MultisampleType.None, 0, true);
            }

            return 0;
        }

        public int TerminateDevice(IntPtr dwID)
        {
            FreeSurfaces();
            return 0;
        }

        public int GetSurface(IntPtr dwUserID, int SurfaceIndex, int SurfaceFlags, out IntPtr lplpSurface)
        {
            lplpSurface = IntPtr.Zero;

            lock (this)
            {
                lplpSurface = _surfaces[SurfaceIndex].NativePointer;
                Marshal.AddRef(lplpSurface);
            }

            return 0;

        }

        public int AdviseNotify(IVMRSurfaceAllocatorNotify9 lpIVMRSurfAllocNotify)
        {
            _surfAllocatorNotify = lpIVMRSurfAllocNotify;
            return _surfAllocatorNotify.SetD3DDevice(_d3Device.NativePointer, _d3d.Adapters[0].Monitor);
        }

        private void FreeSurfaces()
        {
            if (_surfaces == null)
                return;

            foreach (Surface surface in _surfaces)
            {
                surface.Dispose();
            }
            _surfaces = null;
        }

        #endregion

        #region IVMRImagePresenter9_Implementation

        public int StartPresenting(IntPtr dwUserID)
        {
            return 0;
        }

        public int StopPresenting(IntPtr dwUserID)
        {
            return 0;
        }

        public int PresentImage(IntPtr dwUserID, ref VMR9PresentationInfo lpPresInfo)
        {
            _currentFrameSurface = lpPresInfo.lpSurf;
            _frameChanged = true;

            // Invoke new frame available on UI thread - becouse for it may be followed
            // with showing it on screen
            if (NewFrameAvailable != null)
            {
                _imageConverter.Dispatcher.Invoke(() =>
                {
                    // Double check if its null, becouse camera can be paused/stopped
                    // before UI thread invokes the delegate
                    if(NewFrameAvailable != null)
                        NewFrameAvailable();
                });
            }

            return 0;
        }

        #endregion

        #region D3DInitialization

        public void CreateD3Device()
        {
            PresentParameters pparams = new PresentParameters()
            {
                SwapEffect = SwapEffect.Discard,
                Windowed = true,
                BackBufferFormat = _d3d.Adapters[0].CurrentDisplayMode.Format,
                BackBufferHeight = _resolution.Height,
                BackBufferWidth = _resolution.Width,
                PresentFlags = PresentFlags.Video,
                BackBufferCount = 1
            };

            _d3Device?.Dispose();

            _d3Device = new Device(_d3d, _d3d.Adapters[0].Adapter, DeviceType.Hardware, _windowHandle,
                CreateFlags.Multithreaded | CreateFlags.HardwareVertexProcessing, pparams);

            if (_surfAllocatorNotify != null)
            {
                int hr = _surfAllocatorNotify.ChangeD3DDevice(_d3Device.NativePointer, _d3d.Adapters[0].Monitor);
                DsError.ThrowExceptionForHR(hr);
            }
        }

        #endregion

        public delegate void NewFrameAvailableCallback();
        public NewFrameAvailableCallback NewFrameAvailable;

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        // Dispose Dx3DResources
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    FreeSurfaces();
                    if(_d3Device != null)
                        _d3Device.Dispose();
                    if(_d3d != null)
                        _d3d.Dispose();
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
