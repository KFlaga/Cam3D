using CamAlgorithms.Calibration;
using CamCore;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace CamAutomatization
{
    public class CalibrationLinkData
    {
        public CameraPair Cameras { get; set; }
        public List<RealGridData> Grids { get; set; }
    }

    public class CalibrationLink : ILink
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
        private ImagesSizeLinkData _imgSize;
        private UndistortPointsLinkData _points;
        private CalibrationLinkData _linkData;

        private CalibrationAlgorithm _calibrator;

        public CalibrationLink(GlobalData gData)
        {
            _globalData = gData;
            _linkData = new CalibrationLinkData();
        }

        public void Load()
        {
            _config = _globalData.Get<ConfigurationLinkData>();
            _imgSize = _globalData.Get<ImagesSizeLinkData>();

            LoadCalibrationGrids();

            if(LoadDataFromDisc)
            {
                LoadCalibration();
            }
            else
            {
                _points = _globalData.Get<UndistortPointsLinkData>();

                _calibrator = new CalibrationAlgorithm();
                LoadCalibrationParameters();
            }
        }

        public void Process()
        {
            if(false == LoadDataFromDisc)
            {
                SetRealCalibrationPoints();
                CalibrateCameras();
            }
        }

        public void Save()
        {
            if(_storedDataOnDisc)
            {
                SaveCalibration();
            }

            _globalData.Set(_linkData);
        }

        private void LoadCalibrationGrids()
        {
            // <CalibrationGrids path=""/>
            XmlNode gridsNode = _config.RootNode.FirstChildWithName("CalibrationGrids");

            string gridsPath = _config.WorkingDirectory + gridsNode.Attributes["path"].Value;
            using(Stream gridsFile = new FileStream(gridsPath, FileMode.Open))
            {
                _linkData.Grids = CamCore.XmlSerialisation.CreateFromFile<List<RealGridData>>(gridsFile);
            }
        }

        private void LoadCalibrationParameters()
        {
            XmlNode calibConfigNode = _config.RootNode.FirstChildWithName("CalibrationConfig");
            if(calibConfigNode != null)
            {
                LoadCalibrationParameters(calibConfigNode);
            }
            else
            {
                SetDefaultCalibrationParameters();
            }
        }

        private void LoadCalibrationParameters(XmlNode calibConfigNode)
        {
            _calibrator.InitParameters();
            IAlgorithmParameter.ReadParametersFromXml(_calibrator.Parameters,
                calibConfigNode.FirstChildWithName("Parameters"));
            _calibrator.OverwriteGridsWithEstimated = false;
            _calibrator.UpdateParameters();
        }

        private void SetDefaultCalibrationParameters()
        {
            _calibrator.InitParameters();
            _calibrator.MaxIterations = 100;

            _calibrator.LinearOnly = false;
            _calibrator.NormalizeLinear = true;
            _calibrator.NormalizeIterative = false;
            _calibrator.MinimalizeSkew = true;

            _calibrator.UseCovarianceMatrix = true;
            _calibrator.ImageMeasurementVariance_X = 0.25;
            _calibrator.ImageMeasurementVariance_Y = 0.25;
            _calibrator.RealMeasurementVariance_X = 1;
            _calibrator.RealMeasurementVariance_Y = 1;
            _calibrator.RealMeasurementVariance_Z = 1;

            _calibrator.EliminateOuliers = true;
            _calibrator.OutliersCoeff = 1.3;
            _calibrator.OverwriteGridsWithEstimated = false;

            _calibrator.UpdateParameters();
        }

        private void SetRealCalibrationPoints()
        {
            SetRealCalibrationPoints(SideIndex.Left);
            SetRealCalibrationPoints(SideIndex.Right);
        }

        private void SetRealCalibrationPoints(SideIndex idx)
        {
            var calibPoints = _points.GetCalibrationPoints(idx);
            var grids = _linkData.Grids;

            foreach(var cp in calibPoints)
            {
                if(cp.GridNum >= grids.Count)
                {
                    continue;
                }
                // First compute real point for every calib point
                var grid = grids[cp.GridNum];
                cp.RealGridPos = cp.RealGridPos + (idx == SideIndex.Left ? grid.OffsetLeft : grid.OffsetRight);
                cp.Real = grid.GetRealFromCell(cp.RealRow, cp.RealCol);
            }
        }

        private void CalibrateCameras()
        {
            _linkData.Cameras = new CameraPair();
            CalibrateCamera(SideIndex.Left);
            CalibrateCamera(SideIndex.Right);
        }

        private void CalibrateCamera(SideIndex idx)
        {
            _calibrator.Points = _points.GetCalibrationPoints(idx);
            _calibrator.Grids = _linkData.Grids;

            _calibrator.Calibrate();
            _linkData.Cameras.SetCameraMatrix(idx, _calibrator.Camera.Matrix);
        }

        private void SaveCalibration()
        {
            //< CalibrationData path = "" />

            XmlNode oldNode = _config.RootNode.FirstChildWithName("CalibrationData");
            bool oldNodeExists = null != oldNode;

            XmlNode calibDataNode = _config.ConfigDoc.CreateElement("CalibrationData");
            XmlAttribute attPath = _config.ConfigDoc.CreateAttribute("path");

            string outPath = _config.WorkingDirectory + "calibration_data.xml";
            attPath.Value = "calibration_data.xml";
            calibDataNode.Attributes.Append(attPath);

            if(oldNodeExists)
            {
                _config.RootNode.ReplaceChild(calibDataNode, oldNode);
            }
            else
            {
                _config.RootNode.AppendChild(calibDataNode);
            }

            using(Stream outFile = new FileStream(outPath, FileMode.Create))
            {
                XmlSerialisation.SaveToFile(_linkData.Cameras, outFile);
            }
        }

        private void LoadCalibration()
        {
            //< CalibrationData path = "" />

            XmlNode calibDataNode = _config.RootNode.FirstChildWithName("CalibrationData");
            string filePath = _config.WorkingDirectory + calibDataNode.Attributes["path"].Value;
            
            using(Stream file = new FileStream(filePath, FileMode.Open))
            {
                _linkData.Cameras = XmlSerialisation.CreateFromFile<CameraPair>(file);
            }
        }

    }
}
