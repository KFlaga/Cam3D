using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using CamControls;
using CamCore;
using CamImageProcessing;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Text;
using System.Xml;
using System.IO;

namespace CalibrationModule
{
    public partial class CrossCalibrationTab : UserControl
    {
        public List<CalibrationPoint> CalibrationPointsLeft { get; set; } = new List<CalibrationPoint>();
        public List<CalibrationPoint> CalibrationPointsRight { get; set; } = new List<CalibrationPoint>();

        public List<RealGridData> GridsLeft { get; set; } = new List<RealGridData>();
        public List<RealGridData> GridsRight { get; set; } = new List<RealGridData>();

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

        private void Calibrate(object sender, RoutedEventArgs e)
        {
            if(CalibrationData.Data.IsCamLeftCalibrated == false ||
                CalibrationData.Data.IsCamRightCalibrated == false)
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
    }
}
