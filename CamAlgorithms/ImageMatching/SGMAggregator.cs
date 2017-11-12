using CamCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CamAlgorithms.ImageMatching
{
    public class SGMAggregator : CostAggregator
    {
        [DebuggerDisplay("d = {Disparity}, c = {Cost}")]
        public struct DisparityCost
        {
            public double Cost;
            public int Disparity;
            public DisparityCost(int d, double cost)
            {
                Cost = cost;
                Disparity = d;
            }
        }

        public double LowPenaltyCoeff { get; set; } = 0.02; // P1 = coeff * MaxCost
        public double HighPenaltyCoeff { get; set; } = 0.04; // P2 = coeff * MaxCost * (1 - grad * |Ib - Im|)
        public double GradientCoeff { get; set; } = 0.5;
        public int MaxDisparity { get; set; }
        public int MinDisparity { get; set; }
        double _penaltyLow;
        double _penaltyHigh;

        //List<Disparity> _dispIndexMap;
        Path[,][] _paths; // For each border pixel 16 paths [pixel.Y, pixel.X][path]
        DisparityCost[,,] _bestPathCosts;
        Path.BorderPixelGetter[] _borderPixelGetters;

        IntVector2 _matched = new IntVector2();
        int _dispRange;

        int[] _pathsInRun_RightTopDown = new int[]
        {
            (int)PathDirection.PosX,
            (int)PathDirection.PosY,
            (int)PathDirection.PosX_PosY,
            (int)PathDirection.NegX_PosY,
            (int)PathDirection.PosX2_PosY,
            (int)PathDirection.NegX2_PosY,
            (int)PathDirection.PosX_PosY2,
            (int)PathDirection.NegX_PosY2,
        };

        int[] _pathsInRun_RightBottomUp = new int[]
        {
            (int)PathDirection.NegX,
            (int)PathDirection.NegY,
            (int)PathDirection.PosX_NegY,
            (int)PathDirection.NegX_NegY,
            (int)PathDirection.PosX2_NegY,
            (int)PathDirection.NegX2_NegY,
            (int)PathDirection.PosX_NegY2,
            (int)PathDirection.NegX_NegY2,
        };

        int[] _pathsInRun_LeftTopDown = new int[]
        {
            (int)PathDirection.NegX,
            (int)PathDirection.PosY,
            (int)PathDirection.PosX_PosY,
            (int)PathDirection.NegX_PosY,
            (int)PathDirection.PosX2_PosY,
            (int)PathDirection.NegX2_PosY,
            (int)PathDirection.PosX_PosY2,
            (int)PathDirection.NegX_PosY2,
        };

        int[] _pathsInRun_LeftBottomUp = new int[]
        {
            (int)PathDirection.PosX,
            (int)PathDirection.NegY,
            (int)PathDirection.PosX_NegY,
            (int)PathDirection.NegX_NegY,
            (int)PathDirection.PosX2_NegY,
            (int)PathDirection.NegX2_NegY,
            (int)PathDirection.PosX_NegY2,
            (int)PathDirection.NegX_NegY2,
        };

        public SGMAggregator()
        {
            DispComp = new SGMDisparityComputer();
        }

        void CreateBorderPaths()
        {
            _paths = new Path[ImageBase.RowCount, ImageBase.ColumnCount][];

            for(int x = 0; x < ImageBase.ColumnCount; ++x)
            {
                CreatePathsForBorderPixel(y: 0, x: x);
                InitZeroStep(new IntVector2(y: 0, x: x));

                CreatePathsForBorderPixel(y: ImageBase.RowCount - 1, x: x);
                InitZeroStep(new IntVector2(y: ImageBase.RowCount - 1, x: x));
            }

            for(int y = 1; y < ImageBase.RowCount; ++y)
            {
                CreatePathsForBorderPixel(y: y, x: 0);
                InitZeroStep(new IntVector2(y: y, x: 0));

                CreatePathsForBorderPixel(y: y, x: ImageBase.ColumnCount - 1);
                InitZeroStep(new IntVector2(y: y, x: ImageBase.ColumnCount - 1));
            }
        }

        void InitZeroStep(IntVector2 borderPixel)
        {
            for(int i = 0; i < 16; ++i)
            {
                Path path = _paths[borderPixel.Y, borderPixel.X][i];
                if(path != null)
                {
                    path.ImageHeight = ImageBase.RowCount;
                    path.ImageWidth = ImageBase.ColumnCount;
                    path.StartPixel = borderPixel;
                    path.Length = ImageBase.ColumnCount + ImageBase.RowCount;
                    path.LastStepCosts = new double[_dispRange + 1];
                    path.Init();

                    int bestDisp = 0;
                    double bestCost = CostComp.MaxCost + 1.0;
                    _matched.Y = borderPixel.Y;

                    // If base is right, then for each base pixel, matched one is on the right - disparity is positive
                    int maxDisp = IsLeftImageBase ? path.CurrentPixel.X : ImageBase.ColumnCount - 1 - path.CurrentPixel.X;

                    for(int d = 0; d < maxDisp; ++d)
                    {
                        _matched.X = IsLeftImageBase ? path.CurrentPixel.X - d : path.CurrentPixel.X + d;
                        double cost = CostComp.GetCost_Border(borderPixel, _matched);
                        path.LastStepCosts[d] = cost;

                        if(bestCost > cost)
                        {
                            bestCost = cost;
                            bestDisp = d;
                        }
                    }
                    _bestPathCosts[path.CurrentPixel.Y, path.CurrentPixel.X, i] = new DisparityCost(bestDisp, bestCost);
                }
            }
        }

        void CreatePathsForBorderPixel(int y, int x)
        {
            Path[] paths = new Path[16];

            // Create only those paths which can start on pixel (y,x)
            if(x == 0)
                paths[(int)PathDirection.PosX] = new Path_Str_XPos();
            else if(x == ImageBase.ColumnCount - 1)
                paths[(int)PathDirection.NegX] = new Path_Str_XNeg();

            if(y == 0)
                paths[(int)PathDirection.PosY] = new Path_Str_YPos();
            else if(y == ImageBase.RowCount - 1)
                paths[(int)PathDirection.NegY] = new Path_Str_YNeg();

            if(x == 0 || y == 0)
            {
                paths[(int)PathDirection.PosX_PosY] = new Path_Diag_XPosYPos();
                paths[(int)PathDirection.PosX2_PosY] = new Path_Diag2_X2PosYPos();
                paths[(int)PathDirection.PosX_PosY2] = new Path_Diag2_XPosY2Pos();
            }
            if(x == ImageBase.ColumnCount - 1 || y == 0)
            {
                paths[(int)PathDirection.NegX_PosY] = new Path_Diag_XNegYPos();
                paths[(int)PathDirection.NegX2_PosY] = new Path_Diag2_X2NegYPos();
                paths[(int)PathDirection.NegX_PosY2] = new Path_Diag2_XNegY2Pos();
            }
            if(x == 0 || y == ImageBase.RowCount - 1)
            {
                paths[(int)PathDirection.PosX_NegY] = new Path_Diag_XPosYNeg();
                paths[(int)PathDirection.PosX2_NegY] = new Path_Diag2_X2PosYNeg();
                paths[(int)PathDirection.PosX_NegY2] = new Path_Diag2_XPosY2Neg();
            }
            if(x == ImageBase.ColumnCount - 1 || y == ImageBase.RowCount - 1)
            {
                paths[(int)PathDirection.NegX_NegY] = new Path_Diag_XNegYNeg();
                paths[(int)PathDirection.NegX2_NegY] = new Path_Diag2_X2NegYNeg();
                paths[(int)PathDirection.NegX_NegY2] = new Path_Diag2_XNegY2Neg();
            }

            _paths[y, x] = paths;
        }

        void InitBorderPixelGetters()
        {
            _borderPixelGetters = new Path.BorderPixelGetter[16];
            _borderPixelGetters[(int)PathDirection.PosX] = Path_Str_XPos.GetBorderPixel;
            _borderPixelGetters[(int)PathDirection.NegX] = Path_Str_XNeg.GetBorderPixel;
            _borderPixelGetters[(int)PathDirection.PosY] = Path_Str_YPos.GetBorderPixel;
            _borderPixelGetters[(int)PathDirection.NegY] = Path_Str_YNeg.GetBorderPixel;

            _borderPixelGetters[(int)PathDirection.PosX_PosY] = Path_Diag_XPosYPos.GetBorderPixel;
            _borderPixelGetters[(int)PathDirection.NegX_PosY] = Path_Diag_XNegYPos.GetBorderPixel;
            _borderPixelGetters[(int)PathDirection.PosX_NegY] = Path_Diag_XPosYNeg.GetBorderPixel;
            _borderPixelGetters[(int)PathDirection.NegX_NegY] = Path_Diag_XNegYNeg.GetBorderPixel;

            _borderPixelGetters[(int)PathDirection.PosX2_PosY] = Path_Diag2_X2PosYPos.GetBorderPixel;
            _borderPixelGetters[(int)PathDirection.NegX2_PosY] = Path_Diag2_X2NegYPos.GetBorderPixel;
            _borderPixelGetters[(int)PathDirection.PosX2_NegY] = Path_Diag2_X2PosYNeg.GetBorderPixel;
            _borderPixelGetters[(int)PathDirection.NegX2_NegY] = Path_Diag2_X2NegYNeg.GetBorderPixel;
            _borderPixelGetters[(int)PathDirection.PosX_PosY2] = Path_Diag2_XPosY2Pos.GetBorderPixel;
            _borderPixelGetters[(int)PathDirection.NegX_PosY2] = Path_Diag2_XNegY2Pos.GetBorderPixel;
            _borderPixelGetters[(int)PathDirection.PosX_NegY2] = Path_Diag2_XPosY2Neg.GetBorderPixel;
            _borderPixelGetters[(int)PathDirection.NegX_NegY2] = Path_Diag2_XNegY2Neg.GetBorderPixel;
        }

        public override void ComputeMatchingCosts()
        {
        }

        private void FindCost(Path path, int d)
        {

        }

        private double FindCost_Rect(IntVector2 basePixel, Path path, int d, int bestPrevDisp, double bestPrevCost, int dmax, double prevCost)
        {
            double pen0, pen1, pen2;

            pen0 = path.LastStepCosts[d];
            if(d == 0)
                pen1 = path.LastStepCosts[d + 1];
            else if(d > _dispRange - 2)
                pen1 = prevCost; // path.LastStepCosts[d - 1];
            else
                pen1 = Math.Min(path.LastStepCosts[d + 1], prevCost);//  path.LastStepCosts[d - 1]);

            pen2 = double.MaxValue;
            if(bestPrevDisp < d - 1 || bestPrevDisp > d + 1)
            {
                pen2 = bestPrevCost;
            }
            else
            {
                for(int dk = 0; dk < d - 1; ++dk)
                {
                    pen2 = Math.Min(path.LastStepCosts[dk], pen2);
                }

                for(int dk = d + 2; dk < dmax - 1; ++dk)
                {
                    pen2 = Math.Min(path.LastStepCosts[dk], pen2);
                }
            }

            if(basePixel.X >= ImageBase.ColumnCount || _matched.X >= ImageBase.ColumnCount ||
                basePixel.X < 0 || _matched.X < 0)
            {

            }

            if(Math.Min(pen0, Math.Min(pen1 + _penaltyLow, pen2 + _penaltyHigh * (1.0 - GradientCoeff *
                Math.Abs(ImageBase[basePixel.Y, basePixel.X] - ImageMatched[_matched.Y, _matched.X])))) !=
                Math.Min(
                pen0, Math.Min(pen1 + _penaltyLow, bestPrevCost + _penaltyHigh * (1.0 - GradientCoeff *
                Math.Abs(ImageBase[basePixel.Y, basePixel.X] - ImageMatched[_matched.Y, _matched.X])))))
            {

            }

            //return CostComp.GetCost_Border(basePixel, _matched) + Math.Min(
            //    pen0, Math.Min(pen1 + _penaltyLow, pen2 + _penaltyHigh * (1.0 - GradientCoeff *
            //    Math.Abs(ImageBase[basePixel.Y, basePixel.X] - ImageMatched[_matched.Y, _matched.X]))));


            double c = CostComp.GetCost_Border(basePixel, _matched);
            return c + Math.Min(
                pen0, Math.Min(pen1 + _penaltyLow, bestPrevCost + _penaltyHigh * (1.0 - GradientCoeff *
                Math.Abs(ImageBase[basePixel.Y, basePixel.X] - ImageMatched[_matched.Y, _matched.X]))));
        }

        public double GetCost(IntVector2 pixel, Disparity disp)
        {
            return GetCost(pixel, disp.GetMatchedPixel(pixel));
        }

        public double GetCost(IntVector2 basePixel, IntVector2 matchedPixel)
        {
            matchedPixel.X = Math.Max(0, Math.Min(matchedPixel.X, ImageBase.ColumnCount - 1));
            matchedPixel.Y = Math.Max(0, Math.Min(matchedPixel.Y, ImageBase.RowCount - 1));

            return CostComp.GetCost_Border(basePixel, matchedPixel);
        }

        public override void ComputeMatchingCosts_Rectified()
        {
            Vector2 pb_d = new Vector2();
            _bestPathCosts = new DisparityCost[ImageBase.RowCount, ImageBase.ColumnCount, 16];
            _dispRange = ImageBase.ColumnCount - 1;

            _penaltyLow = LowPenaltyCoeff * CostComp.MaxCost;
            _penaltyHigh = HighPenaltyCoeff * CostComp.MaxCost;

            CreateBorderPaths();
            InitBorderPixelGetters();

            FindCostsTopDown();
            FindCostsBottomUp();
            FindDisparities();
        }

        private void FindCostsTopDown()
        {
            int[] paths = IsLeftImageBase ? _pathsInRun_LeftTopDown : _pathsInRun_RightTopDown;
            // 1st run: start from (0,0) move left/downwards
            for(int r = 0; r < ImageBase.RowCount; ++r)
            {
                for(int c = 0; c < ImageBase.ColumnCount; ++c)
                {
                    CurrentPixel.Set(x: c, y: r);
                    _matched.Y = CurrentPixel.Y;

                    foreach(int pathIdx in paths)
                    {
                        FindCostsForPath(pathIdx, false);
                    }
                }
            }
        }

        private void FindCostsBottomUp()
        {
            int[] paths = IsLeftImageBase ? _pathsInRun_LeftBottomUp : _pathsInRun_RightBottomUp;
            // 2nd run: start from (rows,cols) move right/upwards
            for(int r = ImageBase.RowCount - 1; r >= 0; --r)
            {
                for(int c = ImageBase.ColumnCount - 1; c >= 0; --c)
                {
                    CurrentPixel.Set(x: c, y: r);
                    _matched.Y = CurrentPixel.Y;

                    foreach(int pathIdx in paths)
                    {
                        FindCostsForPath(pathIdx, true);
                    }
                }
            }
        }

        private void FindDisparities()
        {
            // 3rd run: compute final disparity based on paths' bests
            // Set disparity to be weighted average of best disparities
            for(int r = 0; r < ImageBase.RowCount; ++r)
            {
                for(int c = 0; c < ImageBase.ColumnCount; ++c)
                {
                    CurrentPixel.Set(x: c, y: r);
                    _matched.Y = CurrentPixel.Y;

                    for(int i = 0; i < 16; ++i)
                    {
                        _matched.X = CurrentPixel.X + _bestPathCosts[CurrentPixel.Y, CurrentPixel.X, i].Disparity;
                        double matchCost = GetCost(CurrentPixel, _matched);
                        DispComp.StoreDisparity(new Disparity()
                        {
                            DX = IsLeftImageBase ? 
                             -_bestPathCosts[CurrentPixel.Y, CurrentPixel.X, i].Disparity :
                              _bestPathCosts[CurrentPixel.Y, CurrentPixel.X, i].Disparity,
                            Cost = matchCost
                        });
                    }
                    DispComp.FinalizeForPixel(CurrentPixel);
                }
            }
        }


        private void FindCostsForPath(int pathIdx, bool startedOnZeroRange)
        {
            IntVector2 borderPixel = _borderPixelGetters[pathIdx](
                                            CurrentPixel, ImageBase.RowCount, ImageBase.ColumnCount);

            Path path = _paths[borderPixel.Y, borderPixel.X][pathIdx];
            if(path.Length <= 0)
            {
                _bestPathCosts[path.CurrentPixel.Y, path.CurrentPixel.X, pathIdx] = new DisparityCost(0, 1e12);
                return;
            }
            if(path.CurrentIndex >= path.Length)
            {
                //_bestPathCosts[path.CurrentPixel.Y, path.CurrentPixel.X, pathIdx] = new DisparityCost(0, 1e12);
                return;
            }

            // If base is right, then for each base pixel, matched one is on the right - disparity is positive
            int maxDisp = IsLeftImageBase ? path.CurrentPixel.X : ImageBase.ColumnCount - 1 - path.CurrentPixel.X;
            int bestDisp = 0;
            double bestCost = double.MaxValue;
            DisparityCost bestPrev = _bestPathCosts[path.PreviousPixel.Y, path.PreviousPixel.X, pathIdx];
            double prevCost = double.MaxValue;

            for(int d = 0; d < maxDisp; ++d)
            {
                _matched.X = IsLeftImageBase ? path.CurrentPixel.X - d : path.CurrentPixel.X + d;

                double cost = FindCost_Rect(path.CurrentPixel, path, d, bestPrev.Disparity, bestPrev.Cost, maxDisp, prevCost);
                prevCost = path.LastStepCosts[d];
                path.LastStepCosts[d] = cost;

                // Save best disparity at current path index
                if(bestCost > cost)
                {
                    bestCost = cost;
                    bestDisp = d;
                }
            }
            _bestPathCosts[path.CurrentPixel.Y, path.CurrentPixel.X, pathIdx] = new DisparityCost(bestDisp, bestCost);

            if(startedOnZeroRange && maxDisp > 0)
            {
                // For disparity greater than max, matched pixel will exceed image dimensions:
                // L[p, d > dmax-1] = Cost(curPix, maxXPix) + LastCost[dmax-1]
                // We actualy need only to compute L[p, dmax] as it will be needed in next iteration
                _matched.X = IsLeftImageBase ? 0 : ImageBase.ColumnCount - 1;
                path.LastStepCosts[maxDisp] = // As LastStepCosts is of size dispRange + 1 we won't exceed max index
                    GetCost(CurrentPixel, _matched) + path.LastStepCosts[maxDisp - 1];
            }

            path.Next();
        }

        public override void InitParameters()
        {
            base.InitParameters();

            //  IntParameter pathLenParam =
            //      new IntParameter("Paths Max Length", "PATH", 10, 1, 10000);
            //  Parameters.Add(pathLenParam);

            DoubleParameter lowPenParam =
                new DoubleParameter("Low Disparity Penalty Coeff", "P1", 0.02, 0.0, 1.0);
            Parameters.Add(lowPenParam);

            DoubleParameter highPenParam =
                new DoubleParameter("High Disparity Penalty Coeff", "P2", 0.04, 0.0, 1.0);
            Parameters.Add(highPenParam);

            DoubleParameter gradientCoeff =
                new DoubleParameter("High Disparity Gradient Coeff", "GRAD", 0.75, 0.0, 2.0);
            Parameters.Add(gradientCoeff);

            IntParameter maxDispParam = new IntParameter(
                "Maximum Disparity", "MAXD", 100, -10000, 10000);
            Parameters.Add(maxDispParam);

            IntParameter minDispParam = new IntParameter(
                "Minimum Disparity (Negative)", "MIND", 10, -10000, 10000);
            Parameters.Add(minDispParam);


            ParametrizedObjectParameter disparityParam = new ParametrizedObjectParameter(
                "Disparity Computer", "DISP_COMP");

            disparityParam.Parameterizables = new List<IParameterizable>();
            var dcSGM = new SGMDisparityComputer();
            dcSGM.InitParameters();
            disparityParam.Parameterizables.Add(dcSGM);

            Parameters.Add(disparityParam);
        }

        public override void UpdateParameters()
        {
            base.UpdateParameters();
            // PathsLength = AlgorithmParameter.FindValue<int>("PATH", Parameters);
            LowPenaltyCoeff = IAlgorithmParameter.FindValue<double>("P1", Parameters);
            HighPenaltyCoeff = IAlgorithmParameter.FindValue<double>("P2", Parameters);
            GradientCoeff = IAlgorithmParameter.FindValue<double>("GRAD", Parameters);
            MaxDisparity = IAlgorithmParameter.FindValue<int>("MAXD", Parameters);
            MinDisparity = IAlgorithmParameter.FindValue<int>("MIND", Parameters);

            DispComp = IAlgorithmParameter.FindValue<DisparityComputer>("DISP_COMP", Parameters);
            DispComp.UpdateParameters();
        }

        public override string Name
        {
            get
            {
                return "SGM Cost Aggregator";
            }
        }
    }
}
