using CamCore;
using System.Collections.Generic;

namespace CamAutomatization
{
    public class UndistortedImagesOutputData
    {
        public string ImageSetName { get; set; }
        public Dictionary<int, ImagesPair> Images { get; } = new Dictionary<int, ImagesPair>();
    }
    
    public class UndistortedImagesInputData
    {
        public string ImageSetName { get; set; }
        public Dictionary<int, ImagesPair> Images { get; } = new Dictionary<int, ImagesPair>();
    }

    public class UndistortImagesLink : ILink
    {
        public bool StoreDataOnDisc { get; set; } = true;
        public bool LoadDataFromDisc { get; set; } = false;

        private GlobalData _globalData;
        private RawCalibrationImagesLinkData _inputImageSet;
        private DistortionModelLinkData _distortion;
        private ConfigurationLinkData _config;
        private ImagesSizeLinkData _imgSize;
        private UndistortedImagesOutputData _outputImageSet;

        public UndistortImagesLink(GlobalData gData)
        {
            _globalData = gData;
            _outputImageSet = new UndistortedImagesOutputData();
        }

        public void Load()
        {
            _config = _globalData.Get<ConfigurationLinkData>();

            if(LoadDataFromDisc)
            {
                LoadUndistortedImages();
            }
            else
            {
                _inputImageSet = _globalData.Get<RawCalibrationImagesLinkData>();
                _imgSize = _globalData.Get<ImagesSizeLinkData>();
                _distortion = _globalData.Get<DistortionModelLinkData>();
            }
        }

        public void Process()
        {
            UndistortImages();
        }

        public void Save()
        {
            if(StoreDataOnDisc)
            {
                SaveUndistortedImages();
            }

            _globalData.Set(_outputImageSet);
        }


        private void UndistortImages()
        {
            LinkUtilities.UndistortImages(_inputImageSet.Images,
                _outputImageSet.Images, _distortion);
        }

        private void SaveUndistortedImages()
        {
            LinkUtilities.SaveImages(_outputImageSet.Images, _config,
                "CalibrationImages_Undistorted", "image_calibration_undistorted");
        }

        private void LoadUndistortedImages()
        {
            LinkUtilities.LoadImages<MaskedImage>(_outputImageSet.Images, _config,
                "CalibrationImages_Undistorted");
        }
    }
}


