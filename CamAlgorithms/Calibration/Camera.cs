using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace CamAlgorithms.Calibration
{
    // Camera matrix M = 
    //  M = KE (E = R[I|-T])
    // Calibration matrix:
    //      |ax  s x0|
    //  K = | 0 ay y0|
    //      | 0  0  1|
    // where:
    //  ax,ay = f*mx,f*my -> m - num of pixels per unit distance, f - focal length, a - pixel dimensions
    // s - skew, x0,y0 - principal point in pixel dimensions
    // Extrinsic params:
    //      |R1 -R1T| (Ri = i-th row of R)
    //  E = |R2 -R2T|
    //      |R3 -R3T|
    // Pcam = R(Pworld − T) (in 3d)
    // pimg = M*Pworld
    //
    // RQ decomposition:
    // R is upper triangle matrix and Q is Orthonormal (?Orthogonal)
    // M = KE * [I|-T] <=> M = [KR | -KRT] 
    // Then in RQ decomposition we have RQ.R = K and RQ.Q = R
    // To find T we use fact that 4th column of M is -KRT and first 3 columns as KR
    // Let C = -KRT and X = KR, then:
    // T = - X^-1 * C
    [DebuggerDisplay("{Matrix}")]
    public class Camera : IXmlSerializable
    {
        public Matrix<double> Matrix { get; set; } = new DenseMatrix(3, 4);
        
        public Matrix<double> InternalMatrix { get; set; } = new DenseMatrix(3, 3);
        public Matrix<double> RotationMatrix { get; set; } = new DenseMatrix(3, 3);
        public Vector<double> Center { get; set; } = new DenseVector(3);

        public RadialDistortion Distortion { get; set; } = new RadialDistortion();

        [XmlIgnore]
        public bool IsCalibrated { get { return System.Math.Abs(Matrix[0, 0]) > 1e-12; } }
        
        public int ImageWidth { get; set; } = 0;
        public int ImageHeight { get; set; } = 0;

        public static Matrix<double> Normalized(Matrix<double> cameraMatrix, Matrix<double> normImage, Matrix<double> normReal)
        {
            return normImage.Multiply(cameraMatrix.Multiply(normReal.Inverse()));
        }

        public void Normalize(Matrix<double> normImage, Matrix<double> normReal)
        {
            Matrix = Normalized(Matrix, normImage, normReal);
        }

        public static Matrix<double> Denormalized(Matrix<double> cameraMatrix, Matrix<double> normImage, Matrix<double> normReal)
        {
            return normImage.Inverse().Multiply(cameraMatrix.Multiply(normReal));
        }

        public void Denormalize(Matrix<double> normImage, Matrix<double> normReal)
        {
            Matrix = Denormalized(Matrix, normImage, normReal);
        }

        public void Decompose()
        {
            Matrix<double> i, r;
            Vector<double> t;
            Matrix = Decomposed(Matrix, out i, out r, out t);
            InternalMatrix = i;
            RotationMatrix = r;
            Center = t;
        }

        public static Matrix<double> Decomposed(Matrix<double> camera, 
            out Matrix<double> internalMatrix, 
            out Matrix<double> rotationMatrix, 
            out Vector<double> translation)
        {
            // In MAthNet we have QR decomposition only
            // To have RQ we need:
            // - flip left/right side of camera matrix  and transpose it: (r,c) -> (c, R-r)
            var flipped = camera.FlipUpsideDown().Transpose();
            var QR = flipped.SubMatrix(0, 3, 0, 3).QR(MathNet.Numerics.LinearAlgebra.Factorization.QRMethod.Full);
            var QR1 = camera.SubMatrix(0, 3, 0, 3).QR(MathNet.Numerics.LinearAlgebra.Factorization.QRMethod.Full);
            
            // Scale so that K[2,2] is 1
            double scaleK = Math.Abs(1.0 / QR.R[0, 0]);
            flipped = flipped.Multiply(scaleK);

            QR = flipped.SubMatrix(0, 3, 0, 3).QR(MathNet.Numerics.LinearAlgebra.Factorization.QRMethod.Full);
            internalMatrix = QR.R.Transpose().FlipUpsideDown().FlipLeftRight();
            rotationMatrix = QR.Q.Transpose().FlipUpsideDown();

            // If fx < 0 (which in practice happens often), then set fx = -fx and [r11,r12,r13] = -[r11,r12,r13]
            // As first row of rotation matrix is multiplied only with fx, then changing sign of both
            // fx and this row won't change matrix M = K*R, and so camera matrix
            if(internalMatrix[0, 0] < 0)
            {
                internalMatrix[0, 0] = -internalMatrix[0, 0];
                rotationMatrix[0, 0] = -rotationMatrix[0, 0];
                rotationMatrix[0, 1] = -rotationMatrix[0, 1];
                rotationMatrix[0, 2] = -rotationMatrix[0, 2];
            }
            if(internalMatrix[1, 1] < 0)
            {
                internalMatrix[1, 1] = -internalMatrix[1, 1];
                internalMatrix[0, 1] = -internalMatrix[0, 1];
                rotationMatrix[1, 0] = -rotationMatrix[1, 0];
                rotationMatrix[1, 1] = -rotationMatrix[1, 1];
                rotationMatrix[1, 2] = -rotationMatrix[1, 2];
            }
            if(internalMatrix[2, 2] < 0)
            {
                internalMatrix[2, 2] = -internalMatrix[2, 2];
                internalMatrix[1, 2] = -internalMatrix[1, 2];
                internalMatrix[0, 2] = -internalMatrix[0, 2];
                rotationMatrix[2, 0] = -rotationMatrix[2, 0];
                rotationMatrix[2, 1] = -rotationMatrix[2, 1];
                rotationMatrix[2, 2] = -rotationMatrix[2, 2];
            }

            translation = -camera.SubMatrix(0, 3, 0, 3).Inverse().Multiply(camera.SubMatrix(0, 3, 3, 1)).Column(0);

            var KR = internalMatrix * rotationMatrix;
            return KR.Append(-KR * translation.ToColumnMatrix());
        }

        public static Camera FromDecomposition(Matrix<double> K, Matrix<double> R, Vector<double> C)
        {
            Matrix<double> Ext = new DenseMatrix(3, 4);
            Ext.SetSubMatrix(0, 0, R);
            Ext.SetColumn(3, -R * C);

            var camera = new Camera()
            {
                Matrix = K * Ext,
                InternalMatrix = K,
                RotationMatrix = R,
                Center = C
            };
            return camera;
        }

        public Camera Clone()
        {
            return new Camera()
            {
                Matrix = this.Matrix.Clone(),
                RotationMatrix = this.RotationMatrix.Clone(),
                InternalMatrix = this.InternalMatrix.Clone(),
                Center = this.Center.Clone(),
                ImageWidth = this.ImageWidth,
                ImageHeight = this.ImageHeight
            };
        }

        public override string ToString()
        {
            StringBuilder result =  new StringBuilder();

            result.AppendLine("Camera Matrix:");
            result.AppendLine(Matrix.CustomToString());
            
            result.AppendLine("Calibration Matrix:");
            result.AppendLine(InternalMatrix.CustomToString());
                
            result.AppendLine("Rotation Matrix:");
            result.AppendLine(RotationMatrix.CustomToString());
                
            result.AppendLine("Translation Vector:");
            result.AppendLine(Center.ToColumnVectorString());

            return result.ToString();
        }

        #region IXmlSerializable
        public XmlSchema GetSchema() { return null; }

        public virtual void ReadXml(XmlReader reader)
        {
            XmlSerialisation.ReadXmlAllProperties(reader, this);
            if(IsCalibrated)
            {
                Decompose();
            }
        }

        public virtual void WriteXml(XmlWriter writer)
        {
            XmlSerialisation.WriteXmlNonIgnoredProperties(writer, this);
        }
        #endregion
    }
}
