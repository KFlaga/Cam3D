using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CamMain;
using CamMain.ProcessingChain;
using System.Xml;
using System.IO;

namespace CamUnitTest
{
    [TestClass]
    public class ProcessingChainTests
    {
        string _inputPath = @"../../resources/calib_input_test.xml";

        GlobalData _globalData;
        ConfigurationLink _configurationLink;
        RawCalibrationImagesLink _rawCalibLink;
        DistortionModelLink _distortionLink;
        UndistortPointsLink _undistortLink;
        CalibrationLink _calibrationLink;
        RectificationLink _rectificationLink;
        MatchedImagesLink _matchedImgsLink;
        ImageMatchingLink _imageMatchingLink;
        DisparityRefinementLink _refinementLink;
        TriangulationLink _triangulationLink;

        void PrepareConfiguration()
        {
            _globalData = new GlobalData();
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(_inputPath);

            _configurationLink = new ConfigurationLink(_globalData, xmlDoc)
            {
                StoreDataOnDisc = false
            };

            _configurationLink.Load();
            _configurationLink.Save();
        }

        void PrepareRawCalibrationData()
        {
            _rawCalibLink = new RawCalibrationImagesLink(_globalData)
            {
                LoadDataFromDisc = true,
                StoreDataOnDisc = false
            };
            _rawCalibLink.Load();
            _rawCalibLink.Save();
        }

        void PrepareDistortionModelData()
        {
            _distortionLink = new DistortionModelLink(_globalData)
            {
                LoadDataFromDisc = true,
                StoreDataOnDisc = false
            };
            _distortionLink.Load();
            _distortionLink.Save();
        }

        void PrepareUndistortPointsData()
        {
            _undistortLink = new UndistortPointsLink(_globalData)
            {
                LoadDataFromDisc = true,
                StoreDataOnDisc = false
            };
            _undistortLink.Load();
            _undistortLink.Save();
        }

        void PrepareCalibrationData()
        {
            _calibrationLink = new CalibrationLink(_globalData)
            {
                LoadDataFromDisc = true,
                StoreDataOnDisc = false
            };
            _calibrationLink.Load();
            _calibrationLink.Save();
        }

        void PrepareRectificationData()
        {
            _rectificationLink = new RectificationLink(_globalData)
            {
                LoadDataFromDisc = true,
                StoreDataOnDisc = false
            };
            _rectificationLink.Load();
            _rectificationLink.Save();
        }

        void PrepareMatchedImagesData()
        {
            _matchedImgsLink = new MatchedImagesLink(_globalData)
            {
                LoadDataFromDisc = true,
                StoreDataOnDisc = false
            };
            _matchedImgsLink.Load();
            _matchedImgsLink.Save();
        }

        void PrepareImageMatchingData()
        {
            _imageMatchingLink = new ImageMatchingLink(_globalData)
            {
                LoadDataFromDisc = true,
                StoreDataOnDisc = false
            };
            _imageMatchingLink.Load();
            _imageMatchingLink.Save();
        }

        void PrepareDisprityRefinementData()
        {
            _refinementLink = new DisparityRefinementLink(_globalData)
            {
                LoadDataFromDisc = true,
                StoreDataOnDisc = false
            };
            _refinementLink.Load();
            _refinementLink.Save();
        }

        void PrepareTriangulationData()
        {
            _triangulationLink = new TriangulationLink(_globalData)
            {
                LoadDataFromDisc = true,
                StoreDataOnDisc = false
            };
            _triangulationLink.Load();
            _triangulationLink.Save();
        }

        void SaveOutput()
        {
            using(Stream outFile = new FileStream(_inputPath, FileMode.Create))
            {
                _globalData.Get<ConfigurationLinkData>().ConfigDoc.Save(outFile);
            }
        }

        [TestMethod]
        public void TestConfigurationLink()
        {
            PrepareConfiguration();

            Assert.IsTrue(_globalData.Get<ConfigurationLinkData>().WorkingDirectory ==
                @"../../resources/");
            Assert.IsTrue(_globalData.Get<ImagesSizeLinkData>().ImageWidth == 640);
            Assert.IsTrue(_globalData.Get<ImagesSizeLinkData>().ImageHeight == 480);
        }

