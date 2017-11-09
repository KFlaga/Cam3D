using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace CamAlgorithms.Calibration
{
    public class CameraPair : IXmlSerializable
    {
        private static CameraPair _data = new CameraPair();
        public static CameraPair Data { get { return _data; } }
        
        public bool AreCalibrated { get { return Left.IsCalibrated && Right.IsCalibrated; } }
        
        private Camera _camLeft = new Camera();
        public Camera Left
        {
            get
            {
                return _camLeft;
            }
            set
            {
                _camLeft = value;
                Update();
            }
        }

        private Camera _camRight = new Camera();
        public Camera Right
        {
            get
            {
                return _camRight;
            }
            set
            {
                _camRight = value;
                Update();
            }
        }

        public Camera GetCamera(SideIndex idx)
        {
            return idx == SideIndex.Left ? Left : Right;
        }

        public void SetCamera(SideIndex idx, Camera camera)
        {
            if(idx == SideIndex.Left) { Left = camera; }
            else { Right = camera; }
        }

        public Matrix<double> GetCameraMatrix(SideIndex idx)
        {
            return idx == SideIndex.Left ? Left.Matrix : Right.Matrix;
        }

        public void SetCameraMatrix(SideIndex idx, Matrix<double> matrix)
        {
            if(idx == SideIndex.Left) { Left.Matrix = matrix; }
            else { Right.Matrix = matrix; }
            Update();
        }

        [XmlIgnore]
        public Matrix<double> Essential { get; protected set; }
        public Matrix<double> Fundamental { get; protected set; }
        public Vector<double> EpiPoleLeft { get; protected set; }
        public Vector<double> EpiPoleRight { get; protected set; }

        public Vector<double> GetEpipole(SideIndex idx)
        {
            return idx == SideIndex.Left ? EpiPoleLeft : EpiPoleRight;
        }

        [XmlIgnore]
        public Matrix<double> EpipoleCrossLeft { get; protected set; }
        [XmlIgnore]
        public Matrix<double> EpipoleCrossRight { get; protected set; }
        [XmlIgnore]
        public bool EpiLeftInInfinity { get; protected set; }
        [XmlIgnore]
        public bool EpiRightInInfinity { get; protected set; }


        public Matrix<double> RectificationLeft { get; set; }
        public Matrix<double> RectificationRight { get; set; }
        [XmlIgnore]
        public Matrix<double> RectificationLeftInverse { get; set; }
        [XmlIgnore]
        public Matrix<double> RectificationRightInverse { get; set; }

        public void Update()
        {
            Left.Decompose();
            Right.Decompose();

            if(AreCalibrated == false)
            {
                return;
            }
            
            // Find e_R = P_R*C_L, e_L = P_L*C_R
            EpiPoleRight = Right.Matrix * new DenseVector(new double[] { Left.Translation.At(0), Left.Translation.At(1), Left.Translation.At(2), 1.0 });
            EpiPoleLeft = Left.Matrix * new DenseVector(new double[] { Right.Translation.At(0), Right.Translation.At(1), Right.Translation.At(2), 1.0 });

            EpiLeftInInfinity = false;
            EpiRightInInfinity = false;

            // Check if any epipole is in infinity:
            if(Math.Abs(EpiPoleLeft.At(2)) < 1e-9)
            {
                EpiLeftInInfinity = true;
                // Normalize epipole not by dividing by w, but so |e| = 1
                EpiPoleLeft = EpiPoleLeft.Normalize(2);
            }
            if(Math.Abs(EpiPoleRight.At(2)) < 1e-9)
            {
                EpiRightInInfinity = true;
                // Normalize epipole not by dividing by w, but so |e| = 1
                EpiPoleRight = EpiPoleRight.Normalize(2);
            }


            //             |  0 -e3  e2|
            // Find [e]x = | e3   0 -e1|
            //             |-e2  e1   0|

            EpipoleCrossRight = new DenseMatrix(3, 3);
            EpipoleCrossRight[0, 0] = 0.0;
            EpipoleCrossRight[1, 0] = EpiPoleRight[2];
            EpipoleCrossRight[2, 0] = -EpiPoleRight[1];
            EpipoleCrossRight[0, 1] = -EpiPoleRight[2];
            EpipoleCrossRight[1, 1] = 0.0;
            EpipoleCrossRight[2, 1] = EpiPoleRight[0];
            EpipoleCrossRight[0, 2] = EpiPoleRight[1];
            EpipoleCrossRight[1, 2] = -EpiPoleRight[0];
            EpipoleCrossRight[2, 2] = 0.0;

            EpipoleCrossLeft = new DenseMatrix(3, 3);
            EpipoleCrossLeft[0, 0] = 0.0;
            EpipoleCrossLeft[1, 0] = EpiPoleLeft[2];
            EpipoleCrossLeft[2, 0] = -EpiPoleLeft[1];
            EpipoleCrossLeft[0, 1] = -EpiPoleLeft[2];
            EpipoleCrossLeft[1, 1] = 0.0;
            EpipoleCrossLeft[2, 1] = EpiPoleLeft[0];
            EpipoleCrossLeft[0, 2] = EpiPoleLeft[1];
            EpipoleCrossLeft[1, 2] = -EpiPoleLeft[0];
            EpipoleCrossLeft[2, 2] = 0.0;

            // F = [er]x * Pr * pseudoinv(Pl)
            Fundamental = EpipoleCrossRight * Right.Matrix * (Left.Matrix.PseudoInverse());
            int rank = Fundamental.Rank();
            if(rank == 3)
            {
                // Need to ensure rank 2, so set smallest singular value to 0
                var svd = Fundamental.Svd();
                var E = svd.W;
                E[2, 2] = 0;
                var oldF = Fundamental;
                Fundamental = svd.U * E * svd.VT;
                var diff = Fundamental - oldF; // Difference should be very small if all is correct

                // NOPE
                // Get new SVD -> last singular value should be 0
                // Vectors corresponding to this value are right/left epipoles (change them to ensure e'^TFe = 0)
               // var evd = _fundamental.Evd();
               // svd = _fundamental.Svd();
            }

            // Scale F, so that F33 = 1
            Fundamental = Fundamental.Divide(Fundamental[2, 2]);

            // E = Kr^T F Kl
            Essential = Right.InternalMatrix.Transpose() * Fundamental * Left.InternalMatrix;
        }
        
        #region IXmlSerializable
        public XmlSchema GetSchema() { return null; }

        public virtual void ReadXml(XmlReader reader)
        {
            XmlSerialisation.ReadXmlAllProperties(reader, this);
            if(RectificationLeft != null) { RectificationLeftInverse = RectificationLeft.Inverse(); }
            if(RectificationRight != null) { RectificationRightInverse = RectificationRight.Inverse(); }
            if(AreCalibrated) { Update(); }
        }

        public virtual void WriteXml(XmlWriter writer)
        {
            XmlSerialisation.WriteXmlNonIgnoredProperties(writer, this);
        }
        #endregion

        public void CopyFrom(CameraPair cameras)
        {
            foreach(var prop in GetType().GetProperties())
            {
                prop.SetValue(this, prop.GetValue(cameras));
            }
            Update();
        }
    }
}
