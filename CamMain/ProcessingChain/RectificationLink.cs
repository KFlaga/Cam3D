using CamCore;
using System.Collections.Generic;
using System.Xml;
using CamImageProcessing;

namespace CamMain.ProcessingChain
{
    public class RectificationLinkData
    {
        public ImageRectification Rectification { get; set; }
    }

    public class RectificationLink : ILink
    {
        public LinkType LinkType
        {
            get
            {
                return LinkType.RectificationComputation;
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
        private UndistortPointsLinkData _points;
        private CalibrationLinkData _calibration;
        private RectificationLinkData _linkData;

        private List<Vector2Pair> _matchedPoints;

        public RectificationLink(GlobalData gData)
        {
            _globalData = gData;
            _linkData = new RectificationLinkData();
        }

        public void Load()
        {
            _config = _globalData.Get<ConfigurationLinkData>();
            _imgSize = _globalData.Get<ImagesSizeLinkData>();
            
            if(LoadDataFromDisc)
            {
                LoadRectification();
            }
            else
            {
                _points = _globalData.Get<UndistortPointsLinkData>();
                _calibration = _globalData.Get<CalibrationLinkData>();
            }
        }

        public void Process()
        {
            if(false == LoadDataFromDisc)
            {
                FindMatchedCalibrationPoints();
                FindRectification();
            }
        }

        public void Save()
        {
            if(_storedDataOnDisc)
            {
                SaveRectification();
            }

            _globalData.Set(_linkData);
        }

        private void FindMatchedCalibrationPoints()
        {
            _matchedPoints = new List<Vector2Pair>();
            foreach(var pointLeft in _points.PointsLeft)
            {
                var pointRight = _points.PointsRight.Find((cp) =>
                {
                    return pointLeft.GridNum == cp.GridNum &&
                        pointLeft.RealGridPos == cp.RealGridPos;
                });

                if(pointRight != null)
                {
                    _matchedPoints.Add(new Vector2Pair()
                    {
                        V1 = pointLeft.Img,
                        V2 = pointRight.Img
                    });
                }
            }
        }

        private void FindRectification()
        {
            ImageRectification zhangLoop = FindRectification(new ImageRectification(new ImageRectification_ZhangLoop()));
            ImageRectification fussUncalib = FindRectification(new ImageRectification(new ImageRectification_FussieloUncalibrated()
            {
                UseInitialCalibration = false
            }));
            ImageRectification fussUncalibWithInitial = FindRectification(new ImageRectification(new ImageRectification_FussieloUncalibrated()
            {
                UseInitialCalibration = true
            }));

            // Return rectification with best quality
            if(zhangLoop.Quality > fussUncalib.Quality &&
                zhangLoop.Quality > fussUncalibWithInitial.Quality)
            {
                _linkData.Rectification = zhangLoop;
            }
            else if(fussUncalib.Quality > fussUncalibWithInitial.Quality)
            {
                _linkData.Rectification = fussUncalib;
            }
            else
            {
                _linkData.Rectification = fussUncalibWithInitial;
            }
        }

        private ImageRectification FindRectification(ImageRectification rectAlg)
        {
            rectAlg.ImageHeight = _imgSize.ImageHeight;
            rectAlg.ImageWidth = _imgSize.ImageWidth;
            rectAlg.CalibData = _calibration.Calibration;
            rectAlg.MatchedPairs = _matchedPoints;

            rectAlg.ComputeRectificationMatrices();
            return rectAlg;
        }

        private void SaveRectification()
        {
            XmlNode oldNode = _config.RootNode.FirstChildWithName("Rectification");
            bool oldNodeExists = null != oldNode;

            XmlNode rectificationNode = _config.ConfigDoc.CreateElement("Rectification");
            XmlAttribute attPath = _config.ConfigDoc.CreateAttribute("path");

            string path = _config.WorkingDirectory + "rectification.xml";
            attPath.Value = "rectification.xml";
            rectificationNode.Attributes.Append(attPath);

            if(oldNodeExists)
            {
                _config.RootNode.ReplaceChild(rectificationNode, oldNode);
            }
            else
            {
                _config.RootNode.AppendChild(rectificationNode);
            }

            CamCore.XmlSerialisation.SaveToFile(_linkData.Rectification, path);
        }

        private void LoadRectification()
        {
            XmlNode calibDataNode = _config.RootNode.FirstChildWithName("Rectification");
            string filePath = _config.WorkingDirectory + calibDataNode.Attributes["path"].Value;
            
            _linkData.Rectification = CamCore.XmlSerialisation.CreateFromFile<ImageRectification>(filePath);
        }
    }
}
