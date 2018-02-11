using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CamDX.WPF
{
    public static class DXWPFExt
    {
        public unsafe static WriteableBitmap GetBitmap(this SharpDX.Direct3D11.Texture2D tex)
        {
            DataRectangle db;
            DataStream data = new DataStream(tex.Description.Height * tex.Description.Width * 4, true, true);
            using(var copy = tex.GetCopy())
            using (var surface = copy.QueryInterface<SharpDX.DXGI.Surface>())
            {
                db = surface.Map(SharpDX.DXGI.MapFlags.Read, out data);
                // can't destroy the surface now with WARP driver

                int w = tex.Description.Width;
                int h = tex.Description.Height;
                var wb = new WriteableBitmap(w, h, 96.0, 96.0, PixelFormats.Bgra32, null);
                wb.Lock();
                try
                {
                    uint* wbb = (uint*)wb.BackBuffer;

                    data.Position = 0;
                    for (int y = 0; y < h; y++)
                    {
                        data.Position = y * db.Pitch;
                        for (int x = 0; x < w; x++)
                        {
                            var c = data.Read<uint>();
                            wbb[y * w + x] = c;
                        }
                    }
                }
                finally
                {
                    wb.AddDirtyRect(new Int32Rect(0, 0, w, h));
                    wb.Unlock();
                    data.Dispose();
                    
                }
                return wb;
            }
        }

        static SharpDX.Direct3D11.Texture2D GetCopy(this SharpDX.Direct3D11.Texture2D tex)
        {
            var teximg = new SharpDX.Direct3D11.Texture2D(tex.Device, new SharpDX.Direct3D11.Texture2DDescription
            {
                Usage = SharpDX.Direct3D11.ResourceUsage.Staging,
                BindFlags = SharpDX.Direct3D11.BindFlags.None,
                CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.Read,
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                OptionFlags = SharpDX.Direct3D11.ResourceOptionFlags.None,
                ArraySize = tex.Description.ArraySize,
                Height = tex.Description.Height,
                Width = tex.Description.Width,
                MipLevels = tex.Description.MipLevels,
                SampleDescription = tex.Description.SampleDescription,
            });
            tex.Device.ImmediateContext.CopyResource(tex, teximg);
            return teximg;
        }

        public static WriteableBitmap GetImage(this DXRenderer rend)
        {
            return rend.BackBuffer.GetBitmap();
        }
    }
}

