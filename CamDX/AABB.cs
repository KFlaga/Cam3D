using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamDX
{
    public struct AABB // Axis aligned bounding box
    {
        public Vector3 TopLeftFront; // Maxmial corner
        public Vector3 BotRightBack; // Minimal corner

        public double SizeX { get { return TopLeftFront.X - BotRightBack.X; } }
        public double SizeY { get { return TopLeftFront.Y - BotRightBack.Y; } }
        public double SizeZ { get { return TopLeftFront.Z - BotRightBack.Z; } }

        public Vector3 Center
        {
            get
            {
                return new Vector3(0.5f * (TopLeftFront.X + BotRightBack.X),
                                0.5f * (TopLeftFront.Y + BotRightBack.Y),
                                0.5f * (TopLeftFront.Z + BotRightBack.Z));
            }
        }

        public Vector3 TopLeftBack { get { return new Vector3(TopLeftFront.X, TopLeftFront.Y, BotRightBack.Z); } }
        public Vector3 TopRightFront { get { return new Vector3(BotRightBack.X, TopLeftFront.Y, TopLeftFront.Z); } }
        public Vector3 TopRightBack { get { return new Vector3(BotRightBack.X, TopLeftFront.Y, BotRightBack.Z); } }
        public Vector3 BotLeftFront { get { return new Vector3(TopLeftFront.X, BotRightBack.Y, TopLeftFront.Z); } }
        public Vector3 BotLeftBack { get { return new Vector3(TopLeftFront.X, BotRightBack.Y, BotRightBack.Z); } }
        public Vector3 BotRightFront { get { return new Vector3(BotRightBack.X, BotRightBack.Y, TopLeftFront.Z); } }
        
        public AABB(Vector3 minCorner, Vector3 maxCorner)
        {
            TopLeftFront = maxCorner;
            BotRightBack = minCorner;
        }

        public void Move(Vector3 move)
        {
            TopLeftFront = TopLeftFront + move;
            BotRightBack = BotRightBack + move;
        }

        public void Scale(Vector3 scale)
        {
            TopLeftFront = TopLeftFront * scale;
            BotRightBack = BotRightBack * scale;
        }

        public void Union(AABB other)
        {
            TopLeftFront.X = Math.Max(TopLeftFront.X, other.TopLeftFront.X);
            TopLeftFront.Y = Math.Max(TopLeftFront.Y, other.TopLeftFront.Y);
            TopLeftFront.Z = Math.Max(TopLeftFront.Z, other.TopLeftFront.Z);
            BotRightBack.X = Math.Min(BotRightBack.X, other.BotRightBack.X);
            BotRightBack.Y = Math.Min(BotRightBack.Y, other.BotRightBack.Y);
            BotRightBack.Z = Math.Min(BotRightBack.Z, other.BotRightBack.Z);
        }

        public void EnclosePoint(Vector3 point)
        {
            TopLeftFront.X = Math.Max(TopLeftFront.X, point.X);
            TopLeftFront.Y = Math.Max(TopLeftFront.Y, point.Y);
            TopLeftFront.Z = Math.Max(TopLeftFront.Z, point.Z);
            BotRightBack.X = Math.Min(BotRightBack.X, point.X);
            BotRightBack.Y = Math.Min(BotRightBack.Y, point.Y);
            BotRightBack.Z = Math.Min(BotRightBack.Z, point.Z);
        }

        public void Transform(Matrix transformMatrix)
        {
            var maxr = Vector3.Transform(TopLeftFront, transformMatrix);
            var minr = Vector3.Transform(BotRightBack, transformMatrix);

            TopLeftFront = new Vector3(
                Math.Max(maxr.X, minr.X),
                Math.Max(maxr.Y, minr.Y),
                Math.Max(maxr.Z, minr.Z));

            BotRightBack = new Vector3(
                Math.Min(maxr.X, minr.X),
                Math.Min(maxr.Y, minr.Y),
                Math.Min(maxr.Z, minr.Z));
        }

        public static AABB Moved(AABB aabb, Vector3 move)
        {
            return new AABB(aabb.TopLeftFront + move,
                aabb.BotRightBack + move);
        }

        public static AABB Scaled(AABB aabb, Vector3 scale)
        {
            return new AABB(aabb.TopLeftFront * scale,
                aabb.BotRightBack * scale);
        }

        public static AABB Transformed(AABB aabb, Matrix trans)
        {
            var maxr = Vector3.Transform(aabb.TopLeftFront, trans);
            var minr = Vector3.Transform(aabb.BotRightBack, trans);

            return new AABB(new Vector3(
                Math.Max(maxr.X, minr.X),
                Math.Max(maxr.Y, minr.Y),
                Math.Max(maxr.Z, minr.Z)),
                    new Vector3(
                Math.Min(maxr.X, minr.X),
                Math.Min(maxr.Y, minr.Y),
                Math.Min(maxr.Z, minr.Z)));
        }

        public static AABB Unioned(AABB aabb, AABB other)
        {
            return new AABB(
                    new Vector3(
            Math.Max(aabb.TopLeftFront.X, other.TopLeftFront.X),
            Math.Max(aabb.TopLeftFront.Y, other.TopLeftFront.Y),
            Math.Max(aabb.TopLeftFront.Z, other.TopLeftFront.Z)),
                    new Vector3(
            Math.Min(aabb.BotRightBack.X, other.BotRightBack.X),
            Math.Min(aabb.BotRightBack.Y, other.BotRightBack.Y),
            Math.Min(aabb.BotRightBack.Z, other.BotRightBack.Z))
                );
        }

        public static AABB operator +(AABB aabb1, AABB aabb2)
        {
            return AABB.Unioned(aabb1, aabb2);
        }

        public static AABB operator +(AABB aabb1, Vector3 move)
        {
            return AABB.Moved(aabb1, move);
        }

        public static AABB operator +(Vector3 move, AABB aabb1)
        {
            return AABB.Moved(aabb1, move);
        }

        public static AABB operator *(AABB aabb1, Vector3 scale)
        {
            return AABB.Scaled(aabb1, scale);
        }

        public static AABB operator *(Vector3 scale, AABB aabb1)
        {
            return AABB.Scaled(aabb1, scale);
        }

        public static AABB operator *(AABB aabb1, Matrix trans)
        {
            return AABB.Transformed(aabb1, trans);
        }

        public static AABB operator *(Matrix trans, AABB aabb1)
        {
            return AABB.Transformed(aabb1, trans);
        }
    }
}
