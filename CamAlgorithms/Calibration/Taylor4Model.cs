using System;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Collections.Generic;
using CamCore;

namespace CamAlgorithms.Calibration
{
    // Rational model of distortion function is unknown here
    // Inverse (undistortion) function is approximated by Taylor series (for x0 = 0 as ru/rd are centered at 0) :
    // ru = R^-1(rd) = k1 + k2*rd + k3*rd^2 + k4*rd^3 + ..., but as R^-1(0) = 0, then k1 = 0
    // so ru = k1*rd + k2*rd^2 + k3*rd^3 + ...
    // then for rd = 0 we have xd = xu = xd * ru/rd = xd(k1+k2rd+...) = xd*k1 => k1 = 1
    // For this to work R^-1(r) must be multiple times differentable (well lets assume it is and check if it works)

    public class Taylor4Model : RadialDistortionModel, IParameterizable
    {
        public override string Name { get { return "Taylor4"; } }
        public override int ParametersCount
        {
            get
            {
                return 6;
            }
        }

        private int _k1Idx { get { return 0; } }
        private int _k2Idx { get { return 1; } }
        private int _k3Idx { get { return 2; } }
        private int _k4Idx { get { return 3; } }
        private int _cxIdx { get { return 4; } }
        private int _cyIdx { get { return 5; } }
        private double _k1 { get { return Coeffs[_k1Idx]; } }
        private double _k2 { get { return Coeffs[_k2Idx]; } }
        private double _k3 { get { return Coeffs[_k3Idx]; } }
        private double _k4 { get { return Coeffs[_k4Idx]; } }
        private double _cx { get { return Coeffs[_cxIdx]; } }
        private double _cy { get { return Coeffs[_cyIdx]; } }
        
        public override Vector2 DistortionCenter
        {
            get
            {
                return new Vector2(_cx, _cy);
            }
            set
            {
                Coeffs[_cxIdx] = value.X;
                Coeffs[_cyIdx] = value.Y;
            }
        }

        public Taylor4Model()
        {
            Coeffs = new DenseVector(ParametersCount);
            Diff_Xf = new DenseVector(ParametersCount);
            Diff_Yf = new DenseVector(ParametersCount);
            Diff_Xu = new DenseVector(ParametersCount);
            Diff_Yu = new DenseVector(ParametersCount);
            Diff_Xd = new DenseVector(ParametersCount);
            Diff_Yd = new DenseVector(ParametersCount);
            Diff_Ru = new DenseVector(ParametersCount);
            Diff_Rd = new DenseVector(ParametersCount);

            Pu = new Vector2();
            Pd = new Vector2();
            Pf = new Vector2();
           // ComputesAspect = true;
        }

        public override void InitCoeffs()
        {
            Coeffs[_k1Idx] = 0.0f;
            Coeffs[_k2Idx] = 0.0f;
            Coeffs[_k3Idx] = 0.0f;
            Coeffs[_k4Idx] = 0.0f;
            Coeffs[_cxIdx] = InitialCenterEstimation.X;
            Coeffs[_cyIdx] = InitialCenterEstimation.Y;
        }

        public override void FullUpdate()
        {
            if(UseNumericDerivative)
            {
                ComputeDiff_Numeric();
                Undistort();
            }
            else
            {
                Undistort();
                ComputeDiff_Pd();
                ComputeDiff_Rd();
                ComputeDiff_Ru();
                ComputeDiff_Pu();
                ComputeDiff_Pf();
            }
        }

        public override void Distort()
        {
            throw new NotImplementedException();
        }

        public override void Undistort()
        {
            ComputePd();
            ComputeRd();
            ComputeRu();
            ComputePu();
            ComputePf();
        }

        private void ComputePd()
        {
            // xd = (x-cx)/sx, yd = y-cy
            Pd.X = (P.X - _cx);// / _sx;
            Pd.Y = P.Y - _cy;
        }

        private void ComputeRd()
        {
            // rd = sqrt(Xd^2+Yd^2)
            Rd = Math.Sqrt(Pd.X * Pd.X + Pd.Y * Pd.Y);
        }

