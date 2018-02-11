using SharpDX;
using System;

namespace CamDX
{
    public static class DXMisc
    {
        public static void SetField<T>(this object obj, ref T field, T newValue) where T : IDisposable
        {
            if (field != null)
                field.Dispose();
            field = newValue;
        }

        public static Vector3 CrossProduct(Vector3 a, Vector3 b)
        {
            Vector3 c = new Vector3();
            c.X = a.Y*b.Z - a.Z*b.Y;
            c.Y = a.Z*b.X - a.X*b.Z;
            c.Z = a.X*b.Y - a.Y*b.X;
            return c;
        }

        public static Vector3 DotProduct(Vector3 a, Vector3 b)
        {
            Vector3 c = new Vector3();
            c.X = a.X * b.X;
            c.Y = a.Y * b.Y;
            c.Z = a.Z * b.Z;
            return c;
        }
    }
}
