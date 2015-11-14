using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;

namespace CalibrationModule
{
    class CamCalibrator
    {
        public List<CalibrationPoint> Points { set; get; }
        public Matrix<float> CorrectedRealPoints { get; set; }
        public Matrix<float> CameraMatrix { private set; get; }
        public bool PerformRadialCorrection { get; set; }

        private Vector<float> _parameters;
        private Matrix<float> _normReal;
        private Matrix<float> _normImage;
        private Matrix<float> _realPoints;
        private Matrix<float> _imagePoints;
        private Matrix<float> _estimatedImagePoints;
        private Matrix<float> _estimatedRealPoints;

        public void Calibrate()
        {
            HomoPoints();

            NormalizeImagePoints();
            NormalizeRealPoints();

            // Matrix<float> equationsMat = new DenseMatrix(2 * Points.Count, 12);
            Matrix<float> A = new DenseMatrix(3 * Points.Count, 12);
            Vector<float> x = new DenseVector(3 * Points.Count);
            Vector<float> par;

            for (int p = 0; p < Points.Count; p++)
            {
                // Fill matrix A with point info
                //  equationsMat.SetRow(2 * p, new float[12] { 0, 0, 0, 0,
                //      -_imagePoints[p,2]*_realPoints[p,0], -_imagePoints[p,2]*_realPoints[p,1], -_imagePoints[p,2]*_realPoints[p,2], _imagePoints[p,2]*_realPoints[p,3],
                //      _imagePoints[p,1] * _realPoints[p,0], _imagePoints[p,1] * _realPoints[p,1], _imagePoints[p,1] * _realPoints[p,2], _imagePoints[p,1]*_realPoints[p,3] });
                //  equationsMat.SetRow(2 * p + 1, new float[12] {
                //      _imagePoints[p,2]* _realPoints[p,0], _imagePoints[p,2]*_realPoints[p,1], _imagePoints[p,2]*_realPoints[p,2], _imagePoints[p,2]*_realPoints[p,3],
                //      0, 0, 0, 0,
                //      -_imagePoints[p,0] * _realPoints[p,0], -_imagePoints[p,0] * _realPoints[p,1], -_imagePoints[p,0] * _realPoints[p,2], -_imagePoints[p,0]*_realPoints[p,3] });
                // ===============
                // Other idea : solve PX = x not x x PX = 0
                A.SetRow(3 * p, new float[12] { _realPoints[0, p], _realPoints[1, p], _realPoints[2, p], _realPoints[3, p], 0, 0, 0, 0, 0, 0, 0, 0 });
                A.SetRow(3 * p + 1, new float[12] { 0, 0, 0, 0,_realPoints[0, p], _realPoints[1, p], _realPoints[2, p], _realPoints[3, p], 0, 0, 0, 0 });
                A.SetRow(3 * p + 2, new float[12] { 0, 0, 0, 0, 0, 0, 0, 0,_realPoints[0, p], _realPoints[1, p], _realPoints[2, p], _realPoints[3, p] });
                x[3 * p] = _imagePoints[0, p];
                x[3 * p + 1] = _imagePoints[1, p];
                x[3 * p + 2] = _imagePoints[2, p];
            }

            CameraMatrix = new DenseMatrix(3, 4);
            //  MathNet.Numerics.LinearAlgebra.Factorization.Svd<float> svd = equationsMat.Svd();
            // CameraMatrix.SetRow(0, new float[4] { svd.VT[11, 0], svd.VT[11, 1], svd.VT[11, 2], svd.VT[11, 3] });
            // CameraMatrix.SetRow(1, new float[4] { svd.VT[11, 4], svd.VT[11, 5], svd.VT[11, 6], svd.VT[11, 7] });
            // CameraMatrix.SetRow(2, new float[4] { svd.VT[11, 8], svd.VT[11, 9], svd.VT[11, 10], svd.VT[11, 11] });

            // CheckEquations(equationsMat, svd.VT.Row(11));
            par = (A.Transpose().Multiply(A)).Inverse().Multiply(A.Transpose()).Multiply(x);
            CameraMatrix.SetRow(0, new float[4] { par[0], par[1], par[2], par[3] });
            CameraMatrix.SetRow(1, new float[4] { par[4], par[5], par[6], par[7] });
            CameraMatrix.SetRow(2, new float[4] { par[8], par[9], par[10], par[11] });
            //ComputePerformance();

           // _estimatedImagePoints = _imagePoints;
           // _estimatedRealPoints = _realPoints;

               MinimizeError();

            // Denormalize camera matrix
            CameraMatrix = _normImage.Inverse().Multiply(CameraMatrix.Multiply(_normReal));

            // Scale so that K(3,3) = 1
            //   var RQ = CameraMatrix.SubMatrix(0, 3, 0, 3).QR();
            //   float divCoeff = RQ.R[2, 2];
            //   CameraMatrix = CameraMatrix.Divide(divCoeff);
            //    CameraMatrix = CameraMatrix.Divide(CameraMatrix[2,3]);

            CorrectedRealPoints = _normReal.Inverse().Multiply(_estimatedRealPoints);
            ComputePerformance();
        }

