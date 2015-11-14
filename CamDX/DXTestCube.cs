using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace CamDX
{
    public class DXTestCube : DX11Model
    {
        public DXSColorVertex[] Vertices { get; set; }

        public DXTestCube(Device device)
        {
            _vertexBuf = new Buffer(device, new BufferDescription()
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                StructureByteStride = 0,
                Usage = ResourceUsage.Dynamic,
                SizeInBytes = 7 * 4 * 8
            });

            _indicesBuf = Buffer.Create(device, new ushort[]
            {
                3,1,0,
                    2,1,3,
                    0,5,4,
                    1,5,0,
                    3,4,7,
                    0,4,3,
                    1,6,5,
                    2,6,1,
                    2,7,6,
                    3,7,2,
                    6,4,5,
                    7,4,6,
            }, new BufferDescription()
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                StructureByteStride = 0,
                Usage = ResourceUsage.Default,
                SizeInBytes = 36*sizeof(ushort)
            });

            ResetVertices();

            _primitiveType = PrimitiveTopology.TriangleList;
            _vertexStride = DXSColorVertex.SizeInBytes;
            _isIndexed = true;
            _indicesCount = 36;
            _vertexCount = 8;
        }

        public void ResetVertices( )
        {
            Vertices = new DXSColorVertex[]
            {
                    new DXSColorVertex(new Vector3(-1.0f,  1.0f, -1.0f), new Color4(1.0f, 0.0f, 0.0f, 1.0f)),
                    new DXSColorVertex(new Vector3( 1.0f,  1.0f, -1.0f), new Color4(1.0f, 0.0f, 1.0f, 0.0f)),
                    new DXSColorVertex(new Vector3( 1.0f,  1.0f,  1.0f), new Color4(1.0f, 0.0f, 1.0f, 1.0f)),
                    new DXSColorVertex(new Vector3(-1.0f,  1.0f,  1.0f), new Color4(1.0f, 1.0f, 0.0f, 0.0f)),
                    new DXSColorVertex(new Vector3(-1.0f, -1.0f, -1.0f), new Color4(1.0f, 1.0f, 0.0f, 1.0f)),
                    new DXSColorVertex(new Vector3( 1.0f, -1.0f, -1.0f), new Color4(1.0f, 1.0f, 1.0f, 0.0f)),
                    new DXSColorVertex(new Vector3( 1.0f, -1.0f,  1.0f), new Color4(1.0f, 1.0f, 1.0f, 1.0f)),
                    new DXSColorVertex(new Vector3(-1.0f, -1.0f,  1.0f), new Color4(1.0f, 0.0f, 0.0f, 0.0f)),
            };

            UpdateBuffers();
        }

        public override void UpdateBuffers()
        {
            DeviceContext device = _vertexBuf.Device.ImmediateContext;
            DataStream stream;
            var dataBox = device.MapSubresource(_vertexBuf, 0, MapMode.WriteDiscard, MapFlags.None);
            stream = new DataStream(dataBox.DataPointer, _vertexBuf.Description.SizeInBytes, true, true);
            stream.WriteRange(Vertices);
            device.UnmapSubresource(_vertexBuf, 0); //to update the data on GPU

            stream.Dispose();
        }
    }
}
