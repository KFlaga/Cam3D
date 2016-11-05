using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace CamDX
{
    public class DXResourceManager : IDisposable
    {
        public DXShadersManager ShaderManager { get; set; }
        Device _dxDevice;

        public DXResourceManager(Device dxDevice, XmlNode dxResourcesNode)
        {
            _dxDevice = dxDevice;

            ShaderManager = new DXShadersManager(dxDevice);

            LoadResources(dxResourcesNode);
        }

        void LoadResources(XmlNode dxResourcesNode)
        {
            //< DXResources >
            //    < Shaders >
            //        < ShaderFile > shaders / ColorShader_NoLight.xml </ ShaderFile >
            //        < ShaderFile > shaders / ColorShader_WithLight.xml </ ShaderFile >
            //    </ Shaders >
            //</ DXResources >

            XmlNode shadersNode = dxResourcesNode.SelectSingleNode("Shaders");
            ShaderManager.LoadShaders(shadersNode);
        }

        #region IDisposable Support
        private bool _disposed = false; // To detect redundant calls

        ~DXResourceManager()
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

                ShaderManager.Dispose();
                ShaderManager = null;

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
