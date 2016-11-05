using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamImageProcessing
{
    public class CalibratedAreaBasedCorrelationImageMatcher : CalibratedImageMatcher
    {
        public override string Name { get { return "LoG-Prematch Correlation Area-based EpilineSearch Matcher"; } }

        public ImageFilter PreMatchFilter { get; set; }
        public int FinalSizeX { get; protected set; }
        public int FinalSizeY { get; protected set; }

        private int _LoG_size;
        private double _LoG_sigma;
        private int _patchSize;
        private bool _useSmoothCorrelation;
        private double _t_match;
        private Patch.CorrelationComputer _corrComputer;

        private Matrix<double> _leftFiltered;
        private Matrix<double> _rightFiltered;
        private Patch _leftPatch;
        private Patch _rightPatch;

        private Matrix<double>[] _leftDisparities;
        private Matrix<double>[] _rightDisparities;

        public override bool Match()
        {
            ComputeEpiGeometry();
            PrefilterImages();
            InitPatches();
            MatchLeft();
            MatchRight();

            LeftImage.ImageMatrix = _leftDisparities[1].Divide(_maxDisparity);
            RightImage.ImageMatrix = _rightDisparities[1].Divide(-_maxDisparity);

            CrossCheckMatches();

            return true;
        }

        private void PrefilterImages()
        {
            Matrix<double> filter = ImageFilter.GetFilter_LoGNorm(_LoG_size, _LoG_sigma);
            _leftFiltered = ImageFilter.ApplyFilter(LeftImage.ImageMatrix, filter);
            _rightFiltered = ImageFilter.ApplyFilter(RightImage.ImageMatrix, filter);

            FinalSizeX = _leftFiltered.ColumnCount;
            FinalSizeY = _leftFiltered.RowCount;
        }

        private void InitPatches()
        {
            _leftPatch = new Patch()
            {
                ImageMatrix = _leftFiltered,
                Rows = _patchSize,
                Cols = _patchSize
            };
            _rightPatch = new Patch()
            {
                ImageMatrix = _rightFiltered,
                Rows = _patchSize,
                Cols = _patchSize
            };
        }

        private void MatchLeft()
        {
            _leftDisparities = new DenseMatrix[3];
            _leftDisparities[0] = new DenseMatrix(FinalSizeY, FinalSizeX);
            _leftDisparities[1] = new DenseMatrix(FinalSizeY, FinalSizeX);
            _leftDisparities[2] = new DenseMatrix(FinalSizeY, FinalSizeX);

            int y, x, p;
            double bestMatch, match;
            int ps2 = _patchSize / 2;

            // For each point in feature map check if theres feature 
            // if yes then find best match
            for(y = ps2; y < FinalSizeY - ps2; ++y)
            {
                for(x = ps2; x < FinalSizeX - ps2; ++x)
                {
                    bestMatch = 0.0f;

                    // In expected disparity area find corners and compare patches
                    _leftPatch.StartCol = x - ps2;
                    _leftPatch.StartRow = y - ps2;

                    FindPotentialMatchPoints(y, x, true);
                    int pointCount = _potentPoints.Length / 2;

                    for(p = 0; p < pointCount; p++)
                    {
                        _rightPatch.StartCol = x + _potentPoints[p, 1] - ps2;
                        _rightPatch.StartRow = y + _potentPoints[p, 0] - ps2;
                        match = _corrComputer(_leftPatch, _rightPatch, 1.3f * _LoG_sigma);

                        if(match > bestMatch)
                        {
                            bestMatch = match;
                            _leftDisparities[1][y, x] = _potentPoints[p, 1];
                            _leftDisparities[2][y, x] = _potentPoints[p, 0];
                        }
                    }

                    if(bestMatch > _t_match)
                    {
                        _leftDisparities[0][y, x] = 1;
                    }
                    else
                    {
                        _leftDisparities[0][y, x] = 0;
                    }
                }
            }
        }

        private void MatchRight()
        {
            _rightDisparities = new DenseMatrix[3];
            _rightDisparities[0] = new DenseMatrix(FinalSizeY, FinalSizeX);
            _rightDisparities[1] = new DenseMatrix(FinalSizeY, FinalSizeX);
            _rightDisparities[2] = new DenseMatrix(FinalSizeY, FinalSizeX);

            int y, x, p;
            double bestMatch, match;
            int ps2 = _patchSize / 2;

            // For each point in feature map check if theres feature 
            // if yes then find best match
            for(y = ps2; y < FinalSizeY - ps2; ++y)
            {
                for(x = ps2; x < FinalSizeX - ps2; ++x)
                {
                    bestMatch = 0.0f;

                    // In expected disparity area find corners and compare patches
                    _rightPatch.StartCol = x - ps2;
                    _rightPatch.StartRow = y - ps2;
                    FindPotentialMatchPoints(y, x, false);
                    int pointCount = _potentPoints.Length / 2;

                    for(p = 0; p < pointCount; p++)
                    {
                        _leftPatch.StartCol = x + _potentPoints[p, 1] - ps2;
                        _leftPatch.StartRow = y + _potentPoints[p, 0] - ps2;
                        match = _corrComputer(_leftPatch, _rightPatch, 1.3f * _LoG_sigma);

                        if(match > bestMatch)
                        {
                            bestMatch = match;
                            _rightDisparities[1][y, x] = _potentPoints[p, 1];
                            _rightDisparities[2][y, x] = _potentPoints[p, 0];
                        }
                    }

                    if(bestMatch > _t_match)
                    {
                        _rightDisparities[0][y, x] = 1;
                    }
                    else
                    {
                        _rightDisparities[0][y, x] = 0;
                    }
                }
            }
        }

        private void CrossCheckMatches()
        {
            MatchedPoints = new List<CamCore.Camera3DPoint>();
            int y, x;
            int ps2 = _patchSize / 2;
            double disp_xl, disp_yl, disp_xr, disp_yr;

            for(y = ps2; y < FinalSizeY - ps2; ++y)
            {
                for(x = ps2; x < FinalSizeX - ps2; ++x)
                {
                    if(_leftDisparities[0][y, x] > 0) // if point matched
                    {
                        disp_xl = _leftDisparities[1][y, x];
                        disp_yl = _leftDisparities[2][y, x];

                        disp_xr = _rightDisparities[1][y + (int)disp_yl, x + (int)disp_xl];
                        disp_yr = _rightDisparities[2][y + (int)disp_yl, x + (int)disp_xl];

                        if(Math.Abs((disp_xl + disp_xr)) + Math.Abs(disp_yl + disp_yr) <= 2)
                        {
                            CamCore.Camera3DPoint matchedPoint = new CamCore.Camera3DPoint()
                            {
                                Cam1Img = new Vector2(x, y),
                                Cam2Img = new Vector2(x + (disp_xl - disp_xr) / 2, y + (disp_yl - disp_yr) / 2)
                            };
                            MatchedPoints.Add(matchedPoint);
                        }
                    }
                }
            }
        }

        public override void InitParameters()
        {
            Parameters = new List<AlgorithmParameter>();

            AlgorithmParameter logSize = new IntParameter(
                "LoG Filter Size", "LFR", 9, 3, 101);

            Parameters.Add(logSize);

            AlgorithmParameter logSigma = new DoubleParameter(
               "LoG Filter Sigma (Dev)", "LFD", 1.6f, 0.5f, 10.0f);

            Parameters.Add(logSigma);

            AlgorithmParameter matchPatchSize = new IntParameter(
               "Correlation Matching Patch Size", "CMPS", 5, 3, 101);

            Parameters.Add(matchPatchSize);

            AlgorithmParameter useSmoothCorrelation = new BooleanParameter(
               "Use Smoothed Correlation", "USC", false);

            Parameters.Add(useSmoothCorrelation);

            AlgorithmParameter correlationThreshold = new DoubleParameter(
               "Correlation Matching Threshold", "CMT", 0.90f, 0.0f, 1.0f);

            Parameters.Add(correlationThreshold);

            AlgorithmParameter maxDisp = new IntParameter(
               "Max Expected Disparity Along Epiline", "MD", 30, 0, 30000);

            Parameters.Add(maxDisp);
        }

        public override void UpdateParameters()
        {
            _LoG_sigma = (float)AlgorithmParameter.FindValue("LFD", Parameters);
            _LoG_size = (int)AlgorithmParameter.FindValue("LFR", Parameters);
            _patchSize = (int)AlgorithmParameter.FindValue("CMPS", Parameters);

            _useSmoothCorrelation = (bool)AlgorithmParameter.FindValue("USC", Parameters);
            if(_useSmoothCorrelation)
                _corrComputer = Patch.ComputePatchesSmoothCorrelation;
            else
                _corrComputer = Patch.ComputePatchesCorrelation;

            _t_match = (float)AlgorithmParameter.FindValue("CMT", Parameters);
            _maxDisparity = (int)AlgorithmParameter.FindValue("MD", Parameters);
        }
    }
}
