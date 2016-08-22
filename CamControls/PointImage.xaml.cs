using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.ComponentModel;

namespace CamControls
{
    public partial class PointImage : UserControl
    {
        private List<PointImagePoint> _points;
        private PointImagePoint _tempPoint = new PointImagePoint() { IsNullPoint = true };
        private PointImagePoint _selectedPoint = new PointImagePoint() { IsNullPoint = true };

        private BitmapSource _baseImage;
        private WriteableBitmap _finalImage;
        private Image _imageControl;

        private bool _showPoints = false;

        public Color CrossColor { get; set; }
        public Color SelectedColor { get; set; }

        public BitmapSource ImageSource
        {
            get
            {
                return _baseImage;
            }
            set
            {
                BitmapSource old = _baseImage;
                _baseImage = value;
                if(_baseImage != null)
                {
                    _finalImage = new WriteableBitmap(_baseImage);
                    foreach(var point in Points)
                    {
                        DrawCross(point.Position, _finalImage, CrossColor);
                    }
                    _imageControl.Source = _finalImage;
                }
                else
                    _imageControl.Source = null;

                ImageSourceChanged?.Invoke(this, new ImageChangedEventArgs()
                {
                    NewImage = value,
                    OldImage = old,
                });
            }
        }
        public List<PointImagePoint> Points { get { return _points; } }

        public event EventHandler<ImageChangedEventArgs> ImageSourceChanged;

        public PointImagePoint SelectedPoint
        {
            get
            {
                return _selectedPoint;
            }
            set
            {
                if(_selectedPoint == value)
                    return;
                PointImagePoint oldpoint = _selectedPoint;
                _selectedPoint = value;
                if(_selectedPoint.IsNullPoint) // Deselect old point
                {
                    oldpoint.IsSelected = false;
                    if(!oldpoint.IsNullPoint)
                        DrawCross(oldpoint.Position, _finalImage, CrossColor);
                }
                else // Remove temp point if there's one and select point
                {
                    RemoveTempPoint();
                    DrawCross(_selectedPoint.Position, _finalImage, SelectedColor);
                    _selectedPoint.IsSelected = true;
                }
                SelectedPointChanged?.Invoke(this, new PointImageEventArgs()
                {
                    OldImagePoint = oldpoint,
                    NewImagePoint = _selectedPoint,
                    IsNewPointSelected = _selectedPoint.IsSelected
                });
            }
        }

        public event PointImageEventHandler SelectedPointChanged;

        public PointImagePoint TemporaryPoint
        {
            get
            {
                return _tempPoint;
            }
            set
            {
                _tempPoint = value;
                if(!value.IsNullPoint)
                    MoveTempPoint(_tempPoint.Position);
            }
        }

        public PointImage()
        {
            _points = new List<PointImagePoint>();
            _imageControl = new Image();

            InitializeComponent();

            _zoomControl.Child = _imageControl;

            TemporaryPoint = new PointImagePoint(true);
            SelectedPoint = new PointImagePoint(true);
            CrossColor = Colors.Black;
            SelectedColor = Colors.White;

            _imageControl.MouseLeftButtonDown += ImageMouseLeftButtonDown;
            _imageControl.MouseMove += ImageMouseMove;

            IsDeletionEnable = false;
        }

        private void PointPositionChanged(object sender, PointImageEventArgs e)
        {
            RemoveCross(e.OldPointPosition, _finalImage);
            if(!e.NewImagePoint.IsNullPoint)
            {
                if(e.NewImagePoint.IsSelected)
                    DrawCross(e.NewPointPosition, _finalImage, SelectedColor);
                else
                    DrawCross(e.NewPointPosition, _finalImage, CrossColor);
            }
        }

        public void AddPoint(PointImagePoint point)
        {
            point.ParentImage = this;
            _points.Add(point);
            point.PositionChanged += PointPositionChanged;
            DrawCross(point.Position, _finalImage, CrossColor);
        }

        public void RemovePoint(PointImagePoint point)
        {
            _points.Remove(point);
            point.PositionChanged -= PointPositionChanged;
            RemoveCross(point.Position, _finalImage);
        }

        public void ResetPoints()
        {
            _points.Clear();
            if(_finalImage != null)
            {
                _finalImage = new WriteableBitmap(_baseImage);
            }
        }

        public PointImagePoint FindPointByPosition(Point pos)
        {
            PointImagePoint p = null;
            foreach(PointImagePoint point in Points)
            {
                if(point.Position.Equals(pos))
                {
                    p = point;
                    break;
                }
            }
            return p;
        }

        public PointImagePoint FindPointByValue(object value)
        {
            PointImagePoint p = null;
            foreach(PointImagePoint point in Points)
            {
                if(point.Value.Equals(value))
                {
                    p = point;
                    break;
                }
            }
            return p;
        }

