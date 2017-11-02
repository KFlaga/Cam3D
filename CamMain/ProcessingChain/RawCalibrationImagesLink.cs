using CamAlgorithms;
using System;
using System.Collections.Generic;
using CamCore;
using System.Xml;

namespace CamMain.ProcessingChain
{
    public class RawCalibrationImagesLinkData
    {
        public Dictionary<int, ImagesPair> Images { get; set; } = new Dictionary<int, ImagesPair>();
        public List<CalibrationPoint> PointsLeft { get; set; } = new List<CalibrationPoint>();
        public List<CalibrationPoint> PointsRight { get; set; } = new List<CalibrationPoint>();
        public List<List<Vector2>> LinesLeft { get; set; } = new List<List<Vector2>>();
        public List<List<Vector2>> LinesRight { get; set; } = new List<List<Vector2>>();

        public List<CalibrationPoint> GetCalibrationPoints(SideIndex idx)
        {
            return idx == SideIndex.Left ? PointsLeft : PointsRight;
        }

        public void AddCalibrationPoint(SideIndex idx, CalibrationPoint cpoint)
        {
            GetCalibrationPoints(idx).Add(cpoint);
        }

        public void AddCalibrationPoints(SideIndex idx, List<CalibrationPoint> cpoints)
        {
            GetCalibrationPoints(idx).AddRange(cpoints);
        }

        public List<List<Vector2>> GetCalibrationLines(SideIndex idx)
        {
            return idx == SideIndex.Left ? LinesLeft : LinesRight;
        }

        public void AddCalibrationLine(SideIndex idx, List<Vector2> line)
        {
            GetCalibrationLines(idx).Add(line);
        }

        public void AddCalibrationLines(SideIndex idx, List<List<Vector2>> lines)
        {
            GetCalibrationLines(idx).AddRange(lines);
        }
    }

    public class RawCalibrationImagesLink : ILink
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
        private RawCalibrationImagesLinkData _linkData;
        private ConfigurationLinkData _config;

        private CalibrationModule.PointsExtraction.CalibrationPointsFinder _calibPointsFinder;
        private CalibrationModule.PointsExtraction.ICalibrationLinesExtractor _calibLinesExtractor;

        public RawCalibrationImagesLink(GlobalData gData)
        {
            _globalData = gData;
            _linkData = new RawCalibrationImagesLinkData();
        }

        public void Load()
        {
            _config = _globalData.Get<ConfigurationLinkData>();

            LoadCalibrationImagesFromDisc();

            if(LoadDataFromDisc)
            {
                LoadCalibrationPoints();
                LoadCalibrationLines();
            }
            else
            {
                InitCalibPointsExtractor();
            }
        }

        public void Process()
        {
            if(false == LoadDataFromDisc)
            {
                ExtractCalibPoints();
            }
        }

        public void Save()
        {
            if(_storedDataOnDisc)
            {
                SaveCalibrationPoints();
                SaveCalibrationLines();
            }

            _globalData.Set(_linkData);
        }

        private void LoadCalibrationImagesFromDisc()
        {
            LinkUtilities.LoadImages<ColorImage>(_linkData.Images, _config, "CalibrationImages_Raw");
        }

        private void ExtractCalibPoints()
        {
            foreach(var entry in _linkData.Images)
            {
                ImagesPair imgPair = entry.Value;
                if(imgPair.Left != null)
                {
                    ExtractCalibPoints(imgPair.Left, entry.Key, SideIndex.Left);
                }

                if(imgPair.Right != null)
                {
                    ExtractCalibPoints(imgPair.Right, entry.Key, SideIndex.Right);
                }
            }
        }

        private void ExtractCalibPoints(IImage image, int id_grid, SideIndex idx)
        {
            _calibPointsFinder.Image = image;
            _calibPointsFinder.FindCalibrationPoints();
            _calibLinesExtractor.ExtractLines();

            _linkData.AddCalibrationPoints(idx, _calibPointsFinder.Points);
            _linkData.AddCalibrationLines(idx, _calibLinesExtractor.CalibrationLines);

            foreach(var p in _calibPointsFinder.Points)
            {
                p.GridNum = id_grid;
            }
        }

        private void InitCalibPointsExtractor()
        {
            // <PointsExtractor type="CalibShape">
            //   <Parameters>....
            XmlNode extractorNode = _config.RootNode.FirstChildWithName("PointsExtractor");

            if(extractorNode == null)
            {
                InitDefaultCalibPointsExtractor();
            }
            else
            {
                InitCalibPointsExtractorFromXml(extractorNode);
            }
        }

        private void InitDefaultCalibPointsExtractor()
        {

            _calibPointsFinder = new CalibrationModule.PointsExtraction.ShapesGridCPFinder();
            _calibLinesExtractor = _calibPointsFinder.LinesExtractor;
            _calibPointsFinder.InitParameters();
            _calibPointsFinder.UpdateParameters();
        }

        private void InitCalibPointsExtractorFromXml(XmlNode extractorNode)
        {
            // Get type of extractor
            string extractorType = extractorNode.Attributes["type"].Value;
            if(extractorType == "CalibShape") { }
            _calibPointsFinder = new CalibrationModule.PointsExtraction.ShapesGridCPFinder();
            _calibLinesExtractor = _calibPointsFinder.LinesExtractor;
            _calibPointsFinder.InitParameters();

            XmlNode paramsNode = extractorNode.FirstChildWithName("Parameters");
            if(paramsNode != null)
            {
                AlgorithmParameter.ReadParametersFromXml(_calibPointsFinder.Parameters, paramsNode);
            }

            _calibPointsFinder.UpdateParameters();
        }

