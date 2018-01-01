using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using System;
using System.Collections.Generic;
using Complex = MathNet.Numerics.Complex32;
using TComplex = MathNet.Numerics.LinearAlgebra.Complex32;

namespace CamCore
{
    // Uses Aberth methid to find all root of polynomial with real coefficients and rank > 2
    public class PolynomialRootFinder
    {
        public Polynomial Poly { get; set; }

        public Vector<Complex> Roots { get; set; }
        public List<float> RealRoots { get; set; }

        public int MaximumIterations { get; set; } = 20; // End iteration condition : max interations are reached
        public int CurrentIteration { get { return _currentIteration; } }
        private int _currentIteration;

        public float MinIterationEnchance { get; set; } = 1e-4f; // Stops if iteration betters result less than this much 
                                                                 // Its rational, so 0.01 is 1% 
        public float MaxZeroErrorSquared { get; set; } = 1e-8f;  // Sum squared error of all zeros when iteration stops (it is multiplied by rank)
        private float _maxError;

        private float _currentError;
        private float _lastError;

        private Polynomial _PDiff;
        private Polynomial _Sw;

        public void Process()
        {
            _currentIteration = 0;
            
            Init();
            _currentError = ComputeRootsError(Roots);

            do
            {
                _currentIteration += 1;
                Iterate();

                _lastError = _currentError;
                _currentError = ComputeRootsError(Roots);
            }
            while(CheckIterationEndConditions() == false);

            foreach(var root in Roots)
            {
                // Root is real if its imaginary part is 10k times smaller or if real == 0, then imag < 1e-12
                if(root.Real != 0 ? root.Imaginary / root.Real < 1e-4f : root.Imaginary < 1e-12f)
                    RealRoots.Add(root.Real);
            }
        }

        public float ComputeRootsError(Vector<Complex> roots)
        {
            float error = 0.0f;
            foreach(var root in Roots)
            {
                error += Poly.At(root).MagnitudeSquared;
            }
            return error;
        }

        public void Init()
        {
            Roots = new TComplex.DenseVector(Poly.Rank);
            RealRoots = new List<float>();
            _maxError = MaxZeroErrorSquared * Poly.Rank;
            ComputePolyDerivative();

            // FInd initial guess
            // Consider polynomians: 
            // R(w) = w^n + c2w^n-2 + .. + cn-1w + cn, created as R(w) = P(z) if z = w - a1/n
            // 
            // S(w) = w^n - |c2|w^n-2 - .. -|cn-1|w - |cn|
            // Let r be its positive root of S(w)
            // Then all roots of P(w) are inside or on circle |w| = r
            // So all roots if P(z) lie in circle |z + c1/n| = r
            // Let r0 > r be approximation of r, then good inital guess is :
            // zk0 = -c1/n + r0 exp( i( (2pi/n)(k-1) + pi/2n) ), (2pi/n)(k-1) + pi/2n) = pi/n (2(k-1) + 0.5)
            // To find r0: 
            // 1) S(0) <= 0
            // 2) Find some r0 for which S(r0) > 0
            EstimateSw();

            float r0 = Find_r0();
            Complex c1n = new Complex(-Poly.Coefficents.At(1) / (float)Poly.Rank, 0.0f);
            float pin = (float)Constants.Pi / ((float)Poly.Rank);

            for(int r = 0; r < Poly.Rank; ++r)
            {
                Roots[r] = c1n + Complex.FromPolarCoordinates(r0, pin * ((float)r + 0.5f));
            }
        }

        public bool CheckIterationEndConditions()
        {
            //bool rootsCloseToZero = true;
            //foreach(var root in Roots)
            //{
            //    var c = Poly.At(root).MagnitudeSquared;
            //    if(Poly.At(root).MagnitudeSquared > MaxZeroError * MaxZeroError)
            //    {
            //        rootsCloseToZero = false;
            //        break;
            //    }
            //}

            return _currentIteration > MaximumIterations ||
                _currentError < _maxError ||
                Math.Abs(_lastError / _currentError - 1.0f) < MinIterationEnchance;
        }

