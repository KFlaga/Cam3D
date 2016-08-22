using System.Collections.Generic;
using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;

namespace CalibrationModule
{
    // Minimalising di'^2 = (Axi + Byi + C)^2 is equivalent to di, so e = sum(di'^2) will be used
    // As LM minimises e = ||f(P) - X||, target cost function can be set as:
    // var2) f(P) = [e1_1', e1_2', e1_3',..., eM_N'], X = 0, where el_i = dL_i'^2 is fit error of L-th line, i-th point in line
    // (all other definitions same as in RadialDistortionCorrector)
    //
    // LSM is used to fit line (Ax+By+C=0) to set of points
    // A,B,C are determined up to the scale we can choose B = 1 ( and A = 1 for vertical lines only )
    //   We have then :
    //   B = 1
    //   C = -(A*Ex + Ey)/N
    //   A = (-b +(-)sqrt(D)) / 2a
    //   D = b^2 + 4a^2
    //   a = Exy - Ex*Ey/N
    //   b = E(y^2) - (Ey)^2/N - (E(x^2) - (Ex)^2/N)  
    //
    // - before computing A,B,C check if line is vertical or horizontal :
    //   if N*Ey^2 - (Ey)^2 < err then line is horizontal 
    //   if N*Ex^2 - (Ex)^2 < err then line is vertical
    //   For horizontal lines we have line equation : By + C = 0, for vertical : Ax + C = 0
    //
    //
    public class LMDistortionBasicLineFitMinimalisation : LevenbergMarquardtBaseAlgorithm
    {
        public RadialDistortionModel DistortionModel { get; set; } // Distortion model, needed for d(pu)/d(P)
        public List<List<Vector2>> LinePoints { get; set; } // List of points of lines ( measured ones )
        public List<List<DistortionPoint>> CorrectedPoints { get; set; }
        public Matrix<double> LineCoeffs { get; set; } // Matrix of coeeficients of lines [line][coeff]
        public List<Orientation> LineOrientations { get; protected set; } // Direction of distortion on lines ( after correction points )
        protected List<List<Vector2>> _correctedPf;

        public enum Orientation
        {
            Horizontal, Vertical, Other
        }

        protected int _totalPoints;
        protected Vector<double> _n;
        protected Vector<double> _sumX2;
        protected Vector<double> _sumY2;
        protected Vector<double> _sumXY;
        protected Vector<double> _sumX;
        protected Vector<double> _sumY;

        // d(sum(x))/d(P) = sum(d(x)/d(P))
        protected Vector<double>[] _dEx;
        // d(sum(y))/d(P) = sum(d(y)/d(P))
        protected Vector<double>[] _dEy;
        // d(sum(x^2))/d(P) = sum(2x*d(x)/d(P))
        protected Vector<double>[] _dEx2;
        // d(sum(y^2))/d(P) = sum(2y*d(y)/d(P))
        protected Vector<double>[] _dEy2;
        // d(sum(xy))/d(P) = sum(y*d(x)/d(P)+x*d(y)/d(P))
        protected Vector<double>[] _dExy;

        public override void Init()
        {
            LineCoeffs = new DenseMatrix(LinePoints.Count, 3);
            _sumX2 = new DenseVector(LinePoints.Count);
            _sumY2 = new DenseVector(LinePoints.Count);
            _sumXY = new DenseVector(LinePoints.Count);
            _sumX = new DenseVector(LinePoints.Count);
            _sumY = new DenseVector(LinePoints.Count);

            _dEx = new Vector<double>[LinePoints.Count];
            _dEy = new Vector<double>[LinePoints.Count];
            _dEx2 = new Vector<double>[LinePoints.Count];
            _dEy2 = new Vector<double>[LinePoints.Count];
            _dExy = new Vector<double>[LinePoints.Count];

            _totalPoints = 0;
            _n = new DenseVector(LinePoints.Count);
            _correctedPf = new List<List<Vector2>>(LinePoints.Count);
            CorrectedPoints = new List<List<DistortionPoint>>(LinePoints.Count);
            LineOrientations = new List<Orientation>();
            for(int line = 0; line < LinePoints.Count; ++line)
            {
                LineOrientations.Add(Orientation.Other);
                _n[line] = LinePoints[line].Count;
                _totalPoints += LinePoints[line].Count;

                var corrPoints = new List<DistortionPoint>(LinePoints[line].Count);
                var corrPf = new List<Vector2>(LinePoints[line].Count);

                for(int i = 0; i < LinePoints[line].Count; ++i)
                {
                    var dpoint = CreateDistortionPoint();
                    dpoint.Pi = new Vector2(LinePoints[line][i]);
                    dpoint.Pf = new Vector2(LinePoints[line][i]);
                    dpoint.Pu = new Vector2(LinePoints[line][i]);
                    dpoint.Pd = new Vector2(LinePoints[line][i]);
                    dpoint.Ru = 1.0;
                    dpoint.Rd = 1.0;
                    corrPoints.Add(dpoint);
                    corrPf.Add(dpoint.Pf);
                }

                CorrectedPoints.Add(corrPoints);
                _correctedPf.Add(corrPf);
            }

            MeasurementsVector = new DenseVector(_totalPoints);

            base.Init();
        }

