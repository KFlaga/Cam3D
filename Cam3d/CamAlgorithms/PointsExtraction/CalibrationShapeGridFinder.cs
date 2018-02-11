using CamCore;
using System;
using System.Collections.Generic;

namespace CamAlgorithms.PointsExtraction
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
            FindLocalAxes();
            InitCalibrationGrid();
            EmplaceCalibShapesOnGrid();
        }

        private void FindLocalAxes()
        {
            ReferncePoint rx = ReferncePoints[0].RealGridPos.X == ReferncePoints[1].RealGridPos.X ? ReferncePoints[2] : ReferncePoints[1];
            ReferncePoint ry = ReferncePoints[0].RealGridPos.Y == ReferncePoints[1].RealGridPos.Y ? ReferncePoints[2] : ReferncePoints[1];
            AxisX = (rx.Shape.Center - ReferncePoints[0].Shape.Center) / (rx.RealGridPos.X - ReferncePoints[0].RealGridPos.X);
            AxisY = (ry.Shape.Center - ReferncePoints[0].Shape.Center) / (ry.RealGridPos.Y - ReferncePoints[0].RealGridPos.Y);
        }

        private void InitCalibrationGrid()
        {
            CalibGrid = new CalibrationGrid(4, 4);
            CalibGrid.Add(ReferncePoints[0].RealGridPos.Y, ReferncePoints[0].RealGridPos.X, ReferncePoints[0].Shape);
            CalibGrid.Add(ReferncePoints[1].RealGridPos.Y, ReferncePoints[1].RealGridPos.X, ReferncePoints[1].Shape);
            CalibGrid.Add(ReferncePoints[2].RealGridPos.Y, ReferncePoints[2].RealGridPos.X, ReferncePoints[2].Shape);
        }


        class ShapeItem
        {
            public CalibrationShape Shape { get; set; }
            public IntVector2 Cell { get; set; }
            public Vector2 MoveX { get; set; } = null;
            public Vector2 MoveY { get; set; } = null;
        }
        
        bool[] visited;
        private void EmplaceCalibShapesOnGrid()
        {
            visited = new bool[CalibShapes.Count];
            foreach(var r in ReferncePoints) { visited[r.IndexInShapeList] = true; }
            for(int i = 0; i < CalibShapes.Count; i++) { CalibShapes[i].Index = i; }

            Queue<ShapeItem>  queue = new Queue<ShapeItem>();
            foreach(var r in ReferncePoints)
            {
                queue.Enqueue(new ShapeItem()
                {
                    Shape = r.Shape,
                    MoveX = AxisX,
                    MoveY = AxisY,
                    Cell = r.RealGridPos
                });
            }

            while(queue.Count > 0)
            {
                ShapeItem shape = queue.Dequeue();

                ShapeItem x = Move(shape, new IntVector2(y: 0, x: 1));
                if(x != null) { queue.Enqueue(x); }
                x = Move(shape, new IntVector2(y: 1, x: 0));
                if(x != null) { queue.Enqueue(x); }
                x = Move(shape, new IntVector2(y: 0, x: -1));
                if(x != null) { queue.Enqueue(x); }
                x = Move(shape, new IntVector2(y: -1, x: 0));
                if(x != null) { queue.Enqueue(x); }
            }
        }

        private ShapeItem Move(ShapeItem shape, IntVector2 step, double coeffMainAxis = 0.5, double coeffSecAxis = 0.15)
        {
            Vector2 move = step.X * shape.MoveX + step.Y * shape.MoveY;
            Vector2 nextShapeCenter = shape.Shape.Center + move;
            CalibrationShape closestShape = FindClosestShape(shape.Shape, nextShapeCenter);

            if(visited[closestShape.Index]) { return null; }

            Vector2 distance = nextShapeCenter - closestShape.Center;
            
            if(CheckIfPointIsCloseEnough(distance, move, coeffMainAxis, coeffSecAxis))
            {
                CalibGrid.Add(shape.Cell + step, closestShape);
                visited[closestShape.Index] = true;
                return new ShapeItem()
                {
                    Shape = closestShape,
                    Cell = shape.Cell + step,
                    MoveX = step.X != 0 ? new Vector2((closestShape.Center - shape.Shape.Center) * step.X) : shape.MoveX,
                    MoveY = step.Y != 0 ? new Vector2((closestShape.Center - shape.Shape.Center) * step.Y) : shape.MoveY,
                };
            }
            return null;
        }

        private CalibrationShape FindClosestShape(CalibrationShape shape, Vector2 nextShapeCenter)
        {
            CalibrationShape closestOne = shape;
            double d = nextShapeCenter.DistanceToSquared(shape.Center);
            foreach(var sh in CalibShapes)
            {
                double d2 = nextShapeCenter.DistanceToSquared(sh.Center);
                if(d2 < d)
                {
                    d = d2;
                    closestOne = sh;
                }
            }
            return closestOne;
        }

        private bool CheckIfPointIsCloseEnough(Vector2 distance, Vector2 move, double c1, double c2)
        {
            Vector2 moveAxis = move.Normalised();
            double moveLen = move.Length();
            Vector2 distanceInMoveFrame = new Vector2(
                distance.X * moveAxis.X + distance.Y * moveAxis.Y, 
                -distance.X * moveAxis.Y + distance.Y * moveAxis.X);
            return Math.Abs(distanceInMoveFrame.X) < (moveLen + 3.0) * c1 &&
                Math.Abs(distanceInMoveFrame.Y) < (moveLen + 3.0) * c2;
        }
    }
}