        private void ComputeRu()
        {
            double srd2 = Math.Sqrt(Rd);
            double srd3 = Math.Pow(Rd, 1.0 / 3.0);
            double srd4 = Math.Pow(Rd, 1.0 / 4.0);
            double srd5 = Math.Pow(Rd, 1.0 / 5.0);
            //  ru = rd + k1*rd^2 + k2*rd^3 + k3*rd^4 + k4*rd^5 = rd(1 + rd(k1 + rd(k2 + rd(k3 + rdk4))))
            //  Ru = Rd * (1.0 + Rd * (_k1 + Rd * (_k2 + Rd * (_k3 + Rd * _k4))));
            Ru = Rd * (1.0 + _k1 * srd2 + _k2 * srd3 + _k3 * srd4 + _k4 * srd5);
        }

        private void ComputePu()
        {
            // xu = xd * ru/rd, yu = yd * ru/rd
            Pu.X = Pd.X * Ru / Rd;
            Pu.Y = Pd.Y * Ru / Rd;
        }

        private void ComputePf()
        {
            // xd*sx + cx = x, yd + cy = y
            Pf.X = Pu.X + _cx; //Pu.X * _sx + _cx;
            Pf.Y = Pu.Y + _cy;
        }

        private void ComputeDiff_Pd()
        {
            // Diff_Xd = d(xd)/d(P)
            // Diff_Yd = d(yd)/d(P)
            // d(xd)/d(k) = 0, d(yd)/d(k) = 0
            Diff_Xd[_k1Idx] = 0.0;
            Diff_Xd[_k2Idx] = 0.0;
            Diff_Xd[_k3Idx] = 0.0;
            Diff_Yd[_k1Idx] = 0.0;
            Diff_Yd[_k2Idx] = 0.0;
            Diff_Yd[_k3Idx] = 0.0;
            // d(xd)/d(cx) = -1/sx, d(yd)/d(cx) = 0
            Diff_Xd[_cxIdx] = -1.0;
            Diff_Yd[_cxIdx] = 0.0;
            // d(xd)/d(cy) = 0, d(yd)/d(cy) = -1
            Diff_Xd[_cyIdx] = 0.0;
            Diff_Yd[_cyIdx] = -1.0;
        }

        private void ComputeDiff_Rd()
        {
            // Diff_Rd = d(rd)/d(P), rd = sqrt(xd^2+yd^2)
            // d(rd)/d(k) = 0
            Diff_Rd[_k1Idx] = 0.0;
            Diff_Rd[_k2Idx] = 0.0;
            Diff_Rd[_k3Idx] = 0.0;
            // d(rd)/d(cx) = (-1/rd)*2*xd*[d(xd)/d(cx)]
            Diff_Rd[_cxIdx] = -2.0 * Pd.X * Diff_Xd[_cxIdx] / Rd;
            // d(rd)/d(cy) = (-1/rd)*2*yd*[d(yd)/d(cx)]
            Diff_Rd[_cyIdx] = -2.0 * Pd.Y * Diff_Yd[_cyIdx] / Rd;
        }


        private void ComputeDiff_Ru()
        {
            // ru = rd + k1*rd^2 + k2*rd^3 + k3*rd^4 + k4*rd^5

            Diff_Ru[_k1Idx] = Rd * Rd;
            Diff_Ru[_k2Idx] = Rd * Rd * Rd;
            Diff_Ru[_k3Idx] = Rd * Rd * Rd * Rd;
            Diff_Ru[_k4Idx] = Rd * Rd * Rd * Rd * Rd;

            Diff_Ru[_cxIdx] = Diff_Rd[_cxIdx] * (1.0 + 2.0 * _k1 * Rd + 3.0 * _k2 * Rd * Rd + 4.0 * _k3 * Rd * Rd * Rd + 5.0 * _k4 * Rd * Rd * Rd * Rd);
            Diff_Ru[_cyIdx] = Diff_Rd[_cyIdx] * (1.0 + 2.0 * _k1 * Rd + 3.0 * _k2 * Rd * Rd + 4.0 * _k3 * Rd * Rd * Rd + 5.0 * _k4 * Rd * Rd * Rd * Rd);
        }

