using System;
using SharpDX.Direct3D11;
using SharpDX;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using SharpDX.Direct3D;

namespace CamDX
{
    public class DX11Renderer : IDisposable
    {
        private Device _dxDevice;
        public Device DxDevice { get { return _dxDevice; } }
        public DeviceContext DxContext { get { return _dxDevice.ImmediateContext; } }
        private SwapChain _swapChain;
        public SwapChain SwapChain { get { return _swapChain; } }
        private IntPtr _windowHandle;
        public IntPtr WindowHandle { get { return _windowHandle; } }

        // Size of render space
        public Size2 RenderSize { get; protected set; }
        public TimeSpan RenderTime { get; protected set; }

        public DXScene CurrentScene { get; set; }

        private Texture2D _backBuffer;
        private RenderTargetView _renderView;
        public Texture2D BackBuffer { get { return _backBuffer; } }

        private Texture2D _depthBuffer;
        private DepthStencilView _depthStencilView;
        private DepthStencilState _depthStencilState;

        private RasterizerState _rasterizerState;

        public DX11Renderer(IntPtr windowHandle, Size2 size, Device dev = null)
        {
            RenderSize = size;
            _windowHandle = windowHandle;
            if(dev != null)
            {
                _dxDevice = dev;
            }
            else
            {
                var swapchainDesc = new SwapChainDescription()
                {
                    BufferCount = 2,
                    ModeDescription =
                        new ModeDescription(size.Width, size.Height,
                                            new Rational(60, 1), Format.R8G8B8A8_UNorm),
                    IsWindowed = true,
                    OutputHandle = windowHandle,
                    SampleDescription = new SampleDescription(1, 0),
                    SwapEffect = SwapEffect.Discard,
                    Usage = Usage.RenderTargetOutput
                };

                // Create Device and SwapChain
                Device.CreateWithSwapChain(DriverType.Hardware,
                   DeviceCreationFlags.None, swapchainDesc, out _dxDevice, out _swapChain);

                // Ignore all windows events 
                //    var factory = _swapChain.GetParent<Factory>();
                //    factory.MakeWindowAssociation(windowHandle, WindowAssociationFlags.None);

                Reset(size.Width, size.Height);
            }
        }

        public void Render()
        {
            _dxDevice.ImmediateContext.ClearDepthStencilView(_depthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);
            _dxDevice.ImmediateContext.ClearRenderTargetView(_renderView, Color.Black);

            CurrentScene.Render(_dxDevice.ImmediateContext);

            _swapChain.Present(0, PresentFlags.None);
        }

        // On resize
        public void Reset(int w, int h)
        {
            if(w < 1)
                throw new ArgumentOutOfRangeException("w");
            if(h < 1)
                throw new ArgumentOutOfRangeException("h");

            if(_backBuffer != null)
                _backBuffer.Dispose();
            if(_renderView != null)
                _renderView.Dispose();

            _swapChain.ResizeBuffers(2, w, h, Format.R8G8B8A8_UNorm, SwapChainFlags.None);

            // New RenderTargetView from the backbuffer 
            this.SetField(ref _backBuffer, Texture2D.FromSwapChain<Texture2D>(_swapChain, 0));
            this.SetField(ref _renderView, new RenderTargetView(_dxDevice, _backBuffer));

            // Create the depth buffer
            this.SetField(ref _depthBuffer, new Texture2D(_dxDevice, new Texture2DDescription()
            {
                Format = Format.D32_Float_S8X24_UInt,
                ArraySize = 1,
                MipLevels = 1,
                Width = w,
                Height = h,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            }));

            // Create the depth buffer state
            this.SetField(ref _depthStencilState, new DepthStencilState(_dxDevice, new DepthStencilStateDescription()
            {
                IsDepthEnabled = true,
                DepthWriteMask = DepthWriteMask.All,
                DepthComparison = Comparison.Less,
                IsStencilEnabled = true,
                StencilReadMask = 0xFF,
                StencilWriteMask = 0xFF,
                // Stencil operations if pixel is front-facing.
                FrontFace = new DepthStencilOperationDescription()
                {
                    Comparison = Comparison.Always,
                    DepthFailOperation = StencilOperation.Increment,
                    FailOperation = StencilOperation.Keep,
                    PassOperation = StencilOperation.Keep
                },
                // Stencil operations if pixel is back-facing.
                BackFace = new DepthStencilOperationDescription()
                {
                    Comparison = Comparison.Always,
                    DepthFailOperation = StencilOperation.Decrement,
                    FailOperation = StencilOperation.Keep,
                    PassOperation = StencilOperation.Keep
                },
            }));

            // Create the depth buffer view
            this.SetField(ref _depthStencilView, new DepthStencilView(_dxDevice, _depthBuffer, new DepthStencilViewDescription()
            {
                Format = Format.D24_UNorm_S8_UInt,
                Dimension = DepthStencilViewDimension.Texture2D
            }));

            this.SetField(ref _rasterizerState, new RasterizerState(_dxDevice, new RasterizerStateDescription()
            {
                CullMode = CullMode.Back,
                DepthBias = 0,
                DepthBiasClamp = 0.0f,
                IsDepthClipEnabled = true,
                FillMode = FillMode.Solid,
                IsAntialiasedLineEnabled = false,
                IsFrontCounterClockwise = false,
                IsMultisampleEnabled = false,
                IsScissorEnabled = false,
                SlopeScaledDepthBias = 0.0f
            }));

            // Setup targets and viewport for rendering
            _dxDevice.ImmediateContext.Rasterizer.SetViewport(
                new Viewport(0, 0, w, h, 0.0f, 1.0f));
            _dxDevice.ImmediateContext.OutputMerger.SetTargets(_depthStencilView, _renderView);
        }

        ~DX11Renderer() { Dispose(false); }
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Before shutting down set to windowed mode or when you release the swap chain it will throw an exception.
            if(_swapChain != null)
            {
                _swapChain.SetFullscreenState(false, null);
            }

            this.SetField(ref _rasterizerState, null);
            this.SetField(ref _depthStencilView, null);
            this.SetField(ref _depthStencilState, null);
            this.SetField(ref _depthBuffer, null);
            this.SetField(ref _renderView, null);
            this.SetField(ref _dxDevice, null);
            this.SetField(ref _swapChain, null);
        }
    }
}
