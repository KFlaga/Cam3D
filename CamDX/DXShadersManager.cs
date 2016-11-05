using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Xml;

namespace CamDX
{
    public class DXShadersManager : IDisposable
    {
        Dictionary<string, DXShader> _shadersByName;

        Device _dxDevice;
        public Device DXDevice
        {
            get { return _dxDevice; } 
            set{ _dxDevice = value; }
        }

        public DXShadersManager(Device dxDevice)
        {
            _dxDevice = dxDevice;
        }

        public DXShader GetShader(string name)
        {
            return _shadersByName[name];
        }

        public void AddShader(DXShader shader)
        {
            DXShader oldShader;
            var idOld = _shadersByName.TryGetValue(shader.Name, out oldShader);
            if(idOld == false)
            {
                _shadersByName.Add(shader.Name, shader);
            }
            else
            {
                oldShader.Dispose();
                _shadersByName[shader.Name] = shader;
            }
        }

        public void LoadShaders(XmlNode shaderResourcesNode)
        {
            //    < Shaders >
            //        < ShaderFile > shaders / ColorShader_NoLight.xml </ ShaderFile >
            //        < ShaderFile > shaders / ColorShader_WithLight.xml </ ShaderFile >
            //    </ Shaders >
            _shadersByName = new Dictionary<string, DXShader>();
            XmlNode shaderFileNode = shaderResourcesNode.FirstChild;
            while(shaderFileNode != null)
            {
                string path = shaderFileNode.InnerText;
                XmlDocument shaderDoc = new XmlDocument();
                shaderDoc.Load(path);
                LoadShader(shaderDoc);

                shaderFileNode = shaderFileNode.NextSibling;
            }
        }

        public void LoadShader(XmlDocument shaderDoc)
        {
            DXShader shader = new DXShader();
            shader.Load(_dxDevice, shaderDoc);
            AddShader(shader);
        }

        public void RemoveShader(DXShader shader)
        {
            var old = _shadersByName[shader.Name];
            if(old != null)
            {
                old.Dispose();
                _shadersByName.Remove(shader.Name);
            }
        }

        public void RemoveShader(string shaderName)
        {
            var old = _shadersByName[shaderName];
            if(old != null)
            {
                old.Dispose();
                _shadersByName.Remove(shaderName);
            }
        }

        public void RemoveAll()
        {
            foreach(var shader in _shadersByName)
            {
                shader.Value.Dispose();
            }
            _shadersByName.Clear();
        }

        #region IDisposable Support
        private bool _disposed = false; // To detect redundant calls

        ~DXShadersManager()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(!_disposed)
            {
                if(disposing)
                {

                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                RemoveAll();

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion


    }
}
