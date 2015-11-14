using System.Windows;

namespace CamControls
{
    // Universal event args for events related to point image
    public class PointImageEventArgs
    {
        public Point NewPointPosition { get; set; } // used if position changed
        public Point OldPointPosition { get; set; }
        public PointImagePoint NewImagePoint { get; set; } // used when point of interest changes and if point remains same as well
        public PointImagePoint OldImagePoint { get; set; } // used only when point changes - its the old value (if removed it is point removed)
        public bool IsNewPointSelected { get; set; } // true if selection changed and point is selected, false if point is unselected
        public bool IsNewPointNull { get; set; }  // true if point of interest changes to null
    }

    public delegate void PointImageEventHandler(object sender, PointImageEventArgs e);
}
