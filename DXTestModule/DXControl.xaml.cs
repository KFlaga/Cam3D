using System.Windows;
using System.Windows.Controls;
using CamDX;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;
using SharpDX.DXGI;
using SharpDX;

namespace DXTestModule
{
    public partial class DXControl : UserControl
    {
        CamDX.WPF.DX11Window dxwindow = null;
        DXCamera camera = null;
        DX11Renderer renderer = null;
        DXTestCube cube = null;
        RenderGroup rg = null;
        DX11Scene scene = null;

        public DXControl()
        {
            InitializeComponent();
        }

        private void ShowWindow(object sender, RoutedEventArgs e)
        {
            // Create and show window -> disable rendering for now, as
            // we show it only to get window's handle
            dxwindow = new CamDX.WPF.DX11Window();
            dxwindow.ResizeMode = ResizeMode.NoResize;
            dxwindow.IsRendering = false;
            dxwindow.Show();
            
            // Create camera
            camera = new CamDX.DXCamera();
            
            // Create renderer for window
            renderer = new DX11Renderer(dxwindow.WinHanldle, new Size2( 800, 600));
            var device = renderer.DxDevice;
            dxwindow.Renderer = renderer;

            // Create scene
            scene = new CamDX.DX11Scene(renderer.DxDevice);
            renderer.CurrentScene = scene;
            scene.CurrentCamera = camera;

            rg = new RenderGroup();

            // Create model to show on our window
            cube = new DXTestCube(renderer.DxDevice);
            rg.Models.Add(cube);
            scene.RenderGroups.Add(rg);

            // Compile Vertex and Pixel shaders
            var vertexShaderByteCode = ShaderBytecode.CompileFromFile("d:\\shaders.fx", "VS", "vs_4_0", ShaderFlags.None, EffectFlags.None);
            var vertexShader = new VertexShader(device, vertexShaderByteCode);

            var pixelShaderByteCode = ShaderBytecode.CompileFromFile("d:\\shaders.fx", "PS", "ps_4_0", ShaderFlags.None, EffectFlags.None);
            var pixelShader = new PixelShader(device, pixelShaderByteCode);

            var signature = ShaderSignature.GetInputSignature(vertexShaderByteCode);
            // Layout from VertexShader input signature
            var layout = new InputLayout(device, signature, new[]
                    {
                        new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
                        new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 16, 0)
                    });

            // Bound shaders to render group
            rg.VertexShader = vertexShader;
            rg.PixelShader = pixelShader;
            rg.InputLayout = layout;

            // Now when all is set, enable dx rendering
            dxwindow.IsRendering = true;
            dxwindow.Render();
        }




    }
}

