using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using CamCore;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Xml.Serialization;

namespace CamAlgorithms
{
    [XmlRoot("Rectification_FusielloUncalibrated")]
    public class ImageRectification_FussieloUncalibrated : ImageRectificationComputer
    {
        public bool UseInitialCalibration { get; set; }

        private Minimalisation _minimalisation;
        private Matrix<double> _H_L, _H_R, _Ht_L, _Ht_R;

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
                _minimalisation.ParametersVector.At(Minimalisation._flIdx,
                    0.5 * (Cameras.Left.InternalMatrix.At(0, 0) +
                    Cameras.Left.InternalMatrix.At(1, 1)));
                // _minimalisation.ParametersVector.At(Minimalisation._pxlIdx,
                //     CalibData.CalibrationLeft.At(0, 2));
                // _minimalisation.ParametersVector.At(Minimalisation._pylIdx,
                //     CalibData.CalibrationLeft.At(1, 2));

                Vector<double> euler = new DenseVector(3);
                RotationConverter.MatrixToEuler(euler, Cameras.Left.RotationMatrix);
                _minimalisation.ParametersVector.At(Minimalisation._eYlIdx, euler.At(1));
                _minimalisation.ParametersVector.At(Minimalisation._eZlIdx, euler.At(2));

                _minimalisation.ParametersVector.At(Minimalisation._frIdx,
                    0.5 * (Cameras.Right.InternalMatrix.At(0, 0) +
                    Cameras.Right.InternalMatrix.At(1, 1)));
                // _minimalisation.ParametersVector.At(Minimalisation._pxrIdx,
                //     CalibData.CalibrationRight.At(0, 2));
                // _minimalisation.ParametersVector.At(Minimalisation._pyrIdx,
                //     CalibData.CalibrationRight.At(1, 2));

                RotationConverter.MatrixToEuler(euler, Cameras.Right.RotationMatrix);
                _minimalisation.ParametersVector.At(Minimalisation._eXrIdx, euler.At(0));
                _minimalisation.ParametersVector.At(Minimalisation._eYrIdx, euler.At(1));
                _minimalisation.ParametersVector.At(Minimalisation._eZrIdx, euler.At(2));
            }
            else
            {
                // Some other normalisation:
                // let unit length be (imgwidth + imgheight)
                // then fx0 = 1, px0 = w/(w+h), py0 = h/(w+h)
                // Also limit angle range to -+pi

                _minimalisation.ParametersVector.At(Minimalisation._flIdx, 1.0);
                _minimalisation.ParametersVector.At(Minimalisation._pxlIdx, 0.5 * (double)ImageWidth / (double)(ImageWidth + ImageHeight));
                _minimalisation.ParametersVector.At(Minimalisation._pylIdx, 0.5 * (double)ImageHeight / (double)(ImageWidth + ImageHeight));
                //_minimalisation.ParametersVector.At(Minimalisation._flIdx, ImageWidth + ImageHeight);
                //_minimalisation.ParametersVector.At(Minimalisation._pxlIdx, ImageWidth / 2);
                //_minimalisation.ParametersVector.At(Minimalisation._pylIdx, ImageHeight / 2);

                _minimalisation.ParametersVector.At(Minimalisation._eYlIdx, 0.0);
                _minimalisation.ParametersVector.At(Minimalisation._eZlIdx, 0.0);

                _minimalisation.ParametersVector.At(Minimalisation._frIdx, 1.0);
                _minimalisation.ParametersVector.At(Minimalisation._pxrIdx, 0.5 * (double)ImageWidth / (double)(ImageWidth + ImageHeight));
                _minimalisation.ParametersVector.At(Minimalisation._pyrIdx, 0.5 * (double)ImageHeight / (double)(ImageWidth + ImageHeight));
                //_minimalisation.ParametersVector.At(Minimalisation._frIdx, ImageWidth + ImageHeight);
                //_minimalisation.ParametersVector.At(Minimalisation._pxrIdx, ImageWidth / 2);
                //_minimalisation.ParametersVector.At(Minimalisation._pyrIdx, ImageHeight / 2);

                _minimalisation.ParametersVector.At(Minimalisation._eXrIdx, 0.0);
                _minimalisation.ParametersVector.At(Minimalisation._eYrIdx, 0.0);
                _minimalisation.ParametersVector.At(Minimalisation._eZrIdx, 0.0);
            }