        public virtual DistortionPoint CreateDistortionPoint()
        {
            return new DistortionPoint(DistortionModel.ParametersCount);
        }

        public override void UpdateAll()
        {
            for(int line = 0; line < LinePoints.Count; ++line)
            {
                UpdateAll(line);
            }
        }

        public virtual void UpdateAll(int line)
        {
            UpdateLinePoints(line);
            ComputeSums(line);
            UpdateLineOrientations(line);
            ComputeLineCoeffs(line);
        }

        public void UpdateLinePoints()
        {
            for(int line = 0; line < LinePoints.Count; ++line)
            {
                UpdateLinePoints(line);
            }
        }

        public virtual void UpdateLinePoints(int line)
        {
            var points = LinePoints[line];
            var corrPoints = CorrectedPoints[line];
            var corrPfs = _correctedPf[line];
            for(int p = 0; p < points.Count; ++p)
            {
                var dpoint = corrPoints[p];
                DistortionModel.FullUpdate(dpoint);
                corrPfs[p] = dpoint.Pf;

                if(double.IsNaN(dpoint.Pf.X) || double.IsNaN(dpoint.Pf.Y))
                {
                    dpoint.Pf.X = 0;
                }
            }
        }

        public void ComputeSums()
        {
            for(int line = 0; line < LinePoints.Count; ++line)
            {
                ComputeSums(line);
            }
        }

        public virtual void ComputeSums(int line)
        {
            _sumX2[line] = 0.0;
            _sumY2[line] = 0.0;
            _sumXY[line] = 0.0;
            _sumX[line] = 0.0;
            _sumY[line] = 0.0;

            _dEx[line] = new DenseVector(DistortionModel.ParametersCount);
            _dEy[line] = new DenseVector(DistortionModel.ParametersCount);
            _dEx2[line] = new DenseVector(DistortionModel.ParametersCount);
            _dEy2[line] = new DenseVector(DistortionModel.ParametersCount);
            _dExy[line] = new DenseVector(DistortionModel.ParametersCount);

            var points = LinePoints[line];
            var corrPoints = CorrectedPoints[line];
            for(int p = 0; p < points.Count; ++p)
            {
                var dpoint = corrPoints[p];

                _sumX2[line] += dpoint.Pf.X * dpoint.Pf.X;
                _sumX[line] += dpoint.Pf.X;
                _sumY2[line] += dpoint.Pf.Y * dpoint.Pf.Y;
                _sumY[line] += dpoint.Pf.Y;
                _sumXY[line] += dpoint.Pf.X * dpoint.Pf.Y;

                _dEx[line] += dpoint.Diff_Xf;
                _dEy[line] += dpoint.Diff_Yf;
                _dEx2[line] += 2.0f * dpoint.Pf.X * dpoint.Diff_Xf;
                _dEy2[line] += 2.0f * dpoint.Pf.Y * dpoint.Diff_Yf;
                _dExy[line] += dpoint.Pf.Y * dpoint.Diff_Xf + dpoint.Pf.X * dpoint.Diff_Yf;
            }
        }

        public double Get_a(int l)
        {
            return _sumXY[l] - _sumX[l] * _sumY[l] / _n[l];
        }

