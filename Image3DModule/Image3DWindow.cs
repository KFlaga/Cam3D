
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
        public DXCamera Camera { get; protected set; }
        public DXScene Scene { get; protected set; }
        public DXResourceManager ResourcesManager { get; protected set; }

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
            cube.Shader = ResourcesManager.ShaderManager.GetShader("ColorShader_NoLight");

            for(int i = 0; i < 8; ++i)
            {
                cube.SetColor(i, color);
            }
            cube.UpdateBuffers();

            _cubePointsMap.Add(cube);

            DXSceneNode node = Scene.RootNode.CreateChildNode();
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
            if (Camera != null)
            {
                Camera.Aspect = (float)(this.ActualWidth / this.ActualHeight);
            }
        }

        void InitDX()
        {
            IsRendering = false;
            base.Show();

            // Create camera
            Camera = new CamDX.DXCamera();
            Camera.FarBound = 2000.0f;
            Camera.NearBound = 1.0f;
            Camera.Position = new SharpDX.Vector3(0.0f, 000.0f, 300.0f);
            Camera.LookAt = new SharpDX.Vector3(0.0f, 0.0f, 0.0f);
            Camera.UpDir = SharpDX.Vector3.Up;

            // Create renderer for window
            _renderer = new DXRenderer(WinHanldle, new Size2(800, 600));
            var device = _renderer.DxDevice;

            // Load resources
            XmlDocument resDoc = new XmlDocument();
            resDoc.Load("resources.xml");
            XmlNode dxResourcesNode = resDoc.SelectSingleNode("//DXResources");
            ResourcesManager = new DXResourceManager(device, dxResourcesNode);

            // Create scene
            Scene = new CamDX.DXScene(_renderer.DxDevice);
            Scene.GlobalLights = new GlobalLightsData()
            {
                Ambient = new Color4(1.0f)
            };
            _renderer.CurrentScene = Scene;
            Scene.CurrentCamera = Camera;

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
            Scene.Dispose();
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
                Scene.Dispose();
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
            else if(e.Key == Key.E)
            {
                Camera.Position = new SharpDX.Vector3(Camera.Position.X, Camera.Position.Y, - Camera.Position.Z);
            }
            if (!ctrlDown)
            { 
                if (e.Key == Key.Up)
                {
                    Camera.MoveZ(10f);
                }
                else if (e.Key == Key.Down)
                {
                    Camera.MoveZ(-10f);
                }
                else if (e.Key == Key.Left)
                {
                    Camera.MoveX(-10f);
                }
                else if (e.Key == Key.Right)
                {
                    Camera.MoveX(10f);
                }
                else if(e.Key == Key.Z)
                {
                    Camera.MoveY(-10f);
                }
                else if(e.Key == Key.X)
                {
                    Camera.MoveY(10f);
                }
            }
            else
            {
                if (e.Key == Key.Up)
                {
                    Camera.RotateY(-(float)Math.PI / 90);
                }
                else if (e.Key == Key.Down)
                {
                    Camera.RotateY((float)Math.PI / 90);
                }
                else if (e.Key == Key.Left)
                {
                    Camera.RotateX(-(float)Math.PI / 90);
                }
                else if (e.Key == Key.Right)
                {
                    Camera.RotateX((float)Math.PI / 90);
                }
                else if(e.Key == Key.Z)
                {
                    Camera.RotateZ(-(float)Math.PI / 90);
                }
                else if(e.Key == Key.X)
                {
                    Camera.RotateZ(-(float)Math.PI / 90);
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