            _minimalisation.PointsLeft = new List<Vector<double>>();
            _minimalisation.PointsRight = new List<Vector<double>>();
            for(int i = 0; i < MatchedPairs.Count; ++i)
            {
                _minimalisation.PointsLeft.Add(MatchedPairs[i].V1.ToMathNetVector3());
                _minimalisation.PointsRight.Add(MatchedPairs[i].V2.ToMathNetVector3());
            }

            _minimalisation.ImageHeight = ImageHeight;
            _minimalisation.ImageWidth = ImageWidth;
            _minimalisation.Process();

            //_minimalisation.Init();
            _minimalisation.BestResultVector.CopyTo(_minimalisation.ResultsVector);
            _minimalisation.UpdateAfterParametersChanged();

            Matrix<double> halfRevolve = new DenseMatrix(3, 3);
            RotationConverter.EulerToMatrix(new double[] { 0.0, 0.0, Math.PI }, halfRevolve);

            Matrix<double> K = (_minimalisation._Kol + _minimalisation._Kor).Multiply(0.5);

            _H_L = K * _minimalisation._Rl * (_minimalisation._Kol.Inverse());
            _H_R = K * _minimalisation._Rr * (_minimalisation._Kor.Inverse());

            ComputeScalingMatrices(ImageWidth, ImageHeight);

