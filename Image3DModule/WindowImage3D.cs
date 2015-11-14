
using CamDX;
using CamDX.WPF;
using CamCore;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace Image3DModule
{
    class Image3DWindow : DX11Window
    {
        DXCamera _camera = null;
        RenderGroup _tetraPoints = null;
        DX11Scene _scene = null;

        List<KeyValuePair<Camera3DPoint, DX11Tetrahedron>> _pointMap = 
            new List<KeyValuePair<Camera3DPoint, DX11Tetrahedron>>();

        public Image3DWindow()
        {
            IsRendering = false;
            ResizeMode = System.Windows.ResizeMode.CanResize;
            
            Focusable = true;
        }

        public void AddPoint(Camera3DPoint point)
        {
            var tetra = new DX11Tetrahedron(Renderer.DxDevice);
            tetra.SetSize(new Vector3((float)point.Real.X, (float)point.Real.Y, (float)point.Real.Z), 
                0.05f);

            _pointMap.Add(new KeyValuePair<Camera3DPoint, DX11Tetrahedron>(
                point, tetra));

            _tetraPoints.Models.Add(tetra);
        }

        public void RemovePoint(Camera3DPoint point)
        {
            KeyValuePair<Camera3DPoint, DX11Tetrahedron> toRemove =
                new KeyValuePair<Camera3DPoint, DX11Tetrahedron>(null, null);
            foreach (var pair in _pointMap)
            {
                if (pair.Key == point)
                {
                    _tetraPoints.Models.Remove(pair.Value);
                    pair.Value.Dispose();
                    toRemove = pair;
                    break;
                }
            }
            if (toRemove.Key != null)
                _pointMap.Remove(toRemove);
        }

        protected override void UpdateSize()
        {
            base.UpdateSize();
            if (_camera != null)
            {
                _camera.Aspect = (float)(this.ActualWidth / this.ActualHeight);
            }
        }

        public new void Show()
        {
            IsRendering = false;
            base.Show();

            Renderer = new DX11Renderer(base.WinHanldle, new SharpDX.Size2(800, 600));
            var device = Renderer.DxDevice;

            // Create camera
            _camera = new CamDX.DXCamera();

            // Create scene
            _scene = new CamDX.DX11Scene(Renderer.DxDevice);
            Renderer.CurrentScene = _scene;
            _scene.CurrentCamera = _camera;

            _tetraPoints = new RenderGroup();
            _scene.RenderGroups.Add(_tetraPoints);

            // Compile Vertex and Pixel shaders
            var vertexShaderByteCode = ShaderBytecode.CompileFromFile("d:\\shaders.fx", "VS", "vs_4_0", ShaderFlags.None, EffectFlags.None);
            var vertexShader = new VertexShader(device, vertexShaderByteCode);

            var pixelShaderByteCode = ShaderBytecode.CompileFromFile("d:\\shaders.fx", "PS", "ps_4_0", ShaderFlags.None, EffectFlags.None);
            var pixelShader = new PixelShader(device, pixelShaderByteCode);

            var signature = ShaderSignature.GetInputSignature(vertexShaderByteCode);
            // Layout from VertexShader input signature
            var layout = new InputLayout(device, signature, new[]
                    {
                        new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                        new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 12, 0)
                    });

            // Bound shaders to render group
            _tetraPoints.VertexShader = vertexShader;
            _tetraPoints.PixelShader = pixelShader;
            _tetraPoints.InputLayout = layout;

            // Now when all is set, enable dx rendering
            IsRendering = true;
            Renderer.Render();
        }
        

        public new void Hide()
        {
            IsRendering = false;
            base.Hide();

            Renderer.Dispose();
            foreach(var tetra in _tetraPoints.Models)
            {
                tetra.Dispose();
            }
            _scene.Dispose();
            
        }

        private bool ctrlDown = false;
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if(e.Key == Key.LeftCtrl)
            {
                ctrlDown = true;
            }
            if (!ctrlDown)
            { 
                if (e.Key == Key.Up)
                {
                    _camera.MoveZ(0.1f);
                }
                else if (e.Key == Key.Down)
                {
                    _camera.MoveZ(-0.1f);
                }
                else if (e.Key == Key.Left)
                {
                    _camera.MoveX(-0.1f);
                }
                else if (e.Key == Key.Right)
                {
                    _camera.MoveX(0.1f);
                }
            }
            else
            {
                if (e.Key == Key.Up)
                {
                    _camera.RotateY(-(float)Math.PI / 180);
                }
                else if (e.Key == Key.Down)
                {
                    _camera.RotateY((float)Math.PI / 180);
                }
                else if (e.Key == Key.Left)
                {
                    _camera.RotateX(-(float)Math.PI / 180);
                }
                else if (e.Key == Key.Right)
                {
                    _camera.RotateX((float)Math.PI / 180);
                }
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.Key == Key.LeftCtrl)
            {
                ctrlDown = false;
            }
        }
    }
}