        public double Get_b(int l)
        {
            return _sumY2[l] - _sumX2[l] + (_sumX[l] * _sumX[l] - _sumY[l] * _sumY[l]) / _n[l];
        }

        public double Get_D(int l)
        {
            double a = Get_a(l);
            double b = Get_b(l);
            return b * b + 4 * a * a;
        }

        public double Get_A(int l)
        {
            double a = Get_a(l);
            double b = Get_b(l);
            double D = Get_D(l);

            return (-b - Math.Sqrt(Get_D(l))) / (2 * a);
        }

        public double Get_C(int l)
        {
            return -(LineCoeffs[l, 0] * _sumX[l] + LineCoeffs[l, 1] * _sumY[l]) / _n[l];
        }

        public void ComputeLineCoeffs()
        {
            for(int line = 0; line < LinePoints.Count; ++line)
            {
                ComputeLineCoeffs(line);
            }
        }

        public virtual void ComputeLineCoeffs(int line)
        {
            if(LineOrientations[line] == Orientation.Horizontal)
            {
                LineCoeffs[line, 0] = 0.0f;
                LineCoeffs[line, 1] = 1.0f;
                LineCoeffs[line, 2] = -(LineCoeffs[line, 1] * _sumY[line]) / _n[line];
            }
            else if(LineOrientations[line] == Orientation.Vertical)
            {
                LineCoeffs[line, 0] = 1.0f;
                LineCoeffs[line, 1] = 0.0f;
                LineCoeffs[line, 2] = -(LineCoeffs[line, 0] * _sumX[line]) / _n[line];
            }
            else
            {
                LineCoeffs[line, 0] = Get_A(line);
                LineCoeffs[line, 1] = 1.0f;
                LineCoeffs[line, 2] = Get_C(line);
            }
        }

        public bool CheckLineIsVertical(int line)
        {
            return Math.Abs(_n[line] * _sumX2[line] - _sumX[line] * _sumX[line]) < Math.Abs(_sumX[line]) * 0.05;
        }

        public bool CheckLineIsHorizontal(int line)
        {
            return Math.Abs(_n[line] * _sumY2[line] - _sumY[line] * _sumY[line]) < Math.Abs(_sumY[line]) * 0.05;
        }

        public void UpdateLineOrientations()
        {
            for(int line = 0; line < LinePoints.Count; ++line)
            {
                UpdateLineOrientations(line);
            }
        }

        public virtual void UpdateLineOrientations(int line)
        {
            if(CheckLineIsVertical(line))
                LineOrientations[line] = Orientation.Vertical;
            else if(CheckLineIsHorizontal(line))
                LineOrientations[line] = Orientation.Horizontal;
            else
                LineOrientations[line] = Orientation.Other;
        }

        public virtual double ComputeErrorForPoint(int l, int p)
        {
            // e = dL_i'^2 = (Axi+Byi+C)^2 / A^2 + B^2
            var point = CorrectedPoints[l][p];
            double d = LineCoeffs[l, 0] * point.Pf.X + LineCoeffs[l, 1] * point.Pf.Y + LineCoeffs[l, 2];
            double error = d * d / (LineCoeffs[l, 0] * LineCoeffs[l, 0] + LineCoeffs[l, 1] * LineCoeffs[l, 1]);
            return error;
        }

        public override void ComputeMappingFucntion(Vector<double> mapFuncResult)
        {
            int idx = 0;
            for(int l = 0; l < LinePoints.Count; ++l)
            {
                var line = LinePoints[l];
                for(int p = 0; p < line.Count; ++p)
                {
                    mapFuncResult[idx] = ComputeErrorForPoint(l, p);
                    ++idx;
                }
            }
        }

        // Error here is equal to mapping function
        public override void ComputeErrorVector(Vector<double> error)
        {
            ComputeMappingFucntion(error);
        }

        #region TEST_METHODS

        public Vector<double> GetDiff_a(int l)
        {
            double Ex = _sumX[l];
            double Ey = _sumY[l];
            double Exy = _sumXY[l];
            Vector<double> dEx = _dEx[l];
            Vector<double> dEy = _dEy[l];
            Vector<double> dExy = _dExy[l];
            double N = _n[l];

            // d(a) = d(Exy) - 1/N * (Ey*d(Ex) + *Ex*d(Ey))
            Vector<double> diff_a = dExy - (1.0 / N) * (dEx * Ey + dEy * Ex);

            return diff_a;
        }