        [TestMethod]
        public void TestRawCalibrationImagesLink_Process()
        {
            PrepareConfiguration();

            _rawCalibLink = new RawCalibrationImagesLink(_globalData)
            {
                LoadDataFromDisc = false,
                StoreDataOnDisc = true
            };
            _rawCalibLink.Load();
            _rawCalibLink.Process();
            _rawCalibLink.Save();

            // Dont know what to test here, most important is that it doesn't throw and to inspect the output file
            Assert.IsTrue(_globalData.Get<RawCalibrationImagesLinkData>().Images.Count == 6); // Should be changed with changed input
            Assert.IsTrue(_globalData.Get<RawCalibrationImagesLinkData>().PointsLeft.Count == 350);
            Assert.IsTrue(_globalData.Get<RawCalibrationImagesLinkData>().PointsRight.Count == 392);
            Assert.IsTrue(_globalData.Get<RawCalibrationImagesLinkData>().LinesLeft.Count == 92);
            Assert.IsTrue(_globalData.Get<RawCalibrationImagesLinkData>().LinesRight.Count == 98);

            SaveOutput();
        }

        [TestMethod]
        public void TestRawCalibrationImagesLink_LoadFromDisc()
        {
            PrepareConfiguration();
            PrepareRawCalibrationData();

            Assert.IsTrue(_globalData.Get<RawCalibrationImagesLinkData>().Images.Count == 6); // Should be changed with changed input
            Assert.IsTrue(_globalData.Get<RawCalibrationImagesLinkData>().PointsLeft.Count == 350);
            Assert.IsTrue(_globalData.Get<RawCalibrationImagesLinkData>().PointsRight.Count == 392);
            Assert.IsTrue(_globalData.Get<RawCalibrationImagesLinkData>().LinesLeft.Count == 92);
            Assert.IsTrue(_globalData.Get<RawCalibrationImagesLinkData>().LinesRight.Count == 98);
        }

        [TestMethod]
        public void TestDistrotionModelLink_Process()
        {
            PrepareConfiguration();
            PrepareRawCalibrationData();

            _distortionLink = new DistortionModelLink(_globalData)
            {
                LoadDataFromDisc = false,
                StoreDataOnDisc = true
            };
            _distortionLink.Load();
            _distortionLink.Process();
            _distortionLink.Save();

            Assert.IsTrue(_globalData.Get<DistortionModelLinkData>().DistortionLeft != null);
            Assert.IsTrue(_globalData.Get<DistortionModelLinkData>().DistortionRight != null);

            SaveOutput();
        }

        [TestMethod]
        public void TestDistrotionModelLink_LoadFromDisc()
        {
            PrepareConfiguration();
            PrepareDistortionModelData();

            Assert.IsTrue(_globalData.Get<DistortionModelLinkData>().DistortionLeft != null);
            Assert.IsTrue(_globalData.Get<DistortionModelLinkData>().DistortionRight != null);
        }

        [TestMethod]
        public void TestUndistortPointsLink_Process()
        {
            PrepareConfiguration();
            PrepareRawCalibrationData();
            PrepareDistortionModelData();

            _undistortLink = new UndistortPointsLink(_globalData)
            {
                LoadDataFromDisc = false,
                StoreDataOnDisc = true
            };
            _undistortLink.Load();
            _undistortLink.Process();
            _undistortLink.Save();

            Assert.IsTrue(_globalData.Get<UndistortPointsLinkData>().PointsLeft.Count == 350);
            Assert.IsTrue(_globalData.Get<UndistortPointsLinkData>().PointsRight.Count == 392);

            SaveOutput();
        }

        [TestMethod]
        public void TestUndistortPointsLink_LoadFromDisc()
        {
            PrepareConfiguration();
            PrepareUndistortPointsData();

            Assert.IsTrue(_globalData.Get<UndistortPointsLinkData>().PointsLeft.Count == 350);
            Assert.IsTrue(_globalData.Get<UndistortPointsLinkData>().PointsRight.Count == 392);
        }

        [TestMethod]
        public void TestCalibrationLink_Process()
        {
            PrepareConfiguration();
            PrepareUndistortPointsData();

            _calibrationLink = new CalibrationLink(_globalData)
            {
                LoadDataFromDisc = false,
                StoreDataOnDisc = true
            };
            _calibrationLink.Load();
            _calibrationLink.Process();
            _calibrationLink.Save();

            Assert.IsTrue(_globalData.Get<CalibrationLinkData>().Grids != null);
            Assert.IsTrue(_globalData.Get<CalibrationLinkData>().Cameras != null);

            SaveOutput();
        }

        [TestMethod]
        public void TestCalibrationLink_LoadFromDisc()
        {
            PrepareConfiguration();
            PrepareCalibrationData();

            Assert.IsTrue(_globalData.Get<CalibrationLinkData>().Grids.Count == 6);
            Assert.IsTrue(_globalData.Get<CalibrationLinkData>().Cameras != null);
            Assert.IsTrue(_globalData.Get<CalibrationLinkData>().Cameras.AreCalibrated == true);
        }

