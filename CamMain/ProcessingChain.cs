//using CalibrationModule;
using CamCore;
using CamImageProcessing;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;
using System.Xml;
using CameraIndex = CamCore.CalibrationData.CameraIndex;

namespace CamMain
{
    // Reads calibration input file and begins processing
    // 1) Calibration raw images :
    //      - should contain set of valid calibration images for each camera
    //      - points/lines from each image are extracted and saved
    //      - calibration image type may be specified in file
    // 2) Distortion model :
    //      - from extracted points distortion model is computed
    //      - first distortion direction is extracted from images and somehow its scale is estimated :
    //          -- get interpolated curve curvature and its distance from center
    //          -- based on it compute distrotion radius and set inital parameters as to reduce it to zero (for this point) 
    //      - compute model parameters and its error
    //      - if error is high or solution did not converge ask for user input
    // 3) Undistort calibration images and save them
    // 4) Extract points again and save them
    // 5) Load CalibrationGirds
    //    (grids may specify additional primary rc offset)
    // 6) Calibrate cameras and save results
    public class ProcessingChain
    {
        private void OpenChainFile()
        {
            FileOperations.LoadFromFile(CalibrationData.Data.LoadFromFile, "Xml File|*.xml");
        }

        private void OpenChainFile(Stream file, string path)
        {
            XmlDocument dataDoc = new XmlDocument();
            dataDoc.Load(file);


        }

        struct ImageIndex
        {
            public int Id;
            public CameraIndex CameraIndex;
            public int ListIndex;

            public static ImageIndex FromXml(XmlNode node)
            {
                ImageIndex idx = new ImageIndex()
                {
                    Id = int.Parse(node.Attributes["id"].Value),
                    CameraIndex = node.Attributes["id"].Value.CompareTo("right") == 0 ?
                        CameraIndex.Right : CameraIndex.Left,
                    ListIndex = -1
                };
                return idx;
            }
        }

        List<ColorImage> _rawCalibImages;
        List<ImageIndex> _calibImagesIndices;

        /// <summary>
        ///     Loads raw calibration images from children of supplied node  
        /// </summary>
        private void LoadCalibrationImages(XmlNode imgsNode)
        {
            //<CalibrationImages_Raw>
            //  <Image id="1" cam="left" path=""/>
            //  <Image id="1" cam="right" path=""/>
            //</CalibrationImages_Raw>

            _rawCalibImages = new List<ColorImage>();
            _calibImagesIndices = new List<ImageIndex>();
            foreach(XmlNode imgNode in imgsNode.ChildNodes)
            {
                ImageIndex idx = ImageIndex.FromXml(imgNode);
                idx.ListIndex = _rawCalibImages.Count;
                string imgPath = imgNode.Attributes["path"].Value;
                BitmapImage bitmap = new BitmapImage(new Uri(imgPath, UriKind.RelativeOrAbsolute));
                ColorImage image = new ColorImage();
                image.FromBitmapSource(bitmap);
                _rawCalibImages.Add(image);
                _calibImagesIndices.Add(idx);
            }
        }

        /// <summary>
        ///     Loads calibration grids from file with path specified in node
        /// </summary>
        private void LoadCalibrationGrids(XmlNode gridsNode)
        {
            // <CalibrationGrids path=""/>

            // Load grids file
            XmlDocument xmlDoc = new XmlDocument();
            string gridsPath = gridsNode.Attributes["path"].Value;
            using(Stream gridsFile = new FileStream(gridsPath, FileMode.Open))
            {
                xmlDoc.Load(gridsFile);
            }
            
            XmlNodeList gridNodeList = xmlDoc.GetElementsByTagName("Grid");
            foreach(XmlNode gridNode in gridNodeList)
            {
                CalibrationModule.RealGridData grid = CalibrationModule.RealGridData.FromXml(gridNode);
            }
        }

        private void ExtractCalibLinesFromRaw(XmlNode extractorNode)
        {
            // TODO: Move Extractor initialization to other method
            // <PointsExtractor type="CalibShape">
            //   <Parameters>....

            CalibrationModule.CalibrationPointsFinder pointsExtractor;
            CalibrationModule.ICalibrationLinesExtractor linesExtractor;
            if(extractorNode == null)
            {
                pointsExtractor = new CalibrationModule.ShapesGridCPFinder();
                linesExtractor = pointsExtractor.LinesExtractor;
                pointsExtractor.InitParameters();
                pointsExtractor.UpdateParameters();
                pointsExtractor.PrimaryShapeChecker = new CalibrationModule.RedNeighbourhoodChecker();
            }
            else
            {
                // Get type of extractor
                string extractorType = extractorNode.Attributes["type"].Value;
                if(extractorType == "CalibShape") { }
                pointsExtractor = new CalibrationModule.ShapesGridCPFinder();
                linesExtractor = pointsExtractor.LinesExtractor;
                pointsExtractor.InitParameters();

                XmlNode paramsNode = extractorNode.FirstChildWithName("Parameters");
                if(paramsNode != null)
                {
                    AlgorithmParameter.ReadParametersFromXml(pointsExtractor.Parameters, paramsNode);
                }

                pointsExtractor.UpdateParameters();
            }

            for(int i = 0; i < _calibImagesIndices.Count; ++i)
            {
                ImageIndex idx = _calibImagesIndices[i];
                ColorImage image = _rawCalibImages[idx.ListIndex];
                pointsExtractor.Image = image;
                pointsExtractor.FindCalibrationPoints();
                linesExtractor.ExtractLines();
            }
        }
    }
}