        public void Iterate()
        {
            Vector<Complex> newRoots = new TComplex.DenseVector(Poly.Rank);
            // dzi = P(zi) / (P(zi)sum{k!=i}[1/(zi-zk)] - P'(zi))
            for(int i = 0; i < Roots.Count; ++i)
            {
                Complex p_zi = Poly.At(Roots.At(i));
                Complex dp_zi = _PDiff.At(Roots.At(i));

                Complex sum1z = Complex.Zero;
                for(int k = 0; k < i; ++k)
                {
                    sum1z += (Roots.At(i) - Roots.At(k)).Reciprocal();
                }
                for(int k = i + 1; k < Roots.Count; ++k)
                {
                    sum1z += (Roots.At(i) - Roots.At(k)).Reciprocal();
                }

                Complex dz = p_zi / (p_zi * sum1z - dp_zi);
                newRoots.At(i, Roots.At(i) + dz);
            }
			newRoots.CopyTo(Roots);
        }

        public float Find_r0()
        {
            // Here's an idea :
            // - check if S(1) > 0
            // - if it is, then find wi for which S(wi) < 0 and set r0 = wi
            //  wi = w(i-1) / t, w0 = 1 ( t may be i.e. 2 or 10 )
            // - if S(1) < 0, then set w0 = sum(|ci|), S(w0) then must be > 0
            // - find wi in similar manner:
            // wi = w(i-1) / t
            // or wi = pow(w0, 1/(i+1))
            // or wi = sqrt(w(i-1))
            float s1 = _Sw.At(1.0f);
            float r0 = 1.0f;
            if(s1 > 0.0f)
            {
                do
                {
                    r0 = r0 * 0.5f;
                } while(_Sw.At(r0) > 0.0f);
                r0 = 2.0f * r0;
            }
            else
            {
                r0 = -_Sw.Coefficents.Sum();
                // wi = w(i - 1) / t
                do
                {
                    r0 = r0 * 0.5f;
                    var c = _Sw.At(r0);
                } while(_Sw.At(r0) > 0.0f);
                r0 = 2.0f * r0;
                // wi = pow(w0, 1/(i+1))
                //int i = 1;
                //float w;
                //do
                //{
                //    ++i;
                //    w = (float)Math.Pow(r0, 1.0 / (double)i);
                //} while(_Sw.At(w) > 0.0f);
                //r0 = (float)Math.Pow(r0, 1.0 / (double)(i-1));
                //// wi = wi = sqrt(w(i-1))
                //do
                //{
                //    r0 = (float)Math.Sqrt(r0);
                //} while(_Sw.At(r0) > 0.0f);
                //r0 = r0 * r0;
            }
            return r0;
        }

        public void EstimateSw()
        {
            // Create estimation matrix : each row is [w, P(z)]
            // w = z + c1/n
            // Let z be: 1 + k*(1/rank), so its in range [1-2]
            float c1n = Poly.Coefficents.At(1) / (float)Poly.Rank;
            float r1 = 1.0f / Poly.Rank;
            Matrix<float> estimationMatrix = new DenseMatrix(Poly.Rank, 2);
            for(int r = 0; r < Poly.Rank; ++r)
            {
                float z = 1.0f + r1 * r;
                estimationMatrix.At(r, 0, z);
                estimationMatrix.At(r, 1, Poly.At(z - c1n));
            }
            _Sw = Polynomial.EstimatePolynomial_Monic(estimationMatrix, Poly.Rank);
            for(int i = 1; i < _Sw.Coefficents.Count; ++i)
            {
                _Sw.Coefficents.At(i, -Math.Abs(_Sw.Coefficents.At(i)));
            }
        }

        public void ComputePolyDerivative()
        {
            _PDiff = new Polynomial();
            _PDiff.Rank = Poly.Rank - 1;
            _PDiff.Coefficents = new DenseVector(Poly.Rank);

            for(int i = 0; i < Poly.Rank; ++i)
            {
                _PDiff.Coefficents.At(i, Poly.Coefficents.At(i) * (Poly.Rank - i));
            }
        }

    }
}
