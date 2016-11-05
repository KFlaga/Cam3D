using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;
using SharpDX.DXGI;
using System.Xml;
using System.Runtime.InteropServices;
using System.IO;

namespace CamDX
{
    public enum ConstantBufferSlots
    {
        WorldViewProjMatrix = 0,
        GlobalLights = 1,
        MaterialIllumination = 2
    }

    public class DXShader : IDisposable
    {
        public string Name { get; set; }

        List<DXShaderPass> _passes = new List<DXShaderPass>();
        public List<DXShaderPass> Passes { get { return _passes; } }

        DXShaderPass _currentPass;
        List<VertexShader> _vertexShaders = new List<VertexShader>();
        List<PixelShader> _pixelShaders = new List<PixelShader>();
        List<InputLayout> _inputLayouts = new List<InputLayout>();
        List<ShaderResourceView> _textureResources = new List<ShaderResourceView>();
        List<SamplerState> _samplers = new List<SamplerState>();
        List<MaterialIlluminationData> _illumiantionDatas = new List<MaterialIlluminationData>();

        public DXShaderPass CurrentPass { get { return _currentPass; } }
        public List<VertexShader> VertexShaders { get { return _vertexShaders; } }
        public List<PixelShader> PixelShaders { get { return _pixelShaders; } }
        public List<InputLayout> InputLayouts { get { return _inputLayouts; } }
        public List<ShaderResourceView> TextureResources { get { return _textureResources; } }
        public List<SamplerState> Samplers { get { return _samplers; } }
        public List<MaterialIlluminationData> IllumiantionDatas { get { return _illumiantionDatas; } }

        public VertexShader CurrentVertexShader { get { return _vertexShaders[_currentPass.VertexShaderIndex]; } }
        public PixelShader CurrentPixelShader { get { return _pixelShaders[_currentPass.PixelShaderIndex]; } }
        public InputLayout CurrentInputLayout { get { return _inputLayouts[_currentPass.InputLayoutIndex]; } }
        public MaterialIlluminationData CurrentIlluminationData { get { return _illumiantionDatas[_currentPass.IlluminationIndex]; } }

        int _currentIlluminationIndex;
        SharpDX.Direct3D11.Buffer _illuminationBuffer;

        public bool Loaded { get; protected set; } = false;

        public DXShader()
        {

        }

        public void RenderFirstPass(DeviceContext deviceContext)
        {
            _currentPass = _passes[0];

            deviceContext.VertexShader.Set(CurrentVertexShader);
            deviceContext.PixelShader.Set(CurrentPixelShader);
            deviceContext.InputAssembler.InputLayout = CurrentInputLayout;

            if(_currentPass.TextureIndices != null)
            {
                for(int i = 0; i < _currentPass.TextureIndices.Length; ++i)
                {
                    deviceContext.PixelShader.SetSampler(i, _samplers[_currentPass.SamplersIndices[i]]);
                    deviceContext.PixelShader.SetShaderResource(i, _textureResources[_currentPass.TextureIndices[i]]);
                }
            }

            _currentIlluminationIndex = _currentPass.IlluminationIndex;
            if(_currentIlluminationIndex > 0)
            {
                // Update illumination buffer
                DataStream stream;
                var dataBox = deviceContext.MapSubresource(_illuminationBuffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None);
                stream = new DataStream(dataBox.DataPointer, _illuminationBuffer.Description.SizeInBytes, true, true);
                stream.Write(MaterialIlluminationData.Default);
                deviceContext.UnmapSubresource(_illuminationBuffer, 0); //to update the data on GPU
                stream.Dispose();
            }
        }

        public void RenderPass(DeviceContext deviceContext, int pass)
        {
            _currentPass = _passes[pass];

            if(deviceContext.VertexShader.Get() != CurrentVertexShader)
                deviceContext.VertexShader.Set(CurrentVertexShader);

            if(deviceContext.PixelShader.Get() != CurrentPixelShader)
                deviceContext.PixelShader.Set(CurrentPixelShader);

            if(deviceContext.InputAssembler.InputLayout != CurrentInputLayout)
                deviceContext.InputAssembler.InputLayout = CurrentInputLayout;

            if(_currentPass.TextureIndices != null)
            {
                for(int i = 0; i < _currentPass.TextureIndices.Length; ++i)
                {
                    deviceContext.PixelShader.SetSampler(i, _samplers[_currentPass.SamplersIndices[i]]);
                    deviceContext.PixelShader.SetShaderResource(i, _textureResources[_currentPass.TextureIndices[i]]);
                }
            }

            if((_currentIlluminationIndex != _currentPass.IlluminationIndex) &&
                _currentPass.IlluminationIndex > 0)
            {
                _currentIlluminationIndex = _currentPass.IlluminationIndex;

                // Update illumination buffer
                DataStream stream;
                var dataBox = deviceContext.MapSubresource(_illuminationBuffer,
                    (int)ConstantBufferSlots.MaterialIllumination, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None);
                stream = new DataStream(dataBox.DataPointer, _illuminationBuffer.Description.SizeInBytes, true, true);
                stream.Write(MaterialIlluminationData.Default);
                deviceContext.UnmapSubresource(_illuminationBuffer, 0); //to update the data on GPU
                stream.Dispose();
            }
        }

