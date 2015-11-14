using System.Windows;

namespace CamControls
{
    public class PointImagePoint
    {
        private Point _position;
        public Point Position 
        {
            get
            {
                return _position;
            }
            set
            {
                if (_position != value)
                {
                    Point oldpos = _position;
                    _position = value;
                    if (PositionChanged != null)
                        PositionChanged(this, new PointImageEventArgs()
                        {
                            NewImagePoint = this,
                            NewPointPosition = _position,
                            OldPointPosition = oldpos
                        });
                }
            }
        }

        public object Value { get; set; }
        public PointImage ParentImage { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    if (IsSelectedChanged != null)
                        IsSelectedChanged(this, new PointImageEventArgs()
                        {
                            IsNewPointSelected = _isSelected
                        });
                }
            }
        }

        private bool _isNull = false;
        public bool IsNullPoint
        {
            get { return _isNull; }
            set
            {
                if (_isNull != value)
                {
                    _isNull = value;
                    if (IsNullChanged != null)
                        IsNullChanged(this, new PointImageEventArgs()
                        {
                            IsNewPointNull = _isNull
                        });
                }
            }
        }
        
        public PointImagePoint(bool isNull = false)
        {
            _isNull = isNull;
        }

        public event PointImageEventHandler PositionChanged;
        public event PointImageEventHandler IsSelectedChanged;
        public event PointImageEventHandler IsNullChanged;
    }
}
