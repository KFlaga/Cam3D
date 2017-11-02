using CamCore;
using CamAlgorithms;
using System;
using System.Collections.Generic;

namespace CalibrationModule.PointsExtraction
{
    public class ReferncePoint
    {
        public CalibrationShape Shape { get; set; } = null;
        public int IndexInShapeList { get; set; } = -1;
        public IntVector2 RealGridPos { get; set; } = new IntVector2(-1, -1);
        public RefernceShapeChecker CheckIsReferncePoint { get; set; } = null;

        public ReferncePoint() { }
        public ReferncePoint(IntVector2 gridPos, RefernceShapeChecker check)
        {
            RealGridPos = gridPos;
            CheckIsReferncePoint = check;
        }

        public static List<ReferncePoint> GetDefaultReferences()
        {
            return new List<ReferncePoint>()
            {
                new ReferncePoint(new IntVector2(0, 0), new ColorShapeChecker(new Vector3(1, 0, 0))),
                new ReferncePoint(new IntVector2(0, 0), new ColorShapeChecker(new Vector3(1, 0, 0))),
                new ReferncePoint(new IntVector2(0, 0), new ColorShapeChecker(new Vector3(1, 0, 0))),
                new ReferncePoint(new IntVector2(0, 0), new ColorShapeChecker(new Vector3(1, 0, 0)))
            };
        }
    }

    public class ShapesGridCPFinder : CalibrationPointsFinder
    {
        public List<CalibrationShape> CalibShapes { get; set; }
        public CalibrationGrid CalibGrid { get; set; }      
        public List<ReferncePoint> ReferncePoints { get; set; }

        public double PointSizeTresholdHigh { get; set; } // How much bigger than primary shape calib shape can be to accept it
        public double PointSizeTresholdLow { get; set; } // How much smaller than primary shape calib shape can be to accept it
        public double BrightnessThreshold { get; set; }
        public int MinShapeSize { get; set; }
        
        public ShapesGridCPFinder()
        {
            LinesExtractor = new ShapeGridLinesExtractor();
            ReferncePoints = ReferncePoint.GetDefaultReferences();
        }
        
        public override void FindCalibrationPoints()
        {
            Points = new List<CalibrationPoint>();

            CalibShapes = new CalibrationShapesExtractor().FindCalibrationShapes(Image, BrightnessThreshold);
            FindReferencePointsAndPruneOutlyingShapes();
            FindCalibrationGrid();
            
            foreach(var shape in CalibGrid)
            {
                if(shape != null && !shape.IsInvalid)
                {
                    Points.Add(new CalibrationPoint()
                    {
                        Img = shape.GravityCenter,
                        RealGridPos = new IntVector2(x: shape.GridPos.X, y: shape.GridPos.Y)
                    });
                }
            }

            ((ShapeGridLinesExtractor)LinesExtractor).CalibGrid = CalibGrid;
        }

        public void FindReferencePointsAndPruneOutlyingShapes()
        {
            RemoveTooSmallShapes();
            FindReferencePoints();
            if(ReferncePoints.Count == 0)
            {
                throw new Exception("No refernce calibration shape detected on calibration image");
            }
            RemoveAllShapesNotOnWhiteFieldsWithRefernceShape();
            RemoveShapesTooSmallComparedToClosestReferenceShape();
        }

        void RemoveTooSmallShapes()
        {
            CalibShapes.RemoveIf((shape) => { return shape.Area < MinShapeSize; });
        }

        void FindReferencePoints()
        {
            foreach(var refPoint in ReferncePoints)
            {
                refPoint.CheckIsReferncePoint.Image = Image;
                for(int i = 0; i < CalibShapes.Count; ++i)
                {
                    var shape = CalibShapes[i];
                    if(refPoint.CheckIsReferncePoint.CheckShape(shape))
                    {
                        if(refPoint.Shape != null) { throw new Exception("More than one Shape match ReferncePoint"); }
                        refPoint.Shape = shape;
                        refPoint.IndexInShapeList = i;
                    }
                }
            }
            ReferncePoints.RemoveIf((refPoint) => { return refPoint.Shape == null; });

            foreach(var p1 in ReferncePoints)
            {
                foreach(var p2 in ReferncePoints)
                {
                    if(p1 != p2 && p1.Shape == p2.Shape)
                    {
                        throw new Exception("More than one ReferncePoint have same Shape");
                    }
                }
            }
        }

        void RemoveAllShapesNotOnWhiteFieldsWithRefernceShape()
        {
            CalibShapes.RemoveIf((shape) =>
            {
                foreach(var refPoint in ReferncePoints)
                {
                    if(refPoint.Shape.Index == shape.Index) { return true; }
                }
                return false;
            });
        }

        void RemoveShapesTooSmallComparedToClosestReferenceShape()
        {
            CalibShapes.RemoveIf((shape) =>
            {
                ReferncePoint closestPoint = FindClosestReferncePoint(shape);
                return shape.Area < closestPoint.Shape.Area * PointSizeTresholdLow || 
                    shape.Area > closestPoint.Shape.Area * PointSizeTresholdHigh;
            });
        }

        ReferncePoint FindClosestReferncePoint(CalibrationShape shape)
        {
            ReferncePoint closestPoint = null;
            double closestDist = 1e12;
            foreach(var refPoint in ReferncePoints)
            {
                double d = refPoint.Shape.GravityCenter.DistanceToSquared(shape.GravityCenter);
                if(closestPoint == null || d < closestDist)
                {
                    closestPoint = refPoint;
                    closestDist = d;
                }
            }
            return closestPoint;
        }

        public void FindCalibrationGrid()
        {
            var finder = new CalibrationShapeGridFinder()
            {
                CalibShapes = CalibShapes,
                ReferncePoints = ReferncePoints
            };
            finder.FillCalibrationGrid();
            CalibGrid = finder.CalibGrid;
        }

        public override string Name
        {
            get
            {
                return "ShapesGridCPFinder";
            }
        }

        public override void InitParameters()
        {
            base.InitParameters();
            
            Parameters.Add(new DoubleParameter(
                "Size Elimination Threshold Low", "PointSizeTresholdLow", 0.1, 0.0001, 1.0));
            Parameters.Add(new DoubleParameter(
               "Size Elimination Threshold High", "PointSizeTresholdHigh", 10.0, 1.0, 1000.0));
            Parameters.Add(new DoubleParameter(
               "Brightness Threshold For White/Dark Backgorund", "BrightnessThreshold", 0.5, 0.0, 1.0));
            Parameters.Add(new IntParameter(
               "Minimal Area of Calibration Shape in [px]", "MinShapeSize", 20, 0, 1000000));
        }

        public override void UpdateParameters()
        {
            base.UpdateParameters();

            PointSizeTresholdLow = AlgorithmParameter.FindValue<double>("PointSizeTresholdLow", Parameters);
            PointSizeTresholdHigh = AlgorithmParameter.FindValue<double>("PointSizeTresholdHigh", Parameters);
            BrightnessThreshold = AlgorithmParameter.FindValue<double>("BrightnessThreshold", Parameters);
            MinShapeSize = AlgorithmParameter.FindValue<int>("MinShapeSize", Parameters);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