        private void ComputeDiff_Pu()
        {
            //  xu = xd * ru/rd, yu = yd * ru/rd

            // d(xu)/d(k) = (xd/rd) * d(ru)/d(k)
            Diff_Xu[_k1Idx] = Pd.X * Diff_Ru[_k1Idx] / Rd;
            Diff_Xu[_k2Idx] = Pd.X * Diff_Ru[_k2Idx] / Rd;
            Diff_Xu[_k3Idx] = Pd.X * Diff_Ru[_k3Idx] / Rd;
            Diff_Xu[_k4Idx] = Pd.X * Diff_Ru[_k4Idx] / Rd;
            // d(xu)/d(cx) = ru/rd * d(xd)/d(cx) + (xd/rd^2)(d(ru)/d(cx) * rd - d(rd)/d(cx) * ru)
            Diff_Xu[_cxIdx] = (Ru / Rd) * Diff_Xd[_cxIdx] + (Pd.X / (Rd * Rd)) * (Rd * Diff_Ru[_cxIdx] - Ru * Diff_Rd[_cxIdx]);
            // d(xu)/d(cy) = (xd/rd^2)(d(ru)/d(cy) * rd - d(rd)/d(cy) * ru)
            Diff_Xu[_cyIdx] = (Pd.X / (Rd * Rd)) * (Rd * Diff_Ru[_cyIdx] - Ru * Diff_Rd[_cyIdx]);
            // d(xu)/d(sx) = ru/rd * d(xd)/d(sx) + (xd/rd^2)(d(ru)/d(sx) * rd - d(rd)/d(sx) * ru)
            // Diff_Xu[_sxIdx] = (Ru / Rd) * Diff_Xd[_sxIdx] + (Pd.X / (Rd * Rd)) * (Rd * Diff_Ru[_sxIdx] - Ru * Diff_Rd[_sxIdx]);

            // d(yu)/d(k) = (yd/rd) * d(ru)/d(k)
            Diff_Yu[_k1Idx] = Pd.Y * Diff_Ru[_k1Idx] / Rd;
            Diff_Yu[_k2Idx] = Pd.Y * Diff_Ru[_k2Idx] / Rd;
            Diff_Yu[_k3Idx] = Pd.Y * Diff_Ru[_k3Idx] / Rd;
            Diff_Yu[_k4Idx] = Pd.Y * Diff_Ru[_k4Idx] / Rd;
            // d(yu)/d(cx) = (yd / rd ^ 2)(d(ru) / d(cy) * rd - d(rd) / d(cy) * ru)
            Diff_Yu[_cxIdx] = (Pd.Y / (Rd * Rd)) * (Rd * Diff_Ru[_cxIdx] - Ru * Diff_Rd[_cxIdx]);
            // d(yu)/d(cy) = ru/rd * d(xd)/d(cx) + (xd/rd^2)(d(ru)/d(cx) * rd - d(rd)/d(cx) * ru)
            Diff_Yu[_cyIdx] = (Ru / Rd) * Diff_Yd[_cyIdx] + (Pd.Y / (Rd * Rd)) * (Rd * Diff_Ru[_cyIdx] - Ru * Diff_Rd[_cyIdx]);
        }

        private void ComputeDiff_Pf()
        {
            // xu*sx + cx = xf, yu + cy = yf

            // d(xf)/d(k) = sx * d(xu)/d(k), d(yf)/d(k) = d(yu)/d(k)
            Diff_Xf[_k1Idx] = Diff_Xu[_k1Idx];
            Diff_Xf[_k2Idx] = Diff_Xu[_k2Idx];
            Diff_Xf[_k3Idx] = Diff_Xu[_k3Idx];
            Diff_Xf[_k4Idx] = Diff_Xu[_k4Idx];

            Diff_Yf[_k1Idx] = Diff_Yu[_k1Idx];
            Diff_Yf[_k2Idx] = Diff_Yu[_k2Idx];
            Diff_Yf[_k3Idx] = Diff_Yu[_k3Idx];
            Diff_Yf[_k4Idx] = Diff_Yu[_k4Idx];
            // d(xu)/d(cx) = sx * d(xu)/d(cx) + 1, d(yf)/d(cx) = d(yu)/d(cx)
            Diff_Xf[_cxIdx] = Diff_Xu[_cxIdx] + 1.0;
            Diff_Yf[_cxIdx] = Diff_Yu[_cxIdx];
            // d(xu)/d(cy) = sx * d(xu)/d(cy), d(yf)/d(cy) = d(yu)/d(cy) + 1
            Diff_Xf[_cyIdx] = Diff_Xu[_cyIdx];
            Diff_Yf[_cyIdx] = Diff_Yu[_cyIdx] + 1.0;
            
        }