        public void Load(SharpDX.Direct3D11.Device dxDevice, XmlDocument xmlDoc)
        {
            //<Shader name="ColorShader_WithLight">
            var shaderList = xmlDoc.GetElementsByTagName("Shader");
            if(shaderList.Count > 0)
            {
                var shaderNode = shaderList[0];
                Name = shaderNode.Attributes["name"].Value;
                //	<Passes>
                //		<Pass>
                //			<VertexShaderIndex value="0"/> ...
                var passesList = shaderNode.SelectNodes("Passes/Pass");
                foreach(XmlNode passNode in passesList)
                {
                    DXShaderPass pass = new DXShaderPass(this);
                    pass.Load(dxDevice, passNode);
                    _passes.Add(pass);
                }
                //	<VertexShaders>
                //		<VertexShader>
                //			<File>"ColorShader_Light.vs</File>
                //			<Entry>"Main"</Entry>
                //			<Profile>"vs_4_0"</Profile>
                //	        <InputLayout padding="0">
                //			    <ElementType type="Position4" slot="0"/ >...
                var vsList = shaderNode.SelectNodes("VertexShaders/VertexShader");
                foreach(XmlNode vsNode in vsList)
                {
                    string filePath = vsNode.SelectSingleNode("File").InnerText;
                    string entry = vsNode.SelectSingleNode("Entry").InnerText;
                    string profile = vsNode.SelectSingleNode("Profile").InnerText;

                    var compliationResult = ShaderBytecode.CompileFromFile(
                        filePath, entry, profile, ShaderFlags.None, EffectFlags.None);
                    if(compliationResult.HasErrors || compliationResult.Bytecode == null)
                        throw new Exception("Failed to compile vertex shader");

                    var vertexShader = new VertexShader(dxDevice, compliationResult.Bytecode.Data);
                    var vsSignature = ShaderSignature.GetInputSignature(compliationResult);

                    var layNode = vsNode.SelectSingleNode("InputLayout");
                    InputLayout layout = InputLayoutCreator.CreateInputLayoutFromXml(dxDevice, vsSignature, layNode);

                    _vertexShaders.Add(vertexShader);
                    _inputLayouts.Add(layout);
                }
                //	<PixelShaders>
                //		<PixelShader>
                //			<File>"ColorShader_Light.ps</File>
                //			<Entry>"Main"</Entry>
                //			<Profile>"ps_4_0"</Profile>
                var psList = shaderNode.SelectNodes("PixelShaders/PixelShader");
                foreach(XmlNode psNode in psList)
                {
                    string filePath = psNode.SelectSingleNode("File").InnerText;
                    string entry = psNode.SelectSingleNode("Entry").InnerText;
                    string profile = psNode.SelectSingleNode("Profile").InnerText;

                    var compliationResult = ShaderBytecode.CompileFromFile(
                        filePath, entry, profile, ShaderFlags.None, EffectFlags.None);
                    if(compliationResult.HasErrors)
                        throw new Exception("Failed to compile pixel shader");
                    var pixelShader = new PixelShader(dxDevice, compliationResult);
                    _pixelShaders.Add(pixelShader);
                }
                //	<IlluminationDatas>
                //		<IlluminationData>
                //			<Ambient a="1.0" r="1.0" g="1.0" b="1.0"/>
                //			<Diffuse a="1.0" r="1.0" g="1.0" b="1.0"/>
                //			<Emmisive a="0.0" r="1.0" g="1.0" b="1.0"/>
                var illList = shaderNode.SelectNodes("IlluminationDatas/IlluminationData");
                foreach(XmlNode illNode in illList)
                {
                    MaterialIlluminationData illData = MaterialIlluminationData.ParserFromXmlNode(illNode);
                    _illumiantionDatas.Add(illData);
                }

                if(_illumiantionDatas.Count > 0)
                {
                    _illuminationBuffer = new SharpDX.Direct3D11.Buffer(dxDevice, new BufferDescription()
                    {
                        Usage = ResourceUsage.Dynamic,
                        SizeInBytes = Marshal.SizeOf<MaterialIlluminationData>(),
                        BindFlags = BindFlags.ConstantBuffer,
                        CpuAccessFlags = CpuAccessFlags.Write,
                        OptionFlags = ResourceOptionFlags.None,
                        StructureByteStride = 0
                    });
                }
            }
        }

        public void LoadTexture(SharpDX.Direct3D11.Device dxDevice, XmlNode textureNode)
        {
            // Load texture from files

            // Create a texture sampler state description
            //_sampleState = new SamplerState(dxDevice, new SamplerStateDescription()
            //{
            //    AddressU = TextureAddressMode.Wrap,
            //    AddressW = TextureAddressMode.Wrap,
            //    AddressV = TextureAddressMode.Wrap,
            //    BorderColor = new SharpDX.Mathematics.Interop.RawColor4(0.0f, 0.0f, 0.0f, 0.0f),
            //    ComparisonFunction = Comparison.Always,
            //    Filter = Filter.ComparisonMinMagLinearMipPoint,
            //    MaximumAnisotropy = 1,
            //    MaximumLod = 1e12f,
            //    MinimumLod = 0.0f,
            //    MipLodBias = 0.0f
            //});
        }

        ~DXShader() { Dispose(false); }
        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            foreach(var vs in _vertexShaders)
            {
                vs.Dispose();
            }
            _vertexShaders.Clear();

            foreach(var ps in _pixelShaders)
            {
                ps.Dispose();
            }
            _pixelShaders.Clear();

            foreach(var lay in _inputLayouts)
            {
                lay.Dispose();
            }
            _inputLayouts.Clear();

            foreach(var res in _textureResources)
            {
                res.Dispose();
            }
            _textureResources.Clear();

            foreach(var sam in _samplers)
            {
                sam.Dispose();
            }
            _samplers.Clear();
            this.SetField(ref _illuminationBuffer, null);

            Loaded = false;
        }
    }
}