        public void SelectPointQuiet(PointImagePoint point) // Do not rise event
        {
            if(_selectedPoint == point)
                return;
            PointImagePoint oldpoint = _selectedPoint;
            _selectedPoint = point;
            if(_selectedPoint.IsNullPoint) // Deselect old point
            {
                oldpoint.IsSelected = false;
                if(!oldpoint.IsNullPoint)
                    DrawCross(oldpoint.Position, _finalImage, CrossColor);
            }
            else // Remove temp point if there's one and select point
            {
                RemoveTempPoint();
                DrawCross(_selectedPoint.Position, _finalImage, SelectedColor);
                _selectedPoint.IsSelected = true;
            }
        }

        public PointImagePoint AcceptTempPoint(object value)
        {
            if(_tempPoint.IsNullPoint)
                return null;
            PointImagePoint newPoint = new PointImagePoint()
            {
                Position = _tempPoint.Position,
                Value = value,
                ParentImage = this
            };
            _points.Add(newPoint);
            newPoint.PositionChanged += PointPositionChanged;
            TemporaryPoint.IsNullPoint = true;
            return newPoint;
        }

        private void MoveTempPoint(Point mpos)
        {
            if(_tempPoint.IsNullPoint)
            {
                _tempPoint.IsNullPoint = false;
            }
            else if(_tempPoint.Position.Equals(mpos))
                return;
            else
                RemoveCross(_tempPoint.Position, _finalImage);
            _tempPoint.Position = mpos;
            DrawCross(_tempPoint.Position, _finalImage, CrossColor);
        }

        private void RemoveTempPoint()
        {
            if(_tempPoint.IsNullPoint)
                return;
            RemoveCross(_tempPoint.Position, _finalImage);
            _tempPoint.IsNullPoint = true;
        }

        private void DrawCross(Point pos, WriteableBitmap img, Color color)
        {
            if(_showPoints)
            {
                if(pos.X > _baseImage.PixelWidth - 5 || pos.Y > _baseImage.PixelHeight - 5 ||
                    pos.X < 5 || pos.Y < 5)
                    return;

                img.DrawLine((int)Math.Max(pos.X - 2, 0), (int)Math.Max(pos.Y - 2, 0),
                    (int)Math.Min(pos.X + 2, img.PixelWidth), (int)Math.Min(pos.Y + 2, img.PixelHeight), color);
                img.DrawLine((int)Math.Min(pos.X + 2, img.PixelWidth), (int)Math.Max(pos.Y - 2, 0),
                    (int)Math.Max(pos.X - 2, 0), (int)Math.Min(pos.Y + 2, img.PixelHeight), color);
            }
        }

        private void RemoveCross(Point point, WriteableBitmap img)
        {
            if(point.X > _baseImage.PixelWidth - 5 || point.Y > _baseImage.PixelHeight - 5 ||
                point.X < 5 || point.Y < 5)
                return;

            Int32[] cross = new Int32[25];
            _baseImage.CopyPixels(new Int32Rect((int)point.X - 2, (int)point.Y - 2, 5, 5), cross, 20, 0);
            img.WritePixels(new Int32Rect((int)point.X - 2, (int)point.Y - 2, 5, 5), cross, 20, 0);
            foreach(var p in _points)
            {
                if(CrossHitTest(p.Position, point))
                {
                    DrawCross(p.Position, img, CrossColor);
                }
            }
        }

        private bool PointHitTest(Point hitting, Point hit)
        {
            return !(hitting.X < hit.X - 2 || hitting.X > hit.X + 2 || hitting.Y < hit.Y - 2 || hitting.Y > hit.Y + 2);
        }

        private bool CrossHitTest(Point hitting, Point hit)
        {
            return PointHitTest(new Point(hitting.X - 2, hitting.Y - 2), hit) ||
                PointHitTest(new Point(hitting.X + 2, hitting.Y - 2), hit) ||
                PointHitTest(new Point(hitting.X - 2, hitting.Y + 2), hit) ||
                PointHitTest(new Point(hitting.X + 2, hitting.Y + 2), hit);
        }

        private void LoadImage(object sender, RoutedEventArgs e)
        {
            string imgPath = CamCore.FileOperations.GetFilePath("BMP|*.bmp|GIF|*.gif|JPG|*.jpg;*.jpeg|PNG|*.png|"
                                + "All Images|*.bmp;*.jpg;*.jpeg;*.png;*.gif");
            if(imgPath != null)
            {
                ImageSource = new BitmapImage(new Uri(imgPath, UriKind.Absolute));
            }
        }

