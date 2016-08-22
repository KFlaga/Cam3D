using System;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using CamCore;
using System.Collections.Generic;

namespace CalibrationModule
{
    // Rational model of distortion function :
    //                        1 + k1*ru
    // rd = R(ru) = ru * -------------------
    //                   1 + k2*ru + k3*ru^2
    // 
    // Inverse (undistortion) function:
    // 
    //  ru = R^-1(rd) = -b - sqrt(#) / 2a, where:
    //  b = (rd*k2 - 1), a = (rd*k3 - k1), c = rd
    //  # = b^2 - 4*a*c
    //  
    // In real scenraios k2*rd should be << 1, and # > 0
    // Also minus sign for sqrt(#) is used -> ensure that R^-1(0) = 0
    //
    // TODO Ensure # > 0 even for every rd
    // if a == 0 then ru = -c/b

    public class Rational3RDModel : RadialDistortionModel, IParametrizedProcessor
    {
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
        private int _cxIdx { get { return 3; } }
        private int _cyIdx { get { return 4; } }
        private int _sxIdx { get { return 5; } }
        private double _k1 { get { return Parameters[_k1Idx]; } }
        private double _k2 { get { return Parameters[_k2Idx]; } }
        private double _k3 { get { return Parameters[_k3Idx]; } }
        private double _cx { get { return Parameters[_cxIdx]; } }
        private double _cy { get { return Parameters[_cyIdx]; } }
        private double _sx { get { return Parameters[_sxIdx]; } }

        List<ProcessorParameter> _procParams;
        List<ProcessorParameter> IParametrizedProcessor.Parameters
        {
            get { return _procParams;  }
        }

        private Vector<double> _diff_delta;
        
        public override Vector2 DistortionCenter
        {
            get
            {
                return new Vector2(_cx, _cy);
            }
            set
            {
                Parameters[_cxIdx] = value.X;
                Parameters[_cyIdx] = value.Y;
            }
        }

        public override double Aspect
        {
            get
            {
                return _sx;
            }
            set
            {
                Parameters[_sxIdx] = value;
            }
        }

        public Rational3RDModel()
        {
            Parameters = new DenseVector(ParametersCount);
            Diff_Xf = new DenseVector(ParametersCount);
            Diff_Yf = new DenseVector(ParametersCount);
            Diff_Xu = new DenseVector(ParametersCount);
            Diff_Yu = new DenseVector(ParametersCount);
            Diff_Xd = new DenseVector(ParametersCount);
            Diff_Yd = new DenseVector(ParametersCount);
            Diff_Ru = new DenseVector(ParametersCount);
            Diff_Rd = new DenseVector(ParametersCount);
            _diff_delta = new DenseVector(ParametersCount);

            Pu = new Vector2();
            Pd = new Vector2();
            Pf = new Vector2();
            ComputesAspect = true;
        }

