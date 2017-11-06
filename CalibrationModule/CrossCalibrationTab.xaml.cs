using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using CamControls;
using CamCore;
using CamAlgorithms;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Text;
using System.Xml;
using System.IO;
using CamAlgorithms.Calibration;

namespace CalibrationModule
{
    public partial class CrossCalibrationTab : UserControl
    {
        public List<CalibrationPoint> CalibrationPointsLeft { get; set; } = new List<CalibrationPoint>();
        public List<CalibrationPoint> CalibrationPointsRight { get; set; } = new List<CalibrationPoint>();

        public List<RealGridData> GridsLeft { get; set; } = new List<RealGridData>();
       // public List<RealGridData> GridsRight { get; set; } = new List<RealGridData>();

        public List<Vector2> MatchedPointsLeft { get; set; } = new List<Vector2>();
        public List<Vector2> MatchedPointsRight { get; set; } = new List<Vector2>();

        CrossCalibrationRefiner _calibrator = new CrossCalibrationRefiner();

        public CrossCalibrationTab()
        {
            InitializeComponent();
            
        }
        
        private void ManageGridsLeft(object sender, RoutedEventArgs e)
        {
            RealGridsManagerWindow gridsManager = new RealGridsManagerWindow();
            gridsManager.RealGrids = GridsLeft;
            bool? res = gridsManager.ShowDialog();
            if(res != null && res == true)
            {
                GridsLeft = gridsManager.RealGrids;
            }
        }

        private void ManagePointsLeft(object sender, RoutedEventArgs e)
        {
            CalibrationPointsManagerWindow pointsManager = new CalibrationPointsManagerWindow();
            pointsManager.CalibrationPoints = CalibrationPointsLeft;
            bool? res = pointsManager.ShowDialog();
            if(res != null && res == true)
            {
                CalibrationPointsLeft = pointsManager.CalibrationPoints;
            }
        }

        //private void ManageGridsRight(object sender, RoutedEventArgs e)
        //{
        //    RealGridsManagerWindow gridsManager = new RealGridsManagerWindow();
        //    gridsManager.RealGrids = GridsRight;
        //    bool? res = gridsManager.ShowDialog();
        //    if(res != null && res == true)
        //    {
        //        GridsRight = gridsManager.RealGrids;
        //    }
        //}

        private void ManagePointsRight(object sender, RoutedEventArgs e)
        {
            CalibrationPointsManagerWindow pointsManager = new CalibrationPointsManagerWindow();
            pointsManager.CalibrationPoints = CalibrationPointsRight;
            bool? res = pointsManager.ShowDialog();
            if(res != null && res == true)
            {
                CalibrationPointsRight = pointsManager.CalibrationPoints;
            }
        }

        private void ManageMatchedPointsLeft(object sender, RoutedEventArgs e)
        {
            MatchedPointsManagerWindow pointsManager = new MatchedPointsManagerWindow();
            pointsManager.Points = MatchedPointsLeft;
            bool? res = pointsManager.ShowDialog();
            if(res != null && res == true)
            {
                MatchedPointsLeft = pointsManager.Points;
            }
        }

        private void ManageMatchedPointsRight(object sender, RoutedEventArgs e)
        {
            MatchedPointsManagerWindow pointsManager = new MatchedPointsManagerWindow();
            pointsManager.Points = MatchedPointsRight;
            bool? res = pointsManager.ShowDialog();
            if(res != null && res == true)
            {
                MatchedPointsRight = pointsManager.Points;
            }
        }

        private void SaveCalibMatched(object sender, RoutedEventArgs e)
        {
            CamCore.FileOperations.SaveToFile(SaveCalibMatched, "Xml File|*.xml");
        }

        private void Calibrate(object sender, RoutedEventArgs e)
        {
            if(CameraPair.Data.IsCamLeftCalibrated == false ||
                CameraPair.Data.IsCamRightCalibrated == false)
            {
                MessageBox.Show("Cameras need to be initialy calibrated");
                return;
            }

            _calibrator.CalibPointsLeft = new List<CalibrationPoint>(CalibrationPointsLeft.Count);
            _calibrator.CalibPointsRight = new List<CalibrationPoint>(CalibrationPointsLeft.Count);
            for(int i = 0; i < CalibrationPointsLeft.Count; ++i)
            {
                var cleft = CalibrationPointsLeft[i];
                var cright = CalibrationPointsRight.Find((cp) =>
               {
                   return cp.GridNum == cleft.GridNum &&
                       cp.RealCol == cleft.RealCol &&
                       cp.RealRow == cleft.RealRow;
               });
                if(cright != null)
                {
                    _calibrator.CalibPointsLeft.Add(cleft);
                    _calibrator.CalibPointsRight.Add(cright);
                }
            }

           // _calibrator.CalibPointsLeft = CalibrationPointsLeft;
            //_calibrator.CalibPointsRight = CalibrationPointsRight;
            _calibrator.CalibGrids = GridsLeft;
           // _calibrator.GridsRight = GridsRight;
            _calibrator.MatchedPointsLeft = MatchedPointsLeft;
            _calibrator.MatchedPointsRight = MatchedPointsRight;

            AlgorithmWindow algWindow = new AlgorithmWindow(_calibrator);
            algWindow.Show();
        }
        
        public void SaveCalibMatched(Stream file, string path)
        {
            XmlDocument dataDoc = new XmlDocument();
            var rootNode = dataDoc.CreateElement("Points");

            var calibMatchedLeft = new List<CalibrationPoint>(CalibrationPointsLeft.Count);
            var calibMatchedRight = new List<CalibrationPoint>(CalibrationPointsRight.Count);
            for(int i = 0; i < CalibrationPointsLeft.Count; ++i)
            {
                var cleft = CalibrationPointsLeft[i];
                var cright = CalibrationPointsRight.Find((cp) =>
                {
                    return cp.GridNum == cleft.GridNum &&
                        cp.RealCol == cleft.RealCol &&
                        cp.RealRow == cleft.RealRow;
                });
                if(cright != null)
                {
                    calibMatchedLeft.Add(cleft);
                    calibMatchedRight.Add(cright);
                }
            }

            for(int i = 0; i < calibMatchedLeft.Count; ++i)
            {
                var cpL = calibMatchedLeft[i];
                var cpR = calibMatchedRight[i];

                var pointNode = dataDoc.CreateElement("Point");

                var attImgX = dataDoc.CreateAttribute("imgx");
                attImgX.Value = cpL.ImgX.ToString("F3");
                var attImgY = dataDoc.CreateAttribute("imgy");
                attImgY.Value = cpL.ImgY.ToString("F3");
                var attImgX2 = dataDoc.CreateAttribute("imgx2");
                attImgX2.Value = cpR.ImgX.ToString("F3");
                var attImgY2 = dataDoc.CreateAttribute("imgy2");
                attImgY2.Value = cpR.ImgY.ToString("F3");

                pointNode.Attributes.Append(attImgX);
                pointNode.Attributes.Append(attImgY);
                pointNode.Attributes.Append(attImgX2);
                pointNode.Attributes.Append(attImgY2);

                rootNode.AppendChild(pointNode);
            }

            dataDoc.InsertAfter(rootNode, dataDoc.DocumentElement);
            dataDoc.Save(file);
        }

    }
}
