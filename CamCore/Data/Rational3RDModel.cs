using System;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Collections.Generic;
using CamCore;

namespace CamCore
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
    // Also plus sign for sqrt(#) is used -> ensure that R^-1(0) = 0
    //
    // TODO Ensure # > 0 even for every rd
    // if a == 0 then ru = -c/b

    public class Rational3RDModel : RadialDistortionModel
    {
        public override string Name { get { return "Rational3"; } }

        public override int ParametersCount
        {
            get
            {
                return 5;
            }
        }

        private int _k1Idx { get { return 0; } }
        private int _k2Idx { get { return 1; } }
        private int _k3Idx { get { return 2; } }
        private int _cxIdx { get { return 3; } }
        private int _cyIdx { get { return 4; } }

        private double _k1 { get { return Coeffs[_k1Idx]; } }
        private double _k2 { get { return Coeffs[_k2Idx]; } }
        private double _k3 { get { return Coeffs[_k3Idx]; } }
        private double _cx { get { return Coeffs[_cxIdx]; } }
        private double _cy { get { return Coeffs[_cyIdx]; } }

        private Vector<double> _diff_delta;

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

        public delegate double K1Finder(double ru, double rd);
        public delegate void InitialParametersSetter(Rational3RDModel model, double k1);
        public enum InitialMethods
        {
            SymmertricK1,
            HighK1,
            Experimental,
            Zero
        }

        InitialMethods _findInitial = InitialMethods.SymmertricK1;
        K1Finder _findK1;
        InitialParametersSetter _setInitialParams;
        public InitialMethods InitialMethod
        {
            get
            {
                return _findInitial;
            }
            set
            {
                _findInitial = value;
                switch(value)
                {
                    case InitialMethods.Experimental:
                        _findK1 = FindK1_GenericModel;
                        _setInitialParams = SetInitialParams_GenericModel;
                        break;
                    case InitialMethods.Zero:
                        _findK1 = FindK1_Zero;
                        _setInitialParams = SetInitialParams_Zero;
                        break;
                    case InitialMethods.HighK1:
                        _findK1 = FindK1_HighK1Model;
                        _setInitialParams = SetInitialParams_HighK1Model;
                        break;
                    case InitialMethods.SymmertricK1:
                    default:
                        _findK1 = FindK1_SymmetricModel;
                        _setInitialParams = SetInitialParams_SymmetricModel;
                        break;
                }
            }
        }


        public Rational3RDModel()
        {
            AllocateWhatNeeded();
            InitialMethod = InitialMethods.SymmertricK1;
        }

        public Rational3RDModel(double k1, double k2, double k3, double cx, double cy)
        {
            AllocateWhatNeeded();
            Coeffs[_k1Idx] = k1;
            Coeffs[_k2Idx] = k2;
            Coeffs[_k3Idx] = k3;
            Coeffs[_cxIdx] = cx;
            Coeffs[_cyIdx] = cy;
            InitialCenterEstimation = new Vector2(_cx, _cy);
        }

        public Rational3RDModel(Vector<double> par)
        {
            AllocateWhatNeeded();
            par.CopyTo(Coeffs);
            InitialCenterEstimation = new Vector2(_cx, _cy);
        }

        void AllocateWhatNeeded()
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
            _diff_delta = new DenseVector(ParametersCount);

            Pu = new Vector2();
            Pd = new Vector2();
            Pf = new Vector2();
        }

        public override void InitCoeffs()
        {
            Coeffs[0] = 0; // ?? : img_diag * 1e-12 
            Coeffs[1] = -0; // ?? : img_diag * 1e-12 
            Coeffs[2] = 0;
            Coeffs[3] = InitialCenterEstimation.X;
            Coeffs[4] = InitialCenterEstimation.Y;
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
            Pu.X = (P.X - _cx);// / _sx;
            Pu.Y = (P.Y - _cy);

            Ru = Math.Sqrt(Pu.X * Pu.X + Pu.Y * Pu.Y);
            Pf.X = Pu.X * (1 + _k1 * Ru) / (1 + _k2 * Ru + _k3 * Ru * Ru) + _cx; //* _sx + _cx;
            Pf.Y = Pu.Y * (1 + _k1 * Ru) / (1 + _k2 * Ru + _k3 * Ru * Ru) + _cy;
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
            //  ru = R^-1(rd) = -b + sqrt(#) / 2a, where:
            //  b = (1 - rd*k2), a = (k1 - rd*k3), c = -rd
            //  # = b^2 - 4*a*c
            double b = 1.0 - Rd * _k2;
            double a = _k1 - Rd * _k3;
            if(Math.Abs(a) < 1e-6 * Rd)
            {
                if(Rd * _k2 >= 1.0 - 1e-6 * Rd) { throw new System.NotFiniteNumberException(); }
                Ru = Rd / b;
            }
            else if(b * b + 4.0 * Rd * a < 0) { throw new System.NotFiniteNumberException(); }
            else
            {
                Ru = (-b + Math.Sqrt(b * b + 4.0 * Rd * a)) / (2.0 * a);
            }
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
            Pf.X = Pu.X + _cx;// Pu.X * _sx + _cx;
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
            // d(rd)/d(cx) = (xd/rd)*[d(xd)/d(cx)]
            Diff_Rd[_cxIdx] = Pd.X * Diff_Xd[_cxIdx] / Rd;
            // d(rd)/d(cy) = (yd/rd)*[d(yd)/d(cx)]
            Diff_Rd[_cyIdx] = Pd.Y * Diff_Yd[_cyIdx] / Rd;
        }


        private void ComputeDiff_Ru()
        {
            //  ru = R^-1(rd) = (-b + sqrt(#)) / 2a, where:
            //  b = (1 - rd*k2), a = (k1 - rd*k3), c = -rd
            //  # = b^2 - 4*a*c = (1 - rd*k2)^2 + 4*rd*(k1 - rd*k3)

            double b = 1.0 - Rd * _k2;
            double a = _k1 - Rd * _k3;
            double delta = Math.Sqrt(b * b + 4.0 * Rd * a);
            _diff_delta[_k1Idx] = 4.0 * Rd; // d(#)/d(k1) = -4*c*d(a) = 4rd
            _diff_delta[_k2Idx] = -2.0 * b * Rd;  // d(#)/d(k2) = 2*b*d(b) = -2*b*rd
            _diff_delta[_k3Idx] = -4.0 * Rd * Rd;// d(#)/d(k3) = -4*c*d(a) = -4rd^2

            // d(#)/d(cx) = 2*b*d(b)/d(cx) - 4*(a*d(c)/d(cx) + c*d(a)/d(cx)) = d(rd)/d(cx) * (-2*b*k2 - 4*(-a + rd*k3))
            double dd = -(2.0 * b * _k2 + 4.0 * (-a + Rd*_k3));
            _diff_delta[_cxIdx] = dd * Diff_Rd[_cxIdx];
            _diff_delta[_cyIdx] = dd * Diff_Rd[_cyIdx];

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
         //   Diff_Xu[_sxIdx] = (Ru / Rd) * Diff_Xd[_sxIdx] + (Pd.X / (Rd * Rd)) * (Rd * Diff_Ru[_sxIdx] - Ru * Diff_Rd[_sxIdx]);

            // d(yu)/d(k) = (yd/rd) * d(ru)/d(k)
            Diff_Yu[_k1Idx] = Pd.Y * Diff_Ru[_k1Idx] / Rd;
            Diff_Yu[_k2Idx] = Pd.Y * Diff_Ru[_k2Idx] / Rd;
            Diff_Yu[_k3Idx] = Pd.Y * Diff_Ru[_k3Idx] / Rd;
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
            Diff_Yf[_k1Idx] = Diff_Yu[_k1Idx];
            Diff_Yf[_k2Idx] = Diff_Yu[_k2Idx];
            Diff_Yf[_k3Idx] = Diff_Yu[_k3Idx];
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

        public override void SetInitialParametersFromQuadrics(List<Quadric> quadrics,
            List<List<Vector2>> linePoints, List<int> fitPoints)
        {
            // 1) Simplify model:
            // - rd = ru(1+k1ru/1+k2ru)
            // - let k2 = -k1
            // - rd/ru = (1+k1ru)/(1-k1ru) -> (rd/ru)(1-k1ru) = 1+k1ru -> (rd/ru) - 1 = k1(ru+rd) -> k1 = ((rd-ru)/ru) / (ru+rd) = (rd-ru)/((ru+rd)ru) 
            // 2) Fit quadrics to lines through closest points, tangents and intersection points of tangent and point-center line
            // 5) Assume intersection points are Pu and quadric points are Pd - from this compute ru, rd and so k1
            // 6) Do it for some points far enough from fit point ( at least one point away)
            // 7) Tangent is further from center than real line, so ru is bigger, rd same.
            //    In result k1 should be smaller than real one, but ok for the start

            DistortionCenter = InitialCenterEstimation;
            double k1sum = 0;
            int count = 0;
            for(int l = 0; l < linePoints.Count; ++l)
            {
                Line2D tangnet = quadrics[l].GetTangentThroughPoint(linePoints[l][fitPoints[l]]);
                for(int p = 0; p < fitPoints[l] - 1; ++p)
                {
                    double k1 = FindK1(linePoints[l][p], tangnet);
                    k1sum += k1;
                    ++count;
                }

                for(int p = fitPoints[l] + 2; p < linePoints[l].Count; ++p)
                {
                    double k1 = FindK1(linePoints[l][p], tangnet);
                    k1sum += k1;
                    ++count;
                }
            }

            k1sum = k1sum / count;
            _setInitialParams(this, k1sum);
        }

        double FindK1(Vector2 quadricPoint, Line2D tangent)
        {
            double rd = quadricPoint.DistanceTo(DistortionCenter);
            Line2D pointToCenter = new Line2D(quadricPoint, DistortionCenter);
            Vector2 intersection = Line2D.IntersectionPoint(pointToCenter, tangent);
            double ru = intersection.DistanceTo(DistortionCenter);
            
            return _findK1(ru, rd);
        }

        static double FindK1_SymmetricModel(double ru, double rd)
        {
            return (rd - ru) / (ru + rd);
        }

        static double FindK1_HighK1Model(double ru, double rd)
        {
            return (rd - ru) / (1.25 * ru * ru - ru * rd);
        }

        static double FindK1_GenericModel(double ru, double rd)
        {
            return rd / ru - 1;
        }

        static double FindK1_Zero(double ru, double rd)
        {
            return 0;
        }

        static void SetInitialParams_SymmetricModel(Rational3RDModel model, double k1)
        {
            if(k1 > 0)
            {
                model.Coeffs[model._k1Idx] = 4.0 * k1;
                model.Coeffs[model._k2Idx] = 4.0 * -k1;
                model.Coeffs[model._k3Idx] = 0.4 * k1;
            }
            else
            {
                model.Coeffs[model._k1Idx] = 3.0 * k1;
                model.Coeffs[model._k2Idx] = 2.0 * -k1;
                model.Coeffs[model._k3Idx] = 0.5 * -k1;
            }
        }

        static void SetInitialParams_HighK1Model(Rational3RDModel model, double k1)
        {
            model.Coeffs[model._k1Idx] = 2 * k1;
            model.Coeffs[model._k2Idx] = 1.8 * k1;
            model.Coeffs[model._k3Idx] = 0.4 * k1;
        }

        static void SetInitialParams_GenericModel(Rational3RDModel model, double k1)
        {
            if(k1 > 0.0)
            {
                model.Coeffs[model._k1Idx] = 2.0 * k1;
                model.Coeffs[model._k2Idx] = k1;
                model.Coeffs[model._k3Idx] = 0.0;
            }
            else
            {
                model.Coeffs[model._k1Idx] = 1.4 * -k1;
                model.Coeffs[model._k2Idx] = 1.3 * -k1;
                model.Coeffs[model._k3Idx] = 0.5 * -k1;
            }
        }

        static void SetInitialParams_Zero(Rational3RDModel model, double k1)
        {
            model.Coeffs[model._k1Idx] = 0;
            model.Coeffs[model._k2Idx] = 0;
            model.Coeffs[model._k3Idx] = 0;
        }

        public override void InitParameters()
        {
            Parameters = new List<IAlgorithmParameter>();
            
            Parameters.Add(new DoubleParameter(
                "Inital Cx", "CX", 320.0, 0.0, 99999.0));
            Parameters.Add(new DoubleParameter(
                "Inital Cy", "CY", 240.0, 0.0, 99999.0));

            var initialParam = new DictionaryParameter("Initial parameters method", "InitialMethod");
            initialParam.ValuesMap = new Dictionary< string, object> ()
            {
                { "k2 = -k1; k3 = 0.1k1", InitialMethods.SymmertricK1 },
                { "k1 = 2.0k1; k2 = 1.6k1; k3 = 0.4k1", InitialMethods.HighK1 },
                { "k1 = k2 = k3 = 0", InitialMethods.Zero },
                { "Experimental", InitialMethods.Experimental }
            };

            Parameters.Add(initialParam);
        }

        public override void UpdateParameters()
        {
            InitialCenterEstimation = new Vector2();
            InitialCenterEstimation.X = IAlgorithmParameter.FindValue<double>("CX", Parameters);
            InitialCenterEstimation.Y = IAlgorithmParameter.FindValue<double>("CY", Parameters);

            InitCoeffs();

            InitialMethod = IAlgorithmParameter.FindValue<InitialMethods>("InitialMethod", Parameters);
        }
    }
}
