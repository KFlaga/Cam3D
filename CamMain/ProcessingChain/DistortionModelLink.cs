using CamImageProcessing;
using System;
using System.Collections.Generic;
using CameraIndex = CamCore.CameraIndex;
using CalibrationPoint = CalibrationModule.CalibrationPoint;
using CamCore;
using System.Xml;
using System.Windows.Media.Imaging;
using System.IO;

namespace CamMain.ProcessingChain
{
    public class DistortionModelLinkData
    {
        public RadialDistortionModel DistortionLeft { get; set; }
        public RadialDistortionModel DistortionRight { get; set; }

        public RadialDistortionModel GetModel(CameraIndex idx)
        {
            return idx == CameraIndex.Left ? DistortionLeft : DistortionRight;
        }
    }

    public class DistortionModelLink : ILink
    {
        public LinkType LinkType
        {
            get
            {
                return LinkType.DistortionModelComputation;
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
        private RawCalibrationImagesLinkData _rawCalibData;
        private DistortionModelLinkData _linkData;
        private ConfigurationLinkData _config;
        private ImagesSizeLinkData _imgSize;

        public DistortionModelLink(GlobalData gData)
        {
            _globalData = gData;
            _linkData = new DistortionModelLinkData();
        }

        public void Load()
        {
            _config = _globalData.Get<ConfigurationLinkData>();

            if(LoadDataFromDisc)
            {
                LoadDistortionModel();
            }
            else
            {
                _rawCalibData = _globalData.Get<RawCalibrationImagesLinkData>();
                _imgSize = _globalData.Get<ImagesSizeLinkData>();
            }
        }

        public void Process()
        {
            if(false == LoadDataFromDisc)
            {
                ComputeDistortionModels();
            }
        }

        public void Save()
        {
            if(_storedDataOnDisc)
            {
                SaveDistortionModel();
            }

            _globalData.Set(_linkData);
        }


        private void ComputeDistortionModels()
        {
            _linkData.DistortionLeft = ComputeDistortionModel(_rawCalibData.LinesLeft);
            _linkData.DistortionRight = ComputeDistortionModel(_rawCalibData.LinesRight);
        }

        private RadialDistortionModel ComputeDistortionModel(List<List<Vector2>> calibLines)
        {
            // - from extracted points distortion model is computed
            // - first distortion direction is extracted from images and somehow its scale is estimated :
            // --- get interpolated curve curvature and its distance from center
            // --- based on it compute distortion radius and set initial parameters as to reduce it to zero (for this point) 
            // - compute model parameters and its error
            // - if error is high or solution did not converge ask for user input
            CalibrationModule.RadialDistortionCorrector distCorrector = new CalibrationModule.RadialDistortionCorrector();

            distCorrector.ImageHeight = _imgSize.ImageHeight;
            distCorrector.ImageWidth = _imgSize.ImageWidth;
            distCorrector.CorrectionLines = calibLines;

            distCorrector.DistortionModel = new Rational3RDModel();
            distCorrector.DistortionModel.InitialCenterEstimation = 
                new Vector2(_imgSize.ImageWidth * 0.5, _imgSize.ImageHeight * 0.5);
            distCorrector.DistortionModel.InitParameters();

            distCorrector.ComputeCorrectionParameters();

            return distCorrector.DistortionModel;
        }

        private void SaveDistortionModel()
        {
            //< DistortionModels >
            //  < ModelLeft path = "" />
            //  < ModelRight path = "" />
            //</ DistortionModels >

            XmlNode oldNode = _config.RootNode.FirstChildWithName("DistortionModels");
            bool oldNodeExists = null != oldNode;
            
            XmlNode distModelsNode = _config.ConfigDoc.CreateElement("DistortionModels");
            XmlNode nodeModelLeft = _config.ConfigDoc.CreateElement("ModelLeft");
            XmlAttribute attLeftPath = _config.ConfigDoc.CreateAttribute("path");
            XmlNode nodeModelRight = _config.ConfigDoc.CreateElement("ModelRight");
            XmlAttribute attRightPath = _config.ConfigDoc.CreateAttribute("path");

            string pathLeft = _config.WorkingDirectory + "dist_model_left.xml";
            attLeftPath.Value = "dist_model_left.xml";
            nodeModelLeft.Attributes.Append(attLeftPath);

            string pathRight = _config.WorkingDirectory + "dist_model_right.xml";
            attRightPath.Value = "dist_model_right.xml";
            nodeModelRight.Attributes.Append(attRightPath);

            distModelsNode.AppendChild(nodeModelLeft);
            distModelsNode.AppendChild(nodeModelRight);

            if(oldNodeExists)
            {
                _config.RootNode.ReplaceChild(distModelsNode, oldNode);
            }
            else
            {
                _config.RootNode.AppendChild(distModelsNode);
            }

            SaveModelToFile(pathLeft, _linkData.DistortionLeft);
            SaveModelToFile(pathRight, _linkData.DistortionRight);
        }

        private void LoadDistortionModel()
        {
            //< DistortionModels >
            //  < ModelLeft path = "" />
            //  < ModelRight path = "" />
            //</ DistortionModels >

            XmlNode distModelsNode = _config.RootNode.FirstChildWithName("DistortionModels");

            XmlNode leftFileNode = distModelsNode.FirstChildWithName("ModelLeft");
            string leftFilePath = _config.WorkingDirectory + leftFileNode.Attributes["path"].Value;

            XmlNode rightFileNode = distModelsNode.FirstChildWithName("ModelRight");
            string rightFilePath = _config.WorkingDirectory + rightFileNode.Attributes["path"].Value;

            _linkData.DistortionLeft = LoadModelFromFile(leftFilePath);
            _linkData.DistortionRight = LoadModelFromFile(rightFilePath);
        }

        RadialDistortionModel LoadModelFromFile(string path)
        {
            RadialDistortionModel model;
            using(Stream modelFile = new FileStream(path, FileMode.Open))
            {
                XmlDocument modelDoc = new XmlDocument();
                modelDoc.Load(modelFile);
                model = XmlExtensions.DistortionModelFromNode(
                    modelDoc.GetElementsByTagName("DistortionModel")[0]);
            }
            return model;
        }

        void SaveModelToFile(string path, RadialDistortionModel model)
        {
            using(Stream modelFile = new FileStream(path, FileMode.Create))
            {
                XmlDocument modelDoc = new XmlDocument();
                modelDoc.AppendChild(XmlExtensions.CreateDistortionModelNode(modelDoc, model));
                modelDoc.Save(modelFile);
            }
        }
    }
}
