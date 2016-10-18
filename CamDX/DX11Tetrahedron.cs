using System;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace CamDX
{
    public class DX11Tetrahedron<VT> : DXModel<VT> where VT : struct, IVertex 
    {
        public DX11Tetrahedron(Device device)
        {
            _vertexBuf = new Buffer(device, new BufferDescription()
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                StructureByteStride = 0,
                Usage = ResourceUsage.Dynamic,
                SizeInBytes = 7 * 4 * 4
            });

            _indicesBuf = Buffer.Create(device, new ushort[]
            {
                
            }, new BufferDescription()
            {
                BindFlags = BindFlags.IndexBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                StructureByteStride = 0,
                Usage = ResourceUsage.Default,
                SizeInBytes = 12*sizeof(ushort)
            });

            //_primitiveType = PrimitiveTopology.TriangleList;
            //_vertexStride = VT.SizeInBytes;
            //_isIndexed = true;
            //_indicesCount = 12;
            //_vertexCount = 4;

            SetSize(new Vector3(0.0f, 0.0f, 0.0f), 1);
        }

        public void SetSize(Vector3 center, float edge)
        {
            //edge = edge / 2;
            //_vertices[0] = new VT(
            //    new Vector3(0.0f + center.X,
            //    -1.0f * edge + center.Y,
            //    0.707f * edge + center.Z),
            //    Color4.White);
            //_vertices[1] = new Vertex_P4C4(
            //    new Vector3(1.0f * edge + center.X,
            //    0.0f * edge + center.Y,
            //    -0.707f * edge + center.Z),
            //    Color4.White);
            //_vertices[2] = new Vertex_P4C4(
            //    new Vector3(-1.0f * edge + center.X,
            //    0.0f * edge + center.Y,
            //    -0.707f * edge + center.Z),
            //    Color4.White);
            //_vertices[3] = new DXSColorVertex(
            //    new Vector3(0.0f * edge + center.X,
            //    1.0f * edge + center.Y,
            //    0.707f * edge + center.Z),
            //    Color4.White);

            UpdateBuffers();
        }

        public override void UpdateBuffers()
        {
            DeviceContext device = _vertexBuf.Device.ImmediateContext;
            DataStream stream;
            var dataBox = device.MapSubresource(_vertexBuf, 0, MapMode.WriteDiscard, MapFlags.None);
            stream = new DataStream(dataBox.DataPointer, _vertexBuf.Description.SizeInBytes, true, true);
            stream.WriteRange(_vertices);
            device.UnmapSubresource(_vertexBuf, 0); //to update the data on GPU

            stream.Dispose();
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
}
