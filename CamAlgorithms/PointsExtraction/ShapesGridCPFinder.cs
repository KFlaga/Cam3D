using CamCore;
using System;
using System.Collections.Generic;
using CamAlgorithms.Calibration;

namespace CamAlgorithms.PointsExtraction
{
    // 3 reference points determine local axes (should be neighbouring shapes)
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
                new ReferncePoint(new IntVector2(x: 4, y: 4), new ColorShapeChecker(new Vector3(0, 0.3, 0))),
                new ReferncePoint(new IntVector2(x: 5, y: 4), new ColorShapeChecker(new Vector3(0.3, 0, 0))),
                new ReferncePoint(new IntVector2(x: 4, y: 5), new ColorShapeChecker(new Vector3(0, 0, 0.3)))
            };
        }
    }

    public class ShapesGridCalibrationPointsFinder : CalibrationPointsFinder
    {
        public List<CalibrationShape> CalibShapes { get; set; }
        public CalibrationGrid CalibGrid { get; set; }      
        public List<ReferncePoint> ReferncePoints { get; set; }

        public double PointSizeTresholdHigh { get; set; } // How much bigger than primary shape calib shape can be to accept it
        public double PointSizeTresholdLow { get; set; } // How much smaller than primary shape calib shape can be to accept it
        public double BrightnessThreshold { get; set; }
        public int MinShapeSize { get; set; }
        
        public ShapesGridCalibrationPointsFinder()
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
                        Img = shape.Center,
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
            ValidateReferncePoints();
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
                refPoint.Shape = null;
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
        }

        void ValidateReferncePoints()
        {
            if(ReferncePoints.Count != 3)
            {
                throw new Exception("Need 3 refernce points which define local axes");
            }

            foreach(var p1 in ReferncePoints)
            {
                if(p1.Shape == null)
                {
                    throw new Exception("Some refernce point not found");
                }

                foreach(var p2 in ReferncePoints)
                {
                    if(p1 != p2 && p1.Shape == p2.Shape)
                    {
                        throw new Exception("More than one ReferncePoint have same Shape");
                    }
                    if(p1.Shape.Index != p2.Shape.Index)
                    {
                        throw new Exception("Refernce points on different white fields");
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
                    if(refPoint.Shape.Index == shape.Index) { return false; }
                }
                return true;
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
                double d = refPoint.Shape.Center.DistanceToSquared(shape.Center);
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

            Parameters.Add(new Vector3Parameter(
                "Main Reference Point Color", "RefColor0", 
                new Vector3(0.3, 0.0, 0.0), new Vector3(0.0, 0.0, 0.0), new Vector3(1.0, 1.0, 1.0)));
            Parameters.Add(new Vector2Parameter(
                "Main Reference Point Position", "RefPos0",
                new Vector2(4, 4), new Vector2(-1000, -1000), new Vector2(1000, 1000)));
            Parameters.Add(new Vector3Parameter(
                "X Reference Point Color", "RefColorX",
                new Vector3(0.0, 0.3, 0.0), new Vector3(0.0, 0.0, 0.0), new Vector3(1.0, 1.0, 1.0)));
            Parameters.Add(new Vector2Parameter(
                "X Reference Point Position", "RefPosX",
                new Vector2(5, 4), new Vector2(-1000, -1000), new Vector2(1000, 1000)));
            Parameters.Add(new Vector3Parameter(
                "Y Reference Point Color", "RefColorY",
                new Vector3(0.0, 0.0, 0.3), new Vector3(0.0, 0.0, 0.0), new Vector3(1.0, 1.0, 1.0)));
            Parameters.Add(new Vector2Parameter(
                "Y Reference Point Position", "RefPosY",
                new Vector2(4, 5), new Vector2(-1000, -1000), new Vector2(1000, 1000)));
        }

        public override void UpdateParameters()
        {
            base.UpdateParameters();

            PointSizeTresholdLow = IAlgorithmParameter.FindValue<double>("PointSizeTresholdLow", Parameters);
            PointSizeTresholdHigh = IAlgorithmParameter.FindValue<double>("PointSizeTresholdHigh", Parameters);
            BrightnessThreshold = IAlgorithmParameter.FindValue<double>("BrightnessThreshold", Parameters);
            MinShapeSize = IAlgorithmParameter.FindValue<int>("MinShapeSize", Parameters);

            ReferncePoints = new List<ReferncePoint>();
            ReferncePoints.Add(new ReferncePoint(
                new IntVector2(IAlgorithmParameter.FindValue<Vector2>("RefPos0", Parameters)),
                new ColorShapeChecker(IAlgorithmParameter.FindValue<Vector3>("RefColor0", Parameters))));
            ReferncePoints.Add(new ReferncePoint(
                new IntVector2(IAlgorithmParameter.FindValue<Vector2>("RefPosX", Parameters)),
                new ColorShapeChecker(IAlgorithmParameter.FindValue<Vector3>("RefColorX", Parameters))));
            ReferncePoints.Add(new ReferncePoint(
                new IntVector2(IAlgorithmParameter.FindValue<Vector2>("RefPosY", Parameters)),
                new ColorShapeChecker(IAlgorithmParameter.FindValue<Vector3>("RefColorY", Parameters))));
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
