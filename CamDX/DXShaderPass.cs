using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;
using SharpDX.DXGI;
using System.Runtime.InteropServices;
using System.Xml;

namespace CamDX
{
    public class DXShaderPass : IDisposable
    {
        protected DXShader _parentShader;

        int _vertexShaderIndex = -1;
        int _pixelShaderIndex = -1;
        int _layoutIndex = -1;
        public int VertexShaderIndex { get { return _vertexShaderIndex; } }
        public int PixelShaderIndex { get { return _pixelShaderIndex; } }
        public int InputLayoutIndex { get { return _layoutIndex; } }

        // For texture-shaders (actual textures are stored in DXShader)
        int[] _textureIndices;
        public int[] TextureIndices { get { return _textureIndices; } }
        // For light-shaders (actual illumination info are stored in DXShader)
        int _illuminationIndex = -1;
        public int IlluminationIndex { get { return _illuminationIndex; } }
        
        public DXShaderPass(DXShader parent)
        {
            _parentShader = parent;
        }
        
        public void Load(SharpDX.Direct3D11.Device dxDevice, XmlNode passNode)
        {
            // <Pass>
            //      <VertexShaderIndex value="0"/>
            //      <PixelShaderIndex value="0"/>
            //      <InputLayoutIndex value="0"/>
            //      <IlluminationIndex value="0"/>
            //      <TextureIndices>
            //          <Index value="0"/>
            //      </TextureIndices>
            //      <Options/Flags/>
            // </Pass>

            var node = passNode.SelectSingleNode("VertexShaderIndex[@value]");
            if(node != null)
                _vertexShaderIndex = int.Parse(node.Attributes["value"].Value);

            node = passNode.SelectSingleNode("PixelShaderIndex[@value]");
            if(node != null)
                _pixelShaderIndex = int.Parse(node.Attributes["value"].Value);

            node = passNode.SelectSingleNode("InputLayoutIndex[@value]");
            if(node != null)
                _layoutIndex = int.Parse(node.Attributes["value"].Value);

            node = passNode.SelectSingleNode("IlluminationIndex[@value]");
            if(node != null)
                _illuminationIndex = int.Parse(node.Attributes["value"].Value);
            
            node = passNode.SelectSingleNode("TextureIndices");
            if(node != null && node.ChildNodes.Count > 0)
            {
                _textureIndices = new int[node.ChildNodes.Count];
                var texNode = node.FirstChild;
                int i = 0;
                while(texNode != null)
                {
                    _textureIndices[i] = int.Parse(texNode.Attributes["value"].Value);
                    texNode = texNode.NextSibling;
                }
            }
        }

        ~DXShaderPass () { Dispose(false); }
        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            _parentShader = null;
        }
    }
}
