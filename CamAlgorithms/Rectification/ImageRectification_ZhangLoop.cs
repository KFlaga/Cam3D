using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Xml.Serialization;

namespace CamAlgorithms
{
    // Computes rectification matrices for 2 cameras from CalibrationData
    // 
    //
    [XmlRoot("Rectification_ZhangLoop")]
    public class ImageRectification_ZhangLoop : ImageRectificationComputer
    {
        private Matrix<double> _A_L;
        private Matrix<double> _B_L;
        private Matrix<double> _A_R;
        private Matrix<double> _B_R;

        private Matrix<double> _Hp_L;
        private Matrix<double> _Hp_R;
        private Matrix<double> _Hr_L;
        private Matrix<double> _Hr_R;
        private Matrix<double> _Hs_L;
        private Matrix<double> _Hs_R;

        public override void ComputeRectificationMatrices()
        {
            if(Cameras.EpiLeftInInfinity)
            {
                ComputeProjectiveMatrices_LeftEpiInInfinity(ImageWidth, ImageHeight);
            }
            else if(Cameras.EpiRightInInfinity)
            {
                ComputeProjectiveMatrices_RightEpiInInfinity(ImageWidth, ImageHeight);
            }
            else
                ComputeProjectiveMatrices(ImageWidth, ImageHeight);

            ComputeSymilarityMatrices (ImageWidth, ImageHeight);

            ComputeShearingMatrices(ImageWidth, ImageHeight);

            RectificationLeft = _Hs_L * _Hr_L * _Hp_L;
            RectificationRight = _Hs_R * _Hr_R * _Hp_R;
        }

        #region Projective Matrix