        private void HomoPoints() // Create homonogeus points matrices form CalibrationPoint list
        {
            _realPoints = new DenseMatrix(4, Points.Count);
            _imagePoints = new DenseMatrix(3, Points.Count);
            for (int point = 0; point < Points.Count; point++)
            {
                _realPoints[0, point] = (float)Points[point].RealX;
                _realPoints[1, point] = (float)Points[point].RealY;
                _realPoints[2, point] = (float)Points[point].RealZ;
                _realPoints[3, point] = 1;
                _imagePoints[0, point] = (float)Points[point].ImgX;
                _imagePoints[1, point] = (float)Points[point].ImgY;
                _imagePoints[2, point] = 1;
            }
        }

        private void NormalizeRealPoints()
        {
            _normReal = new DenseMatrix(4, 4);
            // Compute center of real grid
            float xc = 0, yc = 0, zc = 0;
            foreach (var point in Points)
            {
                xc += point.RealX;
                yc += point.RealY;
                zc += point.RealZ;
            }
            xc /= Points.Count;
            yc /= Points.Count;
            zc /= Points.Count;
            // Get mean distance of points from center
            float dist = 0;
            foreach (var point in Points)
            {
                dist += (float)Math.Sqrt((point.RealX - xc) * (point.RealX - xc) +
                    (point.RealY - yc) * (point.RealY - yc) + (point.RealZ - zc) * (point.RealZ - zc));
            }
            dist /= Points.Count;
            // Normalize in a way that mean dist = sqrt(3)
            float ratio = (float)Math.Sqrt(3) / dist;
            // Noramlize matrix - homonogeus point must be multiplied by it
            _normReal[0, 0] = ratio;
            _normReal[1, 1] = ratio;
            _normReal[2, 2] = ratio;
            _normReal[0, 3] = -ratio * xc;
            _normReal[1, 3] = -ratio * yc;
            _normReal[2, 3] = -ratio * zc;
            _normReal[3, 3] = 1;
            // Normalize points
            _realPoints = _normReal.Multiply(_realPoints);
            for(int p = 0; p < _realPoints.ColumnCount; p++)
            {
                _realPoints[0,p] = _realPoints[0,p] / _realPoints[3,p];
                _realPoints[1,p] = _realPoints[1,p] / _realPoints[3,p];
                _realPoints[2,p] = _realPoints[2,p] / _realPoints[3,p];
                _realPoints[3,p] = 1;
            }
        }

