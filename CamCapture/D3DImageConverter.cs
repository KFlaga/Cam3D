using System;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace CamCapture
{
    // Converts D3DSurfaces (as NativePointers) to BitmapSource
    // for image showing/processing
    // To do so it uses build-in D3DImage methods : SetBackBuffer / CopyBackBuffer
    public class D3DImageConverter : D3DImage
    {
        private BitmapSource _bitmapSource = null;
        public BitmapSource BitmapSource
        {
            get
            {
                return _bitmapSource;
            }
        }

        public void SetD3DSurfaceSource(IntPtr surface)
        {
            Lock();
            SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface);
            _bitmapSource = CopyBackBuffer();
            Unlock();
        }

        protected override BitmapSource CopyBackBuffer()
        {
            return base.CopyBackBuffer();
        }
    }
}
