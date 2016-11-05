
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
using System.Xml;

namespace Image3DModule
{
    class Image3DWindow : DX11Window
    {
        DXCamera _camera = null;
        DXScene _scene = null;

        DXResourceManager _resourcesManager;
        List<DXCube> _cubePointsMap = new List<DXCube>();

        public Image3DWindow()
        {
            IsRendering = false;
            ResizeMode = System.Windows.ResizeMode.CanResize;
            
            Focusable = true;

            InitDX();

            Closed += Image3DWindow_Closed;
        }

        public void AddPointCube(SharpDX.Vector3 point, Color4 color)
        {
            DXCube cube = new DXCube(_renderer.DxDevice, new SharpDX.Vector3(1.0f), new SharpDX.Vector3());
            cube.Shader = _resourcesManager.ShaderManager.GetShader("ColorShader_NoLight");

            for(int i = 0; i < 8; ++i)
            {
                cube.SetColor(i, color);
            }
            cube.UpdateBuffers();

            _cubePointsMap.Add(cube);

            DXSceneNode node = _scene.RootNode.CreateChildNode();
            node.Position = point;
            node.Orientation = Quaternion.RotationAxis(SharpDX.Vector3.UnitY, 0.0f);
            node.UpdateTransfromMatrix();
            node.AttachModel(cube);
        }

        public void RemovePointCube(SharpDX.Vector3 point)
        {
            int idx = _cubePointsMap.FindIndex((c) => { return (c.SceneNode.Position == point); });
            if(idx > 0)
            {
                DXCube cube = _cubePointsMap[idx];
                _cubePointsMap.RemoveAt(idx);
                DXSceneNode node = cube.SceneNode;
                cube.SceneNode.DetachAll();
                cube.Dispose();
                node.Parent.Children.Remove(node);
            }
        }

        public void ResetPoints()
        {
            foreach(var cube in _cubePointsMap)
            {
                DXSceneNode node = cube.SceneNode;
                cube.SceneNode.DetachAll();
                cube.Dispose();
                node.Parent.Children.Remove(node);
            }
            _cubePointsMap.Clear();
        }

        protected override void UpdateSize()
        {
            base.UpdateSize();
            if (_camera != null)
            {
                _camera.Aspect = (float)(this.ActualWidth / this.ActualHeight);
            }
        }

        void InitDX()
        {
            IsRendering = false;
            base.Show();

            // Create camera
            _camera = new CamDX.DXCamera();
            _camera.FarBound = 2000.0f;
            _camera.NearBound = 1.0f;
            _camera.Position = new SharpDX.Vector3(0.0f, 000.0f, 300.0f);
            _camera.LookAt = new SharpDX.Vector3(0.0f, 0.0f, 0.0f);
            // _camera.UpDir = SharpDX.Vector3.Down;

            // Create renderer for window
            _renderer = new DXRenderer(WinHanldle, new Size2(800, 600));
            var device = _renderer.DxDevice;

            // Load resources
            XmlDocument resDoc = new XmlDocument();
            resDoc.Load("resources.xml");
            XmlNode dxResourcesNode = resDoc.SelectSingleNode("//DXResources");
            _resourcesManager = new DXResourceManager(device, dxResourcesNode);

            // Create scene
            _scene = new CamDX.DXScene(_renderer.DxDevice);
            _scene.GlobalLights = new GlobalLightsData()
            {
                Ambient = new Color4(1.0f)
            };
            _renderer.CurrentScene = _scene;
            _scene.CurrentCamera = _camera;

            base.Hide();
        }
        
        public new void Show()
        {
            IsRendering = true;
            base.Show();
            Renderer.Render();
        }

        public new void Hide()
        {
            throw new NotImplementedException("This window does not support Hide()"); 
        }
        
        public new void Close()
        {
            _closingInternal = true;
            IsRendering = false;
            foreach(var cube in _cubePointsMap)
            {
                cube.Dispose();
            }
            _cubePointsMap.Clear();
            _scene.Dispose();
            Renderer.Dispose();
            base.Close();
            _closingInternal = false;
        }
        
        private void Image3DWindow_Closed(object sender, EventArgs e)
        {
            if(_closingInternal == false)
            {
                foreach(var cube in _cubePointsMap)
                {
                    cube.Dispose();
                }
                _cubePointsMap.Clear();
                _scene.Dispose();
                Renderer.Dispose();
            }
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
                    _camera.MoveZ(10f);
                }
                else if (e.Key == Key.Down)
                {
                    _camera.MoveZ(-10f);
                }
                else if (e.Key == Key.Left)
                {
                    _camera.MoveX(-10f);
                }
                else if (e.Key == Key.Right)
                {
                    _camera.MoveX(10f);
                }
            }
            else
            {
                if (e.Key == Key.Up)
                {
                    _camera.RotateY(-(float)Math.PI / 90);
                }
                else if (e.Key == Key.Down)
                {
                    _camera.RotateY((float)Math.PI / 90);
                }
                else if (e.Key == Key.Left)
                {
                    _camera.RotateX(-(float)Math.PI / 90);
                }
                else if (e.Key == Key.Right)
                {
                    _camera.RotateX((float)Math.PI / 90);
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
