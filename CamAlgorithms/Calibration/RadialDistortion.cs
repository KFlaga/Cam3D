using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace CamAlgorithms.Calibration
{
    public enum DistortionDirection : int
    {
        None = 0, // No distortion
        ToCenter = 1, // rd/ru < 1 ( rd < ru )
        FromCenter = 2, // rd/ru > 1 ( rd > ru )
        Barrel = ToCenter,
        Cushion = FromCenter,
        Unknown = 3 // Cannot determine distortion
    }

    public class DistortionPoint
    {
        public Vector2 Pi { set; get; } = new Vector2();
        public Vector2 Pd { set; get; } = new Vector2();
        public Vector2 Pu { set; get; } = new Vector2();
        public Vector2 Pf { set; get; } = new Vector2();
        public double Rd { set; get; }
        public double Ru { set; get; }

        public Vector<double> Diff_Xf { set; get; }
        public Vector<double> Diff_Yf { set; get; }

        public Vector<double> Diff_Xu { set; get; }
        public Vector<double> Diff_Yu { set; get; }

        public Vector<double> Diff_Xd { set; get; }
        public Vector<double> Diff_Yd { set; get; }

        public Vector<double> Diff_Ru { set; get; }
        public Vector<double> Diff_Rd { set; get; }

        public DistortionPoint(int modelParametersCount)
        {
            Diff_Xf = new DenseVector(modelParametersCount);
            Diff_Yf = new DenseVector(modelParametersCount);
            Diff_Xu = new DenseVector(modelParametersCount);
            Diff_Yu = new DenseVector(modelParametersCount);
            Diff_Xd = new DenseVector(modelParametersCount);
            Diff_Yd = new DenseVector(modelParametersCount);
            Diff_Ru = new DenseVector(modelParametersCount);
            Diff_Rd = new DenseVector(modelParametersCount);
        }
    }

    public class RadialDistortion : IXmlSerializable
    {
        [XmlIgnore]
        public RadialDistortionModel Model { get; set; }

        public void FullUpdate() { Model.FullUpdate(); }
        public void FullUpdate(DistortionPoint dpoint) { Model.FullUpdate(dpoint); }
        public void Undistort() { Model.Undistort(); }
        public void Undistort(DistortionPoint dpoint) { Model.Undistort(dpoint); }
        public Vector2 Undistort(Vector2 dpoint) { return Model.Undistort(dpoint); }
        public void Distort() { Model.Distort(); }
        public void Distort(DistortionPoint dpoint) { Model.Distort(dpoint); }
        public Vector2 Distort(Vector2 dpoint) { return Model.Distort(dpoint); }

        #region DistortionDirection
        // Returns direction of distortion based on distorted points and fitted line between them
        // If ends are further from center than their projection on fit line towards center
        // then we got cushion distortion or barrel otherwise
        public static DistortionDirection DirectionFromLine(List<Vector2> line,
            double fitA, double fitB, double fitC, Vector2 distortionCenter)
        {
            // We need to take 2 ends of the line ( points should be sorted )
            Vector2 p1 = line[0];
            Vector2 p2 = line[line.Count - 1];

            return DirectionFromLineEnds(p1, p2, fitA, fitB, fitC, distortionCenter);
        }

        public static DistortionDirection DirectionFromLine(List<DistortionPoint> line,
            double fitA, double fitB, double fitC, Vector2 distortionCenter)
        {
            // We need to take 2 ends of the line ( points should be sorted )
            Vector2 p1 = line[0].Pf;
            Vector2 p2 = line[line.Count - 1].Pf;

            return DirectionFromLineEnds(p1, p2, fitA, fitB, fitC, distortionCenter);
        }

        public static DistortionDirection DirectionFromLineEnds(Vector2 p1, Vector2 p2,
            double fitA, double fitB, double fitC, Vector2 distortionCenter)
        {
            // Get projection towards center :
            // p' = p + k(c-p)
            // Ap'x + Bp'y + C = 0
            // A(px + k(cx-px)) + B(py + k(cy-py)) + C = 0
            // k(A(cx-px) + B(cy-py)) = -(Apx + Bpy + C)
            // k = -(Apx + Bpy + C) / (A(cx-px) + B(cy-py))
            // If k > 0  then projection is closer to center than point, if k < 0 then further
            // Also if A(cx-px) + B(cy-py) is zero, then center lies on line, so k = 0
            double k1 = fitA * (distortionCenter.X - p1.X) + fitB * (distortionCenter.Y - p1.Y) != 0 ?
                -((fitA * p1.X + fitB * p1.Y + fitC) /
                (fitA * (distortionCenter.X - p1.X) + fitB * (distortionCenter.Y - p1.Y))) : 0;

            double k2 = fitA * (distortionCenter.X - p2.X) + fitB * (distortionCenter.Y - p2.Y) != 0 ?
                -((fitA * p2.X + fitB * p2.Y + fitC) /
                (fitA * (distortionCenter.X - p2.X) + fitB * (distortionCenter.Y - p2.Y))) : 0;

            // If k1 = k2 = 0 we have no distortion
            // If both ends are closer, we have cushion dostortion, of both further - barrel
            // If one is closer, one further its undetermined
            if(k1 == 0.0 && k2 == 0.0) { return DistortionDirection.None; } 
            if(k1 >= 0.0 && k2 >= 0.0) { return DistortionDirection.Cushion; }      
            if(k1 <= 0.0 && k2 <= 0.0) { return DistortionDirection.Barrel; }
            return DistortionDirection.Unknown;
        }

        public static DistortionDirection DirectionFromFitQuadric(List<DistortionPoint> linePoints,
            Quadric quadric, Vector2 distCenter, Vector2 fitPoint)
        {
            // 1) Get tangent coeffs
            // 2) Find if points are closer to tangent than center or further
            // 3) If at least 75% of points lies on the same side then we have direction
            //      if not then we have no radial distortion

            Line2D tangent = quadric.GetTangentThroughPoint(fitPoint);

            // Find crossing point of tangent and every point
            int centerCloserToQuadCount = 0, centerCloserToTangnetCount = 0;
            for(int i = 0; i < linePoints.Count; ++i)
            {
                var pt = linePoints[i].Pf;
                if(pt == fitPoint) { continue; }

                // Find line from center to fit point
                Line2D centerToFit = new Line2D(distCenter, pt);
                Vector2 intPoint = Line2D.IntersectionPoint(tangent, centerToFit);

                if(intPoint != null)
                {
                    if(Math.Abs(intPoint.X * tangent.A + intPoint.Y * tangent.B + tangent.C) > 1e-3)
                    {
                        int a = 0;
                    }

                    // Check distance from center to line point and tangent point
                    double dToQuad = distCenter.DistanceToSquared(pt);
                    double dToTangent = distCenter.DistanceToSquared(intPoint);

                    if(dToQuad > dToTangent)
                        centerCloserToTangnetCount += 1;
                    else
                        centerCloserToQuadCount += 1;
                }
            }

            if(centerCloserToQuadCount > (linePoints.Count - 1) * 0.75)
            {
                // Almost all points on quad are closer to center, so we have barrel distortion
                return DistortionDirection.Barrel;
            }
            else if(centerCloserToTangnetCount > (linePoints.Count - 1) * 0.75)
            {
                // Almost all points on tangent are closer to center, so we have cushion distortion
                return DistortionDirection.Cushion;
            }
            else
            {
                // We have no distortion/undefined
                return DistortionDirection.None;
            }
        }
#endregion
        #region Xml
        public XmlSchema GetSchema() { return null; }

        public void ReadXml(XmlReader reader)
        {
            reader.MoveToElement(); // Moves to begining of <RadialDistortion>
            string name = reader.GetAttribute("name");
            if(name == null)
            {
                Model = null;
                reader.Read();
                if(reader.NodeType == XmlNodeType.EndElement)
                {
                    reader.ReadEndElement();
                }
                return;
            }

            if(name.Equals("Rational3", StringComparison.OrdinalIgnoreCase))
            {
                Model = new Rational3RDModel();
            }
            else if(name.Equals("Taylor4", StringComparison.OrdinalIgnoreCase))
            {
                Model = new Taylor4Model();
            }
            else
            {
                throw new XmlException("Unsupported distortion model name: " + name);
            }

            reader.ReadStartElement(); // Moves to <Parameters>
            reader.ReadStartElement(); // Moves to first <Parameter>

            for(int k = 0; k < Model.ParametersCount; ++k)
            {
                Model.Coeffs[k] = reader.ReadElementContentAsDouble(); // Reads from <Parameter> and moves to next one
            }
            reader.ReadEndElement(); // Should read </Parameters>

            Model.ImageScale = reader.ReadElementContentAsDouble(); // Reads from <ImageScale> and moves to next one
            reader.ReadEndElement(); // Should read </RadialDistortion>
        }

        public void WriteXml(XmlWriter writer)
        {
            if(Model == null)
            {
                return;
            }

            string modelName;
            if(Model.GetType().Name == "Rational3RDModel")
            {
                modelName = "Rational3";
            }
            else if(Model.GetType().Name == "Taylor4Model")
            {
                modelName = "Taylor4";
            }
            else
            {
                throw new XmlException("Unsupported distortion model type: " + Model.GetType().Name);
            }
            writer.WriteAttributeString("name", modelName);

            writer.WriteStartElement("Parameters");
            for(int k = 0; k < Model.ParametersCount; ++k)
            {
                writer.WriteElementString("Parameter", Model.Coeffs[k].ToString());
            }
            writer.WriteEndElement();

            writer.WriteElementString("ImageScale", Model.ImageScale.ToString());
        }
        #endregion
    }
}
