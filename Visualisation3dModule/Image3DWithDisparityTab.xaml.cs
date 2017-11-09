using CamControls;
using CamAlgorithms;
using CamCore;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System;
using CamAlgorithms.Triangulation;

namespace Visualisation3dModule
{
    public partial class Image3DWithDisparityTab : UserControl
    {
        public List<TriangulatedPoint> Points3D { get; set; }
        public DisparityMap DispMap { get { return _dispImage.Map; } }

        Image3DWindow _3dwindow;

        public Image3DWithDisparityTab()
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

            ColorImage image = null;
            if(_imageControl.ImageSource != null)
            {
                image = new ColorImage();
                image.FromBitmapSource(_imageControl.ImageSource);
            }
            else
            {
                MessageBox.Show("Need to set image");
                return;
            }

            if(DispMap == null)
            {
                MessageBox.Show("Need to set disparity map");
                return;
            }

            ClosePointsSegmentation segmentation = new ClosePointsSegmentation();
            segmentation.MaxDiffSquared = 10;
            segmentation.SegmentDisparity(DispMap);

            var segments = segmentation.Segments;
            var segmentAssignments = segmentation.SegmentAssignments;
            Point2D<int>[] segmentMin = new Point2D<int>[segments.Count];
            Point2D<int>[] segmentMax = new Point2D<int>[segments.Count];
            var segSort = new List<ImageSegmentation.Segment>(segments);
            segSort.Sort((s1, s2) => { return s2.Pixels.Count.CompareTo(s1.Pixels.Count); });

            for(int i = 0; i < segments.Count; ++i)
            {
                segmentMin[i] = new Point2D<int>(DispMap.ColumnCount + 1, DispMap.RowCount + 1);
                segmentMax[i] = new Point2D<int>(-1, -1);
            }

            // 1) Find segments sizes
            foreach(var point3d in Points3D)
            {
                Point2D<int> imgPoint = new Point2D<int>(y: (int)point3d.ImageLeft.Y, x: (int)point3d.ImageLeft.X);
                int idx = segmentAssignments[imgPoint.Y, imgPoint.X];
                if(idx >= 0)
                {
                    segmentMin[idx] = new Point2D<int>(y: Math.Min(segmentMin[idx].Y, imgPoint.Y),
                        x: Math.Min(segmentMin[idx].X, imgPoint.X));
                    segmentMax[idx] = new Point2D<int>(y: Math.Max(segmentMax[idx].Y, imgPoint.Y),
                        x: Math.Max(segmentMax[idx].X, imgPoint.X));
                }
            }

            // 2) For each segment create Dx surface model
            DXGridSurface[] surfaces = new DXGridSurface[segments.Count];
            for(int i = 0; i < segments.Count; ++i)
            {
                if(!(segmentMin[i].X > DispMap.ColumnCount || segmentMax[i].X < 0))
                {
                    surfaces[i] = new DXGridSurface(_3dwindow.Renderer.DxDevice,
                        segmentMax[i].Y - segmentMin[i].Y + 1, segmentMax[i].X - segmentMin[i].X + 1);
                }
            }

            // 3) For each point add it to surface
            for(int i = 0; i < Points3D.Count; ++i)
            {
                Point2D<int> imgPoint = new Point2D<int>(
                    y: (int)Points3D[i].ImageLeft.Y, x: (int)Points3D[i].ImageLeft.X);
                int idx = segmentAssignments[imgPoint.Y, imgPoint.X];
                if(idx >= 0 && surfaces[idx] != null)
                {
                    SharpDX.Vector3 pos = new SharpDX.Vector3(
                        (float)Points3D[i].Real.X, (float)Points3D[i].Real.Y, (float)Points3D[i].Real.Z);
                    SharpDX.Vector2 texCoords = new SharpDX.Vector2(
                        (float)Points3D[i].ImageLeft.X / (float)DispMap.ColumnCount,
                        (float)Points3D[i].ImageLeft.Y / (float)DispMap.RowCount);
                    SharpDX.Color4 color = new SharpDX.Color4(
                        (float)image[imgPoint.Y, imgPoint.X, RGBChannel.Red], 
                        (float)image[imgPoint.Y, imgPoint.X, RGBChannel.Green], 
                        (float)image[imgPoint.Y, imgPoint.X, RGBChannel.Blue], 1.0f);

                    surfaces[idx].AddVertex(imgPoint.Y - segmentMin[idx].Y, imgPoint.X - segmentMin[idx].X, pos, texCoords, color);
                }
            }

            // 4) Update surfaces
            // 5) For each surface create scene node centered on center
            // 6) Set each surface texture shader with image as texture (or use colored one)
            // 7) Add each surface to window
            for(int i = 0; i < surfaces.Length; ++i)
            {
                if(surfaces[i] != null)
                {
                    surfaces[i].UpdateBuffers();
                    if(surfaces[i].IndicesCount >= 3)
                    {
                        CamDX.DXSceneNode node = _3dwindow.Scene.RootNode.CreateChildNode();
                        node.AttachModel(surfaces[i]);
                        surfaces[i].Shader = _3dwindow.ResourcesManager.ShaderManager.GetShader("ColorShader_NoLight");
                    }
                }
            }
        }
    }
}

