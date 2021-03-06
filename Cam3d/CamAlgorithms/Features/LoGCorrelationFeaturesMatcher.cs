﻿//using CamCore;
//using MathNet.Numerics.LinearAlgebra;
//using MathNet.Numerics.LinearAlgebra.Double;
//using System;
//using System.Collections.Generic;

//namespace CamImageProcessing
//{
//    public class LoGCorrelationFeaturesMatcher : FeaturesMatcher
//    {
//        public override string Name { get { return "LoG-Prematch Correlation Feature-based Matcher"; } }

//        private int _LoG_size;
//        private double _LoG_sigma;
//        private int _patchSize;
//        private bool _useSmoothCorrelation;
//        private double _t_match;
//        private int _maxDispX;
//        private int _maxDispY;
//        private Patch.CorrelationComputer _corrComputer;

//        private Matrix<double> _leftFiltered;
//        private Matrix<double> _rightFiltered;
//        private Patch _leftPatch;
//        private Patch _rightPatch;
//        private Matrix<double> _leftFeatureMap;
//        private Matrix<double> _rightFeatureMap;

//        public override bool Match()
//        {
//            FindFeatures();
//            PrefilterImages();
//            InitPatches();
//            MatchFeatures();

//            return true;
//        }

//        private void FindFeatures()
//        {
//            FeatureDetector.Image = this.LeftImage;
//            FeatureDetector.Detect();
//            _leftFeatureMap = FeatureDetector.FeatureMap.ImageMatrix;

//            FeatureDetector.Image = this.RightImage;
//            FeatureDetector.Detect();
//            _rightFeatureMap = FeatureDetector.FeatureMap.ImageMatrix;
//        }

//        private void PrefilterImages()
//        {
//            Matrix<double> filter = ImageFilter.GetFilter_LoGNorm(_LoG_size, _LoG_sigma);
//            _leftFiltered = ImageFilter.ApplyFilter(LeftImage.ImageMatrix, filter);
//            _rightFiltered = ImageFilter.ApplyFilter(RightImage.ImageMatrix, filter);

//            FinalSizeX = _leftFiltered.ColumnCount;
//            FinalSizeY = _leftFiltered.RowCount;
//        }

//        private void InitPatches()
//        {
//            _leftPatch = new Patch()
//            {
//                ImageMatrix = _leftFiltered,
//                Rows = _patchSize,
//                Cols = _patchSize
//            };
//            _rightPatch = new Patch()
//            {
//                ImageMatrix = _rightFiltered,
//                Rows = _patchSize,
//                Cols = _patchSize
//            };
//        }

//        private void MatchFeatures()
//        {
//            int y, x;
//            double bestMatch, match;
//            CamCore.Camera3DPoint matchPoint = new CamCore.Camera3DPoint();
//            int ps2 = _patchSize / 2;

//            // For each point in feature map check if theres feature 
//            // if yes then find best match
//            for(y = ps2; y < FinalSizeY - ps2; ++y)
//            {
//                for(x = ps2; x < FinalSizeX - ps2; ++x)
//                {
//                    if(!(_leftFeatureMap[y, x] > 0.0f))
//                        continue;

//                    bestMatch = 0.0f;
//                    matchPoint.Cam1Img = new Vector2(x, y);

//                    int dxMax, dxMin, dyMax, dyMin, dx, dy;
//                    // Search boundaries such as not to exceed image dimensions
//                    dxMin = 0;
//                    dxMax = Math.Min(_maxDispX, -x + FinalSizeX - ps2 - 1);
//                    dyMin = Math.Max(-_maxDispY, -y + ps2 + 1);
//                    dyMax = Math.Min(_maxDispY, -y + FinalSizeY - ps2 - 1);
//                    // In expected disparity area find corners and compare patches
//                    _leftPatch.StartCol = x - ps2;
//                    _leftPatch.StartRow = y - ps2;
//                    for(dx = dxMin; dx <= dxMax; dx++)
//                    {
//                        for(dy = dyMin; dy <= dyMax; dy++)
//                        {
//                            if((_rightFeatureMap[y + dy, x + dx] > 0.0f))
//                            {
//                                _rightPatch.StartCol = x + dx - ps2;
//                                _rightPatch.StartRow = y + dy - ps2;
//                                match = _corrComputer(_leftPatch, _rightPatch, 1.3f * _LoG_sigma);

//                                if(match > bestMatch)
//                                {
//                                    bestMatch = match;
//                                    matchPoint.Cam2Img = new Vector2(x + dx, y + dy);
//                                }
//                            }

//                        }
//                    }

//                    if(bestMatch > _t_match)
//                    {
//                        MatchedPoints.Add(matchPoint);
//                        matchPoint = new CamCore.Camera3DPoint();
//                    }

//                }
//            }
//        }

//        public override void InitParameters()
//        {
//            Parameters = new List<AlgorithmParameter>();

//            AlgorithmParameter logSize = new IntParameter(
//                "LoG Filter Size", "LFR", 9, 3, 101);

//            Parameters.Add(logSize);

//            AlgorithmParameter logSigma = new DoubleParameter(
//               "LoG Filter Sigma (Dev)", "LFD", 1.6f, 0.5f, 10.0f);

//            Parameters.Add(logSigma);

//            AlgorithmParameter matchPatchSize = new IntParameter(
//               "Correlation Matching Patch Size", "CMPS", 5, 3, 101);

//            Parameters.Add(matchPatchSize);

//            AlgorithmParameter useSmoothCorrelation = new BooleanParameter(
//               "Use Smoothed Correlation", "USC", false);

//            Parameters.Add(useSmoothCorrelation);

//            AlgorithmParameter correlationThreshold = new DoubleParameter(
//               "Correlation Matching Threshold", "CMT", 0.90f, 0.0f, 1.0f);

//            Parameters.Add(correlationThreshold);

//            AlgorithmParameter maxDispX = new IntParameter(
//               "Max Expected Disparity X", "MDX", 30, 0, 30000);

//            Parameters.Add(maxDispX);

//            AlgorithmParameter maxDispY = new IntParameter(
//               "Max Expected Disparity Y", "MDY", 10, 0, 30000);

//            Parameters.Add(maxDispY);
//        }

//        public override void UpdateParameters()
//        {
//            _LoG_sigma = (float)AlgorithmParameter.FindValue("LFD", Parameters);
//            _LoG_size = (int)AlgorithmParameter.FindValue("LFR", Parameters);
//            _patchSize = (int)AlgorithmParameter.FindValue("CMPS", Parameters);

//            _useSmoothCorrelation = (bool)AlgorithmParameter.FindValue("USC", Parameters);
//            if(_useSmoothCorrelation)
//                _corrComputer = Patch.ComputePatchesSmoothCorrelation;
//            else
//                _corrComputer = Patch.ComputePatchesCorrelation;

//            _t_match = (float)AlgorithmParameter.FindValue("CMT", Parameters);
//            _maxDispX = (int)AlgorithmParameter.FindValue("MDX", Parameters);
//            _maxDispY = (int)AlgorithmParameter.FindValue("MDY", Parameters);
//        }
//    }
//    }
