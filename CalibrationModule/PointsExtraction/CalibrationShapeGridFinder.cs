using CamCore;
using System;
using System.Collections.Generic;

namespace CalibrationModule.PointsExtraction
{
    public class CalibrationShapeGridFinder
    {
        public List<CalibrationShape> CalibShapes { get; set; }
        public CalibrationGrid CalibGrid { get; set; }
        public List<ReferncePoint> ReferncePoints { get; set; }

        public Vector2 AxisX { get; private set; } // Local grid x-axis in image coords
        public Vector2 AxisY { get; private set; } // Local grid y-axis in image coords

        public void FillCalibrationGrid()
        {
            CalibrationShape shapeX, shapeY; // shapeX, shapeY centers defines local axes
            FindLocalAxes(out shapeX, out shapeY);

            if(shapeX == null || shapeY == null)
                return;

            // Init shapes grid [y,x], add primary shape and closest cells
            InitCalibrationGrid(shapeX, shapeY);

            // Distance cost function : distance from estimated point
            // when moving along X: eP(x,y) = P(x-1,y) + (P(x-1,y) - P(x-2,y))
            // when moving along Y: eP(x,y) = P(x,y-1) + (P(x,y-1) - P(x,y-2))
            // 
            // If x = 0, then d(x,y) = P(x+1,y-1) - P(x,y-1)
            // else d(x,y) = P(x,y) - P(x-1,y)
            // eP(x+1,y) = P(x,y) + d(x,y)
            // Having eP, find closest point to eP and it will be P

            // 1st fill 1st row and column
            FillCalibGridLine(2, 0, 0.5f, 2.0f, true);
            FillCalibGridLine(0, 2, 2.0f, 0.5f, false);

            for(int row = 1; row < CalibGrid.RowCount; ++row)
            {
                // Add all rows
                FillCalibGridLine(1, row, 0.33f, 2.0f, true);
            }

            // TODO add more robustness : 
            // - column fill with false-positives elimination
            // - flood-like fill, so irregular grid will be properly scaned
        }

        private void FillCalibGridLine(int x, int y, double distTreshSq_x, double distTreshSq_y, bool isRow)
        {
            Vector2 d = new Vector2();
            bool haveHole = false;

            while(CalibShapes.Count > 0)
            {
                Point2D<int> fromPoint;
                if(isRow)
                    ComputeDistanceToNextPoint_Row(x, y, ref d, out fromPoint);
                else
                    ComputeDistanceToNextPoint_Column(x, y, ref d, out fromPoint);

                double dx = (d * AxisX).LengthSquared();
                double dy = (d * AxisY).LengthSquared();
                int closestIndex;
                double edx, edy;
                Vector2 eP;

                ComputeEstimatedPoint(x, y, d, fromPoint, out eP, out closestIndex, out edx, out edy);

                // Check if point is close enough to estimated point
                if(edx < dx * distTreshSq_x + 1.0f && edy < dy * distTreshSq_y + 1.0f)
                {
                    CalibGrid.Add(y, x, CalibShapes[closestIndex]);
                    CalibShapes.RemoveAt(closestIndex);
                    haveHole = false;
                }
                else if(haveHole)
                {
                    // Grid have 2 holes -> assume then that it have ended
                    break;
                }
                else
                {
                    // We have a hole, fill slot with dummy shape
                    AddDummyShape(x, y, eP);
                    haveHole = true;
                }

                if(isRow)
                    x += 1;
                else
                    y += 1;
            }
        }

        private void ComputeDistanceToNextPoint_Row(int x, int y, ref Vector2 d, out Point2D<int> fromPoint)
        {
            if(x > 1)
            {
                d = CalibGrid[y, x - 1].GravityCenter - CalibGrid[y, x - 2].GravityCenter;
                fromPoint = new Point2D<int>(y: y, x: x - 1);
            }
            else if(x == 1)
            {
                // Adding second in row -> use d from point 'above'
                d = CalibGrid[y - 1, x].GravityCenter - CalibGrid[y - 1, x - 1].GravityCenter;
                fromPoint = new Point2D<int>(y: y, x: x - 1);
            }
            else // x == 0
            {
                // Adding first in row -> move in y direction from 'above'
                d = CalibGrid[y - 1, x].GravityCenter - CalibGrid[y - 2, x].GravityCenter;
                fromPoint = new Point2D<int>(y: y - 1, x: x);
            }
        }

        private void ComputeDistanceToNextPoint_Column(int x, int y, ref Vector2 d, out Point2D<int> fromPoint)
        {
            if(y > 1)
            {
                d = CalibGrid[y - 1, x].GravityCenter - CalibGrid[y - 2, x].GravityCenter;
                fromPoint = new Point2D<int>(y: y - 1, x: x);
            }
            else if(y == 1)
            {
                // Adding second in column -> use d from point on 'left'
                d = CalibGrid[y, x - 1].GravityCenter - CalibGrid[y - 1, x - 1].GravityCenter;
                fromPoint = new Point2D<int>(y: y - 1, x: x);
            }
            else //y == 0
            {
                // Adding first in column -> move in x direction on 'left'
                d = CalibGrid[y, x - 1].GravityCenter - CalibGrid[y, x - 2].GravityCenter;
                fromPoint = new Point2D<int>(y: y, x: x - 1);
            }
        }

