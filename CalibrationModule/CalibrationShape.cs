using CamCore;
using System;
using System.Collections.Generic;
using System.Windows;

namespace CalibrationModule
{
    public class CalibrationShape
    {
        public List<Vector2> Points { get; set; }
        public uint Index { get; set; }
        public Point2D<int> GridPos { get; set; }
        public bool IsInvalid { get; set; }

        private Rect _bbox;
        public Rect BoundingBox
        {
            get { return _bbox; }
        }

        private Vector2 _gravityCenter;
        public Vector2 GravityCenter
        {
            get { return _gravityCenter; }
            set { _gravityCenter = value; }
        }

        public CalibrationShape()
        {
            Points = new List<Vector2>();
            _bbox = new Rect(1e6, 1e6, 0, 0);
            GridPos = new Point2D<int>(-1, -1);
            IsInvalid = false;
        }

        public void AddPoint(Vector2 point)
        {
            Points.Add(point);
            
            _bbox.X = Math.Min(_bbox.X, point.X);
            _bbox.Y = Math.Min(_bbox.Y, point.Y);
            _bbox.Width = Math.Max(_bbox.Width, point.X - _bbox.X);
            _bbox.Height = Math.Max(_bbox.Height, point.Y - _bbox.Y);
        }

        public Vector2 FindCenter()
        {
            double sumx = 0, sumy = 0;

            foreach(Vector2 point in Points)
            {
                sumx += point.X;
                sumy += point.Y;
            }

            _gravityCenter = new Vector2(sumx / Points.Count, sumy / Points.Count);

            return _gravityCenter;
        }
    }
}