            RectificationLeft = _Ht_L * _H_L;
            RectificationRight = _Ht_R * _H_R;
        }

        public void ComputeScalingMatrices(int imgWidth, int imgHeight)
        {
            // Scale and move images (after rectification) so that they have lowest
            // coordinates (0,0) and same width/height as original image
            var H_L = _H_L;
            var H_R = _H_R;
            var tl_L = H_L * new DenseVector(new double[3] { 0.0, 0.0, 1.0 });
            tl_L.DivideThis(tl_L.At(2));
            var tr_L = H_L * new DenseVector(new double[3] { imgWidth - 1, 0.0, 1.0 });
            tr_L.DivideThis(tr_L.At(2));
            var bl_L = H_L * new DenseVector(new double[3] { 0.0, imgHeight - 1, 1.0 });
            bl_L.DivideThis(bl_L.At(2));
            var br_L = H_L * new DenseVector(new double[3] { imgWidth - 1, imgHeight - 1, 1.0 });
            br_L.DivideThis(br_L.At(2));
            var tl_R = H_R * new DenseVector(new double[3] { 0.0, 0.0, 1.0 });
            tl_R.DivideThis(tl_R.At(2));
            var tr_R = H_R * new DenseVector(new double[3] { imgWidth - 1, 0.0, 1.0 });
            tr_R.DivideThis(tr_R.At(2));
            var bl_R = H_R * new DenseVector(new double[3] { 0.0, imgHeight - 1, 1.0 });
            bl_R.DivideThis(bl_R.At(2));
            var br_R = H_R * new DenseVector(new double[3] { imgWidth - 1, imgHeight - 1, 1.0 });
            br_R.DivideThis(br_R.At(2));

            // (scale and y-translation must be same for both images to perserve rectification)
            // Scale so that both images fits to (imgHeight*2, imgWidth*2)
            // If it fits w/o scaling, scale so that bigger image have width imgWidth
            // Translate in y so that left(0,0) is transformed into left'(0,x)
            // Translate in x (independently) so that img(0,0) is transformed into img'(y,0)
            // 1) Find max/min x/y
            double minX_L = Math.Min(br_L.At(0), Math.Min(bl_L.At(0), Math.Min(tl_L.At(0), tr_L.At(0))));
            double minY_L = Math.Min(br_L.At(1), Math.Min(bl_L.At(1), Math.Min(tl_L.At(1), tr_L.At(1))));
            double maxX_L = Math.Max(br_L.At(0), Math.Max(bl_L.At(0), Math.Max(tl_L.At(0), tr_L.At(0))));
            double maxY_L = Math.Max(br_L.At(1), Math.Max(bl_L.At(1), Math.Max(tl_L.At(1), tr_L.At(1))));

            double minX_R = Math.Min(br_R.At(0), Math.Min(bl_R.At(0), Math.Min(tl_R.At(0), tr_R.At(0))));
            double minY_R = Math.Min(br_R.At(1), Math.Min(bl_R.At(1), Math.Min(tl_R.At(1), tr_R.At(1))));
            double maxX_R = Math.Max(br_R.At(0), Math.Max(bl_R.At(0), Math.Max(tl_R.At(0), tr_R.At(0))));
            double maxY_R = Math.Max(br_R.At(1), Math.Max(bl_R.At(1), Math.Max(tl_R.At(1), tr_R.At(1))));

            double wr = (maxX_L - minX_L) / (maxX_R - minX_R);
            double hr = (maxY_L - minY_L) / (maxY_R - minY_R);
            double maxWidth = Math.Max(maxX_L - minX_L, maxX_R - minX_R);
            double maxHeight = Math.Max(maxY_L - minY_L, maxY_R - minY_R);

            // 2) Scale image so that longest of all dimensions is equal to old image size
            double scale = 1.0;
            if(maxWidth > maxHeight)
            {
                // Width is greater
                scale = (double)imgWidth / maxWidth;
            }
            else if(maxWidth < maxHeight)
            {
                // Height is greater
                scale = (double)imgHeight / maxHeight;
            }
            // For now leave scale as it is otherwise
            _Ht_L = DenseMatrix.CreateIdentity(3);
            _Ht_R = DenseMatrix.CreateIdentity(3);
            _Ht_L.At(0, 0, scale);
            _Ht_L.At(1, 1, scale);
            _Ht_R.At(0, 0, scale);
            _Ht_R.At(1, 1, scale);
            tl_L.MultiplyThis(scale);
            tr_L.MultiplyThis(scale);
            bl_L.MultiplyThis(scale);
            br_L.MultiplyThis(scale);
            tl_R.MultiplyThis(scale);
            tr_R.MultiplyThis(scale);
            bl_R.MultiplyThis(scale);
            br_R.MultiplyThis(scale);
            // 3) Translate in y so that minY on both images = 0
            double transY = -Math.Min(minY_L, minY_R) * scale;
            // Translate in x (independently) so that minX on both images = 0
            double transX_L = -minX_L * scale;
            double transX_R = -minX_R * scale;
            _Ht_L.At(0, 2, transX_L);
            _Ht_L.At(1, 2, transY);
            _Ht_R.At(0, 2, transX_R);
            _Ht_R.At(1, 2, transY);

            H_L = _Ht_L * _H_L;
            tl_L = H_L * new DenseVector(new double[3] { 0.0, 0.0, 1.0 });
            tl_L.DivideThis(tl_L.At(2));
            tr_L = H_L * new DenseVector(new double[3] { imgWidth - 1, 0.0, 1.0 });
            tr_L.DivideThis(tr_L.At(2));
            bl_L = H_L * new DenseVector(new double[3] { 0.0, imgHeight - 1, 1.0 });
            bl_L.DivideThis(bl_L.At(2));
            br_L = H_L * new DenseVector(new double[3] { imgWidth - 1, imgHeight - 1, 1.0 });
            br_L.DivideThis(br_L.At(2));
        }

        private class Minimalisation : LevenbergMarquardtBaseAlgorithm
        {
            public List<Vector<double>> PointsLeft { get; set; }
            public List<Vector<double>> PointsRight { get; set; }
            public int ImageWidth { get; set; }
            public int ImageHeight { get; set; }

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

            public override void UpdateAfterParametersChanged()
            {
                // Limit angles to +-(pi+e)
                double lim = Math.PI * 1.01;
                while(ResultsVector.At(_eYlIdx) < -lim)
                    ResultsVector.At(_eYlIdx, ResultsVector.At(_eYlIdx) + 2.0 * Math.PI);
                while(ResultsVector.At(_eYlIdx) > +lim)
                    ResultsVector.At(_eYlIdx, ResultsVector.At(_eYlIdx) - 2.0 * Math.PI);

                while(ResultsVector.At(_eYlIdx) < -lim)
                    ResultsVector.At(_eYlIdx, ResultsVector.At(_eZlIdx) + 2.0 * Math.PI);
                while(ResultsVector.At(_eYlIdx) > +lim)
                    ResultsVector.At(_eYlIdx, ResultsVector.At(_eZlIdx) - 2.0 * Math.PI);

                while(ResultsVector.At(_eYlIdx) < -lim)
                    ResultsVector.At(_eYlIdx, ResultsVector.At(_eXrIdx) + 2.0 * Math.PI);
                while(ResultsVector.At(_eYlIdx) > +lim)
                    ResultsVector.At(_eYlIdx, ResultsVector.At(_eXrIdx) - 2.0 * Math.PI);

                while(ResultsVector.At(_eYlIdx) < -lim)
                    ResultsVector.At(_eYlIdx, ResultsVector.At(_eYrIdx) + 2.0 * Math.PI);
                while(ResultsVector.At(_eYlIdx) > +lim)
                    ResultsVector.At(_eYlIdx, ResultsVector.At(_eYrIdx) - 2.0 * Math.PI);

                while(ResultsVector.At(_eYlIdx) < -lim)
                    ResultsVector.At(_eYlIdx, ResultsVector.At(_eZrIdx) + 2.0 * Math.PI);
                while(ResultsVector.At(_eYlIdx) > +lim)
                    ResultsVector.At(_eYlIdx, ResultsVector.At(_eZrIdx) - 2.0 * Math.PI);

                _eulerL.At(0, 0.0);
                _eulerL.At(1, ResultsVector.At(_eYlIdx));
                _eulerL.At(2, ResultsVector.At(_eZlIdx));

                _eulerR.At(0, ResultsVector.At(_eXrIdx));
                _eulerR.At(1, ResultsVector.At(_eYrIdx));
                _eulerR.At(2, ResultsVector.At(_eZrIdx));

                RotationConverter.EulerToMatrix(_eulerL, _Rl);
                RotationConverter.EulerToMatrix(_eulerR, _Rr);

                double sc = (ImageWidth + ImageHeight);

                _Kol.At(0, 0, ResultsVector.At(_flIdx) * sc);
                _Kol.At(1, 1, ResultsVector.At(_flIdx) * sc);
                _Kol.At(0, 2, ResultsVector.At(_pxlIdx) * sc);
                _Kol.At(1, 2, ResultsVector.At(_pylIdx) * sc);
                _Kol.At(2, 2, 1.0);

                _Kor.At(0, 0, ResultsVector.At(_frIdx) * sc);
                _Kor.At(1, 1, ResultsVector.At(_frIdx) * sc);
                _Kor.At(0, 2, ResultsVector.At(_pxrIdx) * sc);
                _Kor.At(1, 2, ResultsVector.At(_pyrIdx) * sc);
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
            }

            public double ComputeSampsonError(Vector<double> pl, Vector<double> pr)
            {
                double mrFml = pr * _F * pl;
                Vector<double> uFml = _u3x * _F * pl;
                Vector<double> mrFu = pr * _F * _u3x;
                double uFml_d = uFml.DotProduct(uFml);
                double mrFu_d = mrFu.DotProduct(mrFu);
                return (mrFml * mrFml) / (uFml_d + mrFu_d);
                return (mrFml * mrFml);
            }
        }
    }
}