        [TestMethod]
        public void TestRectificationLink_Process()
        {
            PrepareConfiguration();
            PrepareUndistortPointsData();
            PrepareCalibrationData();

            _rectificationLink = new RectificationLink(_globalData)
            {
                LoadDataFromDisc = false,
                StoreDataOnDisc = true
            };
            _rectificationLink.Load();
            _rectificationLink.Process();
            _rectificationLink.Save();

            Assert.IsTrue(_globalData.Get<RectificationLinkData>().Rectification.RectificationLeft != null);
            Assert.IsTrue(_globalData.Get<RectificationLinkData>().Rectification.RectificationRight != null);

            SaveOutput();
        }

        [TestMethod]
        public void TestRectificationLink_LoadFromDisc()
        {
            PrepareConfiguration();
            PrepareRectificationData();

            Assert.IsTrue(_globalData.Get<RectificationLinkData>().Rectification.RectificationLeft != null);
            Assert.IsTrue(_globalData.Get<RectificationLinkData>().Rectification.RectificationRight != null);
        }

        [TestMethod]
        public void TestRectiyCalibrationImagesLink_Process()
        {
            PrepareConfiguration();
            PrepareRawCalibrationData();
            PrepareDistortionModelData();
            PrepareRectificationData();

            UndistortCalibrationImagesLink undistortLink = new UndistortCalibrationImagesLink(_globalData)
            {
                LoadDataFromDisc = false,
                StoreDataOnDisc = true
            };
            undistortLink.Load();
            undistortLink.Process();
            undistortLink.Save();

            RectifyCalibrationImagesLink rectifyLink = new RectifyCalibrationImagesLink(_globalData)
            {
                LoadDataFromDisc = false,
                StoreDataOnDisc = true
            };
            rectifyLink.Load();
            rectifyLink.Process();
            rectifyLink.Save();

            Assert.IsTrue(_globalData.Get<RectifyCalibrationImagesLinkData>().Images.Count == 6);

            SaveOutput();
        }

        [TestMethod]
        public void TestMatchedImagesLink_Process()
        {
            PrepareConfiguration();
            PrepareDistortionModelData();
            PrepareRectificationData();

            _matchedImgsLink = new MatchedImagesLink(_globalData)
            {
                LoadDataFromDisc = false,
                StoreDataOnDisc = true
            };
            _matchedImgsLink.Load();
            _matchedImgsLink.Process();
            _matchedImgsLink.Save();
            
            Assert.IsTrue(_globalData.Get<MatchedImagesLinkData>().RectifiedImages != null);

            SaveOutput();
        }

        [TestMethod]
        public void TestMatchedImagesLink_LoadFromDisc()
        {
            PrepareConfiguration();
            PrepareMatchedImagesData();

            Assert.IsTrue(_globalData.Get<MatchedImagesLinkData>().RectifiedImages != null);
        }

        [TestMethod]
        public void TestImageMatchingLink_Process()
        {
            PrepareConfiguration();
            PrepareMatchedImagesData();

            _imageMatchingLink = new ImageMatchingLink(_globalData)
            {
                LoadDataFromDisc = false,
                StoreDataOnDisc = true
            };
            _imageMatchingLink.Load();
            _imageMatchingLink.Process();
            _imageMatchingLink.Save();

            Assert.IsTrue(_globalData.Get<ImageMatchingLinkData>().MapsLeft != null);
            Assert.IsTrue(_globalData.Get<ImageMatchingLinkData>().MapsRight != null);

            SaveOutput();
        }

        [TestMethod]
        public void TestImageMatchingLink_LoadFromDisc()
        {
            PrepareConfiguration();
            PrepareImageMatchingData();

            Assert.IsTrue(_globalData.Get<ImageMatchingLinkData>().MapsLeft != null);
            Assert.IsTrue(_globalData.Get<ImageMatchingLinkData>().MapsRight != null);
        }

        [TestMethod]
        public void TestDisparityRefinementLink_Process()
        {
            PrepareConfiguration();
            PrepareMatchedImagesData();
            PrepareImageMatchingData();

            _refinementLink = new DisparityRefinementLink(_globalData)
            {
                LoadDataFromDisc = false,
                StoreDataOnDisc = true
            };
            _refinementLink.Load();
            _refinementLink.Process();
            _refinementLink.Save();

            Assert.IsTrue(_globalData.Get<DisparityRefinementLinkData>().Maps != null);

            SaveOutput();
        }

        [TestMethod]
        public void TestDisparityRefinementLink_LoadFromDisc()
        {
            PrepareConfiguration();
            PrepareDisprityRefinementData();

            Assert.IsTrue(_globalData.Get<DisparityRefinementLinkData>().Maps != null);
        }
    }
}
