using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CamControls
{
    public partial class ZoomingScrollControl : UserControl
    {
        public static readonly DependencyProperty ChildProperty =
            DependencyProperty.Register("Child", typeof(FrameworkElement), typeof(ZoomingScrollControl), new PropertyMetadata());

        private FrameworkElement _child = null;
        public FrameworkElement Child
        {
            get 
            { 
                return (FrameworkElement)GetValue(ChildProperty); 
            }
            set
            {
                if (value != null && value != this.Child)
                    this.Initialize(value);
                _zoomBorder.Child = value;
                SetValue(ChildProperty, value);
            }
        }

        private ScaleTransform _sTrans;
        private TranslateTransform _tTrans;

        public ZoomingScrollControl()
        {
            InitializeComponent();
        }

        public void Initialize(FrameworkElement element)
        {
            this._child = element;
            if (_child != null)
            {
                TransformGroup group = new TransformGroup();
                _sTrans = new ScaleTransform();
                group.Children.Add(_sTrans);
                _tTrans = new TranslateTransform();
                group.Children.Add(_tTrans);
                _child.RenderTransform = group;
                _child.RenderTransformOrigin = new Point(0.5, 0.5);
                this.MouseWheel += child_MouseWheel;
            }
        }

        public void Reset()
        {
            if (_child != null)
            {
                // reset zoom
                _sTrans.ScaleX = 1.0;
                _sTrans.ScaleY = 1.0;
                // reset pan
                _tTrans.X = 0.0;
                _tTrans.Y = 0.0;
            }
        }

        public void Zoom(double zoom, Point zoomCenter) // Zoom Center on Screen
        {
            if (_child != null)
            {
                _sTrans.ScaleX += zoom;
                _sTrans.ScaleY += zoom;

                _tTrans.X = _tTrans.X * (_sTrans.ScaleX / (_sTrans.ScaleX - zoom)); // zoom in center of screen
                _tTrans.Y = _tTrans.Y * (_sTrans.ScaleY / (_sTrans.ScaleY - zoom));

                if (Child.ActualHeight * _sTrans.ScaleY > this.ActualHeight - 20) // Content is higher than screen
                {
                    _vScroll.IsEnabled = true;
                }
                else
                {
                    _tTrans.Y = 0;
                    _vScroll.IsEnabled = false;
                }
                if (Child.ActualWidth * _sTrans.ScaleX > this.ActualWidth - 20) // Content is wider than screen
                {
                    _hScroll.IsEnabled = true;
                }
                else
                {
                    _tTrans.X = 0;
                    _hScroll.IsEnabled = false;
                }

                UpdateScrolls();
            }
        }

        public void ScrollVertical(double value)
        {
            // Inverse of UpdateScrolls()
            double distFromCenterNormY = 0.5 - value;
            _tTrans.Y = distFromCenterNormY*(Child.ActualHeight*_sTrans.ScaleY - this.ActualHeight + 20);
        }

        public void ScrollHorizontal(double value)
        {
            // Inverse of UpdateScrolls()
            double distFromCenterNormX = 0.5 - value;
            _tTrans.X = distFromCenterNormX*(Child.ActualWidth*_sTrans.ScaleX - this.ActualWidth + 20);
        }

        private void UpdateScrolls()
        {
            if (_hScroll.IsEnabled)
            {
                // normalized (0-1) distance from center of zoomBorder to center of image -> -0.5 corresponds to left edge, 0.5 to right one 
                double distFromCenterNormX = _tTrans.X / (Child.ActualWidth*_sTrans.ScaleX - this.ActualWidth + 20);
                // move scroll bar in a way that on 0.5 center of image coincides with center of zoomBorder
                _hScroll.Value = 0.5 - distFromCenterNormX;
            }
            if (_vScroll.IsEnabled)
            {
                double distFromCenterNormY = _tTrans.Y / (Child.ActualHeight*_sTrans.ScaleY - this.ActualHeight + 20);
                _vScroll.Value = 0.5 - distFromCenterNormY;
            }
        }

        public Point TransformScreenToChild(Point screenPoint)
        {
            Point imagePoint = new Point();
            imagePoint.X = -((_zoomBorder.ActualWidth/2 - Child.ActualWidth / 2 + _tTrans.X) -// position od zoomed image (0,0) in screen coords
                screenPoint.X)/_sTrans.ScaleX; // distance from spoint to zoomed image (0,0) and unzoom
            imagePoint.Y = (screenPoint.Y - (_zoomBorder.ActualHeight / 2 - 
                Child.ActualHeight / 2 + _tTrans.Y)) / _sTrans.ScaleY;
            return imagePoint;
        }

        public Point TransformScreenToChildZoomed(Point screenPoint)
        {
            Point imagePoint = new Point();
            imagePoint.X = -((_zoomBorder.ActualWidth / 2 - Child.ActualWidth / 2 + _tTrans.X) -// position od zoomed image (0,0) in screen coords
                screenPoint.X); // distance from spoint to zoomed image (0,0)
            imagePoint.Y = (screenPoint.Y - (_zoomBorder.ActualHeight / 2 -
                Child.ActualHeight / 2 + _tTrans.Y));
            return imagePoint;
        }

        private void child_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double zoom = e.Delta > 0 ? .2 : -.2;
            if (!(e.Delta > 0) && (_sTrans.ScaleX < .4 || _sTrans.ScaleY < .4))
                return;

            //Point relative = e.GetPosition(_zoomBorder);

            // Point screenCenterInImageCoords = new Point(_tTrans.X,_tTrans.Y);
            Zoom(zoom, new Point(_zoomBorder.ActualWidth/2, _zoomBorder.ActualHeight/2));
        }
        /* Unused
        private void child_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_child != null)
            {
                var _tTrans = GetTranslateTransform(_child);
                _start = e.GetPosition(this);
                _origin = new Point(_tTrans.X, _tTrans.Y);
                this.Cursor = Cursors.Hand;
                _child.CaptureMouse();
            }
        }

        private void child_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_child != null)
            {
                _child.ReleaseMouseCapture();
                this.Cursor = Cursors.Arrow;
            }
        }

        void child_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.Reset();
        }

        private void child_MouseMove(object sender, MouseEventArgs e)
        {
            if (_child != null)
            {
                if (_child.IsMouseCaptured)
                {
                    var _tTrans = GetTranslateTransform(_child);
                    Vector v = _start - e.GetPosition(this);
                    _tTrans.X = _origin.X - v.X;
                    _tTrans.Y = _origin.Y - v.Y;
                }
            }
        }
        */

        private void _hScroll_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ScrollHorizontal(e.NewValue);
        }

        private void _vScroll_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ScrollVertical(e.NewValue);
        }
    }
    
}