        public Vector<double> GetDiff_b(int l)
        {
            double Ex = _sumX[l];
            double Ey = _sumY[l];
            double Ex2 = _sumX2[l];
            double Ey2 = _sumY2[l];
            Vector<double> dEx = _dEx[l];
            Vector<double> dEy = _dEy[l];
            Vector<double> dEx2 = _dEx2[l];
            Vector<double> dEy2 = _dEy2[l];
            double N = _n[l];

            // d(b) = d(Ey2) - 2/N * Ey * d(Ey) - d(Ex2) + 2/N * Ex * d(Ex)
            Vector<double> diff_b = dEy2 - dEx2 + (2.0 / N) * (dEx * Ex - dEy * Ey);

            return diff_b;
        }

        public Vector<double> GetDiff_D(int l)
        {
            double Ex = _sumX[l];
            double Ey = _sumY[l];
            double Exy = _sumXY[l];
            double Ex2 = _sumX2[l];
            double Ey2 = _sumY2[l];
            Vector<double> dEx = _dEx[l];
            Vector<double> dEy = _dEy[l];
            Vector<double> dExy = _dExy[l];
            Vector<double> dEx2 = _dEx2[l];
            Vector<double> dEy2 = _dEy2[l];
            double N = _n[l];

            double a = Get_a(l);
            double b = Get_b(l);
            double D = Get_D(l);

            Vector<double> diff_a = GetDiff_a(l);
            Vector<double> diff_b = GetDiff_b(l);

            // d(D) = 2b*d(b) + 8a*d(a)
            Vector<double> diff_D = (2.0 * b) * diff_b + (8.0 * a) * diff_a;

            return diff_D;
        }

        public Vector<double> GetDiff_A(int l)
        {
            double a = Get_a(l);
            double b = Get_b(l);
            double D = Get_D(l);

            Vector<double> diff_a = GetDiff_a(l);
            Vector<double> diff_b = GetDiff_b(l);
            Vector<double> diff_D = GetDiff_D(l);

            // d(A) = -a(d(b) + 1/2sqrt(D) * d(D)) + d(a)(b+sqrt(D)) / 2a^2
            Vector<double> diff_A = (0.5 / (a * a)) *
                ((b + Math.Sqrt(D)) * diff_a - a * (diff_b + (0.5 / Math.Sqrt(D)) * diff_D));

            return diff_A;
        }

        public Vector<double> GetDiff_C(int l)
        {
            double A = LineCoeffs[l, 0];
            double Ex = _sumX[l];
            double Ey = _sumY[l];
            Vector<double> dEx = _dEx[l];
            Vector<double> dEy = _dEy[l];
            double N = _n[l];

            Vector<double> diff_A = GetDiff_A(l);
            Vector<double> diff_C = (-1.0 / N) * (Ex * diff_A + A * dEx + dEy);

            return diff_C;
        }

        public double Get_d1(int l, int p)
        {
            double A = LineCoeffs[l, 0];
            double C = LineCoeffs[l, 2];

            var point = CorrectedPoints[l][p];
            return A * point.Pf.X + point.Pf.Y + C;
        }

        public Vector<double> GetDiff_d1(int l, int p)
        {
            double A = LineCoeffs[l, 0];

            Vector<double> diff_a = GetDiff_a(l);
            Vector<double> diff_A = GetDiff_A(l);
            Vector<double> diff_C = GetDiff_C(l);

            var point = CorrectedPoints[l][p];
            Vector<double> diff_dist = diff_A * point.Pf.X +
                A * point.Diff_Xf + point.Diff_Yf + diff_C;

            return diff_dist;
        }

        #endregion

