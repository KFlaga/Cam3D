using CamAlgorithms;
using CamCore;
using System.Collections.Generic;

namespace CamMain.ProcessingChain
{
    public class UndistortCalibrationImagesLinkData
    {
        public Dictionary<int, ImagesPair> Images { get; } = new Dictionary<int, ImagesPair>();
    }

    public class UndistortCalibrationImagesLink : ILink
    {
        public LinkType LinkType
        {
            get
            {
                return LinkType.CalibrationImagesUndistortion;
            }
        }

        bool _storedDataOnDisc = true;
        public bool StoreDataOnDisc
        {
            get { return _storedDataOnDisc; }
            set { _storedDataOnDisc = value; }
        }

        bool _loadDataFromDisc = false;
        public bool LoadDataFromDisc
        {
            get { return _loadDataFromDisc; }
            set { _loadDataFromDisc = value; }
        }

        private GlobalData _globalData;
        private RawCalibrationImagesLinkData _rawCalibData;
        private DistortionModelLinkData _distortion;
        private ConfigurationLinkData _config;
        private ImagesSizeLinkData _imgSize;
        private UndistortCalibrationImagesLinkData _linkData;

        public UndistortCalibrationImagesLink(GlobalData gData)
        {
            _globalData = gData;
            _linkData = new UndistortCalibrationImagesLinkData();
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
                _rawCalibData = _globalData.Get<RawCalibrationImagesLinkData>();
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
            if(_storedDataOnDisc)
            {
                SaveUndistortedImages();
            }

            _globalData.Set(_linkData);
        }


        private void UndistortImages()
        {
            LinkUtilities.UndistortImages(_rawCalibData.Images,
                _linkData.Images, _distortion);
        }

        private void SaveUndistortedImages()
        {
            LinkUtilities.SaveImages(_linkData.Images, _config,
                "CalibrationImages_Undistorted", "image_calibration_undistorted");
        }

        private void LoadUndistortedImages()
        {
            LinkUtilities.LoadImages<MaskedImage>(_linkData.Images, _config,
                "CalibrationImages_Undistorted");
        }
    }
}