        private void NormalizeImagePoints()
        {
            _normImage = new DenseMatrix(3, 3);
            // Compute center of image points
            float xc = 0, yc = 0;
            foreach (var point in Points)
            {
                xc += point.ImgX;
                yc += point.ImgY;
            }
            xc /= Points.Count;
            yc /= Points.Count;
            // Get mean distance of points from center
            float dist = 0;
            foreach (var point in Points)
            {
                dist += (float)Math.Sqrt((point.ImgX - xc) * (point.ImgX - xc) +
                    (point.ImgY - yc) * (point.ImgY - yc));
            }
            dist /= Points.Count;
            // Normalize in a way that mean dist = sqrt(2)
            float ratio = (float)Math.Sqrt(2) / dist;
            // Noramlize matrix - homonogeus point must be multiplied by it
            _normImage[0, 0] = ratio;
            _normImage[1, 1] = ratio;
            _normImage[0, 2] = -ratio * xc;
            _normImage[1, 2] = -ratio * yc;
            _normImage[2, 2] = 1;

            _imagePoints = _normImage.Multiply(_imagePoints);
            for (int p = 0; p < _realPoints.ColumnCount; p++)
            {
                _imagePoints[0, p] = _imagePoints[0, p] / _imagePoints[2, p];
                _imagePoints[1, p] = _imagePoints[1, p] / _imagePoints[2, p];
                _imagePoints[2, p] = 1;
            }
        }

        // Minimize error in image and real points
        // min(a*d(x,x')^2 + b*d(X,X')^2)
        // a = 1/2px^2, b = 1/1mm^2
        // x' = PX'
        // Parameters are elements of P and X'
        private void MinimizeError()
        {
            // Initliaze estimated real points matrix and parameter vector
            _estimatedRealPoints = _realPoints.Clone();
            _estimatedImagePoints = new DenseMatrix(_imagePoints.RowCount, _imagePoints.ColumnCount);
            _parameters = new DenseVector(12);
            float error = 1;
            // points are normalized, + additial weights are (a,b) are introduced -> error equal 1 is 
            // near the mean distance of points from center of all points ( so about half the image / 3d scene )
            // if images are 640x480 and scene is 3 perpendicular A4 papers, so 140x290x140mm
            // Then for image error < 0.5px and reals error < 1mm, min error could be set to
            // for image: 0.5/277 = 0,018 and for reals: 0,011, so overall error could be set to 0.03
            float minError = 0.01f;
            int iteration = 0;
            int maxIterations = 5000;

            for (int p = 0; p < 12; p++)
            {
                _parameters[p] = CameraMatrix[p / 4, p % 4];
            }

            while (error > minError && iteration < maxIterations)
            {
               // _estimatedImagePoints = new DenseMatrix(_imagePoints.RowCount, _imagePoints.ColumnCount);
                // for each point compute its new estimation
                for (int p = 0; p < Points.Count; p++)
               {
                    // x' = P11*X' + P12*Y' + P13*Z' + P14 (W'=1)
                    // y' = P21*X' + P22*Y' + P23*Z' + P24 (W'=1)
                    // w' = P31*X' + P32*Y' + P33*Z' + P34 (W'=1)
                    Vector<float> Xp = _estimatedRealPoints.Column(p);
                    _estimatedImagePoints[2, p] = CameraMatrix.Row(2) * Xp;
                    _estimatedImagePoints[0, p] = (CameraMatrix.Row(0) * Xp / _estimatedImagePoints[2, p]);
                    _estimatedImagePoints[1, p] = (CameraMatrix.Row(1) * Xp / _estimatedImagePoints[2, p]);
                    _estimatedImagePoints[2, p] = 1;
                }
                Vector<float> errVec = ComputeErrorFunction();
                error = errVec.Sum();

                Matrix<float> grad = ComputeRealPointsGradient();
                Matrix<float> hess = ComputeRealPointsHessian();
                float coeff = 0.01f;
                for (int p = 0; p < Points.Count; p++)
                {
                    Matrix<float> H = new DenseMatrix(4,4);
                    H.SetRow(0, hess.Row(p).SubVector(0, 4));
                    H.SetRow(1, hess.Row(p).SubVector(1, 4));
                    H.SetRow(2, hess.Row(p).SubVector(2, 4));
                    H.SetRow(3, hess.Row(p).SubVector(3, 4));

                    Vector<float> dX = -H.Inverse().Multiply(grad.Row(p));
                    _estimatedRealPoints[0, p] += coeff * dX[0];
                    _estimatedRealPoints[1, p] += coeff * dX[1];
                    _estimatedRealPoints[2, p] += coeff *  dX[2];
                }

                //_parameters += correction;
                // Compute P again

                Matrix<float> A = new DenseMatrix(3 * Points.Count, 12);
                Vector<float> x = new DenseVector(3 * Points.Count);
                Vector<float> par;

                for (int p = 0; p < Points.Count; p++)
                {
                    A.SetRow(3 * p, new float[12] { _estimatedRealPoints[0, p], _estimatedRealPoints[1, p], _estimatedRealPoints[2, p], _estimatedRealPoints[3, p], 0, 0, 0, 0, 0, 0, 0, 0 });
                    A.SetRow(3 * p + 1, new float[12] { 0, 0, 0, 0, _estimatedRealPoints[0, p], _estimatedRealPoints[1, p], _estimatedRealPoints[2, p], _estimatedRealPoints[3, p], 0, 0, 0, 0 });
                    A.SetRow(3 * p + 2, new float[12] { 0, 0, 0, 0, 0, 0, 0, 0, _estimatedRealPoints[0, p], _estimatedRealPoints[1, p], _estimatedRealPoints[2, p], _estimatedRealPoints[3, p] });
                    x[3 * p] = _imagePoints[0, p];
                    x[3 * p + 1] = _imagePoints[1, p];
                    x[3 * p + 2] = _imagePoints[2, p];
                }

                CameraMatrix = new DenseMatrix(3, 4);
                par = (A.Transpose().Multiply(A)).Inverse().Multiply(A.Transpose()).Multiply(x);
                CameraMatrix.SetRow(0, new float[4] { par[0], par[1], par[2], par[3] });
                CameraMatrix.SetRow(1, new float[4] { par[4], par[5], par[6], par[7] });
                CameraMatrix.SetRow(2, new float[4] { par[8], par[9], par[10], par[11] });

                iteration++;
            }
        }

