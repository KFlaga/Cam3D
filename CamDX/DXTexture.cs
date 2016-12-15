using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;

namespace CamDX
{
    public class DXTexture : IDisposable
    {
        Texture2D _texture;
        ShaderResourceView _resourceView;
        SharpDX.WIC.BitmapSource _bitmap;
        SamplerState _sampler;
        public ShaderResourceView DxResource { get { return _resourceView; } }
        public Texture2D DxTexture { get { return _texture; } }
        public SharpDX.WIC.BitmapSource BitmapSource
        {
            get { return _bitmap; }
        }
        public SamplerState Sampler { get { return _sampler; } }

        public void Load(Device dxDevice, XmlNode textureNode)
        {
            // <Texture>
            //  <File value="file_path"/> -> if this is not specified texture is null and is expected to be set in code manually (only sampler is created)
            //  <AddressMode value="Wrap/Clamp/Mirror/MirrorOnce/> -> if UV is outside [0,1] (clamp by defualt)
            //  <ComparsionMode value="Always/Never/Equal/NotEqual/Greater/Lower/GreaterEqual/LowerEqual"/> (Always by defaut)
            //  <FilterMode value="Anisotropic/Point/Linear/AnisotropicNoCmp/PointNoCmp/LinearNoCmp"/> (Linear by defaut)
            //  <MaxAnisotropy value=[1-16]/> max anisotropy level for Anisotropic filter (default 1)
            // </Texture>

            XmlNode fileNode = textureNode.SelectSingleNode("File");
            if(fileNode != null)
            {
                string filepath = fileNode.Attributes["value"].Value;
                var bitmapSource = TextureLoader.LoadBitmap(filepath);
                SetBitmapSource(dxDevice, bitmapSource);
            }
            else
            {
                this.SetField(ref _bitmap, null);
                this.SetField(ref _texture, null);
                this.SetField(ref _resourceView, null);
            }

            XmlNode addressModeNode = textureNode.SelectSingleNode("AddressMode");
            TextureAddressMode addressMode = TextureAddressMode.Clamp;
            if(addressModeNode != null)
                addressMode = ParseAddressMode(addressModeNode.Attributes["value"].Value);

            XmlNode comparsionModeNode = textureNode.SelectSingleNode("ComparsionMode");
            Comparison comparsionMode = Comparison.Always;
            if(comparsionModeNode != null)
                comparsionMode = ParseComparsionMode(comparsionModeNode.Attributes["value"].Value);

            XmlNode filterModeNode = textureNode.SelectSingleNode("FilterMode");
            Filter filterMode = Filter.ComparisonMinMagLinearMipPoint;
            if(filterModeNode != null)
                filterMode = ParseFilternMode(filterModeNode.Attributes["value"].Value);

            int maxAnisotropy = 1;
            XmlNode maxAnisotropyNode = textureNode.SelectSingleNode("MaxAnisotropy");
            if(maxAnisotropyNode != null)
            {
                maxAnisotropy = int.Parse(filterModeNode.Attributes["value"].Value);
                maxAnisotropy = Math.Min(16, Math.Max(1, maxAnisotropy));
            }

            this.SetField(ref _sampler, null);
            _sampler = new SamplerState(dxDevice, new SamplerStateDescription()
            {
                AddressU = addressMode,
                AddressW = addressMode,
                AddressV = addressMode,
                BorderColor = new SharpDX.Mathematics.Interop.RawColor4(0.0f, 0.0f, 0.0f, 0.0f),
                ComparisonFunction = comparsionMode,
                Filter = filterMode,
                MaximumAnisotropy = maxAnisotropy,
                MaximumLod = 1e12f,
                MinimumLod = 0.0f,
                MipLodBias = 0.0f
            });
        }

        // Old bitmap is disposed (should be set to null before if it is used in some other place)
        public void SetBitmapSource(Device dxDevice, SharpDX.WIC.BitmapSource bitmapSource)
        {
            this.SetField(ref _texture, null);
            this.SetField(ref _resourceView, null);
            this.SetField(ref _bitmap, null);

            _bitmap = bitmapSource;
            _texture = TextureLoader.CreateTexture2DFromBitmap(dxDevice, bitmapSource);
            _resourceView = new ShaderResourceView(dxDevice, _texture, new ShaderResourceViewDescription()
            {
                Format = _texture.Description.Format,
                Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.Texture2D,
                Texture2D = new ShaderResourceViewDescription.Texture2DResource()
                {
                    MipLevels = 1,
                    MostDetailedMip = 0
                }
            });
        }

        TextureAddressMode ParseAddressMode(string modeName)
        {
            switch(modeName)
            {
                case "Wrap": return TextureAddressMode.Wrap;
                case "MirrorOnce": return TextureAddressMode.MirrorOnce;
                case "Mirror": return TextureAddressMode.Mirror;
                case "Clamp":
                case "Default":
                default: return TextureAddressMode.Clamp;
            }
        }

