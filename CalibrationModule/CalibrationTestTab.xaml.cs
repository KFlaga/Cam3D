using CamControls;
using CamAlgorithms;
using CamCore;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using CamAlgorithms.ImageMatching;
using System;
using System.Windows.Media.Imaging;
using CamAlgorithms.Calibration;

namespace CalibrationModule
{
    public partial class CalibrationTestTab : UserControl
    {
        public delegate void ImagePointTest(Vector2 pointPosition, bool onLeftImage, bool reset);
        public enum TestModes
        {
            None,
            EpiLineTest
        }

        ImagePointTest _currentPointTest;
        TestModes _testMode;
        public TestModes CurrentTestMode
        {
            get { return _testMode; }
            set
            {
                if(_testMode != value)
                {
                    _currentPointTest(null, true, true);
                    _currentPointTest(null, false, true);
                }
                _testMode = value;
                switch(_testMode)
                {
                    case TestModes.EpiLineTest:
                        _currentPointTest = EpiLinePointTest;
                        break;
                    default:
                        _currentPointTest = EmptyPointTest;
                        break;
                }
            }
        }

        public CalibrationTestTab()
        {
            InitializeComponent();

            _testMode = TestModes.None;
            _currentPointTest = EmptyPointTest;

            _camImageFirst.TemporaryPoint.IsNullChanged += LeftImageTempPointChanged;
            _camImageSec.TemporaryPoint.IsNullChanged += RightImageTempPointChanged;
        }

        private void LeftImageTempPointChanged(object sender, PointImageEventArgs e)
        {
            if(e.IsNewPointNull == false)
            {
                e.NewImagePoint.PositionChanged += LeftImagePointPositionChanged;
                Vector2 position = new Vector2(e.NewPointPosition.X, e.NewPointPosition.Y);
                _currentPointTest(position, true, false);
            }
            else
                _currentPointTest(null, true, true);
        }

        private void LeftImagePointPositionChanged(object sender, PointImageEventArgs e)
        {
            Vector2 position = new Vector2(e.NewPointPosition.X, e.NewPointPosition.Y);
            _currentPointTest(position, true, false);
        }

        private void RightImageTempPointChanged(object sender, PointImageEventArgs e)
        {
            if(e.IsNewPointNull == false)
            {
                e.NewImagePoint.PositionChanged += RightImagePointPositionChanged;
                Vector2 position = new Vector2(e.NewPointPosition.X, e.NewPointPosition.Y);
                _currentPointTest(position, false, false);
            }
            else
                _currentPointTest(null, false, true);
        }

        private void RightImagePointPositionChanged(object sender, PointImageEventArgs e)
        {
            Vector2 position = new Vector2(e.NewPointPosition.X, e.NewPointPosition.Y);
            _currentPointTest(position, false, false);
        }

        private void _butEpiLineTest_Click(object sender, RoutedEventArgs e)
        {
            CurrentTestMode = TestModes.EpiLineTest;
        }

        private void EmptyPointTest(Vector2 pointPosition, bool onLeftImage, bool reset)
        {

        }

        private void EpiLinePointTest(Vector2 pointPosition, bool onLeftImage, bool reset)
        {
            if(CameraPair.Data.IsCamLeftCalibrated == false ||
                CameraPair.Data.IsCamRightCalibrated == false)
            {
                return;
            }

            if(reset)
            {
                if(onLeftImage)
                    _camImageSec.ResetPoints();
                else
                    _camImageFirst.ResetPoints();
                return;
            }
            EpiLinePointTest(null, onLeftImage, true);

            EpiLine epiLine;
            if(onLeftImage)
                epiLine = EpiLine.FindCorrespondingEpiline_LineOnRightImage(pointPosition, CameraPair.Data.Fundamental);
            else
                epiLine = EpiLine.FindCorrespondingEpiline_LineOnLeftImage(pointPosition, CameraPair.Data.Fundamental);

            PointImage image;
            if(onLeftImage && _camImageSec.ImageSource != null)
            {
                image = _camImageSec;
            }
            else if(onLeftImage == false && _camImageFirst.ImageSource != null)
            {
                image = _camImageFirst;
            }
            else
                return;

            int rows = image.ImageSource.PixelHeight;
            int cols = image.ImageSource.PixelWidth;
            if(epiLine.IsHorizontal())
            {
                for(int xm = 0; xm < cols; ++xm)
                {
                    image.AddPoint(new PointImagePoint()
                    {
                        Position = new Point(xm, pointPosition.Y)
                    });
                }
            }
            else if(epiLine.IsVertical())
            {
                for(int ym = 0; ym < rows; ++ym)
                {
                    image.AddPoint(new PointImagePoint()
                    {
                        Position = new Point(pointPosition.X, ym)
                    });
                }
            }
            else
            {
                int xmax = epiLine.FindXmax(rows, cols);
                xmax = xmax - 2;

                for(int xm = epiLine.FindX0(rows); xm < xmax; ++xm)
                {
                    double ym0 = epiLine.FindYd(xm);
                    double ym1 = epiLine.FindYd(xm + 1);
                    double ymax = Math.Max(ym0, ym1);
                    for(int ym = (int)Math.Min(ym0, ym1); ym <= (int)ymax; ++ym)
                    {
                        image.AddPoint(new PointImagePoint()
                        {
                            Position = new Point(xm, ym)
                        });
                    }
                }
            }
            image.UpdateImage();
        }
    }
}

