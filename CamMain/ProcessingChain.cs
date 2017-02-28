//using CalibrationModule;
using CamCore;
using CamImageProcessing;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;
using System.Xml;
using CalibrationModule;
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
    //          -- based on it compute distortion radius and set initial parameters as to reduce it to zero (for this point) 
    //      - compute model parameters and its error
    //      - if error is high or solution did not converge ask for user input
    // 3) Undistort calibration images and save them
    // 4) Extract points again and save them
    // 5) Load CalibrationGirds
    //    (grids may specify additional primary rc offset)
    // 6) Calibrate cameras and save results
    public class ProcessingChain
    {
        private struct ImageIndex
        {
            public int Id;
            public CameraIndex CameraIndex;
            public int ListIndex;

            public static ImageIndex FromXml(XmlNode node)
            {
                ImageIndex idx = new ImageIndex()
                {
                    Id = int.Parse(node.Attributes["id"].Value),
                    CameraIndex = node.Attributes["cam"].Value.CompareTo("right") == 0 ?
                        CameraIndex.Right : CameraIndex.Left,
                    ListIndex = -1
                };
                return idx;
            }
        }

        private string _workingDirectory;

        private List<ColorImage> _rawCalibImages;
        private List<MaskedImage> _undistortedCalibImages;
        private List<ImageIndex> _calibImagesIndices;
        private int _imagesWidth;
        private int _imagesHeight;

        private RadialDistortionModel _distortionLeft;
        private RadialDistortionModel _distortionRight;

        private List<CalibrationModule.CalibrationPoint> _calibPointsLeft;
        private List<CalibrationModule.CalibrationPoint> _calibPointsRight;
        private List<CalibrationModule.RealGridData> _calibGrids;
        private List<List<Vector2>> _calibLinesLeft;
        private List<List<Vector2>> _calibLinesRight;

        private XmlDocument _xmlDoc;

        public void OpenChainFile()
        {
            FileOperations.LoadFromFile(OpenChainFile, "Xml File|*.xml");
        }

        private void OpenChainFile(Stream file, string path)
        {
            _xmlDoc = new XmlDocument();
            _xmlDoc.Load(file);
        }

        public void Process()
        {
            //try
            //{
            OpenChainFile();

            XmlNode rootNode = _xmlDoc.GetElementsByTagName("CalibrationInput")[0];
            ReadWorkingDirectory(rootNode);
            LoadCalibrationImages(rootNode);
            LoadCalibrationGrids(rootNode);
            InitCalibPointsExtractor(rootNode);
            ExtractCalibLinesFromRaw();
            SaveCalibrationPointsRaw(_xmlDoc, rootNode);
            SaveCalibrationLinesRaw(_xmlDoc, rootNode);
            ComputeDistortionModel();
            SaveDistortionModel(_xmlDoc, rootNode);
            //}
            //catch (Exception e)
            //{

            //}
        }

        private void ReadWorkingDirectory(XmlNode rootNode)
        {
            // <WorkingDirectory path=""/>

            XmlNode pathNode = rootNode.FirstChildWithName("WorkingDirectory");
            _workingDirectory = pathNode.Attributes["path"].Value;

            if(!(_workingDirectory.EndsWith("\\") || _workingDirectory.EndsWith("/")))
            {
                _workingDirectory = _workingDirectory + "\\";
            }
        }

        private void LoadCalibrationImages(XmlNode rootNode)
        {
            //<CalibrationImages_Raw>
            //  <Image id="1" cam="left" path=""/>
            //  <Image id="1" cam="right" path=""/>
            //</CalibrationImages_Raw>

            XmlNode imgsNode = rootNode.FirstChildWithName("CalibrationImages_Raw");

            _rawCalibImages = new List<ColorImage>();
            _undistortedCalibImages = new List<MaskedImage>();
            _calibImagesIndices = new List<ImageIndex>();
            foreach(XmlNode imgNode in imgsNode.ChildNodes)
            {
                ImageIndex idx = ImageIndex.FromXml(imgNode);
                idx.ListIndex = _rawCalibImages.Count;
                string imgPath = _workingDirectory + imgNode.Attributes["path"].Value;
                BitmapImage bitmap = new BitmapImage(new Uri(imgPath, UriKind.RelativeOrAbsolute));
                ColorImage image = new ColorImage();
                image.FromBitmapSource(bitmap);
                _rawCalibImages.Add(image);
                _undistortedCalibImages.Add(null);
                _calibImagesIndices.Add(idx);
            }
        }

        private void LoadCalibrationGrids(XmlNode rootNode)
        {
            // <CalibrationGrids path=""/>
            XmlNode gridsNode = rootNode.FirstChildWithName("CalibrationGrids");

            string gridsPath = _workingDirectory + gridsNode.Attributes["path"].Value;
            using(Stream gridsFile = new FileStream(gridsPath, FileMode.Open))
            {
                _calibGrids = CamCore.XmlSerialisation.CreateFromFile<List<CalibrationModule.RealGridData>>(gridsFile);
            }
        }

        private CalibrationModule.CalibrationPointsFinder _calibPointsFinder;
        private CalibrationModule.ICalibrationLinesExtractor _calibLinesExtractor;

        private void ExtractCalibLinesFromRaw()
        {
            _calibPointsLeft = new List<CalibrationModule.CalibrationPoint>();
            _calibPointsRight = new List<CalibrationModule.CalibrationPoint>();
            _calibLinesLeft = new List<List<Vector2>>();
            _calibLinesRight = new List<List<Vector2>>();

            foreach(var idx in _calibImagesIndices)
            {
                ColorImage image = _rawCalibImages[idx.ListIndex];
                _calibPointsFinder.Image = image;
                _calibPointsFinder.FindCalibrationPoints();
                _calibLinesExtractor.ExtractLines();

                if(idx.CameraIndex == CameraIndex.Left)
                {
                    _calibPointsLeft.AddRange(_calibPointsFinder.Points);
                    _calibLinesLeft.AddRange(_calibLinesExtractor.CalibrationLines);
                }
                else
                {
                    _calibPointsRight.AddRange(_calibPointsFinder.Points);
                    _calibLinesRight.AddRange(_calibLinesExtractor.CalibrationLines);
                }
            }
        }

        private void InitCalibPointsExtractor(XmlNode rootNode)
        {
            // <PointsExtractor type="CalibShape">
            //   <Parameters>....
            //
            XmlNode extractorNode = rootNode.FirstChildWithName("PointsExtractor");

            if(extractorNode == null)
            {
                _calibPointsFinder = new CalibrationModule.ShapesGridCPFinder();
                _calibLinesExtractor = _calibPointsFinder.LinesExtractor;
                _calibPointsFinder.InitParameters();
                _calibPointsFinder.UpdateParameters();
                _calibPointsFinder.PrimaryShapeChecker = new CalibrationModule.RedNeighbourhoodChecker();
            }
            else
            {
                // Get type of extractor
                string extractorType = extractorNode.Attributes["type"].Value;
                if(extractorType == "CalibShape") { }
                _calibPointsFinder = new CalibrationModule.ShapesGridCPFinder();
                _calibLinesExtractor = _calibPointsFinder.LinesExtractor;
                _calibPointsFinder.InitParameters();

                XmlNode paramsNode = extractorNode.FirstChildWithName("Parameters");
                if(paramsNode != null)
                {
                    AlgorithmParameter.ReadParametersFromXml(_calibPointsFinder.Parameters, paramsNode);
                }

                _calibPointsFinder.UpdateParameters();
            }
        }

        private void SaveCalibrationPointsRaw(XmlDocument xmlDoc, XmlNode rootNode)
        {
            //< !--Output-- >
            //< CalibrationPoints_Raw >
            //  < PointsLeft path = "" />
            //  < PointsRight path = "" />
            //</ CalibrationPoints_Raw >

            XmlNode node = xmlDoc.CreateElement("CalibrationPoints_Raw");
            XmlNode nodePointsLeft = xmlDoc.CreateElement("PointsLeft");
            XmlAttribute attPointsLeftPath = xmlDoc.CreateAttribute("path");
            XmlNode nodePointsRight = xmlDoc.CreateElement("PointsRight");
            XmlAttribute attPointsRightPath = xmlDoc.CreateAttribute("path");

            string pathPointsLeft = _workingDirectory + "calib_points_raw_left.xml";
            attPointsLeftPath.Value = "calib_points_raw_left.xml";
            nodePointsLeft.Attributes.Append(attPointsLeftPath);

            string pathPointsRight = _workingDirectory + "calib_points_raw_right.xml";
            attPointsRightPath.Value = "calib_points_raw_right.xml";
            nodePointsRight.Attributes.Append(attPointsRightPath);

            node.AppendChild(nodePointsLeft);
            node.AppendChild(nodePointsRight);

            rootNode.AppendChild(node);

            CamCore.XmlSerialisation.SaveToFile(_calibPointsLeft, pathPointsLeft);
            CamCore.XmlSerialisation.SaveToFile(_calibPointsRight, pathPointsRight);
        }

        private void SaveCalibrationLinesRaw(XmlDocument xmlDoc, XmlNode rootNode)
        {
            //< !--Output-- >
            //< CalibrationLines_Raw >
            //  < LinesLeft path = "" />
            //  < LinesRight path = "" />
            //</ CalibrationLines_Raw >

            XmlNode node = xmlDoc.CreateElement("CalibrationLines_Raw");
            XmlNode nodeLinesLeft = xmlDoc.CreateElement("LinesLeft");
            XmlAttribute attLeftPath = xmlDoc.CreateAttribute("path");
            XmlNode nodeLinesRight = xmlDoc.CreateElement("LinesRight");
            XmlAttribute attRightPath = xmlDoc.CreateAttribute("path");

            string pathPointsLeft = _workingDirectory + "calib_lines_raw_left.xml";
            attLeftPath.Value = "calib_lines_raw_left.xml";
            nodeLinesLeft.Attributes.Append(attLeftPath);

            string pathPointsRight = _workingDirectory + "calib_lines_raw_right.xml";
            attRightPath.Value = "calib_lines_raw_right.xml";
            nodeLinesRight.Attributes.Append(attRightPath);

            node.AppendChild(nodeLinesLeft);
            node.AppendChild(nodeLinesRight);

            rootNode.AppendChild(node);

            CamCore.XmlSerialisation.SaveToFile(_calibLinesLeft, pathPointsLeft);
            CamCore.XmlSerialisation.SaveToFile(_calibLinesLeft, pathPointsRight);
        }

        private void ComputeDistortionModel()
        {
            // - from extracted points distortion model is computed
            // - first distortion direction is extracted from images and somehow its scale is estimated :
            // --- get interpolated curve curvature and its distance from center
            // --- based on it compute distortion radius and set initial parameters as to reduce it to zero (for this point) 
            // - compute model parameters and its error
            // - if error is high or solution did not converge ask for user input
            CalibrationModule.RadialDistortionCorrector distCorrector = new CalibrationModule.RadialDistortionCorrector();

            distCorrector.ImageHeight = _imagesHeight;
            distCorrector.ImageWidth = _imagesWidth;
            distCorrector.CorrectionLines = _calibLinesExtractor.CalibrationLines;

            distCorrector.DistortionModel = new Rational3RDModel();

            distCorrector.ComputeCorrectionParameters();
        }

        private void SaveDistortionModel(XmlDocument xmlDoc, XmlNode rootNode)
        {
            //< !--Output-- >
            //< DistortionModels >
            //  < ModelLeft path = "" />
            //  < ModelRight path = "" />
            //</ DistortionModels >

            XmlNode node = xmlDoc.CreateElement("DistortionModels");
            XmlNode nodeModelLeft = xmlDoc.CreateElement("ModelLeft");
            XmlAttribute attLeftPath = xmlDoc.CreateAttribute("path");
            XmlNode nodeModelRight = xmlDoc.CreateElement("ModelRight");
            XmlAttribute attRightPath = xmlDoc.CreateAttribute("path");

            string pathLeft = _workingDirectory + "dist_model_left.xml";
            attLeftPath.Value = "dist_model_left.xml";
            nodeModelLeft.Attributes.Append(attLeftPath);

            string pathRight = _workingDirectory + "dist_model_right.xml";
            attRightPath.Value = "dist_model_right.xml";
            nodeModelRight.Attributes.Append(attRightPath);

            node.AppendChild(nodeModelLeft);
            node.AppendChild(nodeModelRight);

            rootNode.AppendChild(node);

            using(Stream modelFile = new FileStream(pathLeft, FileMode.Create))
            {
                XmlDocument modelDoc = new XmlDocument();
                modelDoc.AppendChild(XmlExtensions.CreateDistortionModelNode(modelDoc, _distortionLeft));
                modelDoc.Save(modelFile);
            }

            using(Stream modelFile = new FileStream(pathRight, FileMode.Create))
            {
                XmlDocument modelDoc = new XmlDocument();
                modelDoc.AppendChild(XmlExtensions.CreateDistortionModelNode(modelDoc, _distortionRight));
                modelDoc.Save(modelFile);
            }
        }

        private void UndistortImages()
        {
            ImageTransformer undistort = new ImageTransformer(
                ImageTransformer.InterpolationMethod.Quadratic, 1);

            foreach(var idx in _calibImagesIndices)
            {
                if(idx.CameraIndex == CameraIndex.Left)
                {
                    undistort.Transformation =
                        new RadialDistortionTransformation(_distortionLeft);
                }
                else
                {
                    undistort.Transformation =
                        new RadialDistortionTransformation(_distortionRight);
                }

                MaskedImage img = new MaskedImage(_rawCalibImages[idx.ListIndex]);
                MaskedImage imgFinal = undistort.TransfromImageBackwards(img, true);
                _undistortedCalibImages[idx.ListIndex] = imgFinal;
            }
        }

        private void SaveUndistortedImages(XmlDocument xmlDoc, XmlNode rootNode)
        {
            //< !--Output-- >
            //< CalibrationImages_Undistorted >
            //  < Image id = "0" cam = "left" path = "" />
            //  < Image id = "1" cam = "left" path = "" />         
            //  < Image id = "0" cam = "right" path = "" />              
            //  < Image id = "1" cam = "right" path = "" />                  
            //</ CalibrationImages_Undistorted >

            XmlNode node = xmlDoc.CreateElement("CalibrationImages_Undistorted");

            foreach(var idx in _calibImagesIndices)
            {
                XmlNode nodeImage = xmlDoc.CreateElement("Image");
                XmlAttribute attId = xmlDoc.CreateAttribute("id");
                XmlAttribute attCam = xmlDoc.CreateAttribute("cam");
                XmlAttribute attPath = xmlDoc.CreateAttribute("path");

                attId.Value = idx.Id.ToString();
                attCam.Value = idx.CameraIndex == CameraIndex.Left ? "left" : "right";
                attPath.Value = "image_undistorted_" + attCam.Value + "_" + attId.Value + ".png";
                string path = _workingDirectory + attPath.Value;

                nodeImage.Attributes.Append(attId);
                nodeImage.Attributes.Append(attCam);
                nodeImage.Attributes.Append(attPath);

                using(Stream file = new FileStream(path, FileMode.Create))
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(_undistortedCalibImages[idx.ListIndex].ToBitmapSource()));
                    encoder.Save(file);
                }

                node.AppendChild(nodeImage);
            }
        }

        private void ExtractCalibPointsUndistorted()
        {
            _calibPointsLeft = new List<CalibrationModule.CalibrationPoint>();
            _calibPointsRight = new List<CalibrationModule.CalibrationPoint>();
            for(int i = 0; i < _calibImagesIndices.Count; ++i)
            {
                ImageIndex idx = _calibImagesIndices[i];
                MaskedImage image = _undistortedCalibImages[idx.ListIndex];
                _calibPointsFinder.Image = image;
                _calibPointsFinder.FindCalibrationPoints();

                if(idx.CameraIndex == CameraIndex.Left)
                {
                    _calibPointsLeft.AddRange(_calibPointsFinder.Points);
                }
                else
                {
                    _calibPointsRight.AddRange(_calibPointsFinder.Points);
                }
            }
        }

        private CalibrationModule.CamCalibrator _calibrator;

        private void LoadCalibrationParameters()
        {
            _calibrator = new CalibrationModule.CamCalibrator();

            /* < CalibrationConfig >
                < Parameters >
                    < Parameter desc = "Linear estimation only" id = "LIN" value = "False" />   
                    < Parameter desc = "Normalize points for linear estimation" id = "NPL" value = "True" />     
                    < Parameter desc = "Normalize points for iterative minimalisation" id = "NPI" value = "False" />            
                    < Parameter desc = "Use convarianve matrix" id = "COV" value = "True" />
                    < Parameter desc = "Minimalise skew" id = "SKEW" value = "True" />                       
                    < Parameter desc = "Image measurements variance in X [px]" id = "VIX" value = "0.25" />                            
                    < Parameter desc = "Image measurements variance in Y [px]" id = "VIY" value = "0.25" />
                    < Parameter desc = "Real measurements variance in X [mm]" id = "VRX" value = "1" />
                    < Parameter desc = "Real measurements variance in Y [mm]" id = "VRY" value = "1" />
                    < Parameter desc = "Real measurements variance in Z [mm]" id = "VRZ" value = "1" />
                    < Parameter desc = "Max iterations" id = "MI" value = "300" />
                    < Parameter desc = "Perform outliers elimination" id = "MI" value = "True" />
                    < Parameter desc = "Outliers elimination coeff" id = "MI" value = "1.3" />
                </ Parameters >
              </ CalibrationConfig >
         */

            _calibrator.RealMeasurementVariance_X = 1.0;
            _calibrator.RealMeasurementVariance_Y = 1.0;
            _calibrator.RealMeasurementVariance_Z = 1.0;
            _calibrator.ImageMeasurementVariance_X = 0.25;
            _calibrator.ImageMeasurementVariance_Y = 0.25;
            _calibrator.LinearOnly = false;
            _calibrator.MinimaliseSkew = true;
            _calibrator.EliminateOuliers = true;
            _calibrator.OutliersCoeff = 1.3;
            _calibrator.OverwriteGridsWithEstimated = false;
            _calibrator.NormalizeIterative = false;
            _calibrator.NormalizeLinear = true;
        }

        private void CalibrateCamera(CameraIndex camera)
        {
            var calibPoints = camera == CameraIndex.Left ? _calibPointsLeft : _calibPointsRight;
            var grids = _calibGrids;

            foreach(var cp in calibPoints)
            {
                if(cp.GridNum >= grids.Count)
                {
                    // TODO ERROR
                    continue;
                }
                // First compute real point for every calib point
                var grid = grids[cp.GridNum];
                cp.Real = grid.GetRealFromCell(cp.RealRow, cp.RealCol);
            }

            _calibrator.Points = calibPoints;
            _calibrator.Grids = grids;

            _calibrator.Calibrate();
        }

        private void SaveCalibration()
        {
            //< CalibrationData >
            //</ CalibrationData >
        }

        private void CrossCalibration()
        {

        }

        // So we have calibration done
        // Next will be rectification
        private void FindRectificationMatrices()
        {
            // 1) Find using zhang_loop calibrated
            // 2) Compute error = non-horizontally of matched points (+ some confident features maybe later)
            // 3) Find using fussiello uncalibrated
            // 4) Compute same error
            // 5) Also find distortion ratio (like ratio of base/rectified width/height)
            // 6) Get rectification with better parameters
        }

        private void RectifyImages()
        {

        }

        private void SaveRectifiedImages()
        {

        }

        // Next step is matching
        // Matching itself is quite easy to set up
        // But getting P1, P2 and using gradient needs some testing on real images

        // Disparity refinement
        // On one side its easy as well as steps are obvious but parameters
        // needs 
    }
}