        Comparison ParseComparsionMode(string modeName)
        {
            switch(modeName)
            {
                case "Never": return Comparison.Never;
                case "Equal": return Comparison.Equal;
                case "Greater": return Comparison.Greater;
                case "GreaterEqual": return Comparison.GreaterEqual;
                case "Less": return Comparison.Less;
                case "LessEqual": return Comparison.LessEqual;
                case "NotEqual": return Comparison.NotEqual;
                case "Always":
                case "Default":
                default: return Comparison.Always;
            }
        }

        Filter ParseFilternMode(string modeName)
        {
            switch(modeName)
            {
                case "Anisotropic": return Filter.ComparisonAnisotropic;
                case "Point": return Filter.ComparisonMinMagMipPoint;
                case "Linear":
                case "Default":
                default: return Filter.ComparisonMinMagLinearMipPoint;
                case "AnisotropicNoCmp": return Filter.Anisotropic;
                case "PointNoCmp": return Filter.MinMagMipPoint;
                case "LinearNoCmp": return Filter.MinMagLinearMipPoint;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if(!disposedValue)
            {
                if(disposing)
                {
                    this.SetField(ref _texture, null);
                    this.SetField(ref _resourceView, null);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~DXTexture() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

        public static class TextureLoader
        {
            public static SharpDX.WIC.BitmapSource LoadBitmap(string filename)
            {
                return LoadBitmap(new SharpDX.WIC.ImagingFactory2(), filename);
            }

            public static SharpDX.WIC.BitmapSource LoadBitmap(SharpDX.WIC.ImagingFactory2 factory, string filename)
            {
                var bitmapDecoder = new SharpDX.WIC.BitmapDecoder(
                    factory,
                    filename,
                    SharpDX.WIC.DecodeOptions.CacheOnDemand
                    );

                return LoadBitmap(bitmapDecoder, factory);
            }

            public static SharpDX.WIC.BitmapSource LoadBitmap(Stream stream)
            {
                return LoadBitmap(new SharpDX.WIC.ImagingFactory2(), stream);
            }

            public static SharpDX.WIC.BitmapSource LoadBitmap(SharpDX.WIC.ImagingFactory2 factory, Stream stream)
            {
                var bitmapDecoder = new SharpDX.WIC.BitmapDecoder(
                    factory,
                    stream,
                    SharpDX.WIC.DecodeOptions.CacheOnDemand
                    );

                return LoadBitmap(bitmapDecoder, factory);
            }

            static SharpDX.WIC.BitmapSource LoadBitmap(SharpDX.WIC.BitmapDecoder decoder, SharpDX.WIC.ImagingFactory2 factory)
            {
                var formatConverter = new SharpDX.WIC.FormatConverter(factory);

                formatConverter.Initialize(
                    decoder.GetFrame(0),
                    SharpDX.WIC.PixelFormat.Format32bppPRGBA,
                    SharpDX.WIC.BitmapDitherType.None,
                    null,
                    0.0,
                    SharpDX.WIC.BitmapPaletteType.Custom);

                return formatConverter;
            }

            public static Texture2D CreateTexture2DFromFile(Device device, string filename)
            {
                var bitmapSource = LoadBitmap(filename);
                return CreateTexture2DFromBitmap(device, bitmapSource);
            }

            public static Texture2D CreateTexture2DFromFile(Device device, Stream stream)
            {
                var bitmapSource = LoadBitmap(stream);
                return CreateTexture2DFromBitmap(device, bitmapSource);
            }

            public static Texture2D CreateTexture2DFromBitmap(Device device, SharpDX.WIC.BitmapSource bitmapSource)
            {
                // Allocate DataStream to receive the WIC image pixels
                int stride = bitmapSource.Size.Width * 4;
                using(var buffer = new SharpDX.DataStream(bitmapSource.Size.Height * stride, true, true))
                {
                    // Copy the content of the WIC to the buffer
                    bitmapSource.CopyPixels(stride, buffer);
                    return new Texture2D(device, new SharpDX.Direct3D11.Texture2DDescription()
                    {
                        Width = bitmapSource.Size.Width,
                        Height = bitmapSource.Size.Height,
                        ArraySize = 1,
                        BindFlags = BindFlags.ShaderResource,
                        Usage = ResourceUsage.Immutable,
                        CpuAccessFlags = CpuAccessFlags.None,
                        Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                        MipLevels = 1,
                        OptionFlags = ResourceOptionFlags.None,
                        SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                    }, new SharpDX.DataRectangle(buffer.DataPointer, stride));
                }
            }

            public static Texture2D CreateTexture2DFromBitmap(Device device, SharpDX.WIC.BitmapSource bitmapSource, Texture2DDescription texDesc)
            {
                // Allocate DataStream to receive the WIC image pixels
                int stride = bitmapSource.Size.Width * 4;
                using(var buffer = new SharpDX.DataStream(bitmapSource.Size.Height * stride, true, true))
                {
                    // Copy the content of the WIC to the buffer
                    bitmapSource.CopyPixels(stride, buffer);
                    return new Texture2D(device, texDesc, new SharpDX.DataRectangle(buffer.DataPointer, stride));
                }
            }
        }
    }
}