        // ad(x,x')2 + bd(X,X')2
        // d(x,x') = x^T * x
        // ============
        // Zmiana : błąd miedzy PX'=x' , a punktami kalibracyjnymi na  ( e = |PX'-x| )
        private Vector<float> ComputeErrorFunction()
        {
            Vector<float> error = new DenseVector(Points.Count);
            //float a = 1 / (float)Math.Sqrt(2), b = 1 / (float)Math.Sqrt(3);

            for(int p = 0; p < Points.Count; p++)
            {
                Vector<float> homoxp = _estimatedImagePoints.Column(p);
                Vector<float> homox = _imagePoints.Column(p);
                error[p] = (float)(new DenseVector(new float[2] {
                    homoxp[0]/homoxp[2] - homox[0]/homox[2],
                    homoxp[1]/homoxp[2] - homox[1]/homox[2] })).Norm(2);
            }

            // Extract vectors of differences x-xp,y-yp,w-wp
           // Vector<float> xxp = _imagePoints.Column(0) - _estimatedImagePoints.Column(0);
           // Vector<float> yyp = _imagePoints.Column(1) - _estimatedImagePoints.Column(1);
           // Vector<float> wwp = _imagePoints.Column(2) - _estimatedImagePoints.Column(2);
           // // It's dot products equals distances (x-xp)^2 ...
           // float dx = xxp.DotProduct(xxp);
           // float dy = yyp.DotProduct(yyp);
           // float dw = wwp.DotProduct(wwp);
           // error += a * (dx + dy + dw);
           // Same for d(X, X')
           // Vector < float > XXp = _realPoints.Column(0) - _estimatedRealPoints.Column(0);
           //Vector < float > YYp = _realPoints.Column(1) - _estimatedRealPoints.Column(1);
           //Vector < float > ZZp = _realPoints.Column(2) - _estimatedRealPoints.Column(2);

           // float dX = XXp.DotProduct(XXp);
           // float dY = YYp.DotProduct(YYp);
           // float dZ = ZZp.DotProduct(ZZp);
           // error += b * (dX + dY + dZ);

            return error;
        }