        private void ComputeDiff_Numeric()
        {
            for(int k = 0; k < ParametersCount; ++k)
            {
                double oldK = Coeffs[k];
                double k_n = Math.Abs(oldK) > float.Epsilon ? oldK * (1 - NumericDerivativeStep) : -NumericDerivativeStep * 1E-2;
                double k_p = Math.Abs(oldK) > float.Epsilon ? oldK * (1 + NumericDerivativeStep) : NumericDerivativeStep * 1E-2;

                Coeffs[k] = k_n;
                ComputePd();
                ComputeRd();
                ComputeRu();
                ComputePu();
                ComputePf();
                Vector2 pd_n = new Vector2(Pd);
                double rd_n = Rd;
                double ru_n = Ru;
                Vector2 pu_n = new Vector2(Pu);
                Vector2 pf_n = new Vector2(Pf);

                Coeffs[k] = k_p;
                ComputePd();
                ComputeRd();
                ComputeRu();
                ComputePu();
                ComputePf();
                Vector2 pd_p = new Vector2(Pd);
                double rd_p = Rd;
                double ru_p = Ru;
                Vector2 pu_p = new Vector2(Pu);
                Vector2 pf_p = new Vector2(Pf);

                Diff_Xd[k] = (pd_p.X - pd_n.X) / (k_p - k_n);
                Diff_Yd[k] = (pd_p.Y - pd_n.Y) / (k_p - k_n);
                Diff_Rd[k] = (rd_p - rd_n) / (k_p - k_n);
                Diff_Ru[k] = (ru_p - ru_n) / (k_p - k_n);
                Diff_Xu[k] = (pu_p.X - pu_n.X) / (k_p - k_n);
                Diff_Yu[k] = (pu_p.Y - pu_n.Y) / (k_p - k_n);
                Diff_Xf[k] = (pf_p.X - pf_n.X) / (k_p - k_n);
                Diff_Yf[k] = (pf_p.Y - pf_n.Y) / (k_p - k_n);
                Coeffs[k] = oldK;
            }

            ComputePd();
            ComputeRd();
            ComputeRu();
            ComputePu();
            ComputePf();
        }

        public override void InitParameters()
        {
            Parameters = new List<IAlgorithmParameter>();

            IAlgorithmParameter initalCx = new DoubleParameter(
                "Inital Cx", "ICX", 320.0, 0.0, 99999.0);
            Parameters.Add(initalCx);

            IAlgorithmParameter initalCy = new DoubleParameter(
                "Inital Cy", "ICY", 240.0, 0.0, 99999.0);
            Parameters.Add(initalCy);

            IAlgorithmParameter initalK1 = new DoubleParameter(
                "Inital K1", "IK1", 0.0, -100.0, 100.0);
            Parameters.Add(initalK1);

            IAlgorithmParameter initalK2 = new DoubleParameter(
                "Inital K2", "IK2", 0.0, -100.0, 100.0);
            Parameters.Add(initalK2);

            IAlgorithmParameter initalK3 = new DoubleParameter(
                "Inital K3", "IK3", 0.0, -100.0, 100.0);
            Parameters.Add(initalK3);

            IAlgorithmParameter initalK4 = new DoubleParameter(
                "Inital K4", "IK4", 0.0, -100.0, 100.0);
            Parameters.Add(initalK4);
        }

        public override void UpdateParameters()
        {
            InitialCenterEstimation = new Vector2();
            InitialCenterEstimation.X = IAlgorithmParameter.FindValue<double>("ICX", Parameters);
            InitialCenterEstimation.Y = IAlgorithmParameter.FindValue<double>("ICY", Parameters);

            InitCoeffs();

            Coeffs[_k1Idx] = IAlgorithmParameter.FindValue<double>("IK1", Parameters);
            Coeffs[_k2Idx] = IAlgorithmParameter.FindValue<double>("IK2", Parameters);
            Coeffs[_k3Idx] = IAlgorithmParameter.FindValue<double>("IK3", Parameters);
            Coeffs[_k4Idx] = IAlgorithmParameter.FindValue<double>("IK3", Parameters);
        }

        public override void SetInitialParametersFromQuadrics(List<Quadric> quadrics, List<List<Vector2>> linePoints, List<int> fitPoints)
        {
            InitCoeffs();
        }
    }
}
