using CamCore;
using System.Xml;

namespace CamAutomatization
{
    public class ConfigurationLinkData
    {
        public XmlDocument ConfigDoc { get; set; }
        public XmlNode RootNode { get; set; }
        public string WorkingDirectory { get; set; }
    }

    public class ImagesSizeLinkData
    {
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
    }

    public class ConfigurationLink : ILink
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

        GlobalData _globalData;
        ConfigurationLinkData _config;
        ImagesSizeLinkData _imgSize;

        public ConfigurationLink(GlobalData gData, XmlDocument configDoc)
        {
            _globalData = gData;
            _config = new ConfigurationLinkData();
            _imgSize = new ImagesSizeLinkData();

            _config.ConfigDoc = configDoc;
        }

        public void Load()
        {
            _config.RootNode = _config.ConfigDoc.GetElementsByTagName("CalibrationInput")[0];
            ReadWorkingDirectory();
            ReadImagesSize();
        }

        public void Process()
        {
        }

        public void Save()
        {
            _globalData.Set(_config);
            _globalData.Set(_imgSize);
        }
        
        private void ReadWorkingDirectory()
        {
            // <WorkingDirectory path=""/>

            XmlNode pathNode = _config.RootNode.FirstChildWithName("WorkingDirectory");
            _config.WorkingDirectory = pathNode.Attributes["path"].Value;

            if(!(_config.WorkingDirectory.EndsWith("\\") || _config.WorkingDirectory.EndsWith("/")))
            {
                _config.WorkingDirectory = _config.WorkingDirectory + "\\";
            }
        }

        private void ReadImagesSize()
        {
            // < ImageSize width = "640" height = "480" />
            XmlNode sizeNode = _config.RootNode.FirstChildWithName("ImageSize");
            _imgSize.ImageHeight = int.Parse(sizeNode.Attributes["height"].Value);
            _imgSize.ImageWidth = int.Parse(sizeNode.Attributes["width"].Value);
        }

    }
}
