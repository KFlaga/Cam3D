using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using CamCore;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Xml.Serialization;

namespace CamAlgorithms
{
    [XmlRoot("Rectification_FusielloUncalibrated")]
    public class Rectification_FussieloIrsara : IRectificationAlgorithm
    {
        public bool UseInitialCalibration { get; set; }

        private Minimalisation _minimalisation;

        public override void ComputeRectificationMatrices()
        {
            _minimalisation = new Minimalisation();
            _minimalisation.MaximumResidiual = 1e-12;
            _minimalisation.MaximumIterations = 1000;
            _minimalisation.DoComputeJacobianNumerically = true;
            _minimalisation.DumpingMethodUsed = LevenbergMarquardtBaseAlgorithm.DumpingMethod.Multiplicative;
            _minimalisation.UseCovarianceMatrix = false;
            _minimalisation.NumericalDerivativeStep = 1e-4;

            _minimalisation.ParametersVector = new DenseVector(Minimalisation._paramsCount);
            if(UseInitialCalibration && Cameras.AreCalibrated)
            {
                SetInitialParametersFromCalibration();
            }
            else
            {
                SetInitialParametersFromGuess();
            }

            double N = GetNormalizationCoeff();
            _minimalisation.PointsLeft = new List<Vector<double>>();
            _minimalisation.PointsRight = new List<Vector<double>>();
            for(int i = 0; i < MatchedPairs.Count; ++i)
            {
                _minimalisation.PointsLeft.Add((MatchedPairs[i].V1).ToMathNetVector3());
                _minimalisation.PointsRight.Add((MatchedPairs[i].V2).ToMathNetVector3());
            }

            _minimalisation.ImageHeight = ImageHeight;
            _minimalisation.ImageWidth = ImageWidth;
            _minimalisation.Process();
            
            _minimalisation.BestResultVector.CopyTo(_minimalisation.ResultsVector);
            _minimalisation.UpdateAfterParametersChanged();
            
            Matrix<double> K = (_minimalisation._Kol + _minimalisation._Kor).Multiply(0.5);

            RectificationLeft = K * _minimalisation._Rl * (_minimalisation._Kol.Inverse());
            RectificationRight = K * _minimalisation._Rr * (_minimalisation._Kor.Inverse());
        }

        private double GetNormalizationCoeff()
        {
            return 1.0 / (ImageWidth + ImageHeight);
        }

        private void SetInitialParametersFromGuess()
        {
            _minimalisation.ParametersVector.At(Minimalisation._flIdx, 1.0);
            _minimalisation.ParametersVector.At(Minimalisation._pxlIdx, 0.5 * (double)ImageWidth / (double)(ImageWidth + ImageHeight));
            _minimalisation.ParametersVector.At(Minimalisation._pylIdx, 0.5 * (double)ImageHeight / (double)(ImageWidth + ImageHeight));

            _minimalisation.ParametersVector.At(Minimalisation._eYlIdx, 0.0);
            _minimalisation.ParametersVector.At(Minimalisation._eZlIdx, 0.0);

            _minimalisation.ParametersVector.At(Minimalisation._frIdx, 1.0);
            _minimalisation.ParametersVector.At(Minimalisation._pxrIdx, 0.5 * (double)ImageWidth / (double)(ImageWidth + ImageHeight));
            _minimalisation.ParametersVector.At(Minimalisation._pyrIdx, 0.5 * (double)ImageHeight / (double)(ImageWidth + ImageHeight));

            _minimalisation.ParametersVector.At(Minimalisation._eXrIdx, 0.0);
            _minimalisation.ParametersVector.At(Minimalisation._eYrIdx, 0.0);
            _minimalisation.ParametersVector.At(Minimalisation._eZrIdx, 0.0);

            _minimalisation.EulerLX = 0.0;
        }

        private void SetInitialParametersFromCalibration()
        {
            double N = GetNormalizationCoeff();
            _minimalisation.ParametersVector.At(Minimalisation._flIdx,
                0.5 * N * (Cameras.Left.InternalMatrix.At(0, 0) + Cameras.Left.InternalMatrix.At(1, 1)));
            _minimalisation.ParametersVector.At(Minimalisation._pxlIdx,
                N * Cameras.Left.InternalMatrix.At(0, 2));
            _minimalisation.ParametersVector.At(Minimalisation._pylIdx,
                N * Cameras.Left.InternalMatrix.At(1, 2));

            Vector<double> euler = new DenseVector(3);
            RotationConverter.MatrixToEuler(euler, Cameras.Left.RotationMatrix);
            _minimalisation.EulerLX = euler.At(0);
            _minimalisation.ParametersVector.At(Minimalisation._eYlIdx, euler.At(1));
            _minimalisation.ParametersVector.At(Minimalisation._eZlIdx, euler.At(2));

            _minimalisation.ParametersVector.At(Minimalisation._frIdx,
                0.5 * N * (Cameras.Right.InternalMatrix.At(0, 0) + Cameras.Right.InternalMatrix.At(1, 1)));
            _minimalisation.ParametersVector.At(Minimalisation._pxrIdx,
                N * Cameras.Right.InternalMatrix.At(0, 2));
            _minimalisation.ParametersVector.At(Minimalisation._pyrIdx,
                N * Cameras.Right.InternalMatrix.At(1, 2));

            RotationConverter.MatrixToEuler(euler, Cameras.Right.RotationMatrix);
            _minimalisation.ParametersVector.At(Minimalisation._eXrIdx, euler.At(0));
            _minimalisation.ParametersVector.At(Minimalisation._eYrIdx, euler.At(1));
            _minimalisation.ParametersVector.At(Minimalisation._eZrIdx, euler.At(2));
        }
        
