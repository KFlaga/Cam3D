using CamControls;
using CamAlgorithms;
using CamCore;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using CamAlgorithms.Triangulation;

namespace Image3DModule
{
    public partial class Image3DFromPointsTab : UserControl
    {
        public List<TriangulatedPoint> Points3D { get; set; }

        Image3DWindow _3dwindow;

        private ParametrizedProcessorsSelectionWindow _featuresMatchOpts;

        public Image3DFromPointsTab()
        {
            Points3D = new List<TriangulatedPoint>();
            InitializeComponent();
        }

        private void ManagePoints(object sender, RoutedEventArgs e)
        {
            Points3DManagerWindow pointsManager = new Points3DManagerWindow();
            pointsManager.Points = Points3D;
            bool? res = pointsManager.ShowDialog();
            if(res != null && res == true)
            {
                Points3D = pointsManager.Points;
            }
        }

        private void Build3DImage(object sender, RoutedEventArgs e)
        {
            if(_3dwindow == null)
            {
                _3dwindow = new Image3DWindow();
                _3dwindow.Show();
            }
            else
            {
                if(_3dwindow.IsVisible)
                    _3dwindow.Close();
                _3dwindow = new Image3DWindow();
                _3dwindow.Show();
            }

            _3dwindow.ResetPoints();

            ColorImage image = null;
            if(_imageControl.ImageSource != null)
            {
                image = new ColorImage();
                image.FromBitmapSource(_imageControl.ImageSource);
            }

            foreach(var point in Points3D)
            {
                SharpDX.Vector3 pos = new SharpDX.Vector3((float)point.Real.X, (float)point.Real.Y, (float)point.Real.Z);
                SharpDX.Color4 color = new SharpDX.Color4(1.0f);
                if(image != null)
                {
                    if(!(point.ImageLeft.X < 0.0 ||
                        point.ImageLeft.X > image.ColumnCount ||
                        point.ImageLeft.Y < 0.0 ||
                        point.ImageLeft.Y > image.RowCount))
                    {
                        color = new SharpDX.Color4(
                            (float)image[(int)point.ImageLeft.Y, (int)point.ImageLeft.X, RGBChannel.Red],
                            (float)image[(int)point.ImageLeft.Y, (int)point.ImageLeft.X, RGBChannel.Green],
                            (float)image[(int)point.ImageLeft.Y, (int)point.ImageLeft.X, RGBChannel.Blue],
                            1.0f);
                    }
                }

                _3dwindow.AddPointCube(pos, color);
            }
        }
    }
}