        public void ComputeProjectiveMatrices(int imgWidth, int imgHeight)
        {
            int ih = imgHeight, iw = imgWidth;

            // Minimise sum(wi - wc)/sum(wc), wi are projective weight of point pi, wc is weight of center point
            // We have pc - center point, P = [pi - pc] (3 rows x N cols matrix)
            // sum(wi - wc)/sum(wc) = w^T * P * P^T * w / (w^T * pc * pc^T * w), where w = [ w1, w2, 1 ] is third row of projective matrix
            Matrix<double> PPt = new DenseMatrix(3, 3);
            PPt[0, 0] = iw * ih * (iw * iw - 1) / 12.0;
            PPt[1, 1] = iw * ih * (ih * ih - 1) / 12.0;

            int iw1 = iw - 1;
            int ih1 = ih - 1;
            Matrix<double> pcpct = new DenseMatrix(3, 3);
            pcpct[0, 0] = iw1 * iw1 * 0.25;
            pcpct[0, 1] = iw1 * ih1 * 0.25;
            pcpct[0, 2] = iw1 * 0.5;
            pcpct[1, 0] = iw1 * ih1 * 0.25;
            pcpct[1, 1] = ih1 * ih1 * 0.25;
            pcpct[1, 2] = ih1 * 0.5;
            pcpct[2, 0] = iw1 * 0.5;
            pcpct[2, 1] = ih1 * 0.5;
            pcpct[2, 2] = 1.0;
            
            // As w = [e]x * z, w' = Fz, for some z = [z1 z2 0]
            // Then for both images minimsed sum is equal:
            // zt * [e]x*P*Pt*[e]x * z / (zt * [e]x*pc*pct*[e]x * z) + zt * F*P'*Pt'*F * z / (zt * F*pc'*pct'*F * z)
            // Let A = [e]x*P*Pt*[e]x and B = [e]x*pc*pct*[e]x
            // Only upper 2x2 submatrix is used as z3 = 0
            _A_L = (Cameras.EpipoleCrossLeft.Transpose() * PPt * Cameras.EpipoleCrossLeft).SubMatrix(0, 2, 0, 2);
            _A_R = (Cameras.Fundamental.Transpose() * PPt * Cameras.Fundamental).SubMatrix(0, 2, 0, 2);

            _B_L = (Cameras.EpipoleCrossLeft.Transpose() * pcpct * Cameras.EpipoleCrossLeft).SubMatrix(0, 2, 0, 2);
            _B_R = (Cameras.Fundamental.Transpose() * pcpct * Cameras.Fundamental).SubMatrix(0, 2, 0, 2);

            // Finally we need to such a z to minimise (z^T * A * z) / (z^T * B * z) + (z^T * A' * z) / (z^T * B' * z)
            // 
            // We need to find some initial approximation of z
            // First lets find z that minimise error in one image, which is eqivalent to maximising :
            // e = (z^T * B * z) / (z^T * A * z)

            // Decompose A to A = D^T * D ( as A is symmetric positive definite ) :
            // d11 = sqrt(a11 - a21^2/a22), d12 = 0, d21 = a21/sqrt(a22), d22 = sqrt(a22)
            // D^-1 is defined as 1/(d11*d22) * [ d22, 0; -d21, d11 ]
            // 
            // We have :
            // e = (z^T * B * z) / (z^T * D^T * D * z)
            // Let y = D*z, y^T = z^T * D^T => z = D^-1 * y, z^T = y^T * (D^T)^-1
            // e = (y^T * (D^T)^-1 * B * D^-1 * y) / (y^T * y)
            // As y defined up to scale factor ( same as z ), then let ||y|| = 1
            // e is maximised then if y is an eigenvector of (D^T)^-1 * B * D^-1 associated with the largest eigenvalue
            Matrix<double> D1_L = new DenseMatrix(2, 2);
            double d22 = Math.Sqrt(_A_L[1, 1]);
            double d21 = _A_L[1, 0] / d22;
            double d11 = Math.Sqrt(_A_L[0, 0] - d21 * d21);
            double scale = 1.0 / (d11 * d22);
            D1_L[0, 0] = d22 * scale;
            D1_L[0, 1] = 0.0;
            D1_L[1, 0] = -d21 * scale;
            D1_L[1, 1] = d11 * scale;

            Matrix<double> DtBD_L = D1_L.Transpose() * _B_L * D1_L;
            var evd_L = DtBD_L.Evd();
            var y_L = evd_L.EigenVectors.Row(1);
            var z_L = D1_L * y_L;

            Matrix<double> D1_R = new DenseMatrix(2, 2);
            d22 = Math.Sqrt(_A_R[1, 1]);
            d21 = _A_R[1, 0] / d22;
            d11 = Math.Sqrt(_A_R[0, 0] - d21 * d21);
            scale = 1.0 / (d11 * d22);
            D1_R[0, 0] = d22 * scale;
            D1_R[0, 1] = 0.0;
            D1_R[1, 0] = -d21 * scale;
            D1_R[1, 1] = d11 * scale;

            Matrix<double> DtBD_R = D1_R.Transpose() * _B_R * D1_R;
            var evd_R = DtBD_R.Evd();
            var y_R = evd_R.EigenVectors.Row(1);
            var z_R = D1_R * y_R;

            // Set initial estimate as vector between vectors minimising errors in each image
            // (its a close one, so may serve as final result if error is small enough)
            var z_init = (z_L / z_L.L2Norm() + z_R / z_R.L2Norm()) / 2.0;
            // Scale z, so that z2 = 1
            var z = new DenseVector(3);
            z[0] = z_L[0] / z_L[1];
            z[1] = 1.0;
            z[2] = 0.0;

            double error = ComputeProjectionError(z[0]);

            // Iteratively minimise :
            // zt*A_L*z/(zt*B_L*z) + zt*A_R*z/(zt*B_R*z)
            // z = [ z1 1 0 ]
            // So only one parameter z1
            OneVariableMinimisation miniAlg = new OneVariableMinimisation()
            {
                DoComputeDerivativesNumerically = true,
                NumericalDerivativeStep = 1e-3,
                MaximumIterations = 100,
                InitialParameter = z[0],
                Function = ComputeProjectionError,
                //  Derivative_1st = ComputeProjectionError_Derivative_1st,
                //  Derivative_2nd = ComputeProjectionError_Derivative_2nd
            };
            miniAlg.Process();

            if(error > miniAlg.MinimalValue)
                z[0] = miniAlg.MinimalParameter;

            // w = [e]x * z, w' = Fz
            _Hp_L = DenseMatrix.CreateIdentity(3);
            _Hp_R = DenseMatrix.CreateIdentity(3);
            var w_L = Cameras.EpipoleCrossLeft * z;
            var w_R = Cameras.Fundamental * z;

            _Hp_L[2, 0] = w_L[0] / w_L[2];
            _Hp_L[2, 1] = w_L[1] / w_L[2];

            _Hp_R[2, 0] = w_R[0] / w_R[2];
            _Hp_R[2, 1] = w_R[1] / w_R[2];
        }

