using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace CamCore
{
    // Computes best fit 3D point based on 2 matching points on images
    // made by 2 calibrated cameras (their P, P', F is needed)
    public class TwoPointsTriangulation
    {
        public Matrix<double> Fundamental { get; set; }

        public Matrix<double> CameraLeft { get; set; }
        public Matrix<double> CameraRight { get; set; }

        public Vector<double> EpipoleLeft { get; set; }
        public Vector<double> EpipoleRight { get; set; }

        public List<Vector<double>> PointsLeft { get; set; }
        public List<Vector<double>> PointsRight { get; set; }

        public List<Vector<double>> Points3D { get; protected set; }

        public Vector<double> _pL;
        public Vector<double> _pR;
        public Vector<double> _p3D;

        private Matrix<double> _T_L = DenseMatrix.CreateIdentity(3);
        private Matrix<double> _T_R = DenseMatrix.CreateIdentity(3);
        private Matrix<double> _Tinv_L = DenseMatrix.CreateIdentity(3);
        private Matrix<double> _Tinv_R = DenseMatrix.CreateIdentity(3);
        private Matrix<double> _F_T;
        private Vector<double> _e_T_L = new DenseVector(3);
        private Vector<double> _e_T_R = new DenseVector(3);
        private Matrix<double> _R_L = DenseMatrix.CreateIdentity(3);
        private Matrix<double> _R_R = DenseMatrix.CreateIdentity(3);
        private Matrix<double> _Rinv_L = DenseMatrix.CreateIdentity(3);
        private Matrix<double> _Rinv_R = DenseMatrix.CreateIdentity(3);
        private Matrix<double> _F_TR;
        private double _tParam;

        public bool UseLinearEstimationOnly { get; set; } = false;

        public void Estimate3DPoints()
        {
            Points3D = new List<Vector<double>>(PointsLeft.Count);
            for(int p = 0; p < PointsLeft.Count; ++p)
            {
                _pL = PointsLeft[p];
                _pL = _pL / _pL[2]; // Ensure w = 1

                _pR = PointsRight[p];
                _pR = _pR / _pR[2];

                if(UseLinearEstimationOnly)
                    ComputeBackprojected3DPoint();
                else
                    Estimate3DPoint();

                Points3D.Add(_p3D);
            }
        }

        public void Estimate3DPoint()
        {
            ComputeTransformToOriginMatrices();
            ComputeTransformedFundamental();
            ComputeNormalisedTransformedEpipoles();
            ComputeRotationMatrices();
            ComputeRotatedFundamental();
            // Fundamental should have form:
            //     | f*f'*d -f'*c -f'*d |
            // F = |   -f*b     a     b |
            //     |   -f*d     c     d |
            FindMinimalErrorEpipolarLines();
            FindMinimalErrorPoints();
            TransformEstimatedImagePointsBack();
            ComputeBackprojected3DPoint();
        }

        public void ComputeTransformToOriginMatrices()
        {
            // 1) Define transform matrices, which take points to origin
            //     |1    -x|      |1    -x'|
            // T = |   1 -y| T' = |   1 -y'|
            //     |      1|      |       1|
            _T_L[0, 2] = -_pL[0];
            _T_L[1, 2] = -_pL[1];
            _Tinv_L[0, 2] = _pL[0];
            _Tinv_L[1, 2] = _pL[1];

            _T_R[0, 2] = -_pR[0];
            _T_R[1, 2] = -_pR[1];
            _Tinv_R[0, 2] = _pR[0];
            _Tinv_R[1, 2] = _pR[1];
        }

        public void ComputeTransformedFundamental()
        {
            // 2) Fundamental matrix resulting from appying this transforms
            // over 2 images is equalt to:
            // F_new = T'^-T * F * T^-1
            _F_T = _Tinv_R.Transpose() * Fundamental * _Tinv_L;
        }

        public void ComputeNormalisedTransformedEpipoles()
        {
            // 3) Compute new e and e' such that e'^T*F = 0 and F*e = 0
            // Normalise them so that || e || == || e' || == 1
            // Of course e_new = T*e_old
            _e_T_L = _T_L * EpipoleLeft;
            _e_T_R = _T_R * EpipoleRight;

            double scale = 1.0 / Math.Sqrt(_e_T_L[0] * _e_T_L[0] + _e_T_L[1] * _e_T_L[1]);
            _e_T_L.MultiplyThis(scale);

            scale = 1.0 / Math.Sqrt(_e_T_R[0] * _e_T_R[0] + _e_T_R[1] * _e_T_R[1]);
            _e_T_R.MultiplyThis(scale);
        }

        public void ComputeRotationMatrices()
        {
            // 4) Compute rotation matrices R,R' which will take epipoles to points (rotation is around origin, so points are not moved)
            // e_rot = (1,0,e.w), e_rot' = (1,0,e'.w)
            //               | e.x  e.y  0||e.x| (will hold as ||e||=1)
            // e_rot = R*e = |-e.y  e.x  0||e.y| (same for e')
            //               |   0    0  1||e.w|
            _R_L[0, 0] = _e_T_L[0];
            _R_L[0, 1] = _e_T_L[1];
            _R_L[1, 0] = -_e_T_L[1];
            _R_L[1, 1] = _e_T_L[0];

            _Rinv_L[0, 0] = _e_T_L[0];
            _Rinv_L[0, 1] = -_e_T_L[1];
            _Rinv_L[1, 0] = _e_T_L[1];
            _Rinv_L[1, 1] = _e_T_L[0];

            _R_R[0, 0] = _e_T_R[0];
            _R_R[0, 1] = _e_T_R[1];
            _R_R[1, 0] = -_e_T_R[1];
            _R_R[1, 1] = _e_T_R[0];

            _Rinv_R[0, 0] = _e_T_R[0];
            _Rinv_R[0, 1] = _e_T_R[1];
            _Rinv_R[1, 0] = -_e_T_R[1];
            _Rinv_R[1, 1] = _e_T_R[0];
        }

        public void ComputeRotatedFundamental()
        {
            // 5) Replace F by R'*F*R^T
            _F_TR = _R_R * _F_T * _Rinv_L;
        }

        public void FindMinimalErrorPoints()
        {
            // 10) Having t, we can find closest points to origin on this lines
            // For line l = (a,b,c), closes point is p = (-ac, -bc, a^2 + b^2)
            //
            // We have lines:
            // l = (tf, 1, -t), 
            // l' = (-f'(ct+d), at+b, ct+d),
            // Where : f = e.w, f' = e'.w, a = F22, b = F23, c = F32, d = F33
            double la = _tParam * _e_T_L[2];
            double lb = 1.0;
            double lc = -_tParam;
            double w = 1.0 / (la * la + lb * lb);

            _pL = new DenseVector(3);
            _pL[0] = -la * lc * w;
            _pL[1] = -lb * lc * w;
            _pL[2] = 1.0;

            la = -_e_T_R[2] * (_F_TR[2, 1] * _tParam + _F_TR[2, 2]);
            lb = _F_TR[1, 1] * _tParam + _F_TR[1, 2];
            lc = _F_TR[2, 1] * _tParam + _F_TR[2, 2];
            w = 1.0 / (la * la + lb * lb);

            _pR = new DenseVector(3);
            _pR[0] = -la * lc * w;
            _pR[1] = -lb * lc * w;
            _pR[2] = 1.0;
        }

        public void FindMinimalErrorEpipolarLines()
        {
            // 7) Now find 2 corresponding epilines, which are best fit for our 2 points
            // Our epipoles are at e=(1,0,f)^T and e'=(1,0,f')^T
            // Let l(t) ne epiline trough point z=(0,t,1)^T and e
            // l = (0,t,1)x(1,0,f) = (tf, 1, -t)
            // Distance to origin (and so our transformed point):
            // d(x,l(t))^2 = t^2 / (1+(tf)^2)
            //
            // 8) Find corresponding epiline l'(t)
            // l' = F*z = F*(0,t,1)^T = (-f'(ct+d), at+b, ct+d)^T
            // d(x',l'(t))^2 = (ct + d)^2 / (at+b)^2 + f'^2(ct+d)^2
            // 
            // 9) Let s(t) = d^2 + d'^2 be error to minimise
            // Find t that minimises it : find zeros of s'(t)
            // s'(t) after removing common denominator :
            // g(t) = t((at+b)^2+f'^2(ct+d)^2)^2 - (ad-bc)(ct+d)(at+b)(1+(ft)^2)^2
            // Which gives us polynomial:
            // g(t) = 
            //

            int rank = 6;
            Matrix<float> estimationMatrix = new MathNet.Numerics.LinearAlgebra.Single.DenseMatrix(rank + 1, 2);
            float r1 = 1.0f / rank;
            for(int r = 0; r < rank + 1; ++r)
            {
                float z = 1.0f + r1 * r;
                estimationMatrix.At(r, 0, z);
                estimationMatrix.At(r, 1, (float)ComputeEpilineFitCostDerivative(z));
            }
            Polynomial costDiffPoly = Polynomial.EstimatePolynomial(estimationMatrix, rank);

            // We need monic polynomial for RootFinder, but multipliyng by sclar doesn't change actual zeros
            // of polynomial, so we can divide all coeffs by c0
            float c0 = 1.0f / costDiffPoly.Coefficents.At(0);
            for(int i = 0; i < costDiffPoly.Coefficents.Count; ++i)
                costDiffPoly.Coefficents.At(i, costDiffPoly.Coefficents.At(i) * c0);

            //costDiffPoly = FindCostPolynomial();

            //c0 = 1.0f / costDiffPoly.Coefficents.At(0);
            //for(int i = 0; i < costDiffPoly.Coefficents.Count; ++i)
            //    costDiffPoly.Coefficents.At(i, costDiffPoly.Coefficents.At(i) * c0);

            PolynomialRootFinder rootFinder = new PolynomialRootFinder();
            rootFinder.Poly = costDiffPoly;
            rootFinder.Process();
            var roots = rootFinder.RealRoots;

            // Roots are potentialy minimising t, so check every one
            _tParam = roots[0];
            double minCost = ComputeEpilineFitCost(_tParam);
            for(int i = 1; i < roots.Count; ++i)
            {
                double cost = ComputeEpilineFitCost(roots[i]);
                if(cost < minCost)
                {
                    minCost = cost;
                    _tParam = roots[i];
                }
            }

            // Check also for t = inf
            double costinf = ComputeEpilineFitCost_Inf();
            if(costinf < minCost)
            {
                minCost = costinf;
                _tParam = double.PositiveInfinity;
            }
        }

        public double ComputeEpilineFitCost(double t)
        {
            // Distance to origin (and so our transformed point):
            // d(x,l(t))^2 = t^2 / (1+(tf)^2)
            // d(x',l'(t))^2 = (ct + d)^2 / (at+b)^2 + f'^2(ct+d)^2
            // e = d^2 + d'^2
            // Where : f = e.w, f' = e'.w, a = F22, b = F23, c = F32, d = F33
            double ctd2 = (_F_TR[2, 1] * t + _F_TR[2, 2]) * (_F_TR[2, 1] * t + _F_TR[2, 2]);
            double atb = _F_TR[1, 1] * t + _F_TR[1, 2];
            return (t * t / (1.0 + t * t * _e_T_L[2] * _e_T_L[2])) +
                (ctd2 / (atb * atb + _e_T_R[2] * _e_T_R[2] * ctd2));
        }

        public double ComputeEpilineFitCost_Inf()
        {
            // d(x,l(t))^2 = t^2 / (1+(tf)^2) -> for t = inf : d = 1/f^2
            // d(x',l'(t))^2 = (ct + d)^2 / (at+b)^2 + f'^2(ct+d)^2 -> for t = inf : 1 / ( (a/c)^2  + f'^2 )
            return 1.0 / (_e_T_L[2] * _e_T_L[2]) + 
                1.0 / ((_F_TR[1, 1]* _F_TR[1, 1]) / (_F_TR[2, 2]* _F_TR[2, 2]) + _e_T_R[2] * _e_T_R[2]);
        }

        public double ComputeEpilineFitCostDerivative(double t)
        {
            // s' = 0 <=> g = 0
            // g(t) = t((at+b)^2 + f'^2(ct+d)^2) - (ad-bc)(1+(ft)^2)^2(at+b)(ct+d)
            double ctd = _F_TR[2, 1] * t + _F_TR[2, 2];
            double atb = _F_TR[1, 1] * t + _F_TR[1, 2];
            double ft2 = 1.0 + t * t * _e_T_L[2] * _e_T_L[2];
            return t * (atb * atb + _e_T_R[2] * _e_T_R[2] * ctd * ctd) +
                (_F_TR[1, 2] * _F_TR[2, 1] - _F_TR[1, 1] * _F_TR[2, 2])
                * ft2 * ft2 * atb * ctd;
        }

        public Polynomial FindCostPolynomial()
        {
            Polynomial poly = new Polynomial();
            poly.Rank = 6;
            poly.Coefficents = new MathNet.Numerics.LinearAlgebra.Single.DenseVector(7);

            double a = _F_TR[1, 1];
            double a_2 = a * a;
            double b = _F_TR[1, 2];
            double b_2 = b * b;
            double c = _F_TR[2, 1];
            double c_2 = c * c;
            double d = _F_TR[2, 2];
            double d_2 = d * d;
            double f1 = _e_T_L[2];
            double f2 = _e_T_R[2];
            double f1_2 = f1 * f1;
            double f1_4 = f1_2 * f1_2;
            double f2_2 = f2 * f2;
            double f2_4 = f2_2 * f2_2;

            // (-d*a^2*c*f1^4 + b*a*c^2*f1^4)*t^6 -> f1^4*a*c(bc-da)
            poly.Coefficents[0] = (float)(f1_4 * a * c * (b * c - d * a));

            // (a^4 + 2*a^2*c^2*f2^2 - a^2*d^2*f1^4 + b^2*c^2*f1^4 + c^4*f2^4)*t^5 ->
            // (a^2 + c^2*f2^2)^2 + f1^4(b^2*c^2 - a^2*d^2)
            poly.Coefficents[1] = (float)((a_2 + c_2 * f2_2) * (a_2 + c_2 * f2_2) + f1_4 * (b_2 * c_2 - a_2 * d_2));

            // (4*a^3*b - 2*a^2*c*d*f1^2 + 4*a^2*c*d*f2^2 + 2*a*b*c^2*f1^2 + 4*a*b*c^2*f2^2 - a*b*d^2*f1^4 + b^2*c*d*f1^4 + 4*c^3*d*f2^4)*t^4 ->
            // (4*(a^3*b + c^3*d*f2^4) + 2*a^2*c*d*(2*f2^2 - f1^2) + 2*a*b*c^2*(f1^2 + 2*f2^2) + b*d*f1^4*(bc-ad))
            poly.Coefficents[2] = (float)(4.0 * (a_2 * a * b + c_2 * c * d * f2_4) +
                                          2.0 * a_2 * c * d * (2.0 * f2_2 - f1_2) +
                                          2.0 * a * b * c_2 * (2.0 * f2_2 + f1_2) +
                                          b * d * f1_4 * (b * c - a * d));

            // (6*a^2*b^2 - 2*a^2*d^2*f1^2 + 2*a^2*d^2*f2^2 + 8*a*b*c*d*f2^2 + 2*b^2*c^2*f1^2 + 2*b^2*c^2*f2^2 + 6*c^2*d^2*f2^4)*t^3 ->
            // 6*(a^2*b^2 + c^2*d^2*f2^4) + 2*a^2*d^2*(f2^2 - f1^2) + 2*b^2*c^2*(f1^2 + f2^2) + 8*a*b*c*d*f2^2
            poly.Coefficents[3] = (float)(6.0 * (a_2 * b_2 + c_2 * d_2 * f2_4) +
                                          2.0 * (a_2 * d_2 * (f2_2 - f1_2) + b_2 * c_2 * (f2_2 + f1_2)) +
                                          8.0 * (a * b * c * d * f2_2));

            // (-a^2*c*d + 4*a*b^3 + a*b*c^2 - 2*a*b*d^2*f1^2 + 4*a*b*d^2*f2^2 + 2*b^2*c*d*f1^2 + 4*b^2*c*d*f2^2 + 4*c*d^3*f2^4)*t^2
            poly.Coefficents[4] = (float)(-a_2 * c * d + 4.0 * a * b * b_2 + a * b * c_2 -
                                          2.0 * a * b * d_2 * f1_2 + 4.0 * a * b * d_2 * f2_2 +
                                          2.0 * b_2 * c * d * f1_2 + 4.0 * b_2 * c * d * f2_2 +
                                          4.0 * c * d * d_2 * f2_4);

            // (- a^2*d^2 + b^4 + b^2*c^2 + 2*b^2*d^2*f2^2 + d^4*f2^4)*t -> ( (b^2 + d^2*f2^2)^2 + (b^2*c^2 - a^2*d^2) )
            poly.Coefficents[5] = (float)((b_2 + d_2 * f2_2) * (b_2 + d_2 * f2_2) + (b_2 * c_2 - a_2 * d_2));

            // c*b^2*d - a*b*d^2
            poly.Coefficents[6] = (float)(c * b_2 * d - a * b * d_2);

            return poly;
        }

        public void TransformEstimatedImagePointsBack()
        {
            // 11) Transfer computed p and p' back to original coordinates
            // p_org = T^-1 * R^T * p, p_org' = T'^-1 * R'^T * p'
            _pL = _Tinv_L * _Rinv_L * _pL;
            _pR = _Tinv_R * _Rinv_R * _pR;
        }

        public void ComputeBackprojected3DPoint()
        {
            // 12) Rays from estimated points should intersect in 3D now, so
            // find this 3D point (vai linear method) :
            // From x = PX, x' = P'X we have AX = 0 ( x x PX = 0 stacked )
            //     |    xp3 - p1 |
            //     |    yp3 - p2 |
            // A = | x'p3' - p1' |
            //     | y'p3' - p2' |
            // Where pi is i-th row of P
            // Of course X is obtained by solving AX = 0, as X is defined up to scale
            // so we can impose ||X|| = 1 and use SvdZeroSolver
            // Then scale X so that w = 1
            //
            // (x,y,1) = _pL, (x',y',1) = _pR
            // p1 = [P11 P12 P13 P14]
            // p2 = [P21 P22 P23 P24]
            // p3 = [P31 P32 P33 P34]
            Matrix<double> A = new DenseMatrix(4, 4);
            A[0, 0] = _pL[0] * CameraLeft[2, 0] - CameraLeft[0, 0];
            A[0, 1] = _pL[0] * CameraLeft[2, 1] - CameraLeft[0, 1];
            A[0, 2] = _pL[0] * CameraLeft[2, 2] - CameraLeft[0, 2];
            A[0, 3] = _pL[0] * CameraLeft[2, 3] - CameraLeft[0, 3];

            A[1, 0] = _pL[1] * CameraLeft[2, 0] - CameraLeft[1, 0];
            A[1, 1] = _pL[1] * CameraLeft[2, 1] - CameraLeft[1, 1];
            A[1, 2] = _pL[1] * CameraLeft[2, 2] - CameraLeft[1, 2];
            A[1, 3] = _pL[1] * CameraLeft[2, 3] - CameraLeft[1, 3];

            A[2, 0] = _pR[0] * CameraRight[2, 0] - CameraRight[0, 0];
            A[2, 1] = _pR[0] * CameraRight[2, 1] - CameraRight[0, 1];
            A[2, 2] = _pR[0] * CameraRight[2, 2] - CameraRight[0, 2];
            A[2, 3] = _pR[0] * CameraRight[2, 3] - CameraRight[0, 3];

            A[3, 0] = _pR[1] * CameraRight[2, 0] - CameraRight[1, 0];
            A[3, 1] = _pR[1] * CameraRight[2, 1] - CameraRight[1, 1];
            A[3, 2] = _pR[1] * CameraRight[2, 2] - CameraRight[1, 2];
            A[3, 3] = _pR[1] * CameraRight[2, 3] - CameraRight[1, 3];

            _p3D = SvdZeroFullrankSolver.Solve(A);
            _p3D.DivideThis(_p3D[3]);
        }
    }
}