        private void AddDummyShape(int x, int y, Vector2 eP)
        {
            CalibrationShape dummy = new CalibrationShape();
            dummy.GravityCenter = eP;
            CalibGrid.Add(y, x, dummy);
        }

        private void ComputeEstimatedPoint(int x, int y, Vector2 d, Point2D<int> fromPoint, out Vector2 eP, out int minIndex, out double edx, out double edy)
        {
            eP = CalibGrid[fromPoint.Y, fromPoint.X].GravityCenter + d;
            minIndex = 0;
            double minDist = (CalibShapes[0].GravityCenter - eP).LengthSquared();
            for(int i = 0; i < CalibShapes.Count; ++i)
            {
                double dist = (CalibShapes[i].GravityCenter - eP).LengthSquared();
                if(minDist > dist)
                {
                    minDist = dist;
                    minIndex = i;
                }
            }

            edx = ((CalibShapes[minIndex].GravityCenter - eP) * AxisX).LengthSquared();
            edy = ((CalibShapes[minIndex].GravityCenter - eP) * AxisY).LengthSquared();
        }

        private void InitCalibrationGrid(CalibrationShape shapeX, CalibrationShape shapeY)
        {
            CalibGrid = new CalibrationGrid(4, 4);
            CalibGrid.Add(0, 0, CalibShapes[0]);
            CalibShapes.RemoveAt(0);

            CalibGrid.Add(0, 1, shapeX);
            CalibShapes.Remove(shapeX);

            CalibGrid.Add(1, 0, shapeY);
            CalibShapes.Remove(shapeY);
        }

        private void FindLocalAxes(out CalibrationShape shapeX, out CalibrationShape shapeY)
        {

            // Find calibration grid axes 
            // 1) Sort shapes by distance to primary shape
            CalibShapes.Sort((s1, s2) =>
            {
                // Compares distance of s1 and s2 to primary shape
                return (s1.GravityCenter - CalibShapes[0].GravityCenter).LengthSquared().CompareTo(
                     (s2.GravityCenter - CalibShapes[0].GravityCenter).LengthSquared());
            });

            // 2) Find direction vectors from PS to closest one and then find
            //    closest one with direction rotated by 90deg ( by comparing its dot product ->
            //    for 90deg its 0, having like 10deg tolerance we have :
            //    dot(A,B) / (|A||B|) = cos(a) < 0.17
            // Use 2 next closest ones
            shapeX = null;
            shapeY = null;
            Vector2 direction_1 = (CalibShapes[1].GravityCenter - CalibShapes[0].GravityCenter);
            direction_1.Normalise();
            shapeX = CalibShapes[1];

            //Vector2 direction_2 = direction_1;
            //int pointNum = 2;
            //for(; pointNum < CalibShapes.Count; ++pointNum)
            //{
            //    // Find direction and dot product
            //    direction_2 = (CalibShapes[pointNum].GravityCenter - CalibShapes[0].GravityCenter);
            //    direction_2.Normalise();

            //    if(Math.Abs(direction_1.CosinusTo(direction_2)) < 0.17)
            //    {
            //        // Found prependicular direction
            //        shapeY = CalibShapes[pointNum];
            //        break;
            //    }
            //}

            Vector2 direction_2 = (CalibShapes[2].GravityCenter - CalibShapes[0].GravityCenter);
            direction_2.Normalise();
            double cos2 = Math.Abs(direction_1.CosinusTo(direction_2));

            Vector2 direction_3 = (CalibShapes[3].GravityCenter - CalibShapes[0].GravityCenter);
            direction_3.Normalise();
            double cos3 = Math.Abs(direction_1.CosinusTo(direction_3));

            // Choose one with angle closer to 90deg (so cos closer to 0)
            direction_2 = cos2 < cos3 ? direction_2 : direction_3;
            shapeY = cos2 < cos3 ? CalibShapes[2] : CalibShapes[3];

            // Now depending on sign of sin(a), we can determine which axis is X and Y
            // Using cross-product of d1 and d2 ( casting it to 3d with z = 0 ) we have sin(a) = (d1.x * d2.y - d1.y * d2.x)
            // If sin(a) > 0, then d1 is local x axis, d2 is y
            if(direction_1.SinusTo(direction_2) > 0.0)
            {
                AxisX = direction_1;
                AxisY = direction_2;
            }
            else
            {
                AxisX = direction_2;
                AxisY = direction_1;

                var temp = shapeY;
                shapeY = shapeX;
                shapeX = temp;
            }
        }
    }
}