        private Vector<float> ComputeErrorJacobian()
        {
            Vector<float> J = new DenseVector(12);

            for (int p = 0; p < Points.Count; p++)
            {
                float x = _imagePoints[p, 0];
                float y = _imagePoints[p, 1];
                float w = _imagePoints[p, 2];
                float X = _estimatedRealPoints[p, 0];
                float Y = _estimatedRealPoints[p, 1];
                float Z = _estimatedRealPoints[p, 2];
                // de / dp11 = 2(2p11*X^2 - (x-p12*Y-p13*Z-p14))(x-p11*X-p12*Y-p13*Z-p14)
                J[0] += 2 * (2 * _parameters[0] * X * X - X * (x - _parameters[1] * Y - _parameters[2] * Z - _parameters[3])) * (x - _estimatedImagePoints[p, 0]);
                J[1] += 2 * (2 * _parameters[1] * Y * Y - Y * (x - _parameters[0] * X - _parameters[2] * Z - _parameters[3])) * (x - _estimatedImagePoints[p, 0]);
                J[2] += 2 * (2 * _parameters[2] * Z * Z - Z * (x - _parameters[0] * X - _parameters[1] * Y - _parameters[3])) * (x - _estimatedImagePoints[p, 0]);
                J[3] += 2 * (2 * _parameters[3] - (x - _parameters[0] * X - _parameters[1] * Y - _parameters[2] * Z)) * (x - _estimatedImagePoints[p, 0]);

                J[4] += 2 * (2 * _parameters[4] * X * X - X * (y - _parameters[5] * Y - _parameters[6] * Z - _parameters[7])) * (y - _estimatedImagePoints[p, 1]);
                J[5] += 2 * (2 * _parameters[5] * Y * Y - Y * (y - _parameters[4] * X - _parameters[6] * Z - _parameters[7])) * (y - _estimatedImagePoints[p, 1]);
                J[6] += 2 * (2 * _parameters[6] * Z * Z - Z * (y - _parameters[4] * X - _parameters[5] * Y - _parameters[7])) * (y - _estimatedImagePoints[p, 1]);
                J[7] += 2 * (2 * _parameters[7] - (y - _parameters[4] * X - _parameters[5] * Y - _parameters[6] * Z)) * (y - _estimatedImagePoints[p, 1]);

                J[8] += 2 * (2 * _parameters[8] * X * X - X * (1 - _parameters[9] * Y - _parameters[10] * Z - _parameters[11])) * (1 - _estimatedImagePoints[p, 2]);
                J[9] += 2 * (2 * _parameters[9] * Y * Y - Y * (1 - _parameters[8] * X - _parameters[10] * Z - _parameters[11])) * (1 - _estimatedImagePoints[p, 2]);
                J[10] += 2 * (2 * _parameters[10] * Z * Z - Z * (1 - _parameters[8] * X - _parameters[9] * Y - _parameters[11])) * (1 - _estimatedImagePoints[p, 2]);
                J[11] += 2 * (2 * _parameters[11] - (1 - _parameters[8] * X - _parameters[9] * Y - _parameters[10] * Z)) * (1 - _estimatedImagePoints[p, 2]);
            }

            J /= Points.Count;

            return J;
        }

        private Matrix<float> ComputeRealPointsGradient()
        {
            Matrix<float> RG = new DenseMatrix(Points.Count, 4);

            for (int p = 0; p < Points.Count; p++)
            {
                float x = _imagePoints[0, p];
                float y = _imagePoints[1, p];
                float w = _imagePoints[2, p];
                float ex = _estimatedImagePoints[0, p];
                float ey = _estimatedImagePoints[1, p];
                float ew = _estimatedImagePoints[2, p];
                
                for (int i = 0; i < 4; i++)
                {
                    RG[p, i] = -2 * (CameraMatrix[0, i] * (x - ex) +
                        CameraMatrix[1, i] * (y - ey) +
                        CameraMatrix[2, i] * (w - ew));
                }

            }

            return RG;
        }

        private Matrix<float> ComputeRealPointsHessian()
        {
            Matrix<float> H = new DenseMatrix(Points.Count, 16);

            for (int p = 0; p < Points.Count; p++)
            {
                for(int i = 0; i < 4; i++)
                {
                    for(int j = 0; j < 4; j++)
                    {
                        H[p, 4 * i + j] = 2 * (CameraMatrix[0, i] * CameraMatrix[0, j] +
                            CameraMatrix[1, i] * CameraMatrix[1, j] +
                            CameraMatrix[2, i] * CameraMatrix[2, j]);

                    }
                }
            }

            return H;
        }

