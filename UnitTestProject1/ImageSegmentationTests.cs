using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using CamAlgorithms.ImageMatching;
using CamCore;
using CamAlgorithms;

namespace CamUnitTest
{
    /// <summary>
    /// Summary description for MatchingCostTests
    /// </summary>
    [TestClass]
    public class ImageSegmentationTests
    {
        Matrix<double> _grayImage;
        ColorImage _colorImage;
        DisparityMap _map;

        [TestMethod]
        public void TestClosePoint_TrivialCase()
        {
            ClosePointsSegmentation segmentation = new ClosePointsSegmentation();
            segmentation.InitParameters();
            segmentation.UpdateParameters();
            segmentation.MaxDiffSquared = 0.05 * 0.05;
            TestSegmentation_TrivialCase(segmentation);
        }

        [TestMethod]
        public void TestMeanShift_TrivialCase()
        {
            MeanShiftSegmentation segmentation = new MeanShiftSegmentation();
            segmentation.InitParameters();
            segmentation.UpdateParameters();
            TestSegmentation_TrivialCase(segmentation);
        }

        [TestMethod]
        public void TestWatershed_TrivialCase()
        {
            WatershedSegmentation segmentation = new WatershedSegmentation();
            segmentation.InitParameters();
            segmentation.UpdateParameters();
            TestSegmentation_TrivialCase(segmentation);
        }

        private void TestSegmentation_TrivialCase(ImageSegmentation segm)
        {
            // Image: four blocks in different colors
            int size = 20;
            _grayImage = new DenseMatrix(size);
            _colorImage = new ColorImage() { ImageMatrix = new Matrix<double>[] { new DenseMatrix(size), new DenseMatrix(size), new DenseMatrix(size) } };
            _map = new DisparityMap(size, size);

            FillTopLeft(size);
            FillTopRight(size);
            FillBotLeft(size);
            FillBotRight(size);

            segm.SegmentGray(_grayImage);
            Assert.IsTrue(segm.Segments.Count == 4);

            segm.SegmentColor(_colorImage);
            Assert.IsTrue(segm.Segments.Count == 4);

            segm.SegmentDisparity(_map);
            Assert.IsTrue(segm.Segments.Count == 4);
        }

        private void FillTopLeft(int size)
        {
            FillImages(0, size / 2, 0, size / 2, 0, new double[] { 0, 0, 0 }, new Disparity(new IntVector2(0, 0), new IntVector2(0, 0), 0, 0, (int)DisparityFlags.Valid));
        }

        private void FillTopRight(int size)
        {
            FillImages(size / 2, size, 0, size / 2, 0.25, new double[] { 1.0, 0, 0 }, new Disparity(new IntVector2(0, 0), new IntVector2(2, 0), 0, 0, (int)DisparityFlags.Valid));
        }

        private void FillBotLeft(int size)
        {
            FillImages(0, size / 2, size / 2, size, 0.5, new double[] { 0, 1.0, 0 }, new Disparity(new IntVector2(0, 0), new IntVector2(0, 2), 0, 0, (int)DisparityFlags.Valid));
        }

        private void FillBotRight(int size)
        {
            FillImages(size / 2, size, size / 2, size, 1.0, new double[] { 0, 0, 1.0 }, new Disparity(new IntVector2(0, 0), new IntVector2(2, 2), 0, 0, (int)DisparityFlags.Valid));
        }

        private void FillImages(int xmin, int xmax, int ymin, int ymax, double grey, double[] rgb, Disparity disp )
        {
            for(int x = xmin; x < xmax; ++x)
            {
                for(int y = ymin; y < ymax; ++y)
                {
                    _grayImage[y, x] = grey;
                    _colorImage[y, x, RGBChannel.Red] = rgb[0];
                    _colorImage[y, x, RGBChannel.Green] = rgb[1];
                    _colorImage[y, x, RGBChannel.Blue] = rgb[2];
                    _map[y, x] = disp;
                }
            }
        }
    }
}