        private class Minimalisation : LevenbergMarquardtBaseAlgorithm
        {
            public List<Vector<double>> PointsLeft { get; set; }
            public List<Vector<double>> PointsRight { get; set; }
            public int ImageWidth { get; set; }
            public int ImageHeight { get; set; }

            public double EulerLX { get; set; }

            public Matrix<double> _F;
            public Matrix<double> _Rl, _Rr;
            public Matrix<double> _Kol, _Kor;
            public Matrix<double> _Knl, _Knr;
            public Matrix<double> _u1x;
            public Matrix<double> _u3x;
            Vector<double> _eulerL, _eulerR;

            public const int _flIdx = 0;
            public const int _pxlIdx = 1;
            public const int _pylIdx = 2;
            public const int _eYlIdx = 3;
            public const int _eZlIdx = 4;
            public const int _frIdx = 5;
            public const int _pxrIdx = 6;
            public const int _pyrIdx = 7;
            public const int _eXrIdx = 8;
            public const int _eYrIdx = 9;
            public const int _eZrIdx = 10;
            public const int _paramsCount = 11;

            private Matrix<double> _H_L;
            private Matrix<double> _H_R;

            public override void Init()
            {
                MeasurementsVector = new DenseVector(PointsLeft.Count);

                _u1x = new DenseMatrix(3, 3);
                _u1x.At(1, 2, -1.0);
                _u1x.At(2, 1, 1.0);

                _u3x = new DenseMatrix(3, 3);
                _u3x.At(0, 1, -1.0);
                _u3x.At(1, 0, 1.0);

                _eulerL = new DenseVector(3);
                _eulerR = new DenseVector(3);

                _Kol = new DenseMatrix(3, 3);
                _Kor = new DenseMatrix(3, 3);
                _Knl = new DenseMatrix(3, 3);
                _Knr = new DenseMatrix(3, 3);

                _Rl = new DenseMatrix(3, 3);
                _Rr = new DenseMatrix(3, 3);

                base.Init();
            }

            void LimitAngle(int idx)
            {
                while(ResultsVector.At(idx) < -Math.PI * 1.01)
                {
                    ResultsVector.At(idx, ResultsVector.At(idx) + 2.0 * Math.PI);
                }
                while(ResultsVector.At(idx) > Math.PI * 1.01)
                {
                    ResultsVector.At(idx, ResultsVector.At(idx) - 2.0 * Math.PI);
                }
            }

            public override void UpdateAfterParametersChanged()
            {
                LimitAngle(_eYlIdx);
                LimitAngle(_eZlIdx);
                LimitAngle(_eXrIdx);
                LimitAngle(_eYrIdx);
                LimitAngle(_eZrIdx);
                
                _eulerL.At(0, EulerLX);
                _eulerL.At(1, ResultsVector.At(_eYlIdx));
                _eulerL.At(2, ResultsVector.At(_eZlIdx));

                _eulerR.At(0, ResultsVector.At(_eXrIdx));
                _eulerR.At(1, ResultsVector.At(_eYrIdx));
                _eulerR.At(2, ResultsVector.At(_eZrIdx));

                RotationConverter.EulerToMatrix(_eulerL, _Rl);
                RotationConverter.EulerToMatrix(_eulerR, _Rr);

                double N = ImageHeight + ImageWidth;

                _Kol.At(0, 0, ResultsVector.At(_flIdx) * N);
                _Kol.At(1, 1, ResultsVector.At(_flIdx) * N);
                _Kol.At(0, 2, ResultsVector.At(_pxlIdx) * N);
                _Kol.At(1, 2, ResultsVector.At(_pylIdx) * N);
                _Kol.At(2, 2, 1.0);

                _Kor.At(0, 0, ResultsVector.At(_frIdx) * N);
                _Kor.At(1, 1, ResultsVector.At(_frIdx) * N);
                _Kor.At(0, 2, ResultsVector.At(_pxrIdx) * N);
                _Kor.At(1, 2, ResultsVector.At(_pyrIdx) * N);
                _Kor.At(2, 2, 1.0);

                _F = (_Kor.Inverse().Transpose()) * _Rr.Transpose() * _u1x * _Rl * _Kol.Inverse();
            }

            public override void ComputeMappingFucntion(Vector<double> mapFuncResult)
            {
                
            }

            public override void ComputeErrorVector(Vector<double> error)
            {
                int measuredPointsCount = MeasurementsVector.Count / 3;
                for(int i = 0; i < measuredPointsCount; ++i)
                {
                    error.At(i, ComputeSampsonError(PointsLeft[i], PointsRight[i]));
                }

                //Matrix<double> K = (_Kol + _Kor).Multiply(0.5);
                //_H_L = K * _Rl * (_Kol.Inverse());
                //_H_R = K * _Rr * (_Kor.Inverse());
                //for(int i = 0; i < measuredPointsCount; ++i)
                //{
                //    error.At(i, ComputeMyError(_H_L, PointsLeft[i], _H_R, PointsRight[i]));
                //}
            }

            public double ComputeMyError(Matrix<double> Hl, Vector<double> pl, Matrix<double> Hr, Vector<double> pr)
            {
                var rectLeft = Hl * pl;
                var rectRight = Hr * pr;
                double yError = new Vector2(rectLeft).Y - new Vector2(rectRight).Y;
                return yError;
            }

            public double ComputeSampsonError(Vector<double> pl, Vector<double> pr)
            { 
                double mrFml = pr * (_F * pl);
                Vector<double> uFml = _u3x * _F * pl;
                Vector<double> mrFu = pr * _F * _u3x;
                double uFml_d = uFml.DotProduct(uFml);
                double mrFu_d = mrFu.DotProduct(mrFu);
                return (mrFml * mrFml) / (uFml_d + mrFu_d);
                //return (mrFml * mrFml);
            }
        }
    }
}
