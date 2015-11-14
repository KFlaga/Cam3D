using System;
using Buffer = SharpDX.Direct3D11.Buffer;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace CamDX
{
    public abstract class DX11Model : IDisposable
    {
        protected Buffer _vertexBuf;
        protected int _vertexStride;
        protected int _vertexCount;
        protected Buffer _indicesBuf;
        protected bool _isIndexed;
        protected int _indicesCount;
        protected PrimitiveTopology _primitiveType;
        
        public DX11Model()
        {

        }

        public virtual void Render(DeviceContext device)
        {
            if (_vertexCount <= 0)
                return;

            device.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vertexBuf, _vertexStride, 0));
            device.InputAssembler.PrimitiveTopology = _primitiveType;
            if (_isIndexed)
            {
                device.InputAssembler.SetIndexBuffer(_indicesBuf, Format.R16_UInt, 0);
                device.DrawIndexed(_indicesCount, 0, 0);
            }
            else
            {
                device.Draw(_vertexCount, 0);
            }
        }

        public virtual void UpdateBuffers()
        {

        }

        ~DX11Model() { Dispose(false); }
        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            this.SetField(ref _vertexBuf, null);
            this.SetField(ref _indicesBuf, null);
        }
    }
}