        protected void ImageMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Get mouse pos and transform to image source coords
            Point mpos = Mouse.GetPosition(_imageControl);
            mpos.X *= _finalImage.PixelWidth / _imageControl.ActualWidth;
            mpos.Y *= _finalImage.PixelHeight / _imageControl.ActualHeight;
            // If isn't within image then return
            if(mpos.X < 2 || mpos.Y < 2 &&
                mpos.X > _finalImage.PixelWidth - 3 ||
                mpos.Y > _finalImage.PixelHeight - 3)
            {
                SelectedPoint = new PointImagePoint() { IsNullPoint = true };
                return;
            }
            // Check if mouse hovers over point -> list of such points
            List<PointImagePoint> hoverPoints = new List<PointImagePoint>();
            foreach(var point in _points)
            {
                if(PointHitTest(mpos, point.Position))
                {
                    hoverPoints.Add(point);
                }
            }
            if(hoverPoints.Count > 0)
            {
                // If it hovers over ones then find closest one
                PointImagePoint hoverPoint = new PointImagePoint();
                double minDist = 999999;
                foreach(var point in hoverPoints)
                {
                    double dist = (point.Position.X - mpos.X) + (point.Position.Y - mpos.Y);
                    if(dist < minDist)
                    {
                        hoverPoint = point;
                        minDist = dist;
                    }
                }
                if(hoverPoint != _selectedPoint)// if clicked not on selected one, deselect it and select new one
                {
                    SelectedPoint = new PointImagePoint() { IsNullPoint = true };
                    SelectedPoint = hoverPoint;
                    _imageMouseXPos.Text = hoverPoint.Position.X.ToString();
                    _imageMouseYPos.Text = hoverPoint.Position.Y.ToString();
                }
            }
            else
            {
                // If cursor do not hover over any, set tempPoint as mpos
                SelectedPoint = new PointImagePoint() { IsNullPoint = true };
                MoveTempPoint(mpos);
                _imageMouseXPos.Text = mpos.X.ToString();
                _imageMouseYPos.Text = mpos.Y.ToString();
            }

            base.OnMouseLeftButtonDown(e);
        }

        protected void ImageMouseMove(object sender, MouseEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
            {
                // Get mouse pos and transform to image source coords
                Point mpos = Mouse.GetPosition(_imageControl);
                mpos.X *= _finalImage.PixelWidth / _imageControl.ActualWidth;
                mpos.Y *= _finalImage.PixelHeight / _imageControl.ActualHeight;
                // If isn't within image then return
                if(mpos.X < 2 || mpos.Y < 2 &&
                    mpos.X > _finalImage.PixelWidth - 3 || mpos.Y > _finalImage.PixelWidth - 3)
                    return;
                // States: unselected -> drag temp point
                //         selected -> drag selected
                if(_selectedPoint.IsNullPoint)
                {
                    MoveTempPoint(mpos);
                }
                else
                {
                    _selectedPoint.Position = mpos;
                    _imageControl.Source = _finalImage;
                }
                _imageMouseXPos.Text = mpos.X.ToString();
                _imageMouseYPos.Text = mpos.Y.ToString();
            }
            base.OnMouseMove(e);
        }

        // Toggle point showing
        private void _cbTogglePoints_Checked(object sender, RoutedEventArgs e)
        {
            _showPoints = true;
            _finalImage = new WriteableBitmap(_baseImage);
            // Redraw all points
            foreach(var point in _points)
            {
                if(point.IsSelected)
                    DrawCross(point.Position, _finalImage, SelectedColor);
                else
                    DrawCross(point.Position, _finalImage, CrossColor);
            }
            _imageControl.Source = _finalImage;
        }

        private void _cbTogglePoints_Unchecked(object sender, RoutedEventArgs e)
        {
            _showPoints = false;
            _imageControl.Source = _baseImage;
        }

        private void _butChooseColor_Click(object sender, RoutedEventArgs e)
        {
            ColorPicker colorPicker = new ColorPicker();
            colorPicker.Title = "Choose Cross Color";
            colorPicker.PickedColor = CrossColor;
            colorPicker.ShowDialog();
            if(colorPicker.DialogResult.HasValue && colorPicker.DialogResult.Value == true)
            {
                CrossColor = colorPicker.PickedColor;
                SelectedColor = Color.FromArgb(CrossColor.A, (byte)(255 - CrossColor.R), (byte)(255 - CrossColor.G), (byte)(255 - CrossColor.B));
            }
        }

        #region keyboard input

        // If true selected point can be deleted with delete key
        public bool IsDeletionEnable { get; set; }

        // On delete pressed if deletion is enabled, delete and raise ondelete event
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if(e.Key == Key.Delete)
            {
                if(!SelectedPoint.IsNullPoint)
                {
                    var removed = SelectedPoint;
                    RemovePoint(SelectedPoint);
                    SelectedPoint = new PointImagePoint(true);
                    if(PointDeleted != null)
                        PointDeleted(this, new PointImageEventArgs()
                        {
                            OldImagePoint = removed
                        });
                }
            }
            base.OnKeyDown(e);
        }

        public event PointImageEventHandler PointDeleted;

        #endregion
    }
}