        // Computes d(ei)/d(P) for ith line
        public virtual void ComputeJacobianForLine(Matrix<double> J, int l, int p0)
        {
            double A = LineCoeffs[l, 0];
            double B = LineCoeffs[l, 1];
            double C = LineCoeffs[l, 2];
            double Ex = _sumX[l];
            double Ey = _sumY[l];
            double Exy = _sumXY[l];
            double Ex2 = _sumX2[l];
            double Ey2 = _sumY2[l];
            Vector<double> dEx = _dEx[l];
            Vector<double> dEy = _dEy[l];
            Vector<double> dExy = _dExy[l];
            Vector<double> dEx2 = _dEx2[l];
            Vector<double> dEy2 = _dEy2[l];
            double N = _n[l];

            //   A = (-b - sqrt(D)) / 2a
            //   D = b^2 + 4a^2
            //   a = Exy - Ex*Ey/N
            //   b = E(y^2) - (Ey)^2/N - (E(x^2) - (Ex)^2/N)
            //double a = Exy - Ex * Ey / N;
            //double b = Ey2 - Ex2 + (Ex * Ex - Ey * Ey) / N;
            //double D = b * b + 4 * a * a;

            double a = Get_a(l);
            double b = Get_b(l);
            double D = Get_D(l);

            // d(a) = d(Exy) - 1/N * (Ey*d(Ex) + *Ex*d(Ey))
            Vector<double> diff_a = dExy - (1.0 / N) * (dEx * Ey + dEy * Ex);

            // d(b) = d(Ey2) - 2/N * Ey * d(Ey) - d(Ex2) + 2/N * Ex * d(Ex)
            Vector<double> diff_b = dEy2 - dEx2 + (2.0 / N) * (dEx * Ex - dEy * Ey);

            // d(D) = 2b*d(b) + 8a*d(a)
            Vector<double> diff_D = (2.0 * b) * diff_b + (8.0 * a) * diff_a;

            // d(A) = -a(d(b) + 1/2sqrt(D) * d(D)) + d(a)(b+sqrt(D)) / 2a^2
            Vector<double> diff_A = (0.5 / (a * a)) *
                ((b + Math.Sqrt(D)) * diff_a - a * (diff_b + (0.5 / Math.Sqrt(D)) * diff_D));

            // d(C)/d(P) = -(1/N)(Ex*d(A) + A*d(Ex) + d(Ey))
            Vector<double> diff_C = (-1.0 / N) * (Ex * diff_A + A * dEx + dEy);

            double A212 = 1.0 / ((A * A + 1.0) * (A * A + 1.0));
            var line = CorrectedPoints[l];
            for(int p = 0; p < line.Count; ++p)
            {
                var point = line[p];
                double dist = A * point.Pf.X + point.Pf.Y + C;
                Vector<double> diff_dist = diff_A * point.Pf.X + A * point.Diff_Xf + point.Diff_Yf + diff_C;
                // J[point, k] = d(e)/d(P[k]) =
                //  2 * (Ax+y+C)/(A^2+1)^2 * d((Ax+y+C)) - A*d(A)*(Ax+y+C)
                //  d((Ax+y+C)) = a(A)*x + A*d(x) + d(y) + d(C)
                Vector<double> diff_e = (2.0 * dist * A212) * (
                    diff_dist * (A * A + 1.0) - (A * dist) * diff_A);

                // Throw if NaN found in jacobian
                bool nanInfFound = diff_e.Exists((e) => { return double.IsNaN(e) || double.IsInfinity(e); });
                if(nanInfFound)
                {
                    throw new DivideByZeroException("NaN or Infinity found on jacobian");
                }

                J.SetRow(p0 + p, diff_e);
            }
        }

        // Computes d(ei)/d(P) for ith line numericaly
        public void ComputeJacobianForLine_Numerical(Matrix<double> J, int l, int p0)
        {
            Vector<double> error_n = new DenseVector(_currentErrorVector.Count);
            Vector<double> error_p = new DenseVector(_currentErrorVector.Count);
            for(int k = 0; k < DistortionModel.ParametersCount; ++k)
            {
                double oldK = DistortionModel.Parameters[k];
                double k_n = Math.Abs(oldK) > float.Epsilon ? oldK * (1 - NumericalDerivativeStep) : -NumericalDerivativeStep * 0.01;
                double k_p = Math.Abs(oldK) > float.Epsilon ? oldK * (1 + NumericalDerivativeStep) : NumericalDerivativeStep * 0.01;

                DistortionModel.Parameters[k] = k_n;
                UpdateAll(l);
                ComputeErrorVector(error_n);
                Vector<double> error_n_line = error_n.SubVector(p0, LinePoints[l].Count);

                DistortionModel.Parameters[k] = k_p;
                UpdateAll(l);
                ComputeErrorVector(error_p);
                Vector<double> error_p_line = error_p.SubVector(p0, LinePoints[l].Count);

                Vector<double> diff_e = (1.0 / (k_p - k_n)) * (error_p_line - error_n_line);
                J.SetColumn(k, p0, LinePoints[l].Count, diff_e);

                DistortionModel.Parameters[k] = oldK;

                // Throw if NaN found in jacobian
                bool nanInfFound = diff_e.Exists((e) => { return double.IsNaN(e) || double.IsInfinity(e); });
                if(nanInfFound)
                {
                    throw new DivideByZeroException("NaN or Infinity found on jacobian");
                }
            }

            UpdateAll(l);
        }

