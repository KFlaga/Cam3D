using System.Collections.Generic;
using CamCore;
using System.Xml;
using CamAlgorithms.Calibration;

namespace CamAutomatization
{
    public class UndistortPointsLinkData
    {
        public List<CalibrationPoint> PointsLeft { get; set; } = new List<CalibrationPoint>();
        public List<CalibrationPoint> PointsRight { get; set; } = new List<CalibrationPoint>();
        
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
    }

    public class UndistortPointsLink : ILink
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
        private RawCalibrationImagesLinkData _rawCalibData;
        private DistortionModelLinkData _distortionData;
        private ConfigurationLinkData _config;
        private UndistortPointsLinkData _linkData;

        public UndistortPointsLink(GlobalData gData)
        {
            _globalData = gData;
            _linkData = new UndistortPointsLinkData();
        }

        public void Load()
        {
            _config = _globalData.Get<ConfigurationLinkData>();
            
            if(LoadDataFromDisc)
            {
                LoadPoints();
            }
            else
            {
                _rawCalibData = _globalData.Get<RawCalibrationImagesLinkData>();
                _distortionData = _globalData.Get<DistortionModelLinkData>();
            }
        }

        public void Process()
        {
            if(false == LoadDataFromDisc)
            {
                UndistortPoints();
            }
        }

        public void Save()
        {
            if(_storedDataOnDisc)
            {
                SavePoints();
            }

            _globalData.Set(_linkData);
        }
        
        private void UndistortPoints()
        {
            UndistortPoints(SideIndex.Left);
            UndistortPoints(SideIndex.Right);
        }

        private void UndistortPoints(SideIndex idx)
        {
            RadialDistortion distortion = _distortionData.GetDistortion(idx);
            foreach(var rawPoint in _rawCalibData.GetCalibrationPoints(idx))
            {
                distortion.Model.P = rawPoint.Img * distortion.Model.ImageScale;
                distortion.Model.Undistort();
                CalibrationPoint undistortedPoint = rawPoint.Clone();
                undistortedPoint.Img = distortion.Model.Pf / distortion.Model.ImageScale;
                _linkData.AddCalibrationPoint(idx, undistortedPoint);
            }
        }

        private void SavePoints()
        {
            //< CalibrationPoints_Undistorted >
            //  < PointsLeft path = "" />
            //  < PointsRight path = "" />
            //</ CalibrationPoints_Undistorted >

            XmlNode oldNode = _config.RootNode.FirstChildWithName("CalibrationPoints_Undistorted");
            bool oldNodeExists = null != oldNode;

            XmlNode calibPointsNode = _config.ConfigDoc.CreateElement("CalibrationPoints_Undistorted");
            XmlNode nodePointsLeft = _config.ConfigDoc.CreateElement("PointsLeft");
            XmlAttribute attPointsLeftPath = _config.ConfigDoc.CreateAttribute("path");
            XmlNode nodePointsRight = _config.ConfigDoc.CreateElement("PointsRight");
            XmlAttribute attPointsRightPath = _config.ConfigDoc.CreateAttribute("path");

            string pathPointsLeft = _config.WorkingDirectory + "calib_points_undistorted_left.xml";
            attPointsLeftPath.Value = "calib_points_undistorted_left.xml";
            nodePointsLeft.Attributes.Append(attPointsLeftPath);

            string pathPointsRight = _config.WorkingDirectory + "calib_points_undistorted_right.xml";
            attPointsRightPath.Value = "calib_points_undistorted_right.xml";
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

        private void LoadPoints()
        {
            //< CalibrationPoints_Undistorted >
            //  < PointsLeft path = "" />
            //  < PointsRight path = "" />
            //</ CalibrationPoints_Undistorted >

            XmlNode pointFileListNode = _config.RootNode.FirstChildWithName("CalibrationPoints_Undistorted");

            XmlNode leftFileNode = pointFileListNode.FirstChildWithName("PointsLeft");
            string leftFilePath = _config.WorkingDirectory + leftFileNode.Attributes["path"].Value;

            XmlNode rightFileNode = pointFileListNode.FirstChildWithName("PointsRight");
            string rightFilePath = _config.WorkingDirectory + rightFileNode.Attributes["path"].Value;

            _linkData.PointsLeft = CamCore.XmlSerialisation.CreateFromFile<List<CalibrationPoint>>(leftFilePath);
            _linkData.PointsRight = CamCore.XmlSerialisation.CreateFromFile<List<CalibrationPoint>>(rightFilePath);
        }
    }
}


