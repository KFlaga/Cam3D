using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using Buffer = SharpDX.Direct3D11.Buffer;


namespace CamDX
{
    //public class DX11PointCollection : DXModel
    //{
    //    public List<DXSColorVertex> Vertices { get; set; }
    //    private Device _dxdevice;

    //    public DX11PointCollection(Device device)
    //    {
    //        Vertices = new List<DXSColorVertex>();

    //        _dxdevice = device;

    //        _vertexBuf = new Buffer(device, new BufferDescription()
    //        {
    //            BindFlags = BindFlags.VertexBuffer,
    //            CpuAccessFlags = CpuAccessFlags.Write,
    //            OptionFlags = ResourceOptionFlags.None,
    //            StructureByteStride = 0,
    //            Usage = ResourceUsage.Dynamic,
    //            SizeInBytes = DXSColorVertex.SizeInBytes * 1000
    //        });

    //        _primitiveType = PrimitiveTopology.TriangleList;
    //        _vertexStride = DXSColorVertex.SizeInBytes;
    //        _isIndexed = false;
    //        _vertexCount = 0;
    //    }

    //    public override void UpdateBuffers()
    //    {
    //        _vertexCount = Vertices.Count;
    //        DeviceContext device = _dxdevice.ImmediateContext;
    //        DataStream stream;
    //        var dataBox = device.MapSubresource(_vertexBuf, 0, MapMode.WriteDiscard, MapFlags.None);
    //        stream = new DataStream(dataBox.DataPointer, _vertexBuf.Description.SizeInBytes, true, true);
    //        stream.WriteRange(Vertices.ToArray());
    //        device.UnmapSubresource(_vertexBuf, 0); //to update the data on GPU

    //        stream.Dispose();
    //    }
    //}
}