        public void ComputeProjectiveMatrices_LeftEpiInInfinity(int imgWidth, int imgHeight)
        {
            // Left projective matrix is identity and so w = [0,0,1]
            // Epipole defines direction z (or not?)
            
            // w = [e]x * z, w' = Fz
            _Hp_L = DenseMatrix.CreateIdentity(3);
            _Hp_R = DenseMatrix.CreateIdentity(3);
            // var w_L = EpiCrossLeft * z;
            var w_R = Cameras.Fundamental * Cameras.EpiPoleLeft;

            // _Hp_L[2, 0] = w_L[0] / w_L[2];
            //_Hp_L[2, 1] = w_L[1] / w_L[2];

            _Hp_R[2, 0] = w_R[0] / w_R[2];
            _Hp_R[2, 1] = w_R[1] / w_R[2];
        }

        public void ComputeProjectiveMatrices_RightEpiInInfinity(int imgWidth, int imgHeight)
        {
            // Right projective matrix is identity and so w = [0,0,1]
            // Epipole defines direction z (or not?)
            
            // w = [e]x * z, w' = Fz
            _Hp_L = DenseMatrix.CreateIdentity(3);
            _Hp_R = DenseMatrix.CreateIdentity(3);
            var w_L = Cameras.EpipoleCrossLeft * Cameras.EpiPoleRight;
            //var w_L = FundamentalMatrix * EpipoleLeft;

            _Hp_L[2, 0] = w_L[0] / w_L[2];
            _Hp_L[2, 1] = w_L[1] / w_L[2];

            //_Hp_R[2, 0] = w_R[0] / w_R[2];
            //_Hp_R[2, 1] = w_R[1] / w_R[2];
        }

        public double ComputeProjectionError(double x)
        {
            // Computes e = zt*A*z/(zt*B*z) + zt*A'*z/(zt*B'*z)
            // Let:
            // z = [ x, 1 ]
            // w1 = x^2 * a11 + x(a12 + a21) + a22
            // w2 = x^2 * b11 + x(b12 + b21) + b22
            // w3 = x^2 * a'11 + x(a'12 + a'21) + a'22
            // w4 = x^2 * b'11 + x(b'12 + b'21) + b'22
            // Then e = (w1/w2) + (w3/w4)
            double x2 = x * x;
            return
                ((x2 * _A_L[0, 0] + x * (_A_L[0, 1] + _A_L[1, 0]) + _A_L[1, 1]) /
                 (x2 * _B_L[0, 0] + x * (_B_L[0, 1] + _B_L[1, 0]) + _B_L[1, 1])) +
                ((x2 * _A_R[0, 0] + x * (_A_R[0, 1] + _A_R[1, 0]) + _A_R[1, 1]) /
                 (x2 * _B_R[0, 0] + x * (_B_R[0, 1] + _B_R[1, 0]) + _B_R[1, 1]));
        }

        public double ComputeProjectionError_Derivative_1st(double x)
        {
            // Computes de/dx where z = [ x, 1 ]
            // Let w1-w4 be defined same as above
            // Then de/dx = (w1'w2 - w2'w1)/w2^2 + (w3'w4 - w3'w4)/w4^2
            // w1' = 2xa11 + a12 + a21 etc etc
            double x2 = x * x;
            double w1 = x2 * _A_L[0, 0] + x * (_A_L[0, 1] + _A_L[1, 0]) + _A_L[1, 1];
            double w2 = x2 * _B_L[0, 0] + x * (_B_L[0, 1] + _B_L[1, 0]) + _B_L[1, 1];
            double w3 = x2 * _A_R[0, 0] + x * (_A_R[0, 1] + _A_R[1, 0]) + _A_R[1, 1];
            double w4 = x2 * _B_R[0, 0] + x * (_B_R[0, 1] + _B_R[1, 0]) + _B_R[1, 1];

            return
                (((2 * x * _A_L[0, 0] + _A_L[0, 1] + _A_L[1, 0]) * w2 -
                (2 * x * _B_L[0, 0] + _B_L[0, 1] + _B_L[1, 0]) * w1) / (w2 * w2)) +
                (((2 * x * _A_R[0, 0] + _A_R[0, 1] + _A_R[1, 0]) * w4 -
                (2 * x * _B_R[0, 0] + _B_R[0, 1] + _B_R[1, 0]) * w3) / (w4 * w4));
        }

