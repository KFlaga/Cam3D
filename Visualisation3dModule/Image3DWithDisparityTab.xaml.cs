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
            TriangulatedPointsManagerWindow pointsManager = new TriangulatedPointsManagerWindow();
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

            List<ImageSegmentation.Segment> segments = segmentation.Segments;
            int[,] segmentAssignments = segmentation.SegmentAssignments;
            
            var assignmentsMap = new int[segments.Count];
            var segmentsReduced = new List<ImageSegmentation.Segment>();
            for(int s = 0; s < segments.Count; ++s)
            {
                if(segments[s].Pixels.Count > 6)
                {
                    assignmentsMap[segments[s].SegmentIndex] = segmentsReduced.Count;
                    segmentsReduced.Add(segments[s]);
                }
                else
                {
                    assignmentsMap[segments[s].SegmentIndex] = -1;
                }
            }
            segments = segmentsReduced;

            IntPoint2[] segmentMin = new IntPoint2[segments.Count];
            IntPoint2[] segmentMax = new IntPoint2[segments.Count];
            for(int i = 0; i < segments.Count; ++i)
            {
                segmentMin[i] = new IntPoint2(DispMap.ColumnCount + 1, DispMap.RowCount + 1);
                segmentMax[i] = new IntPoint2(-1, -1);
            }

            // 1) Find segments sizes
            foreach(var point3d in Points3D)
            {
                IntPoint2 imgPoint = new IntPoint2(y: (int)point3d.ImageLeft.Y, x: (int)point3d.ImageLeft.X);
                int idx = segmentAssignments[imgPoint.Y, imgPoint.X];
                if(idx >= 0 && assignmentsMap[idx] >= 0)
                {
                    idx = assignmentsMap[idx];
                    segmentMin[idx] = new IntPoint2(y: Math.Min(segmentMin[idx].Y, imgPoint.Y),
                        x: Math.Min(segmentMin[idx].X, imgPoint.X));
                    segmentMax[idx] = new IntPoint2(y: Math.Max(segmentMax[idx].Y, imgPoint.Y),
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
            double maxZ = -1e12;
            double minZ = 1e12;
            for(int i = 0; i < Points3D.Count; ++i)
            {
                IntPoint2 imgPoint = new IntPoint2(
                    y: (int)Points3D[i].ImageLeft.Y, x: (int)Points3D[i].ImageLeft.X);
                int idx = segmentAssignments[imgPoint.Y, imgPoint.X];
                if(idx >= 0 && assignmentsMap[idx] >= 0 && surfaces[assignmentsMap[idx]] != null)
                {
                    idx = assignmentsMap[idx];
                    SharpDX.Vector3 pos = new SharpDX.Vector3(
                        (float)Points3D[i].Real.X, (float)Points3D[i].Real.Y, (float)Points3D[i].Real.Z);
                    SharpDX.Vector2 texCoords = new SharpDX.Vector2(
                        (float)Points3D[i].ImageLeft.X / (float)DispMap.ColumnCount,
                        (float)Points3D[i].ImageLeft.Y / (float)DispMap.RowCount);
                    SharpDX.Color4 color = new SharpDX.Color4(
                        (float)image[imgPoint.Y, imgPoint.X, RGBChannel.Red], 
                        (float)image[imgPoint.Y, imgPoint.X, RGBChannel.Green], 
                        (float)image[imgPoint.Y, imgPoint.X, RGBChannel.Blue], 1.0f);

                    surfaces[idx].AddVertex(
                        imgPoint.Y - segmentMin[idx].Y, 
                        imgPoint.X - segmentMin[idx].X, 
                        pos, texCoords, color);

                    maxZ = Math.Max(maxZ, Points3D[i].Real.Z);
                    minZ = Math.Min(minZ, Points3D[i].Real.Z);
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

            // 8) set camera
            bool reversed = maxZ < 0.0;
            float zscale = (float)(maxZ - minZ);
            _3dwindow.Camera.FarBound = 4.0f * zscale;
            _3dwindow.Camera.NearBound = 1.0f;
            _3dwindow.Camera.Position = new SharpDX.Vector3(
                0.0f, 0.0f, reversed ? (float)maxZ + zscale : (float)minZ - zscale
            );
            _3dwindow.Camera.LookAt = new SharpDX.Vector3(0.0f, 0.0f, reversed ? -1.0f : 1.0f);
        }
    }
}

