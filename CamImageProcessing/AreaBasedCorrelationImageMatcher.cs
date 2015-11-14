using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
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

        public ImageFilter PreMatchFilter { get; set; }
        public int FinalSizeX { get; protected set; }
        public int FinalSizeY { get; protected set; }

        private int _LoG_size;
        private float _LoG_sigma;
        private int _patchSize;
        private bool _useSmoothCorrelation;
        private float _t_match;
        private int _maxDispX;
        private int _maxDispY;
        private Patch.CorrelationComputer _corrComputer;

        private Matrix<float> _leftFiltered;
        private Matrix<float> _rightFiltered;
        private Patch _leftPatch;
        private Patch _rightPatch;

        private Matrix<float>[] _leftDisparities;
        private Matrix<float>[] _rightDisparities;

        public AreaBasedCorrelationImageMatcher()
        {
            InitParameters();
        }

        public override bool Match()
        {
            SetParameters();
            PrefilterImages();
            InitPatches();
            MatchLeft();
            MatchRight();

            LeftImage.ImageMatrix = _leftDisparities[1].Divide(_maxDispX);
            RightImage.ImageMatrix = _rightDisparities[1].Divide(-_maxDispX);

            CrossCheckMatches();

            return true;
        }

        private void SetParameters()
        {
            _LoG_sigma = (float)ProcessorParameter.FindValue("LFD", Parameters);
            _LoG_size = (int)ProcessorParameter.FindValue("LFR", Parameters);
            _patchSize = (int)ProcessorParameter.FindValue("CMPS", Parameters);

            _useSmoothCorrelation = (bool)ProcessorParameter.FindValue("USC", Parameters);
            if (_useSmoothCorrelation)
                _corrComputer = Patch.ComputePatchesSmoothCorrelation;
            else
                _corrComputer = Patch.ComputePatchesCorrelation;

            _t_match = (float)ProcessorParameter.FindValue("CMT", Parameters);
            _maxDispX = (int)ProcessorParameter.FindValue("MDX", Parameters);
            _maxDispY = (int)ProcessorParameter.FindValue("MDY", Parameters);
        }

        private void PrefilterImages()
        {
            PreMatchFilter = new ImageFilter();
            PreMatchFilter.Filter = ImageFilter.GetFilter_LoGNorm(_LoG_size, _LoG_sigma);
            PreMatchFilter.Image = this.LeftImage;
            _leftFiltered = PreMatchFilter.ApplyFilter().ImageMatrix;
            PreMatchFilter.Image = this.RightImage;
            _rightFiltered = PreMatchFilter.ApplyFilter().ImageMatrix;

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
            float bestMatch, match;
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
            float bestMatch, match;
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
            float disp_xl, disp_yl, disp_xr, disp_yr;

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

        protected override void InitParameters()
        {
            Parameters = new List<ProcessorParameter>();

            ProcessorParameter logSize = new ProcessorParameter(
                "LoG Filter Size", "LFR",
                "System.Int32", 9, 3, 101);

            Parameters.Add(logSize);

            ProcessorParameter logSigma = new ProcessorParameter(
               "LoG Filter Sigma (Dev)", "LFD",
               "System.Single", 1.6f, 0.5f, 10.0f);

            Parameters.Add(logSigma);

            ProcessorParameter matchPatchSize = new ProcessorParameter(
               "Correlation Matching Patch Size", "CMPS",
               "System.Int32", 5, 3, 101);

            Parameters.Add(matchPatchSize);

            ProcessorParameter useSmoothCorrelation = new ProcessorParameter(
               "Use Smoothed Correlation", "USC",
               "System.Boolean", false, false, true);

            Parameters.Add(useSmoothCorrelation);

            ProcessorParameter correlationThreshold = new ProcessorParameter(
               "Correlation Matching Threshold", "CMT",
               "System.Single", 0.90f, 0.0f, 1.0f);

            Parameters.Add(correlationThreshold);

            ProcessorParameter maxDispX = new ProcessorParameter(
               "Max Expected Disparity X", "MDX",
               "System.Int32", 30, 0, 30000);

            Parameters.Add(maxDispX);

            ProcessorParameter maxDispY = new ProcessorParameter(
               "Max Expected Disparity Y", "MDY",
               "System.Int32", 10, 0, 30000);

            Parameters.Add(maxDispY);
        }
    }
}
