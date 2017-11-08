using CamAlgorithms;
using CamAlgorithms.Triangulation;
using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace CamMain.ProcessingChain
{
    public class TriangulationLinkData
    {
        public Dictionary<int, List<TriangulatedPoint>> Points { get; set; } = new Dictionary<int, List<TriangulatedPoint>>();
    }

    public class TriangulationLink : ILink
    {
        public LinkType LinkType
        {
            get
            {
                return LinkType.Triangulation;
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
        private CalibrationLinkData _calibration;
        private RectificationLinkData _rectification;
        private MatchedImagesLinkData _matchedImages;
        private DisparityRefinementLinkData _disparity;
        private TriangulationLinkData _linkData;

        private TwoPointsTriangulation _trinagulation;

        public TriangulationLink(GlobalData gData)
        {
            _globalData = gData;
            _linkData = new TriangulationLinkData();
        }

        public void Load()
        {
            _config = _globalData.Get<ConfigurationLinkData>();
            if(LoadDataFromDisc)
            {
                LoadTriangulationResults();
            }
            else
            {
                _imgSize = _globalData.Get<ImagesSizeLinkData>();
                _calibration = _globalData.Get<CalibrationLinkData>();
                _rectification = _globalData.Get<RectificationLinkData>();
                _matchedImages = _globalData.Get<MatchedImagesLinkData>();
                _disparity = _globalData.Get<DisparityRefinementLinkData>();

                _trinagulation = new TwoPointsTriangulation();
                _trinagulation.Cameras = _calibration.Cameras;
            }

        }

        public void Process()
        {
            if(false == LoadDataFromDisc)
            {
                Traingulate();
            }
        }

        public void Save()
        {
            if(_storedDataOnDisc)
            {
                SaveTriangulationResults();
            }

            _globalData.Set(_linkData);
        }

        void Traingulate()
        {
            foreach(var entry in _disparity.Maps)
            {
                DisparityMap map = entry.Value;

                List<Vector<double>> pointsLeft, pointsRight;
                DerectifyPoints(map, out pointsLeft, out pointsRight);

                _trinagulation.PointsLeft = pointsLeft;
                _trinagulation.PointsRight = pointsRight;
                _trinagulation.Rectified = false;

                _trinagulation.Estimate3DPoints();

                StoreTraingulatedPoints(entry.Key, pointsLeft, pointsRight, _trinagulation.Points3D);
            }
        }

        private void DerectifyPoints(DisparityMap map,
            out List<Vector<double>> pointsLeft,
            out List<Vector<double>> pointsRight)
        {
            pointsLeft = new List<Vector<double>>();
            pointsRight = new List<Vector<double>>();

            // for each disparity
            for(int y = 0; y < _imgSize.ImageHeight; ++y)
            {
                for(int x = 0; x < _imgSize.ImageWidth; ++x)
                {
                    Disparity disp = map[y, x];
                    if(disp.IsValid())
                    {
                        pointsLeft.Add(DerectifyPoint(x, y,
                            _rectification.Rectification.RectificationLeft_Inverse));
                        pointsRight.Add(DerectifyPoint(x + disp.SubDX, y,
                            _rectification.Rectification.RectificationRight_Inverse));
                    }
                }
            }
        }

        private Vector<double> DerectifyPoint(double x, double y, Matrix<double> inverseRectification)
        {
            Vector<double> point = new DenseVector(new double[3] { x, y, 1.0 });
            point = inverseRectification * point;
            point.DivideThis(point.At(2));
            return point;
        }

        private void StoreTraingulatedPoints(int id,
            List<Vector<double>> pointsLeft,
            List<Vector<double>> pointsRight,
            List<Vector<double>> points3d)
        {
            var points = new List<TriangulatedPoint>();
            for(int i = 0; i < points3d.Count; ++i)
            {
                points.Add(new TriangulatedPoint()
                {
                    ImageLeft = new Vector2(pointsLeft[i]),
                    ImageRight = new Vector2(pointsRight[i]),
                    Real = new Vector3(points3d[i])
                });
            }
            _linkData.Points.Add(id, points);
        }

        void SaveTriangulationResults()
        {
            XmlNode triangulationNode = _config.ConfigDoc.CreateElement("TriangulationResults");
            foreach(var entry in _linkData.Points)
            {
                XmlNode resultsNode = _config.ConfigDoc.CreateElement("Results");
                XmlAttribute attId = _config.ConfigDoc.CreateAttribute("id");
                XmlAttribute attPath = _config.ConfigDoc.CreateAttribute("path");

                string triangulationPath = _config.WorkingDirectory + "triangulation_out" + entry.Key.ToString() + ".xml";
                attPath.Value = "triangulation_out.xml";
                attId.Value = entry.Key.ToString();

                triangulationNode.Attributes.Append(attId);
                triangulationNode.Attributes.Append(attPath);

                _config.RootNode.AppendChild(triangulationNode);
                
                CamCore.XmlSerialisation.SaveToFile(entry.Value, triangulationPath);
            }

            _config.RootNode.AppendChild(triangulationNode);
        }

        void LoadTriangulationResults()
        {
            //<TriangulationResults>
            //  <Results id="1" path=""/>
            //</TriangulationResults>
            _linkData.Points = new Dictionary<int, List<TriangulatedPoint>>();

            XmlNode resultsListNode = _config.RootNode.FirstChildWithName("TriangulationResults");

            foreach(XmlNode mapNode in resultsListNode.ChildNodes)
            {
                int id = int.Parse(mapNode.Attributes["id"].Value);

                string path = _config.WorkingDirectory + mapNode.Attributes["path"].Value;

                List<TriangulatedPoint> points = CamCore.XmlSerialisation.CreateFromFile<List<TriangulatedPoint>>(path);
                _linkData.Points.Add(id, points);
            }
        }
    }
}
