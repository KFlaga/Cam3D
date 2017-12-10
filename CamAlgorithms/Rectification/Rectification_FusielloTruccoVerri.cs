using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Xml.Serialization;

namespace CamAlgorithms
{
    [XmlRoot("Rectification_FusielloCalibrated")]
    public class Rectification_FusielloTruccoVerri : IRectificationAlgorithm
    {
        Matrix<double> _R;
        Matrix<double> _K;

        public override void ComputeRectificationMatrices()
        {
            //function[T1, T2, Pn1, Pn2] = rectify(Po1, Po2)
            //% RECTIFY: compute rectification matrices
            //% factorize old PPMs
            //[A1, R1, t1] = art(Po1);
            //[A2, R2, t2] = art(Po2);
            //% optical centers(unchanged)
            //c1 = - inv(Po1(:,1:3))*Po1(:,4);
            //        c2 = - inv(Po2(:,1:3))*Po2(:,4);
            //% new x axis(= direction of the baseline)
            //v1 = (c1-c2);
            //% new y axes(orthogonal to new x and old z)
            //v2 = cross(R1(3,:)',v1);
            //% new z axes(orthogonal to baseline and y)
            //v3 = cross(v1, v2);
            //% new extrinsic parameters
            //R = [v1'/norm(v1)
            //v2'/norm(v2)
            //v3'/norm(v3)];
            //% translation is left unchanged
            //% new intrinsic parameters(arbitrary)
            //A = (A1 + A2)./2;
            //A(1,2)=0; % no skew
            //% new projection matrices
            //Pn1 = A*[R - R * c1];
            //Pn2 = A*[R - R * c2];
            //% rectifying image transformation
            //T1 = Pn1(1:3, 1:3) * inv(Po1(1:3, 1:3));
            //T2 = Pn2(1:3,1:3)* inv(Po2(1:3,1:3));
            Vector<double> c1 = Cameras.Left.Center;
            Vector<double> c2 = Cameras.Right.Center;

            Vector<double> v1 = c1 - c2;
            Vector<double> v2 = Cameras.Left.RotationMatrix.Row(2).Cross(v1);
            Vector<double> v3 = v1.Cross(v2);

            _R = new DenseMatrix(3, 3);
            _R.SetRow(0, v1.Normalize(2));
            _R.SetRow(1, v2.Normalize(2));
            _R.SetRow(2, v3.Normalize(2));
            
            _K = (Cameras.Left.InternalMatrix + Cameras.Right.InternalMatrix).Multiply(0.5);
            _K[0, 1] = 0.0;
            
            RectificationLeft = (_K * _R) * ((Cameras.Left.InternalMatrix * Cameras.Left.RotationMatrix).Inverse());
            RectificationRight = (_K * _R) * ((Cameras.Right.InternalMatrix * Cameras.Right.RotationMatrix).Inverse());
        }
    }
}
