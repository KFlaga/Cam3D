using SharpDX;
using System.Runtime.InteropServices;

namespace CamDX
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct DXSVertex
    {
        public DXSVertex(Vector3 pos)
        {
            Position = pos;
        }
        public Vector3 Position;

        public static int SizeInBytes { get { return 3 * 4; } }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct DXSColorVertex
    {
        public DXSColorVertex(Vector3 pos, Color4 color)
        {
            Position = pos;
            Color = color;
        }
        public Vector3 Position;
        public Color4 Color;

        public static int SizeInBytes { get { return 7 * 4; } }
    }
}
