using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using CamImageProcessing.ImageMatching;
using CamCore;
using CamImageProcessing;

namespace UnitTestProject1
{
    /// <summary>
    /// Summary description for MatchingCostTests
    /// </summary>
    [TestClass]
    public class CostAggregatorsTests
    {
        private Matrix<double> _imageLeft;
        private Matrix<double> _imageRight;

        private int[,] _expectedDisparity;
        private int[,] _computedDisparity;

        private Matrix<double> _F;

        public CostAggregatorsTests()
        {

        }

        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void PrepareTestImages()
        {
            _imageLeft = new DenseMatrix(40, 40);
            _imageRight = new DenseMatrix(40, 40);
            _expectedDisparity = new int[40, 40];

            Random rand = new Random(101);
            for(int c = 0; c < 40; ++c)
            {
                for(int r = 0; r < 40; ++r)
                {
                    double e = rand.NextDouble() * 0.01;
                    if(((c == 1 || c == 4) && (r >= 1 && r <= 5)) ||
                        ((c == 15 || c == 18) && (r >= 14 && r <= 18)))
                    {
                        _imageLeft[r, c] = 0.5;
                    }
                    else if(((c == 2 || c == 3) && (r >= 1 && r <= 5)) ||
                        ((c == 16 || c == 17) && (r >= 14 && r <= 18)))
                    {
                        _imageLeft[r, c] = 1.0;
                    }
                    else
                    {
                        _imageLeft[r, c] = 0.005 * c + e;
                    }

                    if(c < 39)
                        _imageRight[r, c + 1] = 0.005 * c + e;
                    else
                    {
                        _imageRight[r, 0] = e * 0.5;
                    }
                }
            }

            // Let disparity be: 
            // [0,1] for background
            // [0,3] for upper object
            // [0,2] for lower object

            for(int c = 0; c < 40; ++c)
            {
                for(int r = 0; r < 40; ++r)
                {
                    if(((c == 1 || c == 4) && (r >= 1 && r <= 5)) ||
                       ((c == 2 || c == 3) && (r >= 1 && r <= 5)))
                    {
                        _imageRight[r, c + 3] = _imageLeft[r, c];
                        _expectedDisparity[r, c] = 3;
                    }
                    else if((c == 15 && (r >= 14 && r <= 18)) ||
                           ((c == 16 || c == 17) && (r >= 14 && r <= 18)))
                    {
                        _imageRight[r, c + 2] = _imageLeft[r, c];
                        _expectedDisparity[r, c] = 2;
                    }
                    else if((c == 18 && (r >= 14 && r <= 18)))
                    {
                        _expectedDisparity[r, c] = int.MaxValue;
                    }
                    else
                    {
                        _expectedDisparity[r, c] = 1;
                    }
                }
            }

            for(int r = 0; r < 40; ++r)
            {
                _expectedDisparity[r, 39] = int.MaxValue;
            }

            _F = new DenseMatrix(3, 3);
            _F[1, 2] = -1.0;
            _F[2, 1] = 1.0;
        }

        [TestMethod]
        public void Test_EpiScan_Ideal()
        {
            EpilineScanAggregator agg = new EpilineScanAggregator();
            RankCostComputer cost = new RankCostComputer();
            MatchConfidenceComputer conf = new MatchConfidenceComputer();
            DisparityComputer dcomp = new WTADisparityComputer();

            cost.ImageBase = new GrayScaleImage() { ImageMatrix = _imageLeft };
            cost.ImageMatched = new GrayScaleImage() { ImageMatrix = _imageRight };
            cost.RankMaskHeight = 3;
            cost.RankMaskWidth = 3;
            cost.CorrMaskWidth = 3;
            cost.CorrMaskHeight = 3;
            //cost.MaskHeight = 3;
            //cost.MaskWidth = 3;
            agg.CostComp = cost;

            conf.CostComp = cost;
            conf.UsedConfidenceMethod = ConfidenceMethod.TwoAgainstTwo;

            DisparityMap disp = new DisparityMap(_imageLeft.RowCount, _imageLeft.ColumnCount);
            agg.DisparityMap = disp;

            agg.DispComp = dcomp;

            agg.ImageBase = cost.ImageBase;
            agg.ImageMatched = cost.ImageMatched;
            agg.IsLeftImageBase = true;
            agg.Fundamental = _F;

            agg.Init();
            agg.ComputeMatchingCosts();
        }

        [TestMethod]
        public void Test_BSGM_NoNoise()
        {
            SGMAggregator agg = new SGMAggregator();
            CensusCostComputer cost = new CensusCostComputer();
            WTADisparityComputer dispComp = new WTADisparityComputer();

            cost.ImageBase = new GrayScaleImage() { ImageMatrix = _imageLeft };
            cost.ImageMatched = new GrayScaleImage() { ImageMatrix = _imageRight };
            cost.MaskWidth = 3;
            cost.MaskHeight = 3;
            agg.CostComp = cost;

            dispComp.ConfidenceComp.UsedConfidenceMethod = ConfidenceMethod.TwoAgainstTwo;
            dispComp.CostComp = cost;
            dispComp.ImageBase = cost.ImageBase;
            dispComp.ImageMatched = cost.ImageMatched;
            agg.DispComp = dispComp;

            DisparityMap disp = new DisparityMap(_imageLeft.RowCount, _imageLeft.ColumnCount);
            agg.DisparityMap = disp;
            dispComp.DisparityMap = disp;

            agg.ImageBase = cost.ImageBase;
            agg.ImageMatched = cost.ImageMatched;
            agg.IsLeftImageBase = true;
            agg.Fundamental = _F;

           // agg.PathsLength = 10;
            agg.LowPenaltyCoeff = 0.02;
            agg.HighPenaltyCoeff = 0.04;
            agg.MaxDisparity = 10;
            agg.MinDisparity = 1;

            dispComp.Init();
            agg.Init();
            agg.ComputeMatchingCosts();
        }
    }
}
