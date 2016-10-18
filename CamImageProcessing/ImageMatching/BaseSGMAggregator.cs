using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamImageProcessing.ImageMatching
{
    public class BaseSGMAggregator : CostAggregator
    {
        public int PathsLength { get; set; } = 10;

        public double LowPenaltyCoeff { get; set; } = 0.02; // P1 = coeff * MaxCost
        public double HighPenaltyCoeff { get; set; } = 0.04; // P2 = coeff * MaxCost * (1 - grad * |Ib - Im|)
        public double GradientCoeff { get; set; } = 0.5;
        public int MaxDisparity { get; set; }
        public int MinDisparity { get; set; }

        List<Disparity> _dispIndexMap;
        List<Path> _paths;

        public override void ComputeMatchingCosts()
        {
            Vector2 pb_d = new Vector2();
            CreatePaths();
            for(int c = 0; c < ImageBase.ColumnCount; ++c)
            {
                for(int r = 0; r < ImageBase.RowCount; ++r)
                {
                    pb_d.Set(x: c, y: r);
                    CurrentPixel.Set(x: c, y: r);
                    EpiLine epiLine = IsLeftImageBase ?
                        EpiLine.FindCorrespondingEpiline_LineOnRightImage(pb_d, Fundamental) :
                        EpiLine.FindCorrespondingEpiline_LineOnLeftImage(pb_d, Fundamental);

                    if(epiLine.IsHorizontal())
                        CreateDisparityIndexMap_Horizontal(epiLine);
                    else if(epiLine.IsVertical())
                        CreateDisparityIndexMap_Vertical(epiLine);
                    else
                        CreateDisparityIndexMap(epiLine);

                    foreach(Path path in _paths)
                    {
                        path.BasePixel = CurrentPixel;
                        path.Length = PathsLength;
                        path.DisparityRange = _dispIndexMap.Count;

                        path.Init();
                        if(path.Length > 0)
                        {
                            for(int d = 0; d < path.DisparityRange; ++d)
                            {
                                path.PathCost[path.CurrentIndex, d] = GetCost(path.CurrentPixel, _dispIndexMap[d]);
                            }

                            while(path.HaveNextPixel)
                            {
                                path.Next();
                                for(int d = 0; d < path.DisparityRange; ++d)
                                {
                                    FindCost(path, d);
                                }
                            }

                            for(int d = 0; d < path.DisparityRange; ++d)
                            {
                                _dispIndexMap[d].Cost += path.PathCost[path.CurrentIndex, d];
                            };
                        }
                    }

                    for(int d = 0; d < _dispIndexMap.Count; ++d)
                    {
                        DispComp.StoreDisparity(_dispIndexMap[d]);
                    }
                    DispComp.FinalizeForPixel(CurrentPixel);
                }
            }
        }

        private void FindCost(Path path, int d)
        {
            path.PathCost[path.CurrentIndex, d] = GetCost(path.CurrentPixel, _dispIndexMap[d]);

            double pen0 = path.PathCost[path.PreviousIndex, d];
            double pen1;
            if(d == 0)
                pen1 = path.PathCost[path.PreviousIndex, d + 1];
            else if(d == path.DisparityRange - 1)
                pen1 = path.PathCost[path.PreviousIndex, d - 1];
            else
                pen1 = Math.Min(path.PathCost[path.PreviousIndex, d + 1],
                    path.PathCost[path.PreviousIndex, d - 1]);

            double pen2 = double.MaxValue;
            int d2 = 0;

            for(int dk = 0; dk < d - 1; ++dk)
            {
                if(path.PathCost[path.PreviousIndex, dk] < pen2)
                {
                    pen2 = path.PathCost[path.PreviousIndex, dk];
                    d2 = dk;
                }
            }

            for(int dk = d + 2; dk < path.DisparityRange; ++dk)
            {
                if(path.PathCost[path.PreviousIndex, dk] < pen2)
                {
                    pen2 = path.PathCost[path.PreviousIndex, dk];
                    d2 = dk;
                }
            }

            double P1 = LowPenaltyCoeff * CostComp.MaxCost, P2 = HighPenaltyCoeff * CostComp.MaxCost;
            path.PathCost[path.CurrentIndex, d] +=
                Math.Min(pen0, Math.Min(pen1 + P1, pen2 + P2));
        }

        public void CreateDisparityIndexMap(EpiLine epiLine)
        {
            IntVector2 pm = new IntVector2();
            int xmax = epiLine.FindXmax(ImageBase.RowCount, ImageBase.ColumnCount);
            int x0 = epiLine.FindX0(ImageBase.RowCount);
            _dispIndexMap = new List<Disparity>(xmax - x0);
            xmax = xmax - 1;

            for(int xm = x0 + 1; xm < xmax; ++xm)
            {
                double ym0 = epiLine.FindYd(xm);
                double ym1 = epiLine.FindYd(xm + 1);
                for(int ym = (int)ym0; ym < (int)ym1; ++ym)
                {
                    pm.X = xm;
                    pm.Y = ym;
                    _dispIndexMap.Add(new Disparity(CurrentPixel, pm, 0.0, 0.0, (int)DisparityFlags.Valid));
                }
            }
        }

        public void CreateDisparityIndexMap_Horizontal(EpiLine epiLine)
        {
            IntVector2 pm = new IntVector2();
            _dispIndexMap = new List<Disparity>(ImageBase.ColumnCount);
            int x0 = Math.Max(CurrentPixel.X - MinDisparity, 0);
            int xmax = Math.Min(CurrentPixel.X + MaxDisparity, ImageBase.ColumnCount);

            for(int xm = x0; xm < xmax; ++xm)
            {
                pm.X = xm;
                pm.Y = CurrentPixel.Y;
                _dispIndexMap.Add(new Disparity(CurrentPixel, pm, 0.0, 0.0, (int)DisparityFlags.Valid));
            }
        }

        public void CreateDisparityIndexMap_Vertical(EpiLine epiLine)
        {
            IntVector2 pm = new IntVector2();
            _dispIndexMap = new List<Disparity>(ImageBase.RowCount);

            for(int ym = 0; ym < ImageBase.RowCount; ++ym)
            {
                pm.X = CurrentPixel.Y;
                pm.Y = ym;
                _dispIndexMap.Add(new Disparity(CurrentPixel, pm, 0.0, 0.0, (int)DisparityFlags.Valid));
            }
        }

        public double GetCost(IntVector2 pixel, Disparity disp)
        {
            IntVector2 matched = disp.GetMatchedPixel(pixel);
            matched.X = Math.Max(0, Math.Min(matched.X, ImageBase.ColumnCount - 1));
            matched.Y = Math.Max(0, Math.Min(matched.Y, ImageBase.RowCount - 1));

            return CostComp.GetCost_Border(pixel, matched);
        }
        
        public override void ComputeMatchingCosts_Rectified()
        {
            Vector2 pb_d = new Vector2();
            CreatePaths();
            foreach(Path path in _paths)
            {
                // 1) Precompute path costs for each disparity
                // set base pixel to border pixel
            }



            for(int c = 0; c < ImageBase.ColumnCount; ++c)
            {
                for(int r = 0; r < ImageBase.RowCount; ++r)
                {
                    pb_d.Set(x: c, y: r);
                    CurrentPixel.Set(x: c, y: r);
                    EpiLine epiLine = IsLeftImageBase ?
                        EpiLine.FindCorrespondingEpiline_LineOnRightImage(pb_d, Fundamental) :
                        EpiLine.FindCorrespondingEpiline_LineOnLeftImage(pb_d, Fundamental);

                    if(epiLine.IsHorizontal())
                        CreateDisparityIndexMap_Horizontal(epiLine);
                    else if(epiLine.IsVertical())
                        CreateDisparityIndexMap_Vertical(epiLine);
                    else
                        CreateDisparityIndexMap(epiLine);

                    foreach(Path path in _paths)
                    {
                        path.BasePixel = CurrentPixel;
                        path.Length = PathsLength;
                        path.DisparityRange = _dispIndexMap.Count;

                        path.Init();
                        if(path.Length > 0)
                        {
                            for(int d = 0; d < path.DisparityRange; ++d)
                            {
                                path.PathCost[path.CurrentIndex, d] = GetCost(path.CurrentPixel, _dispIndexMap[d]);
                            }

                            while(path.HaveNextPixel)
                            {
                                path.Next();
                                for(int d = 0; d < path.DisparityRange; ++d)
                                {
                                    FindCost(path, d);
                                }
                            }

                            for(int d = 0; d < path.DisparityRange; ++d)
                            {
                                _dispIndexMap[d].Cost += path.PathCost[path.CurrentIndex, d];
                            };
                        }
                    }

                    for(int d = 0; d < _dispIndexMap.Count; ++d)
                    {
                        DispComp.StoreDisparity(_dispIndexMap[d]);
                    }
                    DispComp.FinalizeForPixel(CurrentPixel);
                }
            }
        }

        public override void InitParameters()
        {
            base.InitParameters();

            IntParameter pathLenParam =
                new IntParameter("Paths Max Length", "PATH", 10, 1, 10000);
            Parameters.Add(pathLenParam);

            DoubleParameter lowPenParam =
                new DoubleParameter("Low Disparity Penalty Coeff", "P1", 0.02, 0.0, 1.0);
            Parameters.Add(lowPenParam);

            DoubleParameter highPenParam =
                new DoubleParameter("High Disparity Penalty Coeff", "P2", 0.04, 0.0, 1.0);
            Parameters.Add(highPenParam);

            DoubleParameter gradientCoeff =
                new DoubleParameter("High Disparity Gradient Coeff", "GRAD", 0.5, 0.0, 2.0);
            Parameters.Add(gradientCoeff);

            IntParameter maxDispParam = new IntParameter(
                "Maximum Disparity", "MAXD", 100, -10000, 10000);
            Parameters.Add(maxDispParam);

            IntParameter minDispParam = new IntParameter(
                "Minimum Disparity (Negative)", "MIND", 10, -10000, 10000);
            Parameters.Add(minDispParam);
        }

        public override void UpdateParameters()
        {
            base.UpdateParameters();
            PathsLength = AlgorithmParameter.FindValue<int>("PATH", Parameters);
            LowPenaltyCoeff = AlgorithmParameter.FindValue<double>("P1", Parameters);
            HighPenaltyCoeff = AlgorithmParameter.FindValue<double>("P2", Parameters);
            GradientCoeff = AlgorithmParameter.FindValue<double>("GRAD", Parameters);
            MaxDisparity = AlgorithmParameter.FindValue<int>("MAXD", Parameters);
            MinDisparity = AlgorithmParameter.FindValue<int>("MIND", Parameters);
        }

        public override string ToString()
        {
            return "Base SGM Cost Aggregator";
        }

        public void CreatePaths()
        {
            _paths = new List<Path>(16);
            _paths.Add(new Path_Str_XPos()
            {
                ImageHeight = ImageBase.RowCount,
                ImageWidth = ImageBase.ColumnCount
            });
            _paths.Add(new Path_Str_XNeg()
            {
                ImageHeight = ImageBase.RowCount,
                ImageWidth = ImageBase.ColumnCount
            });
            _paths.Add(new Path_Str_YPos()
            {
                ImageHeight = ImageBase.RowCount,
                ImageWidth = ImageBase.ColumnCount
            });
            _paths.Add(new Path_Str_YNeg()
            {
                ImageHeight = ImageBase.RowCount,
                ImageWidth = ImageBase.ColumnCount
            });
            _paths.Add(new Path_Diag_XPosYPos()
            {
                ImageHeight = ImageBase.RowCount,
                ImageWidth = ImageBase.ColumnCount
            });
            _paths.Add(new Path_Diag_XNegYPos()
            {
                ImageHeight = ImageBase.RowCount,
                ImageWidth = ImageBase.ColumnCount
            });
            _paths.Add(new Path_Diag_XPosYNeg()
            {
                ImageHeight = ImageBase.RowCount,
                ImageWidth = ImageBase.ColumnCount
            });
            _paths.Add(new Path_Diag_XNegYNeg()
            {
                ImageHeight = ImageBase.RowCount,
                ImageWidth = ImageBase.ColumnCount
            });
            _paths.Add(new Path_Diag2_X2PosYPos()
            {
                ImageHeight = ImageBase.RowCount,
                ImageWidth = ImageBase.ColumnCount
            });
            _paths.Add(new Path_Diag2_X2NegYPos()
            {
                ImageHeight = ImageBase.RowCount,
                ImageWidth = ImageBase.ColumnCount
            });
            _paths.Add(new Path_Diag2_X2PosYNeg()
            {
                ImageHeight = ImageBase.RowCount,
                ImageWidth = ImageBase.ColumnCount
            });
            _paths.Add(new Path_Diag2_X2NegYNeg()
            {
                ImageHeight = ImageBase.RowCount,
                ImageWidth = ImageBase.ColumnCount
            });
            _paths.Add(new Path_Diag2_XPosY2Pos()
            {
                ImageHeight = ImageBase.RowCount,
                ImageWidth = ImageBase.ColumnCount
            });
            _paths.Add(new Path_Diag2_XNegY2Pos()
            {
                ImageHeight = ImageBase.RowCount,
                ImageWidth = ImageBase.ColumnCount
            });
            _paths.Add(new Path_Diag2_XPosY2Neg()
            {
                ImageHeight = ImageBase.RowCount,
                ImageWidth = ImageBase.ColumnCount
            });
            _paths.Add(new Path_Diag2_XNegY2Neg()
            {
                ImageHeight = ImageBase.RowCount,
                ImageWidth = ImageBase.ColumnCount
            });
        }
    }
}