        public override void InitParameters()
        {
            Parameters[0] = 0; // ?? : img_diag * 1e-12 
            Parameters[1] = -0; // ?? : img_diag * 1e-12 
            Parameters[2] = 0;
            Parameters[3] = InitialCenterEstimation.X;
            Parameters[4] = InitialCenterEstimation.Y;
            Parameters[5] = InitialAspectEstimation;
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

        public override void Undistort()
        {
            ComputePd();
            ComputeRd();
            ComputeRu();
            ComputePu();
            ComputePf();
        }

        public override void Distort()
        {
            Pu.X = (P.X - _cx) / _sx;
            Pu.Y = (P.Y - _cy);

            Ru = Math.Sqrt(Pu.X * Pu.X + Pu.Y * Pu.Y);
            Pf.X = Pu.X * (1 + _k1 * Ru) / (1 + _k2 * Ru + _k3 * Ru * Ru) * _sx + _cx;
            Pf.Y = Pu.Y * (1 + _k1 * Ru) / (1 + _k2 * Ru + _k3 * Ru * Ru) + _cy;
        }

        private void ComputePd()
        {
            // xd = (x-cx)/sx, yd = y-cy
            Pd.X = (P.X - _cx) / _sx;
            Pd.Y = P.Y - _cy;
        }

        private void ComputeRd()
        {
            // rd = sqrt(Xd^2+Yd^2)
            Rd = Math.Sqrt(Pd.X * Pd.X + Pd.Y * Pd.Y);
        }

        private void ComputeRu()
        {
            //  ru = R^-1(rd) = -b + sqrt(#) / 2a, where:
            //  b = (1 - rd*k2), a = (k1 - rd*k3), c = -rd
            //  # = b^2 - 4*a*c
            double b = 1.0 - Rd * _k2;
            double a = _k1 - Rd * _k3;
            if(Math.Abs(a) < float.Epsilon)
            {
                if(Rd * _k2 >= 1.0 - float.Epsilon)
                    throw new System.NotFiniteNumberException();

                Ru = Rd / b;
            }
            else if(b * b + 4.0 * Rd * a < 0)
                throw new System.NotFiniteNumberException();
            else
                Ru = (-b + Math.Sqrt(b * b + 4.0 * Rd * a)) / (2.0 * a);
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
            Pf.X = Pu.X * _sx + _cx;
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
            Diff_Xd[_cxIdx] = -1.0 / _sx;
            Diff_Yd[_cxIdx] = 0.0;
            // d(xd)/d(cy) = 0, d(yd)/d(cy) = -1
            Diff_Xd[_cyIdx] = 0.0;
            Diff_Yd[_cyIdx] = -1.0;
            // d(xd)/d(sx) = -(x-cx)/2sx^2, d(yd)/d(sx) = 0
            Diff_Xd[_sxIdx] = -Pd.X / (_sx * _sx);
            Diff_Yd[_sxIdx] = 0.0;
        }

        private void ComputeDiff_Rd()
        {
            // Diff_Rd = d(rd)/d(P), rd = sqrt(xd^2+yd^2)
            // d(rd)/d(k) = 0
            Diff_Rd[_k1Idx] = 0.0;
            Diff_Rd[_k2Idx] = 0.0;
            Diff_Rd[_k3Idx] = 0.0;
            // d(rd)/d(cx) = (xd/rd)*[d(xd)/d(cx)]
            Diff_Rd[_cxIdx] = Pd.X * Diff_Xd[_cxIdx] / Rd;
            // d(rd)/d(cy) = (yd/rd)*[d(yd)/d(cx)]
            Diff_Rd[_cyIdx] = Pd.Y * Diff_Yd[_cyIdx] / Rd;
            // d(rd)/d(sx) = (xd/rd)*[d(xd)/d(sx)]
            Diff_Rd[_sxIdx] = Pd.X * Diff_Xd[_sxIdx] / Rd;
        }


        private void ComputeDiff_Ru()
        {
            //  ru = R^-1(rd) = (-b + sqrt(#)) / 2a, where:
            //  b = (1 - rd*k2), a = (k1 - rd*k3), c = -rd
            //  # = b^2 - 4*a*c = (1 - rd*k2)^2 + 4*rd*(k1 - rd*k3)

            double b = 1.0 - Rd * _k2;
            double a = _k1 - Rd * _k3;
            double delta = (double)Math.Sqrt(b * b + 4.0 * Rd * a);
            _diff_delta[_k1Idx] = 4.0 * Rd; // d(#)/d(k1) = -4*c*d(a) = 4rd
            _diff_delta[_k2Idx] = -2.0 * b * Rd;  // d(#)/d(k2) = 2*b*d(b) = -2*b*rd
            _diff_delta[_k3Idx] = -4.0 * Rd * Rd;// d(#)/d(k3) = -4*c*d(a) = -4rd^2

            // d(#)/d(cx) = 2*b*d(b)/d(cx) - 4*(a*d(c)/d(cx) + c*d(a)/d(cx)) = d(rd)/d(cx) * (-2*b*k2 - 4*(-a + rd*k3))
            double dd = -(2.0 * b * _k2 + 4.0 * (-a + Rd*_k3));
            _diff_delta[_cxIdx] = dd * Diff_Rd[_cxIdx];
            _diff_delta[_cyIdx] = dd * Diff_Rd[_cyIdx];
            _diff_delta[_sxIdx] = dd * Diff_Rd[_sxIdx];

            // Limit a and delta with float epsilon ( limit somewhat arbitrary, just to avoid overflow )
            a = Math.Abs(a) < float.Epsilon ? float.Epsilon : a;
            delta = delta < float.Epsilon ? float.Epsilon : delta;
            double deltaInv2 = 0.5 / delta;
            
            // d(ru)/d(k1) = ( a*#Inv2*d(#) - (-b+sqrt(#))) ) / 2a^2
            Diff_Ru[_k1Idx] = (a * deltaInv2 * _diff_delta[_k1Idx] + b - delta) / (2.0 * a * a);
            // d(ru)/d(k2) = -(#Inv2 * d(#) + rd) / 2a
            Diff_Ru[_k2Idx] = (deltaInv2 * _diff_delta[_k2Idx] + Rd) / (2.0 * a);
            // d(ru)/d(k3) = ( a*#Inv2*d(#) + rd*(-b+sqrt(#))) ) / 2a^2
            Diff_Ru[_k3Idx] = (a * deltaInv2 * _diff_delta[_k3Idx] + Rd * (-b + delta)) / (2.0 * a * a);

            // d(ru)/d(cx) = ( a*(k2*d(rd)+#Inv2*d(#)) + k3*d(rd)*(-b+sqrt(#))) ) / 2a^2
            Diff_Ru[_cxIdx] = (a * (_k2 * Diff_Rd[_cxIdx] + deltaInv2 * _diff_delta[_cxIdx]) + 
                _k3 * Diff_Rd[_cxIdx] * (-b + delta)) / (2.0 * a * a);
            // d(ru)/d(cy) = ( a*(k2*d(rd)+#Inv2*d(#)) + k3*d(rd)*(-b+sqrt(#))) ) / 2a^2
            Diff_Ru[_cyIdx] = (a * (_k2 * Diff_Rd[_cyIdx] + deltaInv2 * _diff_delta[_cyIdx]) + 
                _k3 * Diff_Rd[_cyIdx] * (-b + delta)) / (2.0 * a * a);
            // d(ru)/d(sx) = ( a*(k2*d(rd)+#Inv2*d(#)) + k3*d(rd)*(-b+sqrt(#))) ) / 2a^2
            Diff_Ru[_sxIdx] = (a * (_k2 * Diff_Rd[_sxIdx] + deltaInv2 * _diff_delta[_sxIdx]) + 
                _k3 * Diff_Rd[_sxIdx] * (-b + delta)) / (2.0 * a * a);
        }

        private void ComputeDiff_Pu()
        {
            //  xu = xd * ru/rd, yu = yd * ru/rd

            // d(xu)/d(k) = (xd/rd) * d(ru)/d(k)
            Diff_Xu[_k1Idx] = Pd.X * Diff_Ru[_k1Idx] / Rd;
            Diff_Xu[_k2Idx] = Pd.X * Diff_Ru[_k2Idx] / Rd;
            Diff_Xu[_k3Idx] = Pd.X * Diff_Ru[_k3Idx] / Rd;
            // d(xu)/d(cx) = ru/rd * d(xd)/d(cx) + (xd/rd^2)(d(ru)/d(cx) * rd - d(rd)/d(cx) * ru)
            Diff_Xu[_cxIdx] = (Ru / Rd) * Diff_Xd[_cxIdx] + (Pd.X / (Rd * Rd)) * (Rd * Diff_Ru[_cxIdx] - Ru * Diff_Rd[_cxIdx]);
            // d(xu)/d(cy) = (xd/rd^2)(d(ru)/d(cy) * rd - d(rd)/d(cy) * ru)
            Diff_Xu[_cyIdx] = (Pd.X / (Rd * Rd)) * (Rd * Diff_Ru[_cyIdx] - Ru * Diff_Rd[_cyIdx]);
            // d(xu)/d(sx) = ru/rd * d(xd)/d(sx) + (xd/rd^2)(d(ru)/d(sx) * rd - d(rd)/d(sx) * ru)
            Diff_Xu[_sxIdx] = (Ru / Rd) * Diff_Xd[_sxIdx] + (Pd.X / (Rd * Rd)) * (Rd * Diff_Ru[_sxIdx] - Ru * Diff_Rd[_sxIdx]);

            // d(yu)/d(k) = (yd/rd) * d(ru)/d(k)
            Diff_Yu[_k1Idx] = Pd.Y * Diff_Ru[_k1Idx] / Rd;
            Diff_Yu[_k2Idx] = Pd.Y * Diff_Ru[_k2Idx] / Rd;
            Diff_Yu[_k3Idx] = Pd.Y * Diff_Ru[_k3Idx] / Rd;
            // d(yu)/d(cx) = (yd / rd ^ 2)(d(ru) / d(cy) * rd - d(rd) / d(cy) * ru)
            Diff_Yu[_cxIdx] = (Pd.Y / (Rd * Rd)) * (Rd * Diff_Ru[_cxIdx] - Ru * Diff_Rd[_cxIdx]);
            // d(yu)/d(cy) = ru/rd * d(xd)/d(cx) + (xd/rd^2)(d(ru)/d(cx) * rd - d(rd)/d(cx) * ru)
            Diff_Yu[_cyIdx] = (Ru / Rd) * Diff_Yd[_cyIdx] + (Pd.Y / (Rd * Rd)) * (Rd * Diff_Ru[_cyIdx] - Ru * Diff_Rd[_cyIdx]);
            // d(yu)/d(sx) = (yd / rd ^ 2)(d(ru) / d(cy) * rd - d(rd) / d(cy) * ru)
            Diff_Yu[_sxIdx] = (Pd.Y / (Rd * Rd)) * (Rd * Diff_Ru[_sxIdx] - Ru * Diff_Rd[_sxIdx]);
        }

        private void ComputeDiff_Pf()
        {
            // xu*sx + cx = xf, yu + cy = yf

            // d(xf)/d(k) = sx * d(xu)/d(k), d(yf)/d(k) = d(yu)/d(k)
            Diff_Xf[_k1Idx] = _sx * Diff_Xu[_k1Idx];
            Diff_Xf[_k2Idx] = _sx * Diff_Xu[_k2Idx];
            Diff_Xf[_k3Idx] = _sx * Diff_Xu[_k3Idx];
            Diff_Yf[_k1Idx] = Diff_Yu[_k1Idx];
            Diff_Yf[_k2Idx] = Diff_Yu[_k2Idx];
            Diff_Yf[_k3Idx] = Diff_Yu[_k3Idx];
            // d(xu)/d(cx) = sx * d(xu)/d(cx) + 1, d(yf)/d(cx) = d(yu)/d(cx)
            Diff_Xf[_cxIdx] = _sx * Diff_Xu[_cxIdx] + 1.0;
            Diff_Yf[_cxIdx] = Diff_Yu[_cxIdx];
            // d(xu)/d(cy) = sx * d(xu)/d(cy), d(yf)/d(cy) = d(yu)/d(cy) + 1
            Diff_Xf[_cyIdx] = _sx * Diff_Xu[_cyIdx];
            Diff_Yf[_cyIdx] = Diff_Yu[_cyIdx] + 1.0;
            // d(xu)/d(sx) = sx * d(xu)/d(cx) + xu, d(yf)/d(sx) = d(yu)/d(sx)
            Diff_Xf[_sxIdx] = _sx * Diff_Xu[_sxIdx] + Pu.X;
            Diff_Yf[_sxIdx] = Diff_Yu[_sxIdx];
        }

        private void ComputeDiff_Numeric()
        {
            for(int k = 0; k < ParametersCount; ++k)
            {
                double oldK = Parameters[k];
                double k_n = Math.Abs(oldK) > float.Epsilon ? oldK * (1 - NumericDerivativeStep) : -NumericDerivativeStep * 1E-2;
                double k_p = Math.Abs(oldK) > float.Epsilon ? oldK * (1 + NumericDerivativeStep) : NumericDerivativeStep * 1E-2;

                Parameters[k] = k_n;
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

                Parameters[k] = k_p;
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
                Parameters[k] = oldK;
            }

            ComputePd();
            ComputeRd();
            ComputeRu();
            ComputePu();
            ComputePf();
        }
        
        void IParametrizedProcessor.InitParameters()
        {
            _procParams = new List<ProcessorParameter>();

            ProcessorParameter initalCx = new ProcessorParameter(
                "Inital Cx", "ICX",
                "System.Single", 0.5f, 0.0f, 1.0f);
            _procParams.Add(initalCx);

            ProcessorParameter initalCy = new ProcessorParameter(
                "Inital Cy", "ICY",
                "System.Single", 0.5f, 0.0f, 1.0f);
            _procParams.Add(initalCy);

            ProcessorParameter initalSx = new ProcessorParameter(
                "Inital Sx", "ISX",
                "System.Single", 1.0f, 100.0f, 0.01f);
            _procParams.Add(initalSx);

            ProcessorParameter initalK1 = new ProcessorParameter(
                "Inital K1", "IK1",
                "System.Single", 0.0f, 100.0f, -100.0f);
            _procParams.Add(initalK1);

            ProcessorParameter initalK2 = new ProcessorParameter(
                "Inital K2", "IK2",
                "System.Single", 0.0f, 100.0f, -100.0f);
            _procParams.Add(initalK2);

            ProcessorParameter initalK3 = new ProcessorParameter(
                "Inital K3", "IK3",
                "System.Single", 0.0f, 100.0f, -100.0f);
            _procParams.Add(initalK3);
        }

        void IParametrizedProcessor.UpdateParameters()
        {
            InitialAspectEstimation = (float)ProcessorParameter.FindValue("ISX", _procParams);
            InitialCenterEstimation = new Vector2();
            InitialCenterEstimation.X = (float)ProcessorParameter.FindValue("ICX", _procParams);
            InitialCenterEstimation.Y = (float)ProcessorParameter.FindValue("ICY", _procParams);

            InitParameters();

            Parameters[_k1Idx] = (float)ProcessorParameter.FindValue("IK1", _procParams);
            Parameters[_k2Idx] = (float)ProcessorParameter.FindValue("IK2", _procParams);
            Parameters[_k3Idx] = (float)ProcessorParameter.FindValue("IK3", _procParams);
        }
    }
}
