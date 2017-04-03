using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CamImageProcessing
{
    [XmlRoot("Rectification_FusielloCalibrated")]
    public class ImageRectification_FusielloCalibrated : ImageRectificationComputer
    {
        Matrix<double> _R;
        Matrix<double> _K;
        Matrix<double> _Ht_L, _Ht_R;

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
            Vector<double> c1 = CalibData.TranslationLeft;
            Vector<double> c2 = CalibData.TranslationRight;

            Vector<double> v1 = c1 - c2;
            Vector<double> v2 = CalibData.RotationLeft.Row(2).Cross(v1);
            Vector<double> v3 = v1.Cross(v2);

            _R = new DenseMatrix(3, 3);
            _R.SetRow(0, v1.Normalize(2));
            _R.SetRow(1, v2.Normalize(2));
            _R.SetRow(2, v3.Normalize(2));

            Matrix<double> halfRevolve = new DenseMatrix(3, 3);
            RotationConverter.EulerToMatrix(new double[] { 0.0, 0.0, Math.PI }, halfRevolve);
            _R = halfRevolve * _R;

            _K = (CalibData.CalibrationLeft + CalibData.CalibrationRight).Multiply(0.5);
            _K[0, 1] = 0.0;
            
            RectificationLeft = (_K * _R) * ((CalibData.CalibrationLeft * CalibData.RotationLeft).Inverse());
            RectificationRight = (_K * _R) * ((CalibData.CalibrationRight * CalibData.RotationRight).Inverse());
            ComputeScalingMatrices(ImageWidth, ImageHeight);

            RectificationLeft = _Ht_L * RectificationLeft;
            RectificationLeft = _Ht_R * RectificationRight;
        }


        public void ComputeScalingMatrices(int imgWidth, int imgHeight)
        {
            // Scale and move images (after rectification) so that they have lowest
            // coordinates (0,0) and same width/height as original image
            var H_L = RectificationLeft;
            var H_R = RectificationRight;
            var tl_L = H_L * new DenseVector(new double[3] { 0.0, 0.0, 1.0 });
            tl_L.DivideThis(tl_L.At(2));
            var tr_L = H_L * new DenseVector(new double[3] { imgWidth - 1, 0.0, 1.0 });
            tr_L.DivideThis(tr_L.At(2));
            var bl_L = H_L * new DenseVector(new double[3] { 0.0, imgHeight - 1, 1.0 });
            bl_L.DivideThis(bl_L.At(2));
            var br_L = H_L * new DenseVector(new double[3] { imgWidth - 1, imgHeight - 1, 1.0 });
            br_L.DivideThis(br_L.At(2));
            var tl_R = H_R * new DenseVector(new double[3] { 0.0, 0.0, 1.0 });
            tl_R.DivideThis(tl_R.At(2));
            var tr_R = H_R * new DenseVector(new double[3] { imgWidth - 1, 0.0, 1.0 });
            tr_R.DivideThis(tr_R.At(2));
            var bl_R = H_R * new DenseVector(new double[3] { 0.0, imgHeight - 1, 1.0 });
            bl_R.DivideThis(bl_R.At(2));
            var br_R = H_R * new DenseVector(new double[3] { imgWidth - 1, imgHeight - 1, 1.0 });
            br_R.DivideThis(br_R.At(2));

            // (scale and y-translation must be same for both images to perserve rectification)
            // Scale so that both images fits to (imgHeight*2, imgWidth*2)
            // If it fits w/o scaling, scale so that bigger image have width imgWidth
            // Translate in y so that left(0,0) is transformed into left'(0,x)
            // Translate in x (independently) so that img(0,0) is transformed into img'(y,0)
            // 1) Find max/min x/y
            double minX_L = Math.Min(br_L.At(0), Math.Min(bl_L.At(0), Math.Min(tl_L.At(0), tr_L.At(0))));
            double minY_L = Math.Min(br_L.At(1), Math.Min(bl_L.At(1), Math.Min(tl_L.At(1), tr_L.At(1))));
            double maxX_L = Math.Max(br_L.At(0), Math.Max(bl_L.At(0), Math.Max(tl_L.At(0), tr_L.At(0))));
            double maxY_L = Math.Max(br_L.At(1), Math.Max(bl_L.At(1), Math.Max(tl_L.At(1), tr_L.At(1))));

            double minX_R = Math.Min(br_R.At(0), Math.Min(bl_R.At(0), Math.Min(tl_R.At(0), tr_R.At(0))));
            double minY_R = Math.Min(br_R.At(1), Math.Min(bl_R.At(1), Math.Min(tl_R.At(1), tr_R.At(1))));
            double maxX_R = Math.Max(br_R.At(0), Math.Max(bl_R.At(0), Math.Max(tl_R.At(0), tr_R.At(0))));
            double maxY_R = Math.Max(br_R.At(1), Math.Max(bl_R.At(1), Math.Max(tl_R.At(1), tr_R.At(1))));

            double wr = (maxX_L - minX_L) / (maxX_R - minX_R);
            double hr = (maxY_L - minY_L) / (maxY_R - minY_R);
            double maxWidth = Math.Max(maxX_L - minX_L, maxX_R - minX_R);
            double maxHeight = Math.Max(maxY_L - minY_L, maxY_R - minY_R);

            // 2) Scale image so that longest of all dimensions is equal to old image size
            double scale = 1.0;
            if(maxWidth > maxHeight)
            {
                // Width is greater
                scale = (double)imgWidth / maxWidth;
            }
            else if(maxWidth < maxHeight)
            {
                // Height is greater
                scale = (double)imgHeight / maxHeight;
            }
            // For now leave scale as it is otherwise
            _Ht_L = DenseMatrix.CreateIdentity(3);
            _Ht_R = DenseMatrix.CreateIdentity(3);
            _Ht_L.At(0, 0, scale);
            _Ht_L.At(1, 1, scale);
            _Ht_R.At(0, 0, scale);
            _Ht_R.At(1, 1, scale);
            tl_L.MultiplyThis(scale);
            tr_L.MultiplyThis(scale);
            bl_L.MultiplyThis(scale);
            br_L.MultiplyThis(scale);
            tl_R.MultiplyThis(scale);
            tr_R.MultiplyThis(scale);
            bl_R.MultiplyThis(scale);
            br_R.MultiplyThis(scale);
            // 3) Translate in y so that minY on both images = 0
            double transY = -Math.Min(minY_L, minY_R) * scale;
            // Translate in x (independently) so that minX on both images = 0
            double transX_L = -minX_L * scale;
            double transX_R = -minX_R * scale;
            _Ht_L.At(0, 2, transX_L);
            _Ht_L.At(1, 2, transY);
            _Ht_R.At(0, 2, transX_R);
            _Ht_R.At(1, 2, transY);


            H_L = _Ht_L * RectificationLeft;
            tl_L = H_L * new DenseVector(new double[3] { 0.0, 0.0, 1.0 });
            tl_L.DivideThis(tl_L.At(2));
            tr_L = H_L * new DenseVector(new double[3] { imgWidth - 1, 0.0, 1.0 });
            tr_L.DivideThis(tr_L.At(2));
            bl_L = H_L * new DenseVector(new double[3] { 0.0, imgHeight - 1, 1.0 });
            bl_L.DivideThis(bl_L.At(2));
            br_L = H_L * new DenseVector(new double[3] { imgWidth - 1, imgHeight - 1, 1.0 });
            br_L.DivideThis(br_L.At(2));
        }
    }
}
