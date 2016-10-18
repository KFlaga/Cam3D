using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamDX
{
    public class DXSceneNode
    {
        public DXSceneNode Parent { get; set; }
        public List<IModel> AttachedObjects { get; set; } = new List<IModel>();
        public List<DXSceneNode> Children { get; set; } = new List<DXSceneNode>();

        Vector3 _position;
        Quaternion _orientation;
        Vector3 _scale;
        Matrix _transfrom;
        AABB _aabb;
        bool _transfromChanged = false;

        public Vector3 Position
        {
            get { return _position; }
            set
            {
                if(_position != value)
                {
                    _position = value;
                    _transfromChanged = true;
                }
            }
        }

        public Quaternion Orientation
        {
            get { return _orientation; }
            set
            {
                if(_orientation != value)
                {
                    _orientation = value;
                    _transfromChanged = true;
                }
            }
        }

        public Vector3 Scale
        {
            get { return _scale; }
            set
            {
                if(_scale != value)
                {
                    _scale = value;
                    _transfromChanged = true;
                }
            }
        }

        public Matrix TransformationMatrix
        {
            get { return _transfrom; }
        }

        public AABB LocalAABB { get; set; }

        public AABB GlobalAABB
        {
            get
            {
                return AABB.Transformed(LocalAABB, 
                    TransformationMatrixDerived);
            }
        }

        public Vector3 PositionDerived
        {
            get
            {
                return Parent != null ?
                    Position + Parent.PositionDerived :
                    Position;
            }
        }

        public Quaternion OrientationDerived
        {
            get
            {
                return Parent != null ?
                    Orientation * Parent.OrientationDerived :
                    Orientation;
            }
        }

        public Vector3 ScaleDerived
        {
            get
            {
                return Parent != null ?
                    Scale * Parent.ScaleDerived :
                    Scale;
            }
        }

        public Matrix TransformationMatrixDerived
        {
            get
            {
                return Parent != null ?
                    TransformationMatrix * Parent.TransformationMatrixDerived :
                    TransformationMatrix;
            }
        }

        public AABB GlobalAABBDerived
        {
            get
            {
                return Parent != null ?
                    Parent.GlobalAABBDerived : GlobalAABB;
            }
        }

        public void AttachModel(IModel model)
        {
            AttachedObjects.Add(model);
            LocalAABB.Union(AABB.Transformed(model.ModelAABB, TransformationMatrix));
        }

        public void DetachModel(IModel model)
        {
            AttachedObjects.Remove(model);
            RecalculateAABB();
        }

        public void DetachAll()
        {
            AttachedObjects.Clear();
            RecalculateAABB();
        }

        public void RecalculateAABB()
        {
            UpdateTransfromMatrix();

            LocalAABB = new AABB();
            foreach(var child in Children)
            {
                LocalAABB.Union(child.LocalAABB);
            }

            foreach(var model in AttachedObjects)
            {
                LocalAABB.Union(model.ModelAABB * TransformationMatrix);
            }

            if(Parent != null)
                Parent.RecalculateAABB();
        }

        public void UpdateTransfromMatrix()
        {
            if(_transfromChanged)
            {
                // TranslationMatrix * RotationMatrix * ScaleMatrix
                _transfrom = Matrix.Scaling(_scale);
                _transfrom = _transfrom * Matrix.RotationQuaternion(_orientation);
                _transfrom = _transfrom * Matrix.Translation(_position);
                _transfromChanged = false;
            }
        }
    }
}
