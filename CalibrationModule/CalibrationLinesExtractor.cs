using CamCore;
using System.Collections.Generic;

namespace CalibrationModule
{
    public interface ICalibrationLinesExtractor
    {
        List<List<Vector2>> CalibrationLines { get; }
        void ExtractLines();
    }

    // Uses CalibrationPoints and their info on grid coords to create calibration lines
    // points sets ( assumes that points in same row/column lies on line )
    public class ShapeGridLinesExtractor : ICalibrationLinesExtractor
    {
        public CalibrationGrid CalibGrid { get; set; }
        public List<List<Vector2>> CalibrationLines { get; private set; }

        public void ExtractLines()
        {
            if(CalibGrid != null)
            {
                CalibrationLines = new List<List<Vector2>>(CalibGrid.RowCount + CalibGrid.ColumnCount);

                // For each row extract line from valid shapes
                for(int r = 0; r < CalibGrid.RowCount; ++r)
                {
                    var line = new List<Vector2>(CalibGrid.ColumnCount);
                    for(int c = 0; c < CalibGrid.ColumnCount; ++c)
                    {
                        if(CalibGrid[r, c] != null && CalibGrid[r, c].IsInvalid == false)
                        {
                            line.Add(CalibGrid[r, c].GravityCenter);
                        }
                    }
                    if(line.Count >= 3)
                        CalibrationLines.Add(line);
                }

                // Same for each column
                for(int c = 0; c < CalibGrid.ColumnCount; ++c)
                {
                    var line = new List<Vector2>(CalibGrid.RowCount);
                    for(int r = 0; r < CalibGrid.RowCount; ++r)
                    {
                        if(CalibGrid[r, c] != null && CalibGrid[r, c].IsInvalid == false)
                        {
                            line.Add(CalibGrid[r, c].GravityCenter);
                        }
                    }
                    if(line.Count >= 3)
                        CalibrationLines.Add(line);
                }
            }
        }
    }
}
