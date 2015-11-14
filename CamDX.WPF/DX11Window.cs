using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace CamDX.WPF
{
    public class DX11Window : Window
    {
        protected DX11Renderer _renderer;
        private bool _isRendering = false;
        private bool _isRenderingOld = false;

        public IntPtr WinHanldle {  get { return new WindowInteropHelper(this).EnsureHandle(); } }
        public DX11Renderer Renderer
        {
            get
            {
                return _renderer;
            }
            set
            {
                _renderer = value;
                UpdateSize();
                UpdateIsRendering();
            }
        }
        public bool IsRendering
        {
            get { return _isRendering; }
            set
            {
                if (value == _isRendering)
                    return;
                _isRenderingOld = _isRendering;
                _isRendering = value;
                UpdateIsRendering();
            }
        }

        public DX11Window(int width, int height) : base()
        {
            base.SnapsToDevicePixels = true;
            Width = width;
            Height = height;
            DX11Renderer renderer = new DX11Renderer(WinHanldle, new SharpDX.Size2(width, height));
        }

        public DX11Window(DX11Renderer renderer) : base()
        {
            base.SnapsToDevicePixels = true;
            Renderer = renderer;
        }

        public DX11Window() : base()
        {
            base.SnapsToDevicePixels = true;
            Width = 800;
            Height = 600;
        }
 
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize)
        {
            base.ArrangeOverride(finalSize);
            UpdateSize();
            return finalSize;
        }

        protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize)
        {
            int w = (int)Math.Ceiling(availableSize.Width);
            int h = (int)Math.Ceiling(availableSize.Height);
            return new System.Windows.Size(w, h);
        }

        protected override Visual GetVisualChild(int index)
        {
            throw new ArgumentOutOfRangeException();
        }
        protected override int VisualChildrenCount { get { return 0; } }

        void UpdateIsRendering()
        {
            var newValue =
                !IsInDesignMode
                && IsRendering
                && Renderer != null
                && IsVisible;

            if (newValue != _isRenderingOld)
            {
                _isRendering = newValue;
                if (IsRendering)
                {
                    CompositionTarget.Rendering += OnRendering;
                }
                else
                {
                    CompositionTarget.Rendering -= OnRendering;
                }
            }
        }

        void OnRendering(object sender, EventArgs e)
        {
            Render();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            UpdateSize();
        }

        protected virtual void UpdateSize()
        {
            if (Renderer == null)
                return;
            Renderer.Reset((int)this.ActualWidth, (int)this.ActualHeight);
        }
        
        public void Render()
        {
            if (!IsRendering || Renderer == null || IsInDesignMode)
                return;
            Renderer.Render();
        }
       
        /// <summary>
        /// Gets a value indicating whether the control is in design mode
        /// (running in Blend or Visual Studio).
        /// </summary>
        public bool IsInDesignMode
        {
            get { return DesignerProperties.GetIsInDesignMode(this); }
        }
    }
}
