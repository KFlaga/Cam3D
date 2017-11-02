using CamCore;
using System.Collections.Generic;

namespace CalibrationModule.PointsExtraction
{
    public class CalibrationShape
    {
        public List<Vector2> Points { get; set; } = new List<Vector2>();
        public int Index { get; set; } = -1;
        public IntVector2 GridPos { get; set; } = new IntVector2(-1, -1);
        public bool IsInvalid { get { return Index == -1; } }
        
        private Vector2 _gravityCenter = new Vector2(-1, -1);
        public Vector2 GravityCenter
        {
            get { return _gravityCenter; }
            set { _gravityCenter = value; }
        }

        public int Area { get { return Points.Count; } }

        public void AddPoint(Vector2 point)
        {
            Points.Add(point);
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