        private void SaveCalibrationPoints()
        {
            //< CalibrationPoints_Raw >
            //  < PointsLeft path = "" />
            //  < PointsRight path = "" />
            //</ CalibrationPoints_Raw >

            // 1) Check if there is already a node with same name      
            XmlNode oldNode = _config.RootNode.FirstChildWithName("CalibrationPoints_Raw");
            bool oldNodeExists = null != oldNode;

            XmlNode calibPointsNode = _config.ConfigDoc.CreateElement("CalibrationPoints_Raw");
            XmlNode nodePointsLeft = _config.ConfigDoc.CreateElement("PointsLeft");
            XmlAttribute attPointsLeftPath = _config.ConfigDoc.CreateAttribute("path");
            XmlNode nodePointsRight = _config.ConfigDoc.CreateElement("PointsRight");
            XmlAttribute attPointsRightPath = _config.ConfigDoc.CreateAttribute("path");

            string pathPointsLeft = _config.WorkingDirectory + "calib_points_raw_left.xml";
            attPointsLeftPath.Value = "calib_points_raw_left.xml";
            nodePointsLeft.Attributes.Append(attPointsLeftPath);

            string pathPointsRight = _config.WorkingDirectory + "calib_points_raw_right.xml";
            attPointsRightPath.Value = "calib_points_raw_right.xml";
            nodePointsRight.Attributes.Append(attPointsRightPath);

            calibPointsNode.AppendChild(nodePointsLeft);
            calibPointsNode.AppendChild(nodePointsRight);

            if(oldNodeExists)
            {
                _config.RootNode.ReplaceChild(calibPointsNode, oldNode);
            }
            else
            {
                _config.RootNode.AppendChild(calibPointsNode);
            }

            CamCore.XmlSerialisation.SaveToFile(_linkData.PointsLeft, pathPointsLeft);
            CamCore.XmlSerialisation.SaveToFile(_linkData.PointsRight, pathPointsRight);
        }

        private void SaveCalibrationLines()
        {
            //< !--Output-- >
            //< CalibrationLines_Raw >
            //  < LinesLeft path = "" />
            //  < LinesRight path = "" />
            //</ CalibrationLines_Raw >

            // 1) Check if there is already a node with same name      
            XmlNode oldNode = _config.RootNode.FirstChildWithName("CalibrationLines_Raw");
            bool oldNodeExists = null != oldNode;

            XmlNode calibLinesNode = _config.ConfigDoc.CreateElement("CalibrationLines_Raw");
            XmlNode nodeLinesLeft = _config.ConfigDoc.CreateElement("LinesLeft");
            XmlAttribute attLeftPath = _config.ConfigDoc.CreateAttribute("path");
            XmlNode nodeLinesRight = _config.ConfigDoc.CreateElement("LinesRight");
            XmlAttribute attRightPath = _config.ConfigDoc.CreateAttribute("path");

            string pathPointsLeft = _config.WorkingDirectory + "calib_lines_raw_left.xml";
            attLeftPath.Value = "calib_lines_raw_left.xml";
            nodeLinesLeft.Attributes.Append(attLeftPath);

            string pathPointsRight = _config.WorkingDirectory + "calib_lines_raw_right.xml";
            attRightPath.Value = "calib_lines_raw_right.xml";
            nodeLinesRight.Attributes.Append(attRightPath);

            calibLinesNode.AppendChild(nodeLinesLeft);
            calibLinesNode.AppendChild(nodeLinesRight);

            if(oldNodeExists)
            {
                _config.RootNode.ReplaceChild(calibLinesNode, oldNode);
            }
            else
            {
                _config.RootNode.AppendChild(calibLinesNode);
            }

            CamCore.XmlSerialisation.SaveToFile(_linkData.LinesLeft, pathPointsLeft);
            CamCore.XmlSerialisation.SaveToFile(_linkData.LinesRight, pathPointsRight);
        }

        private void LoadCalibrationPoints()
        {
            //< CalibrationPoints_Raw >
            //  < PointsLeft path = "" />
            //  < PointsRight path = "" />
            //</ CalibrationPoints_Raw >

            XmlNode pointFileListNode = _config.RootNode.FirstChildWithName("CalibrationPoints_Raw");

            XmlNode leftFileNode = pointFileListNode.FirstChildWithName("PointsLeft");
            string leftFilePath = _config.WorkingDirectory + leftFileNode.Attributes["path"].Value;

            XmlNode rightFileNode = pointFileListNode.FirstChildWithName("PointsRight");
            string rightFilePath = _config.WorkingDirectory + rightFileNode.Attributes["path"].Value;

            _linkData.PointsLeft = CamCore.XmlSerialisation.CreateFromFile<List<CalibrationPoint>>(leftFilePath);
            _linkData.PointsRight = CamCore.XmlSerialisation.CreateFromFile<List<CalibrationPoint>>(rightFilePath);
        }

        private void LoadCalibrationLines()
        {
            //< CalibrationLines_Raw >
            //  < LinesLeft path = "" />
            //  < LinesRight path = "" />
            //</ CalibrationLines_Raw >

            XmlNode pointFileListNode = _config.RootNode.FirstChildWithName("CalibrationLines_Raw");

            XmlNode leftFileNode = pointFileListNode.FirstChildWithName("LinesLeft");
            string leftFilePath = _config.WorkingDirectory + leftFileNode.Attributes["path"].Value;

            XmlNode rightFileNode = pointFileListNode.FirstChildWithName("LinesRight");
            string rightFilePath = _config.WorkingDirectory + rightFileNode.Attributes["path"].Value;

            _linkData.LinesLeft = CamCore.XmlSerialisation.CreateFromFile<List<List<Vector2>>>(leftFilePath);
            _linkData.LinesRight = CamCore.XmlSerialisation.CreateFromFile<List<List<Vector2>>>(rightFilePath);
        }
    }
}