        public void ComputePerformance()
        {
            // first get pseudo-inverse of P
            Matrix<float> pinvP = CameraMatrix.Transpose().Multiply((CameraMatrix.Multiply(CameraMatrix.Transpose())).Inverse());
            // get camera center
            Vector<float> center = -(CameraMatrix.SubMatrix(0, 3, 0, 3).Inverse().Multiply(CameraMatrix.SubMatrix(0, 3, 3, 1))).Column(0);

            // find center as null-vector of P ( should be same as above )
            var svd = CameraMatrix.Svd();


            float errorImg = 0.0f; // d(x,PX)
            float errorReal = 0.0f; // d(X,P'X)

            // for each point find back-projecting ray : (P'x - C)k + C and find distance of X from this ray
            foreach(var cpoint in Points)
            {
                //  
                // Matrix<float> Px = pinvP.Multiply(img); // real point P'x (homo column 4-vector)
                // Vector<float> X = new DenseVector(new float[] { Px[0, 0]/Px[3, 0], Px[1, 0] / Px[3, 0], Px[2, 0] / Px[3, 0] }); // back-projected point in euclid space
                // Vector<float> v = X - center; // direction vector of back-projected ray
                //Vector<float> R = new DenseVector(new float[] { cpoint.RealX, cpoint.RealY, cpoint.RealZ }); // expected real point
                // k: coeff as above such that point is projection of R onto b-p ray
                // k = v1(r1-c1)+v2(r2-c2)+v3(r3-c3)/2(v1^2+v2^2+v3^2)
                // float k = (v[0] * (R[0] - center[0]) + v[1] * (R[1] - center[1]) + v[2] * (R[2] - center[2])) / (2 * (v[0] * v[0] + v[1] * v[1] + v[2] * v[2]));
                // squared distance from R to ray
                //  float d2 = 0.0f;
                //  for (int i = 0; i < 3; i++)
                //      d2 += (float)Math.Pow(v[i]*k + center[i] - R[i], 2);
                //  errorReal += d2 / (float)R.Norm(2);

                Matrix<float> img = new DenseMatrix(3, 1);
                img.SetColumn(0, new float[] { cpoint.ImgX, cpoint.ImgY, 1 });
                // now find d(x,PX)
                Matrix<float> homoX = new DenseMatrix(4, 1);
                homoX.SetColumn(0, new float[] { cpoint.RealX, cpoint.RealY, cpoint.RealZ, 1 });
                Matrix<float> x = CameraMatrix.Multiply(homoX);
                x[0, 0] /= x[2, 0];
                x[1, 0] /= x[2, 0];
                float d2 = (float)(x.Column(0) - img.Column(0)).Norm(2);
                errorImg += d2 / (float)img.Column(0).Norm(2);
            }
        }

        public void CheckEquations(Matrix<float> eqMat, Vector<float> paramVec)
        {
            float errRes = 0.0f;
            //float errRelMean = 0.0f;
            float errTot = 0.0f; // sum(yi-mean(yi))
            Vector<float> meanVec = new DenseVector(12);
            Vector<float> varVec = new DenseVector(12);
            for (int p = 0; p < eqMat.RowCount; p++)
            {
                float err = eqMat.Row(p).DotProduct(paramVec);
                meanVec += eqMat.Row(p).PointwiseMultiply(eqMat.Row(p));

               // float eqNorm = (float)eqMat.Row(p).Norm(2);
               // float errRel = err * err / eqNorm;
               // errRelMean += errRel;
                errRes += err*err;
            }
            meanVec /= eqMat.RowCount;

            for (int p = 0; p < eqMat.RowCount; p++)
            {
                varVec = eqMat.Row(0) - meanVec;
                errTot += (float)varVec.Norm(2);
            }
            float error = errRes / errTot;
        }
    }
}