        public double ComputeProjectionError_Derivative_2nd(double x)
        {
            // Computes de/dx where z = [ x, 1 ]
            // Let w1-w4 be defined same as above
            // Then d2e/dx2 = w2(w1''w2 - w2''w1)-2(w1'w2 - w2'w1)/w2^3 + w4(w3''w4 - w4''w3)-2(w3'w4 - w4'w3)/w4^3
            // let y1 = w1'w2 - w2'w1, y2 = w3'w4 - w4'w3
            // w1'' = 2a11
            double x2 = x * x;
            double w1 = x2 * _A_L[0, 0] + x * (_A_L[0, 1] + _A_L[1, 0]) + _A_L[1, 1];
            double w2 = x2 * _B_L[0, 0] + x * (_B_L[0, 1] + _B_L[1, 0]) + _B_L[1, 1];
            double w3 = x2 * _A_R[0, 0] + x * (_A_R[0, 1] + _A_R[1, 0]) + _A_R[1, 1];
            double w4 = x2 * _B_R[0, 0] + x * (_B_R[0, 1] + _B_R[1, 0]) + _B_R[1, 1];
            double y1 = ((2 * x * _A_L[0, 0] + _A_L[0, 1] + _A_L[1, 0]) * w2 -
                (2 * x * _B_L[0, 0] + _B_L[0, 1] + _B_L[1, 0]) * w1);
            double y2 = ((2 * x * _A_R[0, 0] + _A_R[0, 1] + _A_R[1, 0]) * w4 -
                (2 * x * _B_R[0, 0] + _B_R[0, 1] + _B_R[1, 0]) * w3);

            return
                2 * ((w2 * (_A_L[0, 0] * w2 - _B_L[0, 0] * w1) - y1) / (w2 * w2 * w2) +
                     (w4 * (_A_R[0, 0] * w2 - _B_R[0, 0] * w3) - y2) / (w4 * w4 * w4));
        }


        #endregion
        #region Similiratity matrix

        public void ComputeSymilarityMatrices(int imgWidth, int imgHeight)
        {
            // Similarity matrix have form :
            //      | v2-v3w2  v3w1-v1   0 |
            // Hr = | v1-v3w1  v2-v3w2  v3 |
            //      |       0        0   1 |

            // Assume projection matrix is known ( and so w = [w1, w2, 1] )
            //
            // Using F = H'^T * [i]x * H :
            //     | v1w1'-v1'w1  v2w1'-v1'w2  v3w1'-v1' |
            // F = | v1w2'-v2'w1  v2w2'-v2'w2  v3w2'-v2' |
            //     |    v1-v3'w1     v2-v3'w2     v3-v3' |
            //
            // F31 = v1 - v3'w1 => v1 = F31 + v3' * w1
            // F32 = v2 - v2'w2 => v2 = F32 + v3' * w2
            // F33 = v3 - v3'   => v3 = F33 + v3
            //
            // F13 = v3w1'- v1' => v1' = F12 + v3 * w1'
            // F23 = v3w2'- v2' => v2' = F23 + v3 * w2'
            //
            // Then symilarity matrices
            //       | F32-w2F33  w1F33-F31        0 |
            //  Hr = | F31-w1F33  F32-w2F33  F33+v3' | 
            //       |         0          0        1 |
            //
            //        | w2'F33-F23  F13-w1'F33    0 |
            //  Hr' = | w1'F33-F13  w2'F33-F23  v3' | 
            //        |         0          0      1 |
            //
            // Similarity transform is defined almost fully by w computed
            // in projective matrix and fundamental matrix
            // Only choosing v3' remains : it influences image coords translation
            // in y direction : we can choose v3' so that y coord of each image is 0
            // Start with v3' = 0

            var F = Cameras.Fundamental;
            _Hr_L = new DenseMatrix(3);
            _Hr_L[0, 0] = F[2, 1] - F[2, 2] * _Hp_L[2, 1];
            _Hr_L[0, 1] = _Hp_L[2, 0] * F[2, 2] - F[2, 0];
            _Hr_L[0, 2] = 0.0;
            _Hr_L[1, 0] = -_Hr_L[0, 1];
            _Hr_L[1, 1] = _Hr_L[0, 0];
            _Hr_L[1, 2] = F[2, 2];
            _Hr_L[2, 0] = 0.0;
            _Hr_L[2, 1] = 0.0;
            _Hr_L[2, 2] = 1.0;

            _Hr_R = new DenseMatrix(3);
            _Hr_R[0, 0] = _Hp_R[2, 1] * F[2, 2] - F[1, 2];
            _Hr_R[0, 1] = F[0, 2] - _Hp_R[2, 0] * F[2, 2];
            _Hr_R[0, 2] = 0.0;
            _Hr_R[1, 0] = -_Hr_R[0, 1];
            _Hr_R[1, 1] = _Hr_R[0, 0];
            _Hr_R[1, 2] = 0.0;
            _Hr_R[2, 0] = 0.0;
            _Hr_R[2, 1] = 0.0;
            _Hr_R[2, 2] = 1.0;

            // We got H = Hr * Hp, assuming v3' = 0
            // p' = H * p
            // As H is projective transform, we need only to check corners and find minY among them
            var H = _Hr_R * _Hp_R;

            var p = new DenseVector(3);
            p[0] = 0.0;
            p[1] = 0.0;
            p[2] = 1.0;
            var pf = H * p;
            double minY = pf[1] / pf[2];

            p[0] = 0.0;
            p[1] = imgHeight - 1;
            pf = H * p;
            minY = Math.Min(pf[1] / pf[2], minY);

            p[0] = 0.0;
            p[1] = imgWidth - 1;
            pf = H * p;
            minY = Math.Min(pf[1] / pf[2], minY);

            p[0] = imgWidth - 1;
            p[1] = imgHeight - 1;
            pf = H * p;
            minY = Math.Min(pf[1] / pf[2], minY);

            double v3_R = -minY;
            _Hr_L[1, 2] += v3_R;
            _Hr_R[1, 2] += v3_R;
        }

