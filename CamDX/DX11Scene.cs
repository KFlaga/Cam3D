using SharpDX;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;
using System;
using System.Collections.Generic;

namespace CamDX
{
    public class DX11Scene : IDisposable
    {
        Matrix WorldMatrix { get; set; }

        Buffer _projectionBuffer;

        public List<RenderGroup> RenderGroups { get; set; }
        public DXCamera CurrentCamera { get; set; }

        public DX11Scene(SharpDX.Direct3D11.Device device)
        {
            RenderGroups = new List<RenderGroup>(); 

            _projectionBuffer = new Buffer(device, new BufferDescription()
            {
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                StructureByteStride = 0,
                SizeInBytes = Utilities.SizeOf<Matrix>()
            });

            WorldMatrix = Matrix.Identity;
        }

        public void Render(DeviceContext device)
        {
            CurrentCamera.UpdateViewMatrix();
            CurrentCamera.UpdateProjectionMatrix();
            Matrix finalTransform = WorldMatrix * CurrentCamera.ViewMat * CurrentCamera.ProjMat;
            finalTransform.Transpose();

            DataStream stream;
            var dataBox = device.MapSubresource(_projectionBuffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None);
            stream = new DataStream(dataBox.DataPointer, _projectionBuffer.Description.SizeInBytes, true, true);
            stream.Write(finalTransform);
            device.UnmapSubresource(_projectionBuffer, 0); //to update the data on GPU
            stream.Dispose();

            device.VertexShader.SetConstantBuffer(0, _projectionBuffer);

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
            this.SetField(ref _projectionBuffer, null);
        }
    }
}
