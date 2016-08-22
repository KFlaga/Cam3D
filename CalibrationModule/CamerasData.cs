using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalibrationModule
{
    public class CamerasData
    {
        public static Matrix<double> Cam1Matrix { get; set; }
        public static Matrix<double> Cam2Matrix { get; set; }
        public static List<CalibrationPoint> Cam1Points { get; set; }
        public static List<CalibrationPoint> Cam2Points { get; set; }
        public static List<RealGridData> Cam1Grids { get; set; }
        public static List<RealGridData> Cam2Grids { get; set; }

        public static bool IsCam1Calibrated { get; set; }
        public static bool IsCam2Calibrated { get; set; }

        public static IEnumerable<string> SaveData()
        {
            // How to save data
            // First line: header -> CALIBRATION
            // NEXT LINE: IsCam1Calibrated (T/F)
            // Cam1Matrix ( 3 lines )
            // IsCam2Calibrated (T/F)
            // Cam2Matrix
            // Num of calib1 points
            // Calib1Points ( 1 in each line ) -> imgx, imgy, gridnum, gridx, gridy
            // Num of calib2 points
            // Calib2Points
            // Num of grids1
            // Grids1 ( 1 in each line ) -> num, hx,hy,hz, wx,wy,wz, x0,y0,z0, rows, cols, label ( rest of line )
            // Num of grids2
            // Grids2

            List<string> lines = new List<string>();
            lines.Add("CALIBRATION");
            if (!IsCam1Calibrated)
            {
                lines.Add("F");
            }
            else // Cam1 calibrated -> write matrix
            {
                lines.Add("T");
                for(int r = 0; r < 3; r++)
                {
                    lines.Add(Cam1Matrix[r, 0].ToString("F4") + "|" + Cam1Matrix[r, 1].ToString("F4")
                        + "|" + Cam1Matrix[r, 2].ToString("F4") + "|" + Cam1Matrix[r, 3].ToString("F4"));
                }
            }
            if (!IsCam2Calibrated)
            {
                lines.Add("F");
            }
            else
            {
                lines.Add("T");
                for (int r = 0; r < 3; r++)
                {
                    lines.Add(Cam2Matrix[r, 0].ToString("F4") + "|" + Cam2Matrix[r, 1].ToString("F4") +
                        "|" + Cam2Matrix[r, 2].ToString("F4") + "|" + Cam2Matrix[r, 3].ToString("F4"));
                }
            }
            lines.Add(Cam1Points.Count.ToString());
            foreach (var point in Cam1Points)
            {
                lines.Add(point.ImgX.ToString("F2") + "|" + point.ImgY.ToString("F2") + "|" + point.GridNum.ToString() + "|" +
                    point.RealCol.ToString() + "|" + point.RealRow.ToString());
            }
            lines.Add(Cam2Points.Count.ToString());
            foreach (var point in Cam2Points)
            {
                lines.Add(point.ImgX.ToString("F2") + "|" + point.ImgY.ToString("F2") + "|" + point.GridNum.ToString() + "|" +
                    point.RealCol.ToString() + "|" + point.RealRow.ToString());
            }
            lines.Add(Cam1Grids.Count.ToString());
            foreach (var grid in Cam1Grids)
            {
                lines.Add(grid.Num.ToString() + "|" + grid.WidthX.ToString("F2") + "|" + grid.WidthY.ToString("F2") + "|" + grid.WidthZ.ToString("F2") + "|"
                    + grid.HeightX.ToString("F2") + "|" + grid.HeightY.ToString("F2") + "|" + grid.HeightZ.ToString("F2") + "|" + grid.ZeroX.ToString("F2") + "|" +
                     grid.ZeroY.ToString("F2") + "|" + grid.ZeroZ.ToString("F2") + "|" + grid.Label);
            }
            lines.Add(Cam2Grids.Count.ToString());
            foreach (var grid in Cam2Grids)
            {
                lines.Add(grid.Num.ToString() + "|" + grid.WidthX.ToString("F2") + "|" + grid.WidthY.ToString("F2") + "|" + grid.WidthZ.ToString("F2") + "|"
                    + grid.HeightX.ToString("F2") + "|" + grid.HeightY.ToString("F2") + "|" + grid.HeightZ.ToString("F2") + "|" + grid.ZeroX.ToString("F2") + "|" +
                     grid.ZeroY.ToString("F2") + "|" + grid.ZeroZ.ToString("F2") + "|" + grid.Label);
            }
            return lines;
        }

        public static bool LoadData(IEnumerable<string> fileLines)
        {
            // First line: header -> CALIBRATION
            // NEXT LINE: IsCam1Calibrated (T/F)
            // Cam1Matrix ( 3 lines )
            // IsCam2Calibrated (T/F)
            // Cam2Matrix
            // Num of calib1 points
            // Calib1Points ( 1 in each line ) -> imgx, imgy, gridnum, gridx, gridy
            // Num of calib2 points
            // Calib2Points
            // Num of grids1
            // Grids1 ( 1 in each line ) -> num, hx,hy,hz, wx,wy,wz, x0,y0,z0, rows, cols, label ( rest of line )
            // Num of grids2
            // Grids2
            var line = fileLines.GetEnumerator();
            line.MoveNext();
            if (!line.Current.Equals("CALIBRATION"))
            {
                return false; // Error - bad header
            }
            if (!line.MoveNext())
            {
                return false; // Error - no next line
            }
            if (line.Current.Equals("T")) // Cam1 is calibrated -> load Cam1Matrix
            {
                if (!line.MoveNext())
                    return false; // No next line

                Cam1Matrix = new DenseMatrix(3,4);
                for (int r = 0; r < 3; r++)
                {
                    var row = line.Current.Split('|');
                    for (int c = 0; c < 4; c++)
                    {
                        Cam1Matrix[r, c] = double.Parse(row.ElementAt(c));
                    }
                }
            }
            if (!line.MoveNext())
            {
                return false; // Error - no next line
            }
            if (line.Current.Equals("T")) // Cam2 is calibrated -> load Cam1Matrix
            {
                if (!line.MoveNext())
                    return false; // No next line

                Cam2Matrix = new DenseMatrix(3, 4);
                for (int r = 0; r < 3; r++)
                {
                    var row = line.Current.Split('|');
                    for (int c = 0; c < 4; c++)
                    {
                        Cam2Matrix[r, c] = double.Parse(row.ElementAt(c));
                    }
                }
            }
            if (!line.MoveNext())
            {
                return false; // Error - no next line
            }
            int calibPoints1 = int.Parse(line.Current);
            Cam1Points = new List<CalibrationPoint>();
            for (int p = 0; p < calibPoints1; p++)
            {
                if (!line.MoveNext())
                    return false; // No next line

                var point = line.Current.Split('|');

                Cam1Points.Add(new CalibrationPoint()
                {
                    ImgX = double.Parse(point.ElementAt(0)),
                    ImgY = double.Parse(point.ElementAt(1)),
                    GridNum = int.Parse(point.ElementAt(2)),
                    RealCol = int.Parse(point.ElementAt(3)),
                    RealRow = int.Parse(point.ElementAt(4))
                });
            }
            if (!line.MoveNext())
            {
                return false; // Error - no next line
            }
            int calibPoints2 = int.Parse(line.Current);
            Cam2Points = new List<CalibrationPoint>();
            for (int p = 0; p < calibPoints2; p++)
            {
                if (!line.MoveNext())
                    return false; // No next line

                var point = line.Current.Split('|');

                Cam2Points.Add(new CalibrationPoint()
                {
                    ImgX = double.Parse(point.ElementAt(0)),
                    ImgY = double.Parse(point.ElementAt(1)),
                    GridNum = int.Parse(point.ElementAt(2)),
                    RealCol = int.Parse(point.ElementAt(3)),
                    RealRow = int.Parse(point.ElementAt(4))
                });
            }
            if (!line.MoveNext())
            {
                return false; // Error - no next line
            }
            int gridsCount1 = int.Parse(line.Current);
            Cam1Grids = new List<RealGridData>();
            for (int g = 0; g < gridsCount1; g++)
            {
                if (!line.MoveNext())
                    return false; // No next line

                var grid = line.Current.Split('|');

                Cam1Grids.Add(new RealGridData()
                {
                    Num = int.Parse(grid.ElementAt(0)),
                    WidthX = double.Parse(grid.ElementAt(1)),
                    WidthY = double.Parse(grid.ElementAt(2)),
                    WidthZ = double.Parse(grid.ElementAt(3)),
                    HeightX = double.Parse(grid.ElementAt(4)),
                    HeightY = double.Parse(grid.ElementAt(5)),
                    HeightZ = double.Parse(grid.ElementAt(6)),
                    ZeroX = double.Parse(grid.ElementAt(7)),
                    ZeroY = double.Parse(grid.ElementAt(8)),
                    ZeroZ = double.Parse(grid.ElementAt(9)),
                    Label = grid.ElementAt(10)
                });
            }
            if (!line.MoveNext())
            {
                return false; // Error - no next line
            }
            int gridsCount2 = int.Parse(line.Current);
            Cam2Grids = new List<RealGridData>();
            for (int g = 0; g < gridsCount2; g++)
            {
                if (!line.MoveNext())
                    return false; // No next line

                var grid = line.Current.Split('|');

                Cam2Grids.Add(new RealGridData()
                {
                    Num = int.Parse(grid.ElementAt(0)),
                    HeightX = double.Parse(grid.ElementAt(1)),
                    HeightY = double.Parse(grid.ElementAt(2)),
                    HeightZ = double.Parse(grid.ElementAt(3)),
                    WidthX = double.Parse(grid.ElementAt(4)),
                    WidthY = double.Parse(grid.ElementAt(5)),
                    WidthZ = double.Parse(grid.ElementAt(6)),
                    ZeroX = double.Parse(grid.ElementAt(7)),
                    ZeroY = double.Parse(grid.ElementAt(8)),
                    ZeroZ = double.Parse(grid.ElementAt(9)),
                    Label = grid.ElementAt(10)
                });
            }
            return true;
        }

    }
}