        #endregion

        public void ComputeShearingMatrices(int imgWidth, int imgHeight)
        {
            _Hs_L = ComputeShearingMatrix(imgWidth, imgHeight, _Hr_L * _Hp_L);
            _Hs_R = ComputeShearingMatrix(imgWidth, imgHeight, _Hr_R * _Hp_R);
        }

        public Matrix<double> ComputeShearingMatrix(int imgWidth, int imgHeight, Matrix<double> H)
        {
            // 4 points : middles of image edges
            Vector<double> a = new DenseVector(new double[] { (imgWidth - 1) * 0.5, 0.0, 1.0 });
            Vector<double> b = new DenseVector(new double[] { imgWidth - 1, (imgHeight - 1) * 0.5, 1.0 });
            Vector<double> c = new DenseVector(new double[] { (imgWidth - 1) * 0.5, imgHeight - 1, 1.0 });
            Vector<double> d = new DenseVector(new double[] { 0.0, (imgHeight - 1) * 0.5, 1.0 });
            // Transform those points
            Vector<double> ar = H * a;
            Vector<double> br = H * b;
            Vector<double> cr = H * c;
            Vector<double> dr = H * d;
            // Scale so that weights are 1
            ar[0] = ar[0] / ar[2];
            ar[1] = ar[1] / ar[2];
            br[0] = br[0] / br[2];
            br[1] = br[1] / br[2];
            cr[0] = cr[0] / cr[2];
            cr[1] = cr[1] / cr[2];
            dr[0] = dr[0] / dr[2];
            dr[1] = dr[1] / dr[2];
            // 2 vectors corresponding to lines joining opposite ends of above points
            var x = br - dr;
            var y = cr - ar;

            double h = imgHeight;
            double w = imgWidth;

            // Result for Hs to : preserve x, y perpendicularity and their aspect ratio
            double s1 = (h * h * x[1] * x[1] + w * w * y[1] * y[1]) /
                (h * w * (x[1] * y[0] - x[0] * y[1]));
            double s2 = (h * h * x[1] * x[0] + w * w * y[1] * y[0]) /
                (h * w * (x[0] * y[1] - x[1] * y[0]));

            if(s1 < 0.0) // ??
            {
                s1 = -s1;
                s2 = -s2;
            }

            var Hs = DenseMatrix.CreateIdentity(3);
            Hs[0, 0] = s1;
            Hs[0, 1] = s2;
            return Hs;
        }
        
    }
}
