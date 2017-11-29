using CamCore;
using CamAlgorithms.ImageMatching;
using System.Collections.Generic;
using System.Xml;

namespace CamAutomatization
{
    public class ImageMatchingLinkData
    {
        public Dictionary<int, DisparityMap> MapsLeft { get; set; }
        public Dictionary<int, DisparityMap> MapsRight { get; set; }

        public DisparityMap GetMap(int id, SideIndex idx)
        {
            DisparityMap map;
            bool res;
            if(idx == SideIndex.Left)
                res = MapsLeft.TryGetValue(id, out map);
            else
                res = MapsRight.TryGetValue(id, out map);

            if(res)
            {
                return map;
            }
            else
            {
                throw new KeyNotFoundException("No raw disparity map for key: " + id.ToString());
            }
        }
    }

    public class ImageMatchingLink : ILink
    {
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
        private MatchedImagesLinkData _matchedImages;
        private ImageMatchingLinkData _linkData;

        GenericImageMatchingAlgorithm _matcher;

        public ImageMatchingLink(GlobalData gData)
        {
            _globalData = gData;
            _linkData = new ImageMatchingLinkData();
        }

        public void Load()
        {
            _config = _globalData.Get<ConfigurationLinkData>();

            if(LoadDataFromDisc)
            {
               LoadDisparityMaps();
            }
            else
            {
                _matchedImages = _globalData.Get<MatchedImagesLinkData>();
                LoadMatcherParameters();
            }
        }

        public void Process()
        {
            if(false == LoadDataFromDisc)
            {
                MatchImages();
            }
        }

        public void Save()
        {
            if(_storedDataOnDisc)
            {
                SaveRawDisparityMaps();
            }

            _globalData.Set(_linkData);
        }

        void LoadMatcherParameters()
        {
            _matcher = new GenericImageMatchingAlgorithm();

            XmlNode sgmMatcherNode = _config.RootNode.FirstChildWithName("ImageMatcher");
            if(sgmMatcherNode != null)
            {
                LoadMatcherParameters(sgmMatcherNode);
            }
            else
            {
                SetDefaultMatcherParameters();
            }
        }

        void LoadMatcherParameters(XmlNode sgmMatcherNode)
        {
            // <SGMMatcher>
            //     <Parameters>
            //         ...
            //         <Parameter desc="Disparity Computer" id="DISP_COMP" value="SGM Disparity Computer">
            //             <Parameters>
            //              ...

            SGMAggregator sgmAgg = new SGMAggregator();
            sgmAgg.InitParameters();

            XmlNode sgmParamsNode = sgmMatcherNode.FirstChildWithName("Parameters");
            IAlgorithmParameter.ReadParametersFromXml(sgmAgg.Parameters, sgmParamsNode);

            sgmAgg.UpdateParameters();

            _matcher.Aggregator = sgmAgg;
        }

        void SetDefaultMatcherParameters()
        {
            _matcher = new GenericImageMatchingAlgorithm();

            SGMAggregator sgmAgg = new SGMAggregator();
            sgmAgg.InitParameters();
            sgmAgg.UpdateParameters();

            _matcher.Aggregator = sgmAgg;
        }

        void MatchImages()
        {
            _linkData.MapsLeft = new Dictionary<int, DisparityMap>();
            _linkData.MapsRight = new Dictionary<int, DisparityMap>();
            foreach(var entry in _matchedImages.RectifiedImages)
            {
                ImagesPair imgPar = entry.Value;
                _matcher.ImageLeft = imgPar.Left;
                _matcher.ImageRight = imgPar.Right;
                _matcher.Rectified = true;
                _matcher.MatchImages();

                _linkData.MapsLeft.Add(entry.Key, _matcher.MapLeft);
                _linkData.MapsRight.Add(entry.Key, _matcher.MapRight);
            }
        }

        void SaveRawDisparityMaps()
        {
            LinkUtilities.SaveDisparityMaps(_linkData.MapsLeft, _config, "DisparityMaps_Raw_Left", "map_raw_left");
            LinkUtilities.SaveDisparityMaps(_linkData.MapsRight, _config, "DisparityMaps_Raw_Right", "map_raw_right");
        }

        void LoadDisparityMaps()
        {
            _linkData.MapsLeft = new Dictionary<int, DisparityMap>();
            _linkData.MapsRight = new Dictionary<int, DisparityMap>();
            LinkUtilities.LoadDisparityMaps(_linkData.MapsLeft, _config, "DisparityMaps_Raw_Left");
            LinkUtilities.LoadDisparityMaps(_linkData.MapsRight, _config, "DisparityMaps_Raw_Right");
        }
    }
}
