using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace CamAlgorithms.Calibration
{
    public class Camera : IXmlSerializable
    {
        public Matrix<double> Matrix = new DenseMatrix(3, 4);
        
        public Matrix<double> InternalMatrix = new DenseMatrix(3, 3);
        public Matrix<double> RotationMatrix = new DenseMatrix(3, 3);
        public Vector<double> Translation = new DenseVector(3);

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
            Matrix = Decomposed(Matrix, out InternalMatrix, out RotationMatrix, out Translation);
        }

        public static Matrix<double> Decomposed(Matrix<double> camera, 
            out Matrix<double> internalMatrix, 
            out Matrix<double> rotationMatrix, 
            out Vector<double> translation)
        {
            var RQ = camera.SubMatrix(0, 3, 0, 3).QR();

            double scaleK = 1.0 / RQ.R[2, 2];
            camera = camera.Multiply(scaleK);

            RQ = camera.SubMatrix(0, 3, 0, 3).QR();
            internalMatrix = RQ.R;
            rotationMatrix = RQ.Q;

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

            translation = camera.SubMatrix(0, 3, 0, 3).Inverse().
                Multiply(camera.SubMatrix(0, 3, 3, 1)).Column(0);

            return camera;
        }

        public Camera Clone()
        {
            return new Camera()
            {
                Matrix = this.Matrix.Clone(),
                RotationMatrix = this.RotationMatrix.Clone(),
                InternalMatrix = this.InternalMatrix.Clone(),
                Translation = this.Translation.Clone()
            };
        }

        public override string ToString()
        {
            StringBuilder result =  new StringBuilder();

            result.AppendLine("Camera Matrix:");
            result.AppendLine(Matrix.CustomToString());

            if(InternalMatrix != null)
            {
                result.AppendLine("Calibration Matrix:");
                result.AppendLine(InternalMatrix.CustomToString());
                
                result.AppendLine("Rotation Matrix:");
                result.AppendLine(RotationMatrix.CustomToString());
                
                result.AppendLine("Translation Vector:");
                result.AppendLine(Translation.CustomToString());
            }

            return result.ToString();
        }

        #region IXmlSerializable
        public XmlSchema GetSchema() { return null; }

        public virtual void ReadXml(XmlReader reader)
        {
            XmlSerialisation.ReadXmlAllProperties(reader, this);
        }

        public virtual void WriteXml(XmlWriter writer)
        {
            XmlSerialisation.WriteXmlNonIgnoredProperties(writer, this);
        }
        #endregion
    }
}
