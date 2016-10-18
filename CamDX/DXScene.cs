using SharpDX;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;
using System;
using System.Collections.Generic;

namespace CamDX
{
    public class DXScene : IDisposable
    {
        DXSceneNode _rootNode;
        DXSceneNode RootNode { get; set; }

        Matrix WorldMatrix { get; set; }
        Buffer _transformBuffer;

        // Lights
        GlobalLightsData _globalLights;
        GlobalLightsData GlobalLights { get; set; }
        Buffer _globalLightsBuffer;
        
        public List<RenderGroup> RenderGroups { get; set; }
        public DXCamera CurrentCamera { get; set; }

        public DXScene(SharpDX.Direct3D11.Device device)
        {
            RenderGroups = new List<RenderGroup>();

            _transformBuffer = new Buffer(device, new BufferDescription()
            {
                Usage = ResourceUsage.Dynamic,
                SizeInBytes = System.Runtime.InteropServices.Marshal.SizeOf<Matrix>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                StructureByteStride = 0
            });

            _globalLightsBuffer = new Buffer(device, new BufferDescription()
            {
                Usage = ResourceUsage.Dynamic,
                SizeInBytes = System.Runtime.InteropServices.Marshal.SizeOf<GlobalLightsData>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                StructureByteStride = 0
            });

            WorldMatrix = Matrix.Identity;
            _rootNode = new DXSceneNode()
            {
                Position = new Vector3(),
                Orientation = Quaternion.Identity,
                Scale = new Vector3(1.0f)
            };
        }

        public void UpdateScene()
        {
            // Prepares all models for render :
            // for each node:
            // - check if any model should be / not be rendered (rather check scene node AABB with camera frustum)
            // - check if any model have no render group assigned
            // - check if any model should have group changed
            // - update buffers for model
            // repeat for children
        }

        public void Render(DeviceContext device)
        {
            CurrentCamera.UpdateViewMatrix();
            CurrentCamera.UpdateProjectionMatrix();
            Matrix finalTransform = WorldMatrix * CurrentCamera.ViewMat * CurrentCamera.ProjMat;
            finalTransform.Transpose();

            DataStream stream;
            var dataBox = device.MapSubresource(_transformBuffer, 
                (int)ConstantBufferSlots.WorldViewProjMatrix, MapMode.WriteDiscard, MapFlags.None);
            stream = new DataStream(dataBox.DataPointer, _transformBuffer.Description.SizeInBytes, true, true);
            stream.Write(finalTransform);
            device.UnmapSubresource(_transformBuffer, 0); //to update the data on GPU
            stream.Dispose();
            
            dataBox = device.MapSubresource(_globalLightsBuffer, (int)ConstantBufferSlots.GlobalLights, 
                MapMode.WriteDiscard, MapFlags.None);
            stream = new DataStream(dataBox.DataPointer, _globalLightsBuffer.Description.SizeInBytes, true, true);
            stream.Write(_globalLights);
            device.UnmapSubresource(_globalLightsBuffer, 0); //to update the data on GPU
            stream.Dispose();

            device.PixelShader.SetConstantBuffer(0, _globalLightsBuffer);

            foreach (var rgroup in RenderGroups)
            {
                rgroup.Render(device);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected void Dispose(bool disposing)
        {
            this.SetField(ref _transformBuffer, null);
            this.SetField(ref _globalLightsBuffer, null);
        }
    }
}
