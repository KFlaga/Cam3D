using CamCore;
using CamImageProcessing;
using CamImageProcessing.ImageMatching;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace CamMain.ProcessingChain
{
    public class DisparityRefinementLinkData
    {
        public Dictionary<int, DisparityMap> Maps { get; set; }
    }

    public class DisparityRefinementLink : ILink
    {
        public LinkType LinkType
        {
            get
            {
                return LinkType.DisparityRefinement;
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
        private ImagesSizeLinkData _imgSize;
        private MatchedImagesLinkData _matchedImages;
        private ImageMatchingLinkData _disparityRaw;
        private DisparityRefinementLinkData _linkData;

        List<DisparityRefinement> _refinementChain;

        public DisparityRefinementLink(GlobalData gData)
        {
            _globalData = gData;
            _linkData = new DisparityRefinementLinkData();
        }

        public void Load()
        {
            _config = _globalData.Get<ConfigurationLinkData>();

            if(LoadDataFromDisc)
            {
                LoadRefinedDisparityMaps();
            }
            else
            {
                _imgSize = _globalData.Get<ImagesSizeLinkData>();
                _matchedImages = _globalData.Get<MatchedImagesLinkData>();
                _disparityRaw = _globalData.Get<ImageMatchingLinkData>();

                LoadDisparityRefinementChain();
            }
        }

        public void Process()
        {
            if(false == LoadDataFromDisc)
            {
                RefineDisparities();
            }
        }

        public void Save()
        {
            if(_storedDataOnDisc)
            {
                SaveRefinedDisparityMaps();
            }

            _globalData.Set(_linkData);
        }

        void LoadDisparityRefinementChain()
        {
            _refinementChain = new List<DisparityRefinement>();

            // <DisparityRefinement>
            //  <Refiner name="">
            //    <Parameter desc="" id="" value=""/>
            XmlNode disparityRefinementNode = _config.RootNode.FirstChildWithName("DisparityRefinement");
            Assembly refinerAssembly = typeof(DisparityRefinement).Assembly;

            XmlNode refinerNode = disparityRefinementNode.FirstChildWithName("Refiner");
            while(refinerNode != null)
            {
                try
                {
                    string className = refinerNode.Attributes["name"].Value;
                    string fullClassName = "CamImageProcessing.ImageMatching." + className;

                    Type refinerType = refinerAssembly.GetType(fullClassName);
                    DisparityRefinement refiner = Activator.CreateInstance(refinerType) as DisparityRefinement;

                    refiner.InitParameters();
                    AlgorithmParameter.ReadParametersFromXml(refiner.Parameters, refinerNode);
                    refiner.UpdateParameters();

                    _refinementChain.Add(refiner);

                    refinerNode = refinerNode.NextSibling;
                }
                catch(Exception e)
                {
                    MessageBox.Show("Failed to load Refiner : " + e.Message +
                        Environment.NewLine + ". Stack: " +
                        Environment.NewLine + e.StackTrace);
                }
            }
        }

        void RefineDisparities()
        {
            _linkData.Maps = new Dictionary<int, DisparityMap>();
            foreach(var id in _disparityRaw.MapsLeft.Keys)
            {
                // Apply each refiner
                DisparityMap mapLeft = _disparityRaw.MapsLeft[id];
                DisparityMap mapRight = _disparityRaw.MapsRight[id];
                IImage imageLeft = _matchedImages.RectifiedImages[id].Left;
                IImage imageRight = _matchedImages.RectifiedImages[id].Right;

                foreach(var refiner in _refinementChain)
                {
                    refiner.MapLeft = mapLeft;
                    refiner.MapRight = mapRight;
                    refiner.ImageLeft = imageLeft;
                    refiner.ImageRight = imageRight;

                    refiner.RefineMaps();

                    mapLeft = refiner.MapLeft;
                    mapRight = refiner.MapRight;
                }

                _linkData.Maps.Add(id, _refinementChain[_refinementChain.Count - 1].MapLeft);
            }
        }

        void SaveRefinedDisparityMaps()
        {
            LinkUtilities.SaveDisparityMaps(_linkData.Maps, _config, "DisparityMaps_Refined", "map_refined_left");
        }

        void LoadRefinedDisparityMaps()
        {
            _linkData.Maps = new Dictionary<int, DisparityMap>();
            LinkUtilities.LoadDisparityMaps(_linkData.Maps, _config, "DisparityMaps_Refined");
        }
    }
}
