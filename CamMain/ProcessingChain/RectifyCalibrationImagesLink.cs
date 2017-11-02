using CamAlgorithms;
using CamCore;
using System.Collections.Generic;

namespace CamMain.ProcessingChain
{
    public class RectifyCalibrationImagesLinkData
    {
        public Dictionary<int, ImagesPair> Images { get; } = new Dictionary<int, ImagesPair>();
    }

    public class RectifyCalibrationImagesLink : ILink
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
        private ConfigurationLinkData _config;
        private UndistortCalibrationImagesLinkData _images;
        private RectificationLinkData _rectification;
        private RectifyCalibrationImagesLinkData _linkData;

        public RectifyCalibrationImagesLink(GlobalData gData)
        {
            _globalData = gData;
            _linkData = new RectifyCalibrationImagesLinkData();
        }

        public void Load()
        {
            _config = _globalData.Get<ConfigurationLinkData>();

            if(LoadDataFromDisc)
            {
                LoadRectifiedImages();
            }
            else
            {
                _images = _globalData.Get<UndistortCalibrationImagesLinkData>();
                _rectification = _globalData.Get<RectificationLinkData>();
            }
        }

        public void Process()
        {
            RectifyImages();
        }

        public void Save()
        {
            if(_storedDataOnDisc)
            {
                SaveRectifiedImages();
            }

            _globalData.Set(_linkData);
        }
        
        private void RectifyImages()
        {
            LinkUtilities.RectifyImages(_images.Images,
                _linkData.Images, _rectification);
        }

        private void SaveRectifiedImages()
        {
            LinkUtilities.SaveImages(_linkData.Images, _config,
                "CalibrationImages_Rectified", "image_calibration_rectified");
        }

        private void LoadRectifiedImages()
        {
            LinkUtilities.LoadImages<MaskedImage>(_linkData.Images, _config,
                "CalibrationImages_Rectified");
        }
    }
}


