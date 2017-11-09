using CamAlgorithms;
using System.Collections.Generic;
using CamCore;
using CamAlgorithms.Calibration;

namespace CamAutomatization
{
    public class MatchedImagesLinkData
    {
        public Dictionary<int, ImagesPair> RawImages { get; } = new Dictionary<int, ImagesPair>();
        public Dictionary<int, ImagesPair> UndistortedImages { get; } = new Dictionary<int, ImagesPair>();
        public Dictionary<int, ImagesPair> RectifiedImages { get; } = new Dictionary<int, ImagesPair>();
    }

    public class MatchedImagesLink : ILink
    {
        public LinkType LinkType
        {
            get
            {
                return LinkType.RawCalibrationImagesExtraction;
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
        private DistortionModelLinkData _distortion;
        private RectificationLinkData _rectification;
        private MatchedImagesLinkData _linkData;
        private ConfigurationLinkData _config;
        private ImagesSizeLinkData _imgSize;

        public MatchedImagesLink(GlobalData gData)
        {
            _globalData = gData;
            _linkData = new MatchedImagesLinkData();
        }

        public void Load()
        {
            _config = _globalData.Get<ConfigurationLinkData>();
            
            if(LoadDataFromDisc)
            {
                LoadRectifiedMatchedImagesFromDisc();
            }
            else
            {
                _imgSize = _globalData.Get<ImagesSizeLinkData>();
                _distortion = _globalData.Get<DistortionModelLinkData>();
                _rectification = _globalData.Get<RectificationLinkData>();

                LoadMatchedImagesFromDisc();
                RemoveUnmatchedImages();
            }
        }

        public void Process()
        {
            UndistortImages();
            RectifyImages();
        }

        public void Save()
        {
            if(_storedDataOnDisc)
            {
                SaveUndistortedImages();
                SaveRectifiedImages();
            }

            _globalData.Set(_linkData);
        }

        private void LoadMatchedImagesFromDisc()
        {
            LinkUtilities.LoadImages<ColorImage>(_linkData.RawImages, _config, "MatchedImages");
        }

        private void RemoveUnmatchedImages()
        {
            // Removes all images for which there is no [left,right] matching ids
            _linkData.RawImages.RemoveAll((pair) => { return (pair.Left == null || pair.Right == null); });
        }

        private void UndistortImages()
        {
            LinkUtilities.UndistortImages(_linkData.RawImages, 
                _linkData.UndistortedImages, _distortion);
        }

        private IImage UndistortImage(IImage imgRaw, RadialDistortionModel distortion, ImageTransformer undistort)
        {
            undistort.Transformation =
                new RadialDistortionTransformation(distortion);

            MaskedImage img = new MaskedImage(imgRaw);
            MaskedImage imgFinal = undistort.TransfromImageBackwards(img, true);
            return imgFinal;
        }

        private void SaveUndistortedImages()
        {
            LinkUtilities.SaveImages(_linkData.UndistortedImages, _config, 
                "MatchedImages_Undistorted", "image_matched_undistorted");
        }

        private void RectifyImages()
        {
            LinkUtilities.RectifyImages(_linkData.UndistortedImages, 
                _linkData.RectifiedImages, _rectification);
        }

        private void SaveRectifiedImages()
        {
            LinkUtilities.SaveImages(_linkData.RectifiedImages, _config, 
                "MatchedImages_Rectified", "image_matched_rectified");
        }

        private void LoadRectifiedMatchedImagesFromDisc()
        {
            LinkUtilities.LoadImages<ColorImage>(_linkData.RectifiedImages, _config, "MatchedImages_Rectified");
        }
    }
}
