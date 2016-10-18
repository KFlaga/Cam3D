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
    public class AreaBasedCorrelationImageMatcher : ImagesMatcher
    {
        public override string Name { get { return "LoG-Prematch Correlation Area-based Matcher"; } }
        
        public int FinalSizeX { get; protected set; }
        public int FinalSizeY { get; protected set; }

        private int _LoG_size;
        private double _LoG_sigma;
        private int _patchSize;
        private bool _useSmoothCorrelation;
        private double _t_match;
        private int _maxDispX;
        private int _maxDispY;
        private Patch.CorrelationComputer _corrComputer;

        private Matrix<double> _leftFiltered;
        private Matrix<double> _rightFiltered;
        private Patch _leftPatch;
        private Patch _rightPatch;

        private Matrix<double>[] _leftDisparities;
        private Matrix<double>[] _rightDisparities;
        
        public override bool Match()
        {
            PrefilterImages();
            InitPatches();
            MatchLeft();
            MatchRight();

            LeftImage.ImageMatrix = _leftDisparities[1].Divide(_maxDispX);
            RightImage.ImageMatrix = _rightDisparities[1].Divide(-_maxDispX);

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

            int y, x;
            double bestMatch, match;
            int ps2 = _patchSize / 2;

            // For each point in feature map check if theres feature 
            // if yes then find best match
            for (y = ps2; y < FinalSizeY - ps2; ++y)
            {
                for (x = ps2; x < FinalSizeX - ps2; ++x)
                {
                    bestMatch = 0.0f;

                    int dxMax, dxMin, dyMax, dyMin, dx, dy;
                    // Search boundaries such as not to exceed image dimensions
                    dxMin = 0;
                    dxMax = Math.Min(_maxDispX, -x + FinalSizeX - ps2 - 1);
                    dyMin = Math.Max(-_maxDispY, -y + ps2 + 1);
                    dyMax = Math.Min(_maxDispY, -y + FinalSizeY - ps2 - 1);
                    // In expected disparity area find corners and compare patches
                    _leftPatch.StartCol = x - ps2;
                    _leftPatch.StartRow = y - ps2;
                    for (dx = dxMin; dx <= dxMax; dx++)
                    {
                        for (dy = dyMin; dy <= dyMax; dy++)
                        {
                            // Add uniqueness check ( if ie. 3 there are more than 3 points with corr diff < 0.1 
                            // then point is ambigous )


                            _rightPatch.StartCol = x + dx - ps2;
                            _rightPatch.StartRow = y + dy - ps2;
                            match = _corrComputer(_leftPatch, _rightPatch, 1.3f * _LoG_sigma);

                            if (match > bestMatch)
                            {
                                bestMatch = match;
                                _leftDisparities[1][y,x] = dx;
                                _leftDisparities[2][y,x] = dy;
                            }
                        }

                    }

                    if (bestMatch > _t_match)
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

            int y, x;
            double bestMatch, match;
            int ps2 = _patchSize / 2;

            // For each point in feature map check if theres feature 
            // if yes then find best match
            for (y = ps2; y < FinalSizeY - ps2; ++y)
            {
                for (x = ps2; x < FinalSizeX - ps2; ++x)
                {
                    bestMatch = 0.0f;

                    int dxMax, dxMin, dyMax, dyMin, dx, dy;
                    // Search boundaries such as not to exceed image dimensions
                    dxMax = 0;
                    dxMin = -Math.Min(_maxDispX, x - ps2);
                    dyMin = Math.Max(-_maxDispY, -y + ps2 + 1);
                    dyMax = Math.Min(_maxDispY, -y + FinalSizeY - ps2 - 1);
                    // In expected disparity area find corners and compare patches
                    _rightPatch.StartCol = x - ps2;
                    _rightPatch.StartRow = y - ps2;
                    for (dx = dxMin; dx <= dxMax; dx++)
                    {
                        for (dy = dyMin; dy <= dyMax; dy++)
                        {

                            _leftPatch.StartCol = x + dx - ps2;
                            _leftPatch.StartRow = y + dy - ps2;
                            match = _corrComputer(_leftPatch, _rightPatch, 1.3f * _LoG_sigma);

                            if (match > bestMatch)
                            {
                                bestMatch = match;
                                _rightDisparities[1][y, x] = dx;
                                _rightDisparities[2][y, x] = dy;
                            }
                        }

                    }

                    if (bestMatch > _t_match)
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

            for (y = ps2; y < FinalSizeY - ps2; ++y)
            {
                for (x = ps2; x < FinalSizeX - ps2; ++x)
                {
                    if(_leftDisparities[0][y,x] > 0) // if point matched
                    {
                        disp_xl = _leftDisparities[1][y, x];
                        disp_yl = _leftDisparities[2][y, x];

                        disp_xr = _rightDisparities[1][y + (int)disp_yl, x + (int)disp_xl];
                        disp_yr = _rightDisparities[2][y + (int)disp_yl, x + (int)disp_xl];

                        if(Math.Abs((disp_xl+disp_xr)) + Math.Abs(disp_yl+disp_yr) <= 2)
                        {
                            CamCore.Camera3DPoint matchedPoint = new CamCore.Camera3DPoint()
                            {
                                Cam1Img = new System.Windows.Point(x, y),
                                Cam2Img = new System.Windows.Point(x + (disp_xl - disp_xr) / 2, y + (disp_yl - disp_yr) / 2)
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
               "LoG Filter Sigma (Dev)", "LFD", 1.6, 0.5, 10.0);

            Parameters.Add(logSigma);

            AlgorithmParameter matchPatchSize = new IntParameter(
               "Correlation Matching Patch Size", "CMPS", 5, 3, 101);

            Parameters.Add(matchPatchSize);

            AlgorithmParameter useSmoothCorrelation = new BooleanParameter(
               "Use Smoothed Correlation", "USC", false);

            Parameters.Add(useSmoothCorrelation);

            AlgorithmParameter correlationThreshold = new DoubleParameter(
               "Correlation Matching Threshold", "CMT", 0.90, 0.0, 1.0);

            Parameters.Add(correlationThreshold);

            AlgorithmParameter maxDispX = new IntParameter(
               "Max Expected Disparity X", "MDX", 30, 0, 30000);

            Parameters.Add(maxDispX);

            AlgorithmParameter maxDispY = new IntParameter(
               "Max Expected Disparity Y", "MDY", 10, 0, 30000);

            Parameters.Add(maxDispY);
        }

        public override void UpdateParameters()
        {
            _LoG_sigma = AlgorithmParameter.FindValue<double>("LFD", Parameters);
            _LoG_size = AlgorithmParameter.FindValue<int>("LFR", Parameters);
            _patchSize = AlgorithmParameter.FindValue<int>("CMPS", Parameters);

            _useSmoothCorrelation = AlgorithmParameter.FindValue<bool>("USC", Parameters);
            if(_useSmoothCorrelation)
                _corrComputer = Patch.ComputePatchesSmoothCorrelation;
            else
                _corrComputer = Patch.ComputePatchesCorrelation;

            _t_match = AlgorithmParameter.FindValue<double>("CMT", Parameters);
            _maxDispX = AlgorithmParameter.FindValue<int>("MDX", Parameters);
            _maxDispY = AlgorithmParameter.FindValue<int>("MDY", Parameters);
        }
    }
}
