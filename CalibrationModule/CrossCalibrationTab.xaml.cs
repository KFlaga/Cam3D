using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using CamControls;
using CamCore;
using System.Xml;
using System.IO;
using CamAlgorithms.Calibration;

namespace CalibrationModule
{
    public partial class CrossCalibrationTab : UserControl
    {
        public List<CalibrationPoint> CalibrationPointsLeft { get; set; } = new List<CalibrationPoint>();
        public List<CalibrationPoint> CalibrationPointsRight { get; set; } = new List<CalibrationPoint>();

        public List<RealGridData> Grids { get; set; } = new List<RealGridData>();

        public List<Vector2> MatchedPointsLeft { get; set; } = new List<Vector2>();
        public List<Vector2> MatchedPointsRight { get; set; } = new List<Vector2>();

        public List<Vector2Pair> CalibrationPointsMatched { get; set; } = new List<Vector2Pair>();

        CrossCalibrationRefiner _calibrator = new CrossCalibrationRefiner();

        public CrossCalibrationTab()
        {
            InitializeComponent();
            
        }
        
        private void ManageGridsLeft(object sender, RoutedEventArgs e)
        {
            RealGridsManagerWindow gridsManager = new RealGridsManagerWindow();
            gridsManager.RealGrids = Grids;
            bool? res = gridsManager.ShowDialog();
            if(res != null && res == true)
            {
                Grids = gridsManager.RealGrids;
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
            //MatchedPointsManagerWindow pointsManager = new MatchedPointsManagerWindow();
            //pointsManager.Points = MatchedPointsLeft;
            //bool? res = pointsManager.ShowDialog();
            //if(res != null && res == true)
            //{
            //    MatchedPointsLeft = pointsManager.Points;
            //}
        }

        private void ManageMatchedPointsRight(object sender, RoutedEventArgs e)
        {
            //MatchedPointsManagerWindow pointsManager = new MatchedPointsManagerWindow();
            //pointsManager.Points = MatchedPointsRight;
            //bool? res = pointsManager.ShowDialog();
            //if(res != null && res == true)
            //{
            //    MatchedPointsRight = pointsManager.Points;
            //}
        }

        private void SaveCalibMatched(object sender, RoutedEventArgs e)
        {
            CamCore.FileOperations.SaveToFile(SaveCalibMatched, "Xml File|*.xml");
        }

        private void SaveCalibTriangulated(object sender, RoutedEventArgs e)
        {
            CamCore.FileOperations.SaveToFile(SaveCalibTriangulated, "Xml File|*.xml");
        }

        private void Calibrate(object sender, RoutedEventArgs e)
        {
            if(CameraPair.Data.AreCalibrated)
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
            _calibrator.CalibGrids = Grids;
           // _calibrator.GridsRight = GridsRight;
            _calibrator.MatchedPointsLeft = MatchedPointsLeft;
            _calibrator.MatchedPointsRight = MatchedPointsRight;

            AlgorithmWindow algWindow = new AlgorithmWindow(_calibrator);
            algWindow.Show();
        }
        
        public void SaveCalibMatched(Stream file, string path)
        {
            CalibrationPointsMatched = new List<Vector2Pair>();
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
                    CalibrationPointsMatched.Add(new Vector2Pair()
                    {
                        V1 = cleft.Img,
                        V2 = cright.Img
                    });
                }
            }

            XmlSerialisation.SaveToFile(CalibrationPointsMatched, file);
        }
        
        public void SaveCalibTriangulated(Stream file, string path)
        {
            var triangulated = new List<TriangulatedPoint>();
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
                    triangulated.Add(new TriangulatedPoint()
                    {
                        ImageLeft = cleft.Img,
                        ImageRight = cright.Img,
                        Real = Grids[cleft.GridNum].GetRealFromCell(cleft.RealGridPos)
                    });
                }
            }

            XmlSerialisation.SaveToFile(triangulated, file);
        }
    }
}
