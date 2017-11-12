using CamAlgorithms.Triangulation;
using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace CamAlgorithms.Calibration
{
    // Corrects camera matrices of cameras calibrated separately using
    // information on matching points : changes values
    // of cameras matrices so that distance from epiline corresponding to
    // base pixel to matched pixel is minimised
    // Uses camera matrices/etc from CalibrationData
    // Error :
    // e = a*sum[d(e(pb),pm)^2] + D( B1*d(CM1, eCM1)^2 + B2*d(CM2, eCM2)^2 )
    // Error contains also distance between camera paramters to ensure that estimated cameras
    // are as close as possible to original ones and scaling paramteres a and B1, B2, 
    // where B1/B2 are matrices scaling each d(CM(r,c),eCM(r,c))
    // 
    // As computing B1/B2 is quite difficult task ( for sure more then rest of minimisation problem )
    // instead of d(CM), same error as for sepearate calibration will be used.
    // In addition to CMs which will serve as initial parameters, estimated grids should be supplied
    // Over-all error is then e = c1*Ematch/Nm + c2*(Eimg1 + Eimg2)/Ni + c3*(Ereal1 + Ereal2)/Nr
    // Errors are scaled by (c1,c2,c3) and divided by used points count
    // Of course due to quantization match wont be ideal -> true image points could be estimated as well, but how to combine errors?
    //
    // Ok, so parameter vector is :
    // P[0-11] -> CM1
    // P[12-23] -> CM2
    //
    // Measurement vector :
    // - well dont use it
    // 
    // Error vector :
    // e[Nm] = c1*Ematch/Nm
    // e[Ni] = c2*Eimg/Nc
    // e[Ng] = c3*Egrid/Ng
    //
    // Error match:
    // - having point pair p1, p2, find l2 = epi(p1), l1 = epi(p2)
    // - error = d(l1,p1)^2 + d(l2,p2)^2
    // - so for each pair we have e[n] = d(l1,p1), e[n+1] = d(l2,p2)
    // - for l = [a,b,c] normalized line ln = [a/D,b/D,c/D] where D = sqrt(A^2+B^2)
    // - d(p,l) = l * p (assuming p.w = 1)
    // 
    // Error img/grid same as in LMCMGridMini
    public class CrossCalibrationRefiner : LevenbergMarquardtBaseAlgorithm, IControllableAlgorithm, IParameterizable
    {
        TwoPointsTriangulation _triangulation = new TwoPointsTriangulation();

        public List<CalibrationPoint> CalibPointsLeft { set; get; }
        public List<CalibrationPoint> CalibPointsRight { set; get; }

        // public List<RealGridData> GridsLeft { get; set; }
        public List<RealGridData> CalibGrids { get; set; }
        RealGridData[] _grids;
        //RealGridData[] _gridsRight;

        public List<Vector2> MatchedPointsLeft { set; get; }
        public List<Vector2> MatchedPointsRight { set; get; }

        public double GridsErrorCoeff { get; set; }
        public double ImagesErrorCoeff { get; set; }
        public double MatchErrorCoeff { get; set; }
        public double TriangulationErrorCoeff { get; set; }
        double _coeffGrids;
        double _coeffImages;
        double _coeffMatch;
        double _coeffTriang;

        private Mutex _refreshMutex = new Mutex(false);

        Vector<double>[] _reals;
        // Vector<double>[] _realsRight;
        Vector<double>[] _imgsLeft;
        Vector<double>[] _imgsRight;

        int _cameraParamsCount = 11;
       // Vector2 _imgCenterLeft;
       // Vector2 _imgCenterRight;

        int _fxIdx = 0;
        int _fyIdx = 1;
        int _skIdx = 2;
        int _pxIdx = 3;
        int _pyIdx = 4;
        int _euXIdx = 5;
        int _euYIdx = 6;
        int _euZIdx = 7;
        int _cXIdx = 8;
        int _cYIdx = 9;
        int _cZIdx = 10;

        CameraPair Cameras { get { return CameraPair.Data; } }
        
        public override void Process()
        {
            Status = AlgorithmStatus.Running;
            Terminate = false;
            base.Process();
            Status = AlgorithmStatus.Finished;
        }

        public override void Init()
        {
            //ParametersVector = new DenseVector(24);
            //for(int i = 0; i < 12; ++i)
            //{
            //    ParametersVector.At(i, CameraLeft.At(i / 4, i & 3));
            //    ParametersVector.At(i + 12, CameraRight.At(i / 4, i & 3));
            //}

            // Parameters:
            // Full: [fx, fy, s, px, py, eaX, eaY, eaZ, Cx, Cy, Cz]
            // Center fixed: [fx, fy, s, eaX, eaY, eaZ, Cx, Cy, Cz]
            ParametersVector = new DenseVector(_cameraParamsCount * 2);
            if(_fxIdx >= 0) ParametersVector.At(_fxIdx, Cameras.Left.InternalMatrix.At(0, 0));
            if(_fyIdx >= 0) ParametersVector.At(_fyIdx, Cameras.Left.InternalMatrix.At(1, 1));
            if(_skIdx >= 0) ParametersVector.At(_skIdx, Cameras.Left.InternalMatrix.At(0, 1));
            if(_pxIdx >= 0) ParametersVector.At(_pxIdx, Cameras.Left.InternalMatrix.At(0, 2));
            if(_pyIdx >= 0) ParametersVector.At(_pyIdx, Cameras.Left.InternalMatrix.At(1, 2));

            Vector<double> euler = new DenseVector(3);
            RotationConverter.MatrixToEuler(euler, Cameras.Left.RotationMatrix);
            if(_euXIdx >= 0) ParametersVector.At(_euXIdx, euler.At(0));
            if(_euYIdx >= 0) ParametersVector.At(_euYIdx, euler.At(1));
            if(_euZIdx >= 0) ParametersVector.At(_euZIdx, euler.At(2));

            if(_cXIdx >= 0) ParametersVector.At(_cXIdx, Cameras.Left.Translation.At(0));
            if(_cYIdx >= 0) ParametersVector.At(_cYIdx, Cameras.Left.Translation.At(1));
            if(_cZIdx >= 0) ParametersVector.At(_cZIdx, Cameras.Left.Translation.At(2));

            int n0 = _cameraParamsCount;
            if(_fxIdx >= 0) ParametersVector.At(_fxIdx + n0, Cameras.Right.InternalMatrix.At(0, 0));
            if(_fyIdx >= 0) ParametersVector.At(_fyIdx + n0, Cameras.Right.InternalMatrix.At(1, 1));
            if(_skIdx >= 0) ParametersVector.At(_skIdx + n0, Cameras.Right.InternalMatrix.At(0, 1));
            if(_pxIdx >= 0) ParametersVector.At(_pxIdx + n0, Cameras.Right.InternalMatrix.At(0, 2));
            if(_pyIdx >= 0) ParametersVector.At(_pyIdx + n0, Cameras.Right.InternalMatrix.At(1, 2));

            RotationConverter.MatrixToEuler(euler, Cameras.Right.RotationMatrix);
            if(_euXIdx >= 0) ParametersVector.At(_euXIdx + n0, euler.At(0));
            if(_euYIdx >= 0) ParametersVector.At(_euYIdx + n0, euler.At(1));
            if(_euZIdx >= 0) ParametersVector.At(_euZIdx + n0, euler.At(2));

            if(_cXIdx >= 0) ParametersVector.At(_cXIdx + n0, Cameras.Right.Translation.At(0));
            if(_cYIdx >= 0) ParametersVector.At(_cYIdx + n0, Cameras.Right.Translation.At(1));
            if(_cZIdx >= 0) ParametersVector.At(_cZIdx + n0, Cameras.Right.Translation.At(2));

            //_imgCenterLeft = new Vector2(Cameras.Left.InternalMatrix.At(0, 2),
            //    Cameras.Left.InternalMatrix.At(1, 2));
            //_imgCenterRight = new Vector2(Cameras.Right.InternalMatrix.At(0, 2),
            //    Cameras.Right.InternalMatrix.At(1, 2));

            ResultsVector = new DenseVector(ParametersVector.Count + CalibGrids.Count * 12);
            BestResultVector = new DenseVector(ResultsVector.Count);
            ParametersVector.CopySubVectorTo(ResultsVector, 0, 0, ParametersVector.Count);

            _grids = new RealGridData[CalibGrids.Count];

            int N = ParametersVector.Count;
            for(int i = 0; i < CalibGrids.Count; ++i)
            {
                ResultsVector.At(N + i * 12, CalibGrids[i].TopLeft.X);
                ResultsVector.At(N + i * 12 + 1, CalibGrids[i].TopLeft.Y);
                ResultsVector.At(N + i * 12 + 2, CalibGrids[i].TopLeft.Z);
                ResultsVector.At(N + i * 12 + 3, CalibGrids[i].TopRight.X);
                ResultsVector.At(N + i * 12 + 4, CalibGrids[i].TopRight.Y);
                ResultsVector.At(N + i * 12 + 5, CalibGrids[i].TopRight.Z);
                ResultsVector.At(N + i * 12 + 6, CalibGrids[i].BotLeft.X);
                ResultsVector.At(N + i * 12 + 7, CalibGrids[i].BotLeft.Y);
                ResultsVector.At(N + i * 12 + 8, CalibGrids[i].BotLeft.Z);
                ResultsVector.At(N + i * 12 + 9, CalibGrids[i].BotRight.X);
                ResultsVector.At(N + i * 12 + 10, CalibGrids[i].BotRight.Y);
                ResultsVector.At(N + i * 12 + 11, CalibGrids[i].BotRight.Z);
                _grids[i] = new RealGridData();
                _grids[i].Columns = CalibGrids[i].Columns;
                _grids[i].Rows = CalibGrids[i].Rows;
            }
            ResultsVector.CopyTo(BestResultVector);

            _coeffMatch = MatchedPointsLeft.Count > 0 ? Math.Sqrt(MatchErrorCoeff * 0.5 / (double)MatchedPointsLeft.Count) : 0;
            _coeffImages = Math.Sqrt(ImagesErrorCoeff * 0.5 / (double)(CalibPointsLeft.Count + CalibPointsRight.Count));
            _coeffGrids = Math.Sqrt(GridsErrorCoeff * (CalibPointsLeft.Count + CalibPointsRight.Count) / (double)(CalibGrids.Count * 12));
            _coeffTriang = Math.Sqrt(TriangulationErrorCoeff * 0.33 / (double)CalibPointsLeft.Count);

            _currentErrorVector = new DenseVector(
                MatchedPointsLeft.Count * 2 + // Matched
                CalibPointsLeft.Count * 3 + // Triangulation
                CalibPointsLeft.Count * 2 + CalibPointsRight.Count * 2 + // Image
                CalibGrids.Count * 12); // Grids

            _J = new DenseMatrix(_currentErrorVector.Count, ResultsVector.Count);
            _Jt = new DenseMatrix(ResultsVector.Count, _currentErrorVector.Count);
            _JtJ = new DenseMatrix(ResultsVector.Count, ResultsVector.Count);
            _Jte = new DenseVector(ResultsVector.Count);
            _delta = new DenseVector(ResultsVector.Count);

            _reals = new Vector<double>[CalibPointsLeft.Count];
            _imgsLeft = new Vector<double>[CalibPointsLeft.Count];
            _imgsRight = new Vector<double>[CalibPointsRight.Count];
            
            _triangulation.UseLinearEstimationOnly = true;
            _triangulation.PointsLeft = new List<Vector<double>>(CalibPointsLeft.Count);
            _triangulation.PointsRight = new List<Vector<double>>(CalibPointsLeft.Count);
            for(int i = 0; i < CalibPointsLeft.Count; ++i)
            {
                _triangulation.PointsLeft.Add(CalibPointsLeft[i].Img.ToMathNetVector3());
                _triangulation.PointsRight.Add(CalibPointsRight[i].Img.ToMathNetVector3());
            }

            UseCovarianceMatrix = false;
            DumpingMethodUsed = DumpingMethod.Multiplicative;
            UpdateAfterParametersChanged();
            _lam = 1e-3f;

            _lastResidiual = _currentResidiual;
        }

        public override void UpdateAfterParametersChanged()
        {
            while(_refreshMutex.WaitOne() == false) { };
            base.UpdateAfterParametersChanged();

            // Update camera matrices / fundamental
            //Matrix<double> camLeft = new DenseMatrix(3, 4);
            //Matrix<double> camRight = new DenseMatrix(3, 4);
            //for(int i = 0; i < 12; ++i)
            //{
            //    camLeft.At(i / 4, i & 3, ResultsVector.At(i));
            //    camRight.At(i / 4, i & 3, ResultsVector.At(i + 12));
            //}
            // CameraLeft = camLeft;
            // CameraRight = camRight;
            Cameras.Left.Matrix = CameraMatrixFromParameterization(0);
            Cameras.Right.Matrix = CameraMatrixFromParameterization(9);
            Cameras.Update();

            UpdateGrids(_grids, ParametersVector.Count);
            UpdateRealPoints(CalibPointsLeft, _grids, _reals);
            UpdateImagePoints();

            CameraPair cdata = new CameraPair();
            cdata.Left = Cameras.Left.Clone();
            cdata.Right = Cameras.Right.Clone();
            _triangulation.Cameras = cdata;
           // _triangulation.PointsLeft = new List<Vector<double>>(_imgsLeft);
           // _triangulation.PointsRight = new List<Vector<double>>(_imgsRight);
            _triangulation.Estimate3DPoints();

            _refreshMutex.ReleaseMutex();
        }

        private Matrix<double> CameraMatrixFromParameterization(int n0)
        {
            // Parameters:
            // Full: [fx, fy, s, px, py, eaX, eaY, eaZ, Cx, Cy, Cz]
            // Center fixed: [fx, fy, s, eaX, eaY, eaZ, Cx, Cy, Cz]
            // Aspect fixed: [f, eaX, eaY, eaZ, Cx, Cy, Cz]
            //
            // Camera : M = KR[I|-C]
            double fx = ParametersVector.At(n0 + _fxIdx);
            double fy = ParametersVector.At(n0 + _fyIdx);
            double sk = ParametersVector.At(n0 + _skIdx);
            double px = ParametersVector.At(n0 + _pxIdx);
            double py = ParametersVector.At(n0 + _pyIdx);
            double eX = ParametersVector.At(n0 + _euXIdx);
            double eY = ParametersVector.At(n0 + _euYIdx);
            double eZ = ParametersVector.At(n0 + _euZIdx);
            Vector<double> euler = new DenseVector(new double[3] { eX, eY, eZ });
            double cX = ParametersVector.At(n0 + _cXIdx);
            double cY = ParametersVector.At(n0 + _cYIdx);
            double cZ = ParametersVector.At(n0 + _cZIdx);
            Vector<double> center = new DenseVector(new double[3] { -cX, -cY, -cZ });

            Matrix<double> intMat = new DenseMatrix(3, 3);
            intMat.At(0, 0, fx);
            intMat.At(0, 1, sk);
            intMat.At(0, 2, px);
            intMat.At(1, 1, fy);
            intMat.At(1, 2, py);
            intMat.At(2, 2, 1.0);

            Matrix<double> rotMat = new DenseMatrix(3, 3);
            RotationConverter.EulerToMatrix(euler, rotMat);

            Matrix<double> extMat = new DenseMatrix(3, 4);
            extMat.SetSubMatrix(0, 0, rotMat);
            extMat.SetColumn(3, rotMat * center);

            return intMat * extMat;
        }

        private void UpdateImagePoints()
        {
            for(int i = 0; i < CalibPointsLeft.Count; ++i)
            {
                _imgsLeft[i] = Cameras.Left.Matrix * _reals[i];
                _imgsLeft[i].DivideThis(_imgsLeft[i].At(2));
            }

            for(int i = 0; i < CalibPointsRight.Count; ++i)
            {
                _imgsRight[i] = Cameras.Right.Matrix * _reals[i];
                _imgsRight[i].DivideThis(_imgsRight[i].At(2));
            }
        }

        private void UpdateRealPoints(List<CalibrationPoint> cpoints, RealGridData[] grids, Vector<double>[] reals)
        {
            for(int i = 0; i < CalibPointsLeft.Count; ++i)
            {
                var cp = cpoints[i];
                var grid = grids[cp.GridNum];
                var real = grid.GetRealFromCell(cp.RealRow, cp.RealCol);
                reals[i] = new DenseVector(new double[4] { real.X, real.Y, real.Z, 1.0 });
            }
        }

        private void UpdateGrids(RealGridData[] grids, int n0)
        {
            for(int i = 0; i < CalibGrids.Count; ++i)
            {
                int gridPos = 12 * i + n0;
                grids[i].TopLeft.X = ResultsVector.At(gridPos);
                grids[i].TopLeft.Y = ResultsVector.At(gridPos + 1);
                grids[i].TopLeft.Z = ResultsVector.At(gridPos + 2);
                grids[i].TopRight.X = ResultsVector.At(gridPos + 3);
                grids[i].TopRight.Y = ResultsVector.At(gridPos + 4);
                grids[i].TopRight.Z = ResultsVector.At(gridPos + 5);
                grids[i].BotLeft.X = ResultsVector.At(gridPos + 6);
                grids[i].BotLeft.Y = ResultsVector.At(gridPos + 7);
                grids[i].BotLeft.Z = ResultsVector.At(gridPos + 8);
                grids[i].BotRight.X = ResultsVector.At(gridPos + 9);
                grids[i].BotRight.Y = ResultsVector.At(gridPos + 10);
                grids[i].BotRight.Z = ResultsVector.At(gridPos + 11);
            }
        }

        public override void ComputeMappingFucntion(Vector<double> mapFuncResult)
        {
            // exi = Lxi/Mi, eyi = Lyi/Mi
            //for(int i = 0; i < CalibrationPoints.Count; ++i)
            //{
            //    mapFuncResult.At(2 * i, _Lx.At(i) / _M.At(i));
            //    mapFuncResult.At(2 * i + 1, _Ly.At(i) / _M.At(i));
            //}
        }

        public override void ComputeErrorVector(Vector<double> error)
        {
            while(_refreshMutex.WaitOne() == false) { };

            for(int i = 0; i < MatchedPointsLeft.Count; ++i)
            {
                double dL, dR;
                FindMatchingDistance(i, out dL, out dR);
                // - error = d(l1,p1)^2 + d(l2,p2)^2
                // - so for each pair we have e[n] = d(l1,p1), e[n+1] = d(l2,p2)
                error.At(2 * i, dL * _coeffMatch);
                error.At(2 * i + 1, dR * _coeffMatch);
            }

            int n0 = MatchedPointsLeft.Count * 2;
            for(int i = 0; i < CalibPointsLeft.Count; ++i)
            {
                double dy, dx, dz;
                FindTriangulationDistance(i, out dx, out dy, out dz);
                // - error = d(l1,p1)^2 + d(l2,p2)^2
                // - so for each pair we have e[n] = d(l1,p1), e[n+1] = d(l2,p2)
                error.At(n0 + 3 * i, dx * _coeffTriang);
                error.At(n0 + 3 * i + 1, dy * _coeffTriang);
                error.At(n0 + 3 * i + 2, dz * _coeffTriang);
            }

            n0 += CalibPointsLeft.Count * 3;
            for(int i = 0; i < CalibPointsLeft.Count; ++i)
            {
                error.At(n0 + 2 * i, (CalibPointsLeft[i].ImgX - _imgsLeft[i].At(0)) * _coeffImages);
                error.At(n0 + 2 * i + 1, (CalibPointsLeft[i].ImgY - _imgsLeft[i].At(1)) * _coeffImages);
            }

            n0 += CalibPointsLeft.Count * 2;
            for(int i = 0; i < CalibPointsRight.Count; ++i)
            {
                error.At(n0 + 2 * i, (CalibPointsRight[i].ImgX - _imgsRight[i].At(0)) * _coeffImages);
                error.At(n0 + 2 * i + 1, (CalibPointsRight[i].ImgY - _imgsRight[i].At(1)) * _coeffImages);
            }

            n0 += CalibPointsRight.Count * 2;
            for(int i = 0; i < CalibGrids.Count; ++i)
            {
                SetGridError(error, _grids[i], CalibGrids[i], n0 + 12 * i);
            }

            _refreshMutex.ReleaseMutex();
        }

        public void FindTriangulationDistance(int index, out double dx, out double dy, out double dz)
        {
            var p3d = _triangulation.Points3D[index];
            var cp = CalibPointsLeft[index];
            var d = p3d - CalibGrids[cp.GridNum].GetRealFromCell(cp.RealRow, cp.RealCol).ToMathNetVector4();
            dx = d.At(0);
            dy = d.At(1);
            dz = d.At(2);
        }

        public void FindMatchingDistance(int index, out double dL, out double dR)
        {
            var p1 = new Vector2(_imgsLeft[index]);
            var p2 = new Vector2(_imgsRight[index]);

            // -having point pair p1, p2, find l2 = epi(p1), l1 = epi(p2)
            EpiLine epiLeft = EpiLine.FindCorrespondingEpiline_LineOnLeftImage(p2, CameraPair.Data.Fundamental);
            EpiLine epiRight = EpiLine.FindCorrespondingEpiline_LineOnRightImage(p1, CameraPair.Data.Fundamental);
            //- for l = [a, b, c] normalized line ln = [a / D, b / D, c / D] where D = sqrt(A ^ 2 + B ^ 2)
            epiLeft.Normalize();
            epiRight.Normalize();
            // -d(p, l) = l * p(assuming p.w = 1)
            dL = p1.X * epiLeft.Coeffs.At(0) + p1.Y * epiLeft.Coeffs.At(1) + epiLeft.Coeffs.At(2);
            dR = p2.X * epiRight.Coeffs.At(0) + p2.Y * epiRight.Coeffs.At(1) + epiLeft.Coeffs.At(2);
        }

        public void SetGridError(Vector<double> error, RealGridData gridEst, RealGridData gridBase, int n0)
        {
            error.At(n0, _coeffGrids * (gridEst.TopLeft.X - gridBase.TopLeft.X));
            error.At(n0 + 1, _coeffGrids * (gridEst.TopLeft.Y - gridBase.TopLeft.Y));
            error.At(n0 + 2, _coeffGrids * (gridEst.TopLeft.Z - gridBase.TopLeft.Z));
            error.At(n0 + 3, _coeffGrids * (gridEst.TopRight.X - gridBase.TopRight.X));
            error.At(n0 + 4, _coeffGrids * (gridEst.TopRight.Y - gridBase.TopRight.Y));
            error.At(n0 + 5, _coeffGrids * (gridEst.TopRight.Z - gridBase.TopRight.Z));
            error.At(n0 + 6, _coeffGrids * (gridEst.BotLeft.X - gridBase.BotLeft.X));
            error.At(n0 + 7, _coeffGrids * (gridEst.BotLeft.Y - gridBase.BotLeft.Y));
            error.At(n0 + 8, _coeffGrids * (gridEst.BotLeft.Z - gridBase.BotLeft.Z));
            error.At(n0 + 9, _coeffGrids * (gridEst.BotRight.X - gridBase.BotRight.X));
            error.At(n0 + 10, _coeffGrids * (gridEst.BotRight.Y - gridBase.BotRight.Y));
            error.At(n0 + 11, _coeffGrids * (gridEst.BotRight.Z - gridBase.BotRight.Z));
        }

        public override void ComputeJacobian(Matrix<double> J)
        {
            J.Clear();
            ComputeJacobian_Numerical(J);
        }

        // Computes d(ei)/d(P) for ith line numericaly
        public void ComputeJacobian_Numerical(Matrix<double> J)
        {
            Vector<double> error_n = new DenseVector(_currentErrorVector.Count);
            Vector<double> error_p = new DenseVector(_currentErrorVector.Count);
            for(int k = 0; k < ResultsVector.Count; ++k)
            {
                double oldK = ResultsVector[k];
                double k_n = Math.Abs(oldK) > float.Epsilon ? oldK * (1 - NumericalDerivativeStep) : -NumericalDerivativeStep * 0.01;
                double k_p = Math.Abs(oldK) > float.Epsilon ? oldK * (1 + NumericalDerivativeStep) : NumericalDerivativeStep * 0.01;

                ResultsVector[k] = k_n;
                UpdateAfterParametersChanged();
                ComputeErrorVector(error_n);

                ResultsVector[k] = k_p;
                UpdateAfterParametersChanged();
                ComputeErrorVector(error_p);

                Vector<double> diff_e = 1.0 / (k_p - k_n) * (error_p - error_n);
                J.SetColumn(k, diff_e);

                ResultsVector[k] = oldK;

                // Throw if NaN found in jacobian
                bool nanInfFound = diff_e.Exists((e) => { return double.IsNaN(e) || double.IsInfinity(e); });
                if(nanInfFound)
                {
                    throw new NotFiniteNumberException("NaN or Infinity found on jacobian");
                }
            }

            UpdateAfterParametersChanged();
        }

        #region IParametrizable
        List<IAlgorithmParameter> _params = new List<IAlgorithmParameter>();
        public List<IAlgorithmParameter> Parameters
        {
            get
            {
                return _params;
            }
        }

        public void InitParameters()
        {
            _params = new List<IAlgorithmParameter>();
            DoubleParameter matchCoeffParam = new DoubleParameter(
                "c_m: Matching Error Coeff (e_m = d^2 * c_m / 2Nm)", "CMATCH", 0.01, 0.0, 100000.0);
            _params.Add(matchCoeffParam);
            
            DoubleParameter trinagCoeffParam = new DoubleParameter(
                "c_t: Triangulation Error Coeff (e_t = d^2 * c_t / 3Ni)", "CTRI", 5.0, 0.0, 100000.0);
            _params.Add(trinagCoeffParam);

            DoubleParameter imgCoeffParam = new DoubleParameter(
                "c_i: Reprojection Error Coeff (e_i = d^2 * c_i / 2Ni)", "CIMAGE", 5.0, 0.0, 100000.0);
            _params.Add(imgCoeffParam);

            DoubleParameter gridsCoeffParam = new DoubleParameter(
                "c_g: Grids Error Coeff (e_g = d^2 * c_g * 3Ni / 12Ng)", "CGRIDS", 1.0, 0.0, 100000.0);
            _params.Add(gridsCoeffParam);

            IntParameter maxItParam = new IntParameter(
                "Max Iterations", "ITERS", 100, 1, 10000);
            _params.Add(maxItParam);
        }

        public void UpdateParameters()
        {
            MaximumIterations = IAlgorithmParameter.FindValue<int>("ITERS", _params);
            MatchErrorCoeff = IAlgorithmParameter.FindValue<double>("CMATCH", _params);
            ImagesErrorCoeff = IAlgorithmParameter.FindValue<double>("CIMAGE", _params);
            GridsErrorCoeff = IAlgorithmParameter.FindValue<double>("CGRIDS", _params);
            TriangulationErrorCoeff = IAlgorithmParameter.FindValue<double>("CTRI", _params);
        }

        #endregion

        #region IControllableAlgorithm

        public string Name
        {
            get
            {
                return "Cross-Calibrator";
            }
        }
        
        public bool IsTerminable { get { return true; } }
        public bool IsParametrizable { get { return true; } }

        private AlgorithmStatus _status = AlgorithmStatus.Idle;
        public AlgorithmStatus Status
        {
            get { return _status; }
            set
            {
                AlgorithmStatus old = _status;
                _status = value;
                StatusChanged?.Invoke(this, new AlgorithmEventArgs()
                { CurrentStatus = _status, OldStatus = old });
            }
        }

        public event EventHandler<AlgorithmEventArgs> StatusChanged;
        public event EventHandler<EventArgs> ParamtersAccepted;

        public string GetResults()
        {
            return PrepareResults();
        }

        public string GetProgress()
        {
            return "Iteration " + CurrentIteration.ToString() +
                     " of " + MaximumIterations.ToString();
        }

        void IControllableAlgorithm.Terminate()
        {
            Terminate = true;
        }

        public void ShowParametersWindow()
        {
            // TODO
            //ParametersSelectionWindow window = new ParametersSelectionWindow();
            //window.Processor = this;
            //window.ShowDialog();
            //if(window.Accepted)
            //{
            //    UpdateParameters();
            //    ParamtersAccepted?.Invoke(this, new EventArgs());
            //}
        }

        private string PrepareResults()
        {
            while(_refreshMutex.WaitOne() == false) { };
            try
            {
                StringBuilder result = new StringBuilder();
                result.Append("State: ");

                if(Status == AlgorithmStatus.Finished)
                    result.Append("Finished");
                else if(Status != AlgorithmStatus.Error)
                    result.Append("Not Finished");
                else
                    result.Append("Error");

                result.AppendLine();
                result.AppendLine();

                ResultsMatrices(result, SideIndex.Left);
                result.AppendLine();
                result.AppendLine();
                ResultsMatrices(result, SideIndex.Right);
                result.AppendLine();
                result.AppendLine();
                ResultReprojectionError(result, SideIndex.Left);
                ResultReprojectionError(result, SideIndex.Right);

                double error = 0.0;
                for(int i = 0; i < CalibPointsLeft.Count; ++i)
                {
                    double dx, dy, dz;
                    FindTriangulationDistance(i, out dx, out dy, out dz);
                    error += dx * dx + dy * dy + dz * dz;
                }

                result.AppendLine();
                result.AppendLine("Triangulation error ( d(X, Xt)^2 ): ");
                result.AppendLine("Points count: " + CalibPointsLeft.Count.ToString());
                result.AppendLine("Total: " + error.ToString("F4"));
                result.AppendLine("Mean: " + (error / CalibPointsLeft.Count).ToString("F4"));

                error = 0.0;
                for(int i = 0; i < CalibPointsLeft.Count; ++i)
                {
                    double dL, dR;
                    FindMatchingDistance(i, out dL, out dR);
                    error += dL * dL + dR * dR;
                }

                result.AppendLine();
                result.AppendLine("Matching error ( d(pi, e(pi'))^2 + d(pi', e(pi))^2 ): ");
                result.AppendLine("Points count: " + CalibPointsLeft.Count.ToString());
                result.AppendLine("Total: " + error.ToString("F4"));
                result.AppendLine("Mean: " + (error / CalibPointsLeft.Count).ToString("F4"));
                _refreshMutex.ReleaseMutex();
                return result.ToString();
            }
            catch(Exception e)
            {
                _refreshMutex.ReleaseMutex();
                throw e;
            }
        }

        private void ResultReprojectionError(StringBuilder result, SideIndex camIdx)
        {
            Matrix<double> camera = CameraPair.Data.GetCameraMatrix(camIdx);
            var imgs = camIdx == SideIndex.Left ? _imgsLeft : _imgsRight;
            var reals = _reals; //camIdx == CameraIndex.Left ? _realsLeft : _realsRight;
            var cpoints = camIdx == SideIndex.Left ? CalibPointsLeft : CalibPointsRight;
            var grids = CalibGrids;  //camIdx == CameraIndex.Left ? GridsLeft : GridsRight;
            string name = camIdx == SideIndex.Left ? "Left" : "Right";

            double error = 0.0;
            double error2 = 0.0;
            double relerror = 0.0;
            double rerrx = 0.0;
            double rerry = 0.0;
            for(int p = 0; p < imgs.Length; ++p)
            {
                Vector<double> ip = cpoints[p].Img.ToMathNetVector3();
                Vector<double> rp = reals[p];

                Vector<double> eip = camera * rp;
                eip.DivideThis(eip[2]);

                var d = (ip - eip);
                double e = d.L2Norm();
                error += e;
                error2 += e * e;
                ip[2] = 0.0;
                relerror += e / ip.L2Norm();
                double rx = Math.Abs(d[0]) / Math.Abs(ip[0]), ry = Math.Abs(d[1]) / Math.Abs(ip[1]);
                rerrx += rx;
                rerry += ry;
            }

            result.AppendLine();
            result.AppendLine("Reprojection error " + name + "( d(xi, PXr) ): ");
            result.AppendLine("Points count: " + imgs.Length.ToString());
            result.AppendLine("Total: " + error.ToString("F4"));
            result.AppendLine("Total Squared: " + error2.ToString("F4"));
            result.AppendLine("Mean: " + (error / imgs.Length).ToString("F4"));

        }

        private void ResultsMatrices(StringBuilder result, SideIndex camIdx)
        {
            if(camIdx == SideIndex.Left)
            {
                result.AppendLine("Camera Matrix Left: ");
            }
            else
            {
                result.AppendLine("Camera Matrix Right: ");
            }
            Matrix<double> camera = CameraPair.Data.GetCamera(camIdx).Matrix;
            Matrix<double> calib = CameraPair.Data.GetCamera(camIdx).InternalMatrix;
            Matrix<double> rotation = CameraPair.Data.GetCamera(camIdx).RotationMatrix;
            Vector<double> center = CameraPair.Data.GetCamera(camIdx).Translation;
            Vector<double> epiPole = CameraPair.Data.GetEpipole(camIdx);

            result.Append("|" + camera[0, 0].ToString("F3"));
            result.Append("; " + camera[0, 1].ToString("F3"));
            result.Append("; " + camera[0, 2].ToString("F3"));
            result.Append("; " + camera[0, 3].ToString("F3"));
            result.AppendLine("|");

            result.Append("|" + camera[1, 0].ToString("F3"));
            result.Append("; " + camera[1, 1].ToString("F3"));
            result.Append("; " + camera[1, 2].ToString("F3"));
            result.Append("; " + camera[1, 3].ToString("F3"));
            result.AppendLine("|");

            result.Append("|" + camera[2, 0].ToString("F3"));
            result.Append("; " + camera[2, 1].ToString("F3"));
            result.Append("; " + camera[2, 2].ToString("F3"));
            result.Append("; " + camera[2, 3].ToString("F3"));
            result.AppendLine("|");

            result.AppendLine("");
            result.AppendLine("Internal Parameters:");
            result.Append("|" + calib[0, 0].ToString("F3"));
            result.Append("; " + calib[0, 1].ToString("F3"));
            result.Append("; " + calib[0, 2].ToString("F3"));
            result.AppendLine("|");

            result.Append("|" + calib[1, 0].ToString("F3"));
            result.Append("; " + calib[1, 1].ToString("F3"));
            result.Append("; " + calib[1, 2].ToString("F3"));
            result.AppendLine("|");

            result.Append("|" + calib[2, 0].ToString("F3"));
            result.Append("; " + calib[2, 1].ToString("F3"));
            result.Append("; " + calib[2, 2].ToString("F3"));
            result.AppendLine("|");

            result.AppendLine("");
            result.AppendLine("Rotation:");
            result.Append("|" + rotation[0, 0].ToString("F3"));
            result.Append("; " + rotation[0, 1].ToString("F3"));
            result.Append("; " + rotation[0, 2].ToString("F3"));
            result.AppendLine("|");

            result.Append("|" + rotation[1, 0].ToString("F3"));
            result.Append("; " + rotation[1, 1].ToString("F3"));
            result.Append("; " + rotation[1, 2].ToString("F3"));
            result.AppendLine("|");

            result.Append("|" + rotation[2, 0].ToString("F3"));
            result.Append("; " + rotation[2, 1].ToString("F3"));
            result.Append("; " + rotation[2, 2].ToString("F3"));
            result.AppendLine("|");

            result.AppendLine("");
            result.AppendLine("Camera Center:");
            result.Append("|" + center[0].ToString("F3"));
            result.Append("; " + center[1].ToString("F3"));
            result.Append("; " + center[2].ToString("F3"));
            result.AppendLine("|");

            result.AppendLine();
            result.AppendLine("Epipole: ");
            result.Append("[" + epiPole[0] / epiPole[2] + ", ");
            result.Append(epiPole[1] / epiPole[2] + "]");
        }
        #endregion
    }
}
