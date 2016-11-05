using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

namespace CamDX
{
    public class DXCube : DXModel<Vertex_P4N3C4T2>
    {
        public enum VertexPosition : int
        {
            BotLeftBack = 0, BotLeftFront,
            BotRightBack, BotRightFront,
            TopLeftBack, TopLeftFront,
            TopRightBack, TopRightFront
        }

        public DXCube(Device device, Vector3 size = new Vector3(), Vector3 center = new Vector3())
        {
            _vertexBuf = new Buffer(device, new BufferDescription()
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                StructureByteStride = 0,
                Usage = ResourceUsage.Dynamic,
                SizeInBytes = Vertex_P4N3C4T2.Size * 8
            });

            _indicesBuf = Buffer.Create(device, new ushort[]
            {
                (ushort)VertexPosition.BotLeftBack, (ushort)VertexPosition.BotRightBack, (ushort)VertexPosition.BotRightFront , // Bottom 
                (ushort)VertexPosition.BotLeftBack, (ushort)VertexPosition.BotRightFront, (ushort)VertexPosition.BotLeftFront, 
                (ushort)VertexPosition.BotLeftFront, (ushort)VertexPosition.BotRightFront , (ushort)VertexPosition.TopRightFront, // Front
                (ushort)VertexPosition.BotLeftFront, (ushort)VertexPosition.TopRightFront, (ushort)VertexPosition.TopLeftFront,
                (ushort)VertexPosition.BotLeftBack, (ushort)VertexPosition.BotLeftFront , (ushort)VertexPosition.TopLeftFront, // Left
                (ushort)VertexPosition.BotLeftBack, (ushort)VertexPosition.TopLeftFront, (ushort)VertexPosition.TopLeftBack,
                (ushort)VertexPosition.TopLeftFront, (ushort)VertexPosition.TopRightFront , (ushort)VertexPosition.TopRightBack, // Top
                (ushort)VertexPosition.TopLeftFront, (ushort)VertexPosition.TopRightBack, (ushort)VertexPosition.TopLeftBack,
                (ushort)VertexPosition.BotRightFront, (ushort)VertexPosition.BotRightBack , (ushort)VertexPosition.TopRightBack, // Right
                (ushort)VertexPosition.BotRightFront, (ushort)VertexPosition.TopRightBack, (ushort)VertexPosition.TopRightFront,
                (ushort)VertexPosition.BotRightBack, (ushort)VertexPosition.BotLeftBack , (ushort)VertexPosition.TopRightBack, // Back
                (ushort)VertexPosition.BotLeftBack,(ushort)VertexPosition.TopLeftBack, (ushort)VertexPosition.TopRightBack,  
            }, new BufferDescription()
            {
                BindFlags = BindFlags.IndexBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                StructureByteStride = 0,
                Usage = ResourceUsage.Immutable,
                SizeInBytes = 36 * sizeof(ushort)
            });

            _primitiveType = PrimitiveTopology.TriangleList;
            _vertexStride = Vertex_P4N3C4T2.Size;
            _isIndexed = true;
            _indicesCount = 36;
            _vertexCount = 8;
            _vertices = new Vertex_P4N3C4T2[_vertexCount];

            _isVertexBufMutable = true;
            _isIndexBufMutable = false;

            SetSize(size, center);
            UpdateBuffers();
        }

        public void SetSize(Vector3 halfSize, Vector3 center)
        {
            _vertices[0] = new Vertex_P4N3C4T2() // BotLeftBack
            {
                Position = new Vector3(center.X - halfSize.X, center.Y - halfSize.Y, center.Z - halfSize.Z),
                Normal = new Vector3(),
                Color = new Color4(1.0f)
            };
            _vertices[1] = new Vertex_P4N3C4T2() // BotLeftFront
            {
                Position = new Vector3(center.X - halfSize.X, center.Y - halfSize.Y, center.Z + halfSize.Z),
                Normal = new Vector3(),
                Color = new Color4(1.0f)
            };
            _vertices[2] = new Vertex_P4N3C4T2() // BotRightBack
            {
                Position = new Vector3(center.X + halfSize.X, center.Y - halfSize.Y, center.Z - halfSize.Z),
                Normal = new Vector3(),
                Color = new Color4(1.0f)
            };
            _vertices[3] = new Vertex_P4N3C4T2() // BotRightFront
            {
                Position = new Vector3(center.X + halfSize.X, center.Y - halfSize.Y, center.Z + halfSize.Z),
                Normal = new Vector3(),
                Color = new Color4(1.0f)
            };
            _vertices[4] = new Vertex_P4N3C4T2() // TopLeftBack
            {
                Position = new Vector3(center.X - halfSize.X, center.Y + halfSize.Y, center.Z - halfSize.Z),
                Normal = new Vector3(),
                Color = new Color4(1.0f)
            };
            _vertices[5] = new Vertex_P4N3C4T2() // TopLeftFront
            {
                Position = new Vector3( center.X - halfSize.X, center.Y + halfSize.Y, center.Z + halfSize.Z),
                Normal = new Vector3(),
                Color = new Color4(1.0f)
            };
            _vertices[6] = new Vertex_P4N3C4T2() // TopRightBack
            {
                Position = new Vector3(center.X + halfSize.X, center.Y + halfSize.Y, center.Z - halfSize.Z),
                Normal = new Vector3(),
                Color = new Color4(1.0f)
            };
            _vertices[7] = new Vertex_P4N3C4T2() // TopRightFront
            {
                Position = new Vector3(center.X + halfSize.X, center.Y + halfSize.Y, center.Z + halfSize.Z),
                Normal = new Vector3(),
                Color = new Color4(1.0f)
            };
        }

        public void SetColor(VertexPosition vIdx, Color4 color)
        {
            var vertex = _vertices[(int)vIdx];
            vertex.Color = color;
            _vertices[(int)vIdx] = vertex;
        }

        public void SetTexCoords(VertexPosition vIdx, Vector2 uv)
        {
            var vertex = _vertices[(int)vIdx];
            vertex.TexCoords = uv;
            _vertices[(int)vIdx] = vertex;
        }

        public void SetColor(int vIdx, Color4 color)
        {
            var vertex = _vertices[vIdx];
            vertex.Color = color;
            _vertices[vIdx] = vertex;
        }

        public void SetTexCoords(int vIdx, Vector2 uv)
        {
            var vertex = _vertices[vIdx];
            vertex.TexCoords = uv;
            _vertices[vIdx] = vertex;
        }
    }
}