        // Computes d(ei)/d(P) for ith line numericaly
        public void ComputeJacobian_Numerical(Matrix<double> J)
        {
            Vector<double> error_n = new DenseVector(_currentErrorVector.Count);
            Vector<double> error_p = new DenseVector(_currentErrorVector.Count);
            for(int k = 0; k < DistortionModel.ParametersCount; ++k)
            {
                double oldK = DistortionModel.Parameters[k];
                double k_n = Math.Abs(oldK) > float.Epsilon ? oldK * (1 - NumericalDerivativeStep) : -NumericalDerivativeStep * 0.01;
                double k_p = Math.Abs(oldK) > float.Epsilon ? oldK * (1 + NumericalDerivativeStep) : NumericalDerivativeStep * 0.01;

                DistortionModel.Parameters[k] = k_n;
                UpdateAll();
                ComputeErrorVector(error_n);

                DistortionModel.Parameters[k] = k_p;
                UpdateAll();
                ComputeErrorVector(error_p);

                Vector<double> diff_e = 1.0 / (k_p - k_n) * (error_p - error_n);
                J.SetColumn(k, diff_e);

                DistortionModel.Parameters[k] = oldK;

                // Throw if NaN found in jacobian
                bool nanInfFound = diff_e.Exists((e) => { return double.IsNaN(e) || double.IsInfinity(e); });
                if(nanInfFound)
                {
                    throw new DivideByZeroException("NaN or Infinity found on jacobian");
                }
            }

            UpdateAll();
        }

        public override void ComputeJacobian(Matrix<double> J)
        {
            J.Clear();

            if(DoComputeJacobianNumerically)
            {
                ComputeJacobian_Numerical(J);
            }
            else
            {
                int pointIdx = 0;
                for(int line = 0; line < LinePoints.Count; ++line)
                {
                    if(LineOrientations[line] != Orientation.Other)
                        ComputeJacobianForLine_Numerical(J, line, pointIdx);
                    else
                        ComputeJacobianForLine(J, line, pointIdx);
                    pointIdx += LinePoints[line].Count;
                }
            }
        }

        public override void Iterate()
        {   
            // Start with computing error
            ComputeErrorVector(_currentErrorVector);
            
            ComputeDelta(_delta);
            var oldRes = ResultsVector.Clone();

            ResultsVector += _delta;
            ResultsVector.CopyTo(DistortionModel.Parameters);
            UpdateAll();

            _lastResidiual = _currentResidiual;
            // Compute new residiual
            ComputeErrorVector(_currentErrorVector);
            _currentResidiual = ComputeResidiual();
            
            if(MinimumResidiual > _currentResidiual)
            {
                MinimumResidiual = _currentResidiual;
                ResultsVector.CopyTo(BestResultVector);
            }
            else
            {
                oldRes.CopyTo(ResultsVector);
            }

            if(_currentResidiual < MinimumResidiual * 1.01)
            {
                // Update lambda -> lower only if new residiual is good enough
                // (1% difference as it tends to stuck with high lambda and almost no change in results)
                _lam *= 0.1;
            }
            else if(_currentResidiual >= _lastResidiual)
            {
                _lam *= 10.0;
            }
        }

        // Returns just sum of errors
        public override double ComputeResidiual()
        {
            ComputeErrorVector(_currentErrorVector);
            return _currentErrorVector.Sum();
            // return _currentErrorVector.DotProduct(_currentErrorVector);
        }
    }
}
