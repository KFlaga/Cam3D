using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamDX
{
    public class DXCamera
    {
        public Vector3 Position { get; set; }
        public Vector3 LookAt { get; set; }
        public AngleSingle FieldOfView { get; set; }
        public float NearBound { get; set; }
        public float FarBound { get; set; }
        public Vector3 UpDir { get; set; }
        public float Aspect { get; set; }

        public Matrix ViewMat { get; set; }
        public Matrix ProjMat { get; set; }

        private Vector3 _angles;

        public DXCamera()
        {
            Position = new Vector3(0.0f, 0.0f, -10.0f);
            LookAt = new Vector3(0.0f, 0.0f, 0.0f);
            FieldOfView = new AngleSingle((float)22.5, AngleType.Degree);
            NearBound = 1.0f;
            FarBound = 100.0f;
            UpDir = new Vector3(0.0f, 1.0f, 0.0f);
            Aspect = 4.0f / 3.0f;
            _angles = Vector3.Zero;

            UpdateViewMatrix();
            UpdateProjectionMatrix();
        }

        public void UpdateViewMatrix()
        {
            ViewMat = Matrix.LookAtLH(Position, LookAt, UpDir);
        }

        public void UpdateProjectionMatrix()
        {
            ProjMat = Matrix.PerspectiveFovLH(FieldOfView.Radians, Aspect, NearBound, FarBound);
        }

        public void MoveCamera(Vector3 dist)
        {
            Position += dist;
            LookAt += dist;
        }
        
        public void MoveZ(float dist)
        {
            Vector3 dir = (LookAt - Position);
            dir.Normalize();
            Position += dir * dist;
            LookAt += dir * dist;
        }
        
        public void MoveX(float dist)
        {
            Vector3 dir = DXMisc.CrossProduct(UpDir, (LookAt - Position));
            dir.Normalize();
            Position += dir * dist;
            LookAt += dir * dist;
        }

        public void MoveY(float dist)
        {
            Position += UpDir * dist;
            LookAt += UpDir * dist;
        }

        public void RotateZ(float angle)
        {
            //  float oldAngle = (float)Math.Asin(UpDir.X);
            //  SetRotationZ(angle + oldAngle);
            Matrix rot = Matrix.RotationAxis((LookAt - Position), angle);
            UpDir = Vector3.TransformCoordinate(UpDir, rot);
            _angles.Z += angle;
        }

        public void SetRotationZ(float angle)
        {
         //   UpDir = new Vector3((float)Math.Sin(angle), (float)Math.Cos(angle), 0.0f);
        }

        public void RotateX(float angle)
        {
            UpDir.Normalize();
            Matrix rot = Matrix.RotationAxis(UpDir, angle);
            LookAt = Vector3.TransformCoordinate(LookAt - Position, rot) + Position;
            _angles.Y += angle;
        }

        public void SetRotationX(float angle)
        {

        }

        public void RotateY(float angle)
        {
            Vector3 axis = DXMisc.CrossProduct(UpDir, (LookAt - Position));
            axis.Normalize();
            Matrix rot = Matrix.RotationAxis(axis, angle);
            LookAt = Vector3.TransformCoordinate(LookAt - Position, rot) + Position;
            _angles.X += angle;
        }
    }
}
