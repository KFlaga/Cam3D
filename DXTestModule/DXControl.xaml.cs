using System.Windows;
using System.Windows.Controls;
using CamDX;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;
using SharpDX.DXGI;
using SharpDX;
using System.Xml;

namespace DXTestModule
{
    public partial class DXControl : UserControl
    {
        CamDX.WPF.DX11Window _dxWindow;
        DXCamera _camera;
        DXRenderer _renderer;
        DXScene _scene;
        DXResourceManager _resourceManager;
        DXCube _testCube;
        DXSceneNode _cubeNode;

        public DXControl()
        {
            InitializeComponent();
        }

        private void ShowWindow(object sender, RoutedEventArgs e)
        {
            // Create and show window -> disable rendering for now, as
            // we show it only to get window's handle
            _dxWindow = new CamDX.WPF.DX11Window();
            _dxWindow.ResizeMode = ResizeMode.NoResize;
            _dxWindow.IsRendering = false;
            _dxWindow.Show();
            
            // Create camera
            _camera = new CamDX.DXCamera();
            _camera.FarBound = 400.0f;
            _camera.NearBound = 1.0f;
            _camera.Position = new Vector3(0.0f, 60.0f, 260.0f);
            _camera.LookAt = new Vector3(0.0f, 0.0f, 0.0f);
            
            // Create renderer for window
            _renderer = new DXRenderer(_dxWindow.WinHanldle, new Size2( 800, 600));
            var device = _renderer.DxDevice;
            _dxWindow.Renderer = _renderer;

            // Load resources
            XmlDocument resDoc = new XmlDocument();
            resDoc.Load("resources.xml");
            XmlNode dxResourcesNode = resDoc.SelectSingleNode("//DXResources");
            _resourceManager = new DXResourceManager(device, dxResourcesNode);

            // Create scene
            _scene = new CamDX.DXScene(_renderer.DxDevice);
            _renderer.CurrentScene = _scene;
            _scene.CurrentCamera = _camera;

            // Create test cube and scene node for it, add to scene
            _testCube = new DXCube(device, new Vector3(10.0f, 15.0f, 5.0f));
            _testCube.Shader = _resourceManager.ShaderManager.GetShader("TextureShader_NoLight");
            _testCube.SetColor(DXCube.VertexPosition.BotLeftBack, new Color4(1.0f, 1.0f, 0.0f, 1.0f));
            _testCube.SetTexCoords(DXCube.VertexPosition.BotLeftBack, new Vector2(0.0f, 1.0f));
            _testCube.SetColor(DXCube.VertexPosition.BotLeftFront, new Color4(0.7f, 0.7f, 0.7f, 1.0f));
            _testCube.SetTexCoords(DXCube.VertexPosition.BotLeftFront, new Vector2(0.0f, 1.0f));
            _testCube.SetColor(DXCube.VertexPosition.BotRightBack, new Color4(1.0f, 0.0f, 1.0f, 1.0f));
            _testCube.SetTexCoords(DXCube.VertexPosition.BotRightBack, new Vector2(1.0f, 1.0f));
            _testCube.SetColor(DXCube.VertexPosition.BotRightFront, new Color4(0.7f, 0.7f, 0.7f, 1.0f));
            _testCube.SetTexCoords(DXCube.VertexPosition.BotRightFront, new Vector2(1.0f, 1.0f));
            _testCube.SetColor(DXCube.VertexPosition.TopLeftBack, new Color4(0.0f, 1.0f, 1.0f, 1.0f));
            _testCube.SetTexCoords(DXCube.VertexPosition.TopLeftBack, new Vector2(0.0f, 0.0f));
            _testCube.SetColor(DXCube.VertexPosition.TopLeftFront, new Color4(0.7f, 0.7f, 0.7f, 1.0f));
            _testCube.SetTexCoords(DXCube.VertexPosition.TopLeftFront, new Vector2(0.0f, 0.0f));
            _testCube.SetColor(DXCube.VertexPosition.TopRightBack, new Color4(1.0f, 1.0f, 1.0f, 1.0f));
            _testCube.SetTexCoords(DXCube.VertexPosition.TopRightBack, new Vector2(1.0f, 0.0f));
            _testCube.SetColor(DXCube.VertexPosition.TopRightFront, new Color4(0.7f, 0.7f, 0.7f, 1.0f));
            _testCube.SetTexCoords(DXCube.VertexPosition.TopRightFront, new Vector2(1.0f, 0.0f));
            _testCube.UpdateBuffers();

            _cubeNode = new DXSceneNode();
            _cubeNode.Position = new Vector3(0.0f);
            _cubeNode.Scale = new Vector3(1.0f);
            _cubeNode.Orientation = Quaternion.RotationAxis(Vector3.UnitY, 0.0f);
            _cubeNode.UpdateTransfromMatrix();

            _cubeNode.AttachModel(_testCube);
            _scene.RootNode.Children.Add(_cubeNode);

            // Load texture for test cube
            var bitmapSource = DXTexture.TextureLoader.LoadBitmap("shaders/tsu0.png");
            _testCube.Shader.Textures[0].SetBitmapSource(_renderer.DxDevice, bitmapSource);

            // Now when all is set, enable dx rendering
            _dxWindow.IsRendering = true;
            _dxWindow.Render();

            _dxWindow.PreRender += _dxWindow_PreRender;
        }

        float _angle = 0.0f;
        Vector3 _axis = new Vector3(0.0f, 1.0f, 0.0f); 
        private void _dxWindow_PreRender(object sender, System.EventArgs e)
        {
            // Rotate test cube
            //_angle = _angle > 6.28f ? 0.0f : _angle + 0.02f;
            //Quaternion qnew = Quaternion.RotationAxis(_axis, _angle);
            //_cubeNode.Orientation = qnew;
            //_cubeNode.UpdateTransfromMatrix();
        }
    }
}

