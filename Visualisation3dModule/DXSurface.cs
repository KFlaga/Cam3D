using CamDX;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Visualisation3dModule
{
    public class DXGridSurface : DXModelUIntIndexed<Vertex_P4N3C4T2>
    {
        Device _dxDevice;
        uint[,] _vertexGridIndices;
        List<Vertex_P4N3C4T2> _verticesList;
        int _rows, _cols;

        public DXGridSurface(Device device, int rows, int cols)
        {
            PrimitiveType = PrimitiveTopology.TriangleList;
            VertexStride = Vertex_P4N3C4T2.Size;
            _rows = rows;
            _cols = cols;
            _dxDevice = device;

            _vertexGridIndices = new uint[rows, cols];
            for(int r = 0; r < rows; ++r)
                for(int c = 0; c < cols; ++c)
                    _vertexGridIndices[r, c] = uint.MaxValue;
            _verticesList = new List<Vertex_P4N3C4T2>();

            IndicesCount = 0;
            _isVertexBufMutable = false;
            _isIndexBufMutable = false;
            IsIndexed = true;
        }
        
        public void AddVertex(int row, int col, Vector3 position, Vector2 texCoord = new Vector2(), Color4 color = new Color4())
        {
            Vertex_P4N3C4T2 vertex = new Vertex_P4N3C4T2()
            {
                Position = position,
                TexCoords = texCoord,
                Color = color
            };
            uint vidx = (uint)_verticesList.Count;
            _verticesList.Add(vertex);
            _vertexGridIndices[row, col] = vidx;
        }
        public override void UpdateBuffers()
        {
            if(_verticesList.Count < 3)
            {
                VertexCount = 0;
                return;
            }

            // Create indices list
            List<uint> indices = new List<uint>();
            // For each vertex:
            // if column even : create triangle with upper/right
            // else : create trinagle with lower/left
            for(int r = 1; r < _rows - 1; ++r)
            {
                for(int c = 0; c < _cols - 1; ++c)
                {
                    if( !(_vertexGridIndices[r, c] == uint.MaxValue ||
                        _vertexGridIndices[r, c + 1] == uint.MaxValue ||
                        _vertexGridIndices[r - 1, c] == uint.MaxValue))
                    {
                        indices.Add(_vertexGridIndices[r, c]);
                        indices.Add(_vertexGridIndices[r - 1, c]);
                        indices.Add(_vertexGridIndices[r, c + 1]);
                    }
                }

                for(int c = 1; c < _cols; ++c)
                {
                    if(!(_vertexGridIndices[r, c] == uint.MaxValue ||
                        _vertexGridIndices[r, c - 1] == uint.MaxValue ||
                        _vertexGridIndices[r + 1, c] == uint.MaxValue))
                    {
                        indices.Add(_vertexGridIndices[r, c]);
                        indices.Add(_vertexGridIndices[r + 1, c]);
                        indices.Add(_vertexGridIndices[r, c - 1]);
                    }
                }
            }

            if(_rows > 1)
            {
                // For first row add odd column triangles
                for(int c = 1; c < _cols; ++c)
                {
                    if(!(_vertexGridIndices[0, c] == uint.MaxValue ||
                        _vertexGridIndices[0, c - 1] == uint.MaxValue ||
                        _vertexGridIndices[1, c] == uint.MaxValue))
                    {
                        indices.Add(_vertexGridIndices[0, c]);
                        indices.Add(_vertexGridIndices[1, c]);
                        indices.Add(_vertexGridIndices[0, c - 1]);
                    }
                }
            
                // For last row add even column triangles
                for(int c = 0; c < _cols - 1; ++c)
                {
                    if(!(_vertexGridIndices[_rows - 1, c] == uint.MaxValue ||
                        _vertexGridIndices[_rows - 1, c + 1] == uint.MaxValue ||
                        _vertexGridIndices[_rows - 2, c] == uint.MaxValue))
                    {
                        indices.Add(_vertexGridIndices[_rows - 1, c]);
                        indices.Add(_vertexGridIndices[_rows - 2, c]);
                        indices.Add(_vertexGridIndices[_rows - 1, c + 1]);
                    }
                }
            }

            _vertices = _verticesList.ToArray();
            VertexCount = _verticesList.Count;
            _indices = indices.ToArray();
            IndicesCount = indices.Count;

            if(IndicesCount >= 3)
            {
                // Create new vertex/index buffers with data
                _indicesBuf = Buffer.Create(_dxDevice, _indices, new BufferDescription()
                {
                    BindFlags = BindFlags.IndexBuffer,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.None,
                    StructureByteStride = 0,
                    Usage = ResourceUsage.Immutable,
                    SizeInBytes = IndicesCount * sizeof(uint)
                });

                _vertexBuf = Buffer.Create(_dxDevice, _vertices, new BufferDescription()
                {
                    BindFlags = BindFlags.VertexBuffer,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.None,
                    StructureByteStride = 0,
                    Usage = ResourceUsage.Immutable,
                    SizeInBytes = VertexCount * Vertex_P4N3C4T2.Size
                });
            }
            else
                VertexCount = 0;
        }
    }
}
