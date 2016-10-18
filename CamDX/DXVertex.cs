using SharpDX;
using System.Runtime.InteropServices;
using System;

namespace CamDX
{
    public interface IVertex
    {
        LayoutElementType Layout { get; }

        Vector3 Position { get; set; }
        Vector3 Normal { get; set; }
        Color4 Color { get; set; }
        Vector2 TexCoords { get; set; }

        int SizeInBytes { get; }
    }

    public static class InvalidTypes
    {
        public static Vector2 Vector2 { get { return new Vector2(float.PositiveInfinity); } }
        public static Vector3 Vector3 { get { return new Vector3(float.PositiveInfinity); } }
        public static Vector4 Vector4 { get { return new Vector4(float.PositiveInfinity); } }
        public static Color4 Color4 { get { return new Color4(float.PositiveInfinity); } }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Vertex_P4 : IVertex
    {
        public Vector4 _position;
        public Vertex_P4(Vector4 pos)
        {
            _position = pos;
        }

        public Vertex_P4(Vector3 pos)
        {
            _position = new Vector4(pos, 1.0f);
        }

        public LayoutElementType Layout
        {
            get
            {
                return LayoutElementType.Position4;
            }
        }

        public Vector3 Position
        {
            get
            {
                return new Vector3(_position.X, _position.Y, _position.Z);
            }

            set
            {
                _position = new Vector4(value, 1.0f);
            }
        }

        public Vector3 Normal { get { return InvalidTypes.Vector3; } set { } }
        public Color4 Color { get { return InvalidTypes.Color4; } set { } }
        public Vector2 TexCoords { get { return InvalidTypes.Vector2; } set { } }

        public int SizeInBytes { get { return 16; } }
        public static int Size { get { return 16; } }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Vertex_P4C4 : IVertex
    {
        public Vector4 _position;
        public Color4 _color;

        public Vertex_P4C4(Vector4 pos, Color4 col = new Color4())
        {
            _position = pos;
            _color = col;
        }

        public Vertex_P4C4(Vector3 pos, Color4 col = new Color4())
        {
            _position = new Vector4(pos, 1.0f);
            _color = col;
        }

        public LayoutElementType Layout
        {
            get
            {
                return LayoutElementType.Position4 | LayoutElementType.Color4;
            }
        }

        public Vector3 Position
        {
            get
            {
                return new Vector3(_position.X, _position.Y, _position.Z);
            }

            set
            {
                _position = new Vector4(value, 1.0f);
            }
        }

        public Vector3 Normal { get { return InvalidTypes.Vector3; } set { } }
        public Color4 Color
        {
            get
            {
                return _color;
            }

            set
            {
                _color = value;
            }
        }

        public Vector2 TexCoords { get { return InvalidTypes.Vector2; } set { } }

        public int SizeInBytes { get { return 32; } }
        public static int Size { get { return 32; } }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Vertex_P4N3C4 : IVertex
    {
        public Vector4 _position;
        public Vector3 _normal;
        public Color4 _color;

        public Vertex_P4N3C4(Vector4 pos, Vector3 normal, Color4 col = new Color4())
        {
            _position = pos;
            _normal = normal;
            _color = col;
        }

        public Vertex_P4N3C4(Vector3 pos, Vector3 normal, Color4 col = new Color4())
        {
            _position = new Vector4(pos, 1.0f);
            _normal = normal;
            _color = col;
        }

        public LayoutElementType Layout
        {
            get
            {
                return LayoutElementType.Position4 | 
                    LayoutElementType.Normal3 | 
                    LayoutElementType.Color4;
            }
        }

        public Vector3 Position
        {
            get
            {
                return new Vector3(_position.X, _position.Y, _position.Z);
            }

            set
            {
                _position = new Vector4(value, 1.0f);
            }
        }

        public Vector3 Normal
        {
            get { return _normal; }
            set { _normal = value; }
        }

        public Color4 Color
        {
            get { return _color; }
            set { _color = value; }
        }

        public Vector2 TexCoords { get { return InvalidTypes.Vector2; } set { } }

        public int SizeInBytes { get { return 44; } }
        public static int Size { get { return 44; } }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Vertex_P4N3T2 : IVertex
    {
        public Vector4 _position;
        public Vector3 _normal;
        public Vector2 _uv;

        public Vertex_P4N3T2(Vector4 pos, Vector3 normal, Vector2 uv)
        {
            _position = pos;
            _normal = normal;
            _uv = uv;
        }

        public Vertex_P4N3T2(Vector3 pos, Vector3 normal, Vector2 uv)
        {
            _position = new Vector4(pos, 1.0f);
            _normal = normal;
            _uv = uv;
        }

        public LayoutElementType Layout
        {
            get
            {
                return LayoutElementType.Position4 |
                    LayoutElementType.Normal3 |
                    LayoutElementType.TexCoords2;
            }
        }

        public Vector3 Position
        {
            get
            {
                return new Vector3(_position.X, _position.Y, _position.Z);
            }

            set
            {
                _position = new Vector4(value, 1.0f);
            }
        }

        public Vector3 Normal
        {
            get { return _normal; }
            set { _normal = value; }
        }

        public Color4 Color { get { return InvalidTypes.Color4; } set { } }

        public Vector2 TexCoords
        {
            get { return _uv; }
            set { _uv = value; }
        }

        public int SizeInBytes { get { return 36; } }
        public static int Size { get { return 36; } }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Vertex_P4N3C4T2 : IVertex
    {
        public Vector4 _position;
        public Vector3 _normal;
        public Color4 _color;
        public Vector2 _uv;

        public Vertex_P4N3C4T2(Vector4 pos, Vector3 normal, Vector2 uv, Color4 col = new Color4())
        {
            _position = pos;
            _normal = normal;
            _color = col;
            _uv = uv;
        }

        public Vertex_P4N3C4T2(Vector3 pos, Vector3 normal, Vector2 uv, Color4 col = new Color4())
        {
            _position = new Vector4(pos, 1.0f);
            _normal = normal;
            _color = col;
            _uv = uv;
        }

        public LayoutElementType Layout
        {
            get
            {
                return LayoutElementType.Position4 |
                    LayoutElementType.Normal3 |
                    LayoutElementType.Color4 |
                    LayoutElementType.TexCoords2;
            }
        }

        public Vector3 Position
        {
            get
            {
                return new Vector3(_position.X, _position.Y, _position.Z);
            }

            set
            {
                _position = new Vector4(value, 1.0f);
            }
        }

        public Vector3 Normal
        {
            get { return _normal; }
            set { _normal = value; }
        }

        public Color4 Color
        {
            get { return _color; }
            set { _color = value; }
        }

        public Vector2 TexCoords
        {
            get { return _uv; }
            set { _uv = value; }
        }

        public int SizeInBytes { get { return 52; } }
        public static int Size { get { return 52; } }
    }
}
