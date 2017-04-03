using CamCore;
using CamImageProcessing;
using CamImageProcessing.ImageMatching;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CamMain.ProcessingChain
{
    public class ImageMatchingLinkData
    {
        public Dictionary<int, DisparityMap> MapsLeft { get; set; }
        public Dictionary<int, DisparityMap> MapsRight { get; set; }

        public DisparityMap GetMap(int id, CameraIndex idx)
        {
            DisparityMap map;
            bool res;
            if(idx == CameraIndex.Left)
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
        public LinkType LinkType
        {
            get
            {
                return LinkType.ImageMatching;
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
        private MatchedImagesLinkData _matchedImages;
        private ImageMatchingLinkData _linkData;

        ImageMatchingAlgorithm _matcher;

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
            _matcher = new ImageMatchingAlgorithm();

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
            AlgorithmParameter.ReadParametersFromXml(sgmAgg.Parameters, sgmParamsNode);

            sgmAgg.UpdateParameters();

            _matcher.Aggregator = sgmAgg;
        }

        void SetDefaultMatcherParameters()
        {
            _matcher = new ImageMatchingAlgorithm();

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
            XmlNode dispMapListNode = _config.ConfigDoc.CreateElement("DisparityMaps_Raw");
            foreach(var entry in _linkData.MapsLeft)
            {
                SaveDisparityMap(entry.Value, dispMapListNode, entry.Key, CameraIndex.Left);
            }

            foreach(var entry in _linkData.MapsRight)
            {
                SaveDisparityMap(entry.Value, dispMapListNode, entry.Key, CameraIndex.Right);

            }
            _config.RootNode.AppendChild(dispMapListNode);
        }

        void SaveDisparityMap(DisparityMap map, XmlNode dispMapListNode, int id, CameraIndex idx)
        {
            XmlNode dispMapNode = _config.ConfigDoc.CreateElement("Map");
            XmlAttribute attId = _config.ConfigDoc.CreateAttribute("id");
            XmlAttribute attCam = _config.ConfigDoc.CreateAttribute("cam");
            XmlAttribute attPath = _config.ConfigDoc.CreateAttribute("path");

            attId.Value = id.ToString();
            attCam.Value = idx == CameraIndex.Left ? "left" : "right";
            attPath.Value = "dispmap_raw_" + attCam.Value + "_" + attId.Value + ".xml";
            string path = _config.WorkingDirectory + attPath.Value;

            dispMapNode.Attributes.Append(attId);
            dispMapNode.Attributes.Append(attCam);
            dispMapNode.Attributes.Append(attPath);

            dispMapListNode.AppendChild(dispMapNode);

            XmlDocument dispDoc = new XmlDocument();
            XmlNode mapNode = map.CreateMapNode(dispDoc);

            dispDoc.InsertAfter(mapNode, dispDoc.DocumentElement);

            using(FileStream file = new FileStream(path, FileMode.Create))
            {
                dispDoc.Save(file);
            }
        }
    }
}
