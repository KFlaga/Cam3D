using System;
using Buffer = SharpDX.Direct3D11.Buffer;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System.Collections.Generic;
using SharpDX;

namespace CamDX
{
    public interface IModel : IRenderable, IDisposable
    {
        DXSceneNode SceneNode { set; get; }
        List<IModel> SubModels { get; }
        DXShader Shader { get; }
        AABB ModelAABB { get; }

        // Should be called if any vertex changed
        void UpdateBuffers();
        void UpdateAABB();
    }

    public abstract class DXModel : IModel
    {
        protected Buffer _vertexBuf;
        protected int _vertexStride;
        protected int _vertexCount;
        protected Buffer _indicesBuf;
        protected bool _isIndexed;
        protected int _indicesCount;
        protected PrimitiveTopology _primitiveType;

        protected DXShader _shader;
        protected AABB _bounds;
        
        public List<IModel> SubModels { get { return null; } }
        public DXShader Shader { get { return _shader; } set { _shader = value; } }
        public AABB ModelAABB { get { return _bounds; } }
        public DXSceneNode SceneNode { set; get; }

        public virtual void Render(DeviceContext device)
        {
            if(_vertexCount <= 0)
                return;

            device.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vertexBuf, _vertexStride, 0));
            device.InputAssembler.PrimitiveTopology = _primitiveType;
            if(_isIndexed)
            {
                device.InputAssembler.SetIndexBuffer(_indicesBuf, Format.R16_UInt, 0);
                device.DrawIndexed(_indicesCount, 0, 0);
            }
            else
            {
                device.Draw(_vertexCount, 0);
            }
        }

        public abstract void UpdateBuffers();
        public abstract void UpdateAABB();

        bool _disposed = false;
        ~DXModel()
        {
            if(!_disposed)
                Dispose(false);
        }

        public void Dispose()
        {
            if(!_disposed)
            {
                GC.SuppressFinalize(this);
                Dispose(true);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            _disposed = true;
            this.SetField(ref _vertexBuf, null);
            this.SetField(ref _indicesBuf, null);
        }
    }

    public class DXModel<VT> : DXModel where VT : struct, IVertex
    {
        protected VT[] _vertices;
        public VT[] VertexList { get { return _vertices; } }

        protected ushort[] _indices;
        public ushort[] IndexList { get { return _indices; } }

        protected bool _isVertexBufMutable;
        protected bool _isIndexBufMutable;

        public override void UpdateBuffers()
        {
            DeviceContext device = _vertexBuf.Device.ImmediateContext;
            DataStream stream;

            if(_isVertexBufMutable)
            {
                var dataBox = device.MapSubresource(_vertexBuf, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None);
                stream = new DataStream(dataBox.DataPointer, _vertexBuf.Description.SizeInBytes, true, true);
                stream.WriteRange(_vertices);
                device.UnmapSubresource(_vertexBuf, 0);
                stream.Dispose();
            }

            if(_isIndexed && _isIndexBufMutable)
            {
                var dataBox = device.MapSubresource(_indicesBuf, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None);
                stream = new DataStream(dataBox.DataPointer, _indicesBuf.Description.SizeInBytes, true, true);
                stream.WriteRange(_indices);
                device.UnmapSubresource(_indicesBuf, 0);
                stream.Dispose();
            }
        }

        public override void UpdateAABB()
        {
            AABB aabb = new AABB(_vertices[0].Position, _vertices[0].Position);
            foreach(var vertex in _vertices)
            {
                aabb.EnclosePoint(vertex.Position);
            }
        }
    }

    public class DXCompoundModel : IModel
    {
        protected List<IModel> _subModels = new List<IModel>();
        public List<IModel> SubModels { get { return _subModels; } }
        public DXShader Shader { get { return null; } }
        public DXSceneNode SceneNode { set; get; }

        protected AABB _bounds;
        public AABB ModelAABB { get { return _bounds; } }

        public void AddModel(IModel model)
        {
            _subModels.Add(model);
            _bounds.Union(model.ModelAABB);
        }

        public void RemoveModel(IModel model)
        {
            _subModels.Remove(model);
            UpdateAABB();
        }

        public void Render(DeviceContext device)
        {
            foreach(var model in _subModels)
            {
                model.Render(device);
            }
        }

        public void UpdateBuffers()
        {
            foreach(var model in _subModels)
            {
                model.UpdateBuffers();
            }
        }

        public void UpdateAABB()
        {
            _bounds = new AABB();
            foreach(var model in _subModels)
            {
                model.UpdateAABB();
                _bounds.Union(model.ModelAABB);
            }
        }

        bool _disposed = false;
        ~DXCompoundModel()
        {
            if(!_disposed)
                Dispose(false);
        }

        public void Dispose()
        {
            if(!_disposed)
            {
                GC.SuppressFinalize(this);
                Dispose(true);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            _disposed = true;
            foreach(var model in _subModels)
            {
                model.Dispose();
            }
        }
    }
}
