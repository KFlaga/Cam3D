﻿using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using CamAlgorithms;
using CamCore;
using CamAlgorithms.ImageMatching;

namespace CamUnitTest
{
    /// <summary>
    /// Summary description for MatchingCostTests
    /// </summary>
    [TestClass]
    public class MatchingCostTests
    {
        private Matrix<double> _imageLeft;
        private Matrix<double> _imageRight;
        

        public MatchingCostTests()
        {

        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void PrepareTestImages()
        {
            _imageLeft = new DenseMatrix(10, 10);
            _imageRight = new DenseMatrix(10, 10);

            for(int c = 0; c < 10; ++c)
            {
                for(int r = 0; r < 10; ++r)
                {
                    if((c + r) % 3 == 0)
                    {
                        _imageLeft[r, c] = 0.1;
                        _imageRight[r, c] = 0.5;
                    }
                    else if((c + r) % 3 == 1)
                    {
                        _imageLeft[r, c] = 0.5;
                        _imageRight[r, c] = 1;
                    }
                    else
                    {
                        _imageLeft[r, c] = 1;
                        _imageRight[r, c] = 0.1;
                    }
                }
            }

        }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion
            
        [TestMethod]
        public void Test_Rank()
        {
            RankCostComputer costCpu = new RankCostComputer();
            costCpu.RankMaskHeight = 1;
            costCpu.RankMaskWidth = 1;
            costCpu.CorrMaskHeight = 1;
            costCpu.CorrMaskWidth = 1;
            costCpu.ImageBase = new GrayScaleImage() { ImageMatrix = _imageLeft };
            costCpu.ImageMatched = new GrayScaleImage() { ImageMatrix = _imageRight };

            costCpu.Init();

            // Expected rank in points (base):
            // [0,0] : 0, [0,1] : 3, [1,1] : 6, [2,2] : 3
            Assert.IsTrue(costCpu.RankBase[0, 0] == 0, "Wrong rank transform in point [0,0]. Expected: 0; Actual: " + costCpu.RankBase[0, 0]);
            Assert.IsTrue(costCpu.RankBase[1, 0] == 3, "Wrong rank transform in point [0,1]. Expected: 3; Actual: " + costCpu.RankBase[0, 1]);
            Assert.IsTrue(costCpu.RankBase[1, 1] == 6, "Wrong rank transform in point [1,1]. Expected: 6; Actual: " + costCpu.RankBase[1, 1]);
            Assert.IsTrue(costCpu.RankBase[2, 2] == 3, "Wrong rank transform in point [2,2]. Expected: 3; Actual: " + costCpu.RankBase[2, 2]);

            // Expected matching cost in points (base, matched):
            // ([2,2], [2,3]) : 36
            // ([2,2], [2,4]) : 0
            // ([2,2], [2,2]) : 36
            double cost = costCpu.GetCost(new IntVector2(2, 2), new IntVector2(3, 2));
            Assert.IsTrue(Math.Abs(cost - 36.0) < 1e-6, "Wrong rank cost in points ([2,2],[2,3]). Expected: 4; Actual: " + cost);
            cost = costCpu.GetCost(new IntVector2(2, 2), new IntVector2(4, 2));
            Assert.IsTrue(Math.Abs(cost - 0.0) < 1e-6, "Wrong rank cost in points ([2,2],[2,4]). Expected: 0; Actual: " + cost);
            cost = costCpu.GetCost(new IntVector2(2, 2), new IntVector2(2, 2));
            Assert.IsTrue(Math.Abs(cost - 36.0) < 1e-6, "Wrong rank cost in points ([2,2],[2,2]). Expected: 4; Actual: " + cost);
        }

        [TestMethod]
        public void Test_Census()
        {
            CensusCostComputer costCpu = new CensusCostComputer();
            costCpu.HeightRadius = 1;
            costCpu.WidthRadius = 1;
            costCpu.ImageBase = new GrayScaleImage() { ImageMatrix = _imageLeft };
            costCpu.ImageMatched = new GrayScaleImage() { ImageMatrix = _imageRight };

            costCpu.Init();

            // Expected census in points (base):
            // [0,0] : 0000_0000, [0,1] : 0100_0101, [1,1] : 1101_1011, [2,2] : 0101_0001
            IBitWord c00 = BitWord.CreateBitWord(new uint[1] { 0 });
            IBitWord c10 = BitWord.CreateBitWord(new uint[1] { 1 << 8 | 0 << 7 | 1 << 6 | 0 << 5 | 0 << 4 | 0 << 3 | 0 << 2 | 1 << 1 | 0 << 0 });
            IBitWord c11 = BitWord.CreateBitWord(new uint[1] { 1 << 8 | 1 << 7 | 0 << 6 | 1 << 5 | 0 << 4 | 1 << 3 | 0 << 2 | 1 << 1 | 1 << 0 });
            IBitWord c22 = BitWord.CreateBitWord(new uint[1] { 1 << 8 | 0 << 7 | 0 << 6 | 0 << 5 | 0 << 4 | 1 << 3 | 0 << 2 | 1 << 1 | 0 << 0 });
            int diff = costCpu.CensusBase[0, 0].GetHammingDistance(c00);
            Assert.IsTrue(diff == 0,
                "Wrong census transform in point [0,0]. Census: " + costCpu.CensusBase[0, 0].ToString());
            diff = costCpu.CensusBase[0, 1].GetHammingDistance(c10);
            Assert.IsTrue(diff == 0,
                "Wrong census transform in point [0,1]. Census: " + costCpu.CensusBase[0, 1].ToString());
            diff = costCpu.CensusBase[1, 1].GetHammingDistance(c11);
            Assert.IsTrue(diff == 0,
                "Wrong census transform in point [1,1]. Census: " + costCpu.CensusBase[1, 1].ToString());
            diff = costCpu.CensusBase[2, 2].GetHammingDistance(c22);
            Assert.IsTrue(diff == 0,
                "Wrong census transform in point [2,2]. Census: " + costCpu.CensusBase[2, 2].ToString());

            // Expected matching cost in points (base, matched):
            // ([2,2], [3,2]) : 3
            // ([2,2], [4,2]) : 0
            // ([2,2], [2,2]) : 3
            double cost = costCpu.GetCost(new IntVector2(2, 2), new IntVector2(3, 2));
            Assert.IsTrue(cost.Round() == 3, "Wrong rank cost in points ([2,2],[3,2]). Expected: 3; Actual: " + cost);
            cost = costCpu.GetCost(new IntVector2(2, 2), new IntVector2(4, 2));
            Assert.IsTrue(cost.Round() == 0, "Wrong rank cost in points ([2,2],[4,2]). Expected: 0; Actual: " + cost);
            cost = costCpu.GetCost(new IntVector2(2, 2), new IntVector2(2, 2));
            Assert.IsTrue(cost.Round() == 3, "Wrong rank cost in points ([2,2],[2,2]). Expected: 3; Actual: " + cost);
        }

        [TestMethod]
        public void Test_BitWordCost_Interface()
        {
            HammingLookup.ComputeWordBitsLookup();
            int size = 500;
            IBitWord[,] map0 = new IBitWord[size, size];
            IBitWord[,] map1 = new IBitWord[size, size];

            for(int x = 0; x < size; ++x)
            {
                for(int y = 0; y < size; ++y)
                {
                    map0[y, x] = new BitWord_32(new uint[] { 1, 2, 3, 4, 5, 6, 7, 8 });
                    map1[y, x] = new BitWord_32(new uint[] { 1, 2, 3, 4, 5, 6, 7, 0 });
                }
            }

            double totalCost = 0;
            for(int i = 0; i < 40; ++i)
            {
                for(int x = 0; x < size; ++x)
                {
                    for(int y = 0; y < size; ++y)
                    {
                        totalCost += map0[y, x].GetHammingDistance(map1[y, x]);
                    }
                }
            }
            System.Console.WriteLine(totalCost);
        }

        [TestMethod]
        public void Test_BitWordCost_Concrete()
        {
            HammingLookup.ComputeWordBitsLookup();
            int size = 500;
            BitWord_32[,] map0 = new BitWord_32[size, size];
            BitWord_32[,] map1 = new BitWord_32[size, size];

            for(int x = 0; x < size; ++x)
            {
                for(int y = 0; y < size; ++y)
                {
                    map0[y, x] = new BitWord_32(new uint[] { 1, 2, 3, 4, 5, 6, 7, 8 });
                    map1[y, x] = new BitWord_32(new uint[] { 1, 2, 3, 4, 5, 6, 7, 0 });
                }
            }

            double totalCost = 0;
            for(int i = 0; i < 50; ++i)
            {
                for(int x = 0; x < size; ++x)
                {
                    for(int y = 0; y < size; ++y)
                    {
                        totalCost += map0[y, x].GetHammingDistance(map1[y, x]);
                    }
                }
            }
            System.Console.WriteLine(totalCost);
        }
    }
}
