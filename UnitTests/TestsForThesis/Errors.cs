using CamAlgorithms;
using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CamUnitTest.TestsForThesis
{
    public class Deviation
    {
        public List<double> variations { get; set; }
        public double sum { get; set; }
        public double mean { get; set; }
        public double max { get; set; }
        public double most { get; set; }
        public int pointCount { get; set; }
        public string name { get; set; }

        public Deviation(List<double> variations, string name = "")
        {
            SetErrors(variations);
            this.name = name;
        }

        public delegate List<double> GetVariationsList();
        public Deviation(GetVariationsList variationsListGetter, string name = "")
        {
            SetErrors(variationsListGetter());
            this.name = name;
        }

        void SetErrors(List<double> variations)
        {
            this.variations = variations;
            pointCount = variations.Count;
            sum = Math.Sqrt(variations.Sum());
            mean = sum / Math.Sqrt(pointCount);
            variations.Sort((a, b) => { return -a.CompareTo(b); });
            max = Math.Sqrt(variations[0]);
            most = Math.Sqrt(variations[(int)(pointCount * 0.05) + 1]);
        }

        public void Store(Context context, string info = "", bool shortVer = false)
        {
            if(shortVer)
            {
                context.Output.AppendLine(mean.ToString("E3"));
                context.Output.AppendLine(most.ToString("E3"));
                context.Output.AppendLine(max.ToString("E3"));
            }
            else
            {
                if(info.Length > 0)
                {
                    context.Output.AppendLine("Case: " + info);
                }
                context.Output.AppendLine(name + " Mean: " + mean.ToString("E3"));
                context.Output.AppendLine(name + " 95 %: " + most.ToString("E3"));
                context.Output.AppendLine(name + " Max: " + max.ToString("E3"));
            }
            context.Output.AppendLine();
        }
    }

    // d(x, PX)
    public class ReprojectionError : Deviation
    {
        public ReprojectionError(Matrix<double> P, List<Vector3> realPoints, List<Vector2> imagePoints) :
            base(() =>
            {
                List<double> errors = new List<double>();
                for(int i = 0; i < realPoints.Count; ++i)
                {
                    Vector<double> rp = realPoints[i].ToMathNetVector4();
                    Vector<double> ip = imagePoints[i].ToMathNetVector3();
                    Vector<double> eip = P * rp;
                    eip.MultiplyThis(1 / eip[2]);
                    errors.Add((eip - ip).L2Norm());
                }
                return errors;
            }, "Reprojection Error")
        { }

        public ReprojectionError(Matrix<double> P, List<Vector<double>> realPoints, List<Vector<double>> imagePoints) :
             base(() =>
             {
                 List<double> errors = new List<double>();
                 for(int i = 0; i < realPoints.Count; ++i)
                 {
                     Vector<double> eip = P * realPoints[i];
                     eip.MultiplyThis(1 / eip[2]);
                     errors.Add((eip - imagePoints[i]).L2Norm());
                 }
                 return errors;
             }, "Reprojection Error")
        { }
    }

    public class RegressionDeviation : Deviation
    {
        public RegressionDeviation(List<List<Vector2>> correctedLines, double meanRadius) :
            base(() =>
            {
                List<double> variation = new List<double>();
                for(int l = 0; l < correctedLines.Count; ++l)
                {
                    var regLine = Line2D.GetRegressionLine(correctedLines[l]);
                    for(int i = 0; i < correctedLines[l].Count; ++i)
                    {
                        var p = correctedLines[l][i];
                        variation.Add(regLine.DistanceToSquared(p) / (meanRadius * meanRadius));
                    }
                }
                return variation;
            })
        { }

        public RegressionDeviation(List<Vector2> points, double meanRadius) :
            base(() =>
            {
                List<double> variation = new List<double>();
                var regLine = Line2D.GetRegressionLine(points);
                for(int i = 0; i < points.Count; ++i)
                {
                    variation.Add(regLine.DistanceToSquared(points[i]) / (meanRadius * meanRadius));
                }
                return variation;
            })
        { }
    }

    public class NonhorizontalityError : Deviation
    {
        public NonhorizontalityError(List<Vector2Pair> matchedPairs, Matrix<double> Hl, Matrix<double> Hr) :
            base(() =>
            {
                List<double> errors = new List<double>();
                for(int i = 0; i < matchedPairs.Count; ++i)
                {
                    var pair = matchedPairs[i];
                        // rectify points pair
                        var rectLeft = Hl * pair.V1.ToMathNetVector3();
                    var rectRight = Hr * pair.V2.ToMathNetVector3();
                        // get error -> squared difference of y-coord
                        double yError = new Vector2(rectLeft).Y - new Vector2(rectRight).Y;
                    errors.Add(yError * yError);
                }
                return errors;
            }, "Nonhorizontality Error")
        { }
    }

    public class NonperpendicularityError
    {
        double errorLeft;
        double errorRight;

        struct EdgeCenters
        {
            public Vector2 top;
            public Vector2 bot;
            public Vector2 left;
            public Vector2 right;

            public Vector2 horizontal;
            public Vector2 vertical;

            public EdgeCenters(Matrix<double> H, Vector2 imageSize)
            {
                var topLeft = new Vector2(H * new DenseVector(new double[3] { 0.0, 0.0, 1.0 }));
                var topRight = new Vector2(H * new DenseVector(new double[3] { imageSize.X - 1, 0.0, 1.0 }));
                var botLeft = new Vector2(H * new DenseVector(new double[3] { 0.0, imageSize.Y - 1, 1.0 }));
                var botRight = new Vector2(H * new DenseVector(new double[3] { imageSize.X - 1, imageSize.Y - 1, 1.0 }));

                top = (topLeft + topRight) * 0.5;
                bot = (botLeft + botRight) * 0.5;
                left = (topLeft + botLeft) * 0.5;
                right = (topRight + botRight) * 0.5;

                vertical = bot - top;
                horizontal = right - left;
            }
        }

        public NonperpendicularityError(Matrix<double> Hl, Matrix<double> Hr, Vector2 imageSize)
        {
            EdgeCenters left = new EdgeCenters(Hl, imageSize);
            EdgeCenters right = new EdgeCenters(Hr, imageSize);

            // Error is angle - pi/2 between lines joining centers
            double angleLeft = Math.Abs(left.horizontal.AngleTo(left.vertical));
            errorLeft = angleLeft - Math.PI / 2;
            double angleRight = Math.Abs(right.horizontal.AngleTo(right.vertical));
            errorRight = angleLeft - Math.PI / 2;
        }

        public void Store(Context context, string info = "", bool shortVer = false)
        {
            if(shortVer)
            {
                context.Output.AppendLine(errorLeft.ToString("F3"));
                context.Output.AppendLine(errorRight.ToString("F3"));
            }
            else
            {
                if(info.Length > 0)
                {
                    context.Output.AppendLine("Case: " + info);
                }
                context.Output.AppendLine("Left Angle to pi/2: " + errorLeft.ToString("F3"));
                context.Output.AppendLine("Right Angle to pi/2: " + errorRight.ToString("F3"));
            }
            context.Output.AppendLine();
        }
    }

    public class AspectError
    {
        struct EdgeLengths
        {
            public double top;
            public double bot;
            public double left;
            public double right;

            public double ratioWidth;
            public double ratioHeight;

            public EdgeLengths(Matrix<double> H, Vector2 imageSize)
            {
                var topLeft = new Vector2(H * new DenseVector(new double[3] { 0.0, 0.0, 1.0 }));
                var topRight = new Vector2(H * new DenseVector(new double[3] { imageSize.X - 1, 0.0, 1.0 }));
                var botLeft = new Vector2(H * new DenseVector(new double[3] { 0.0, imageSize.Y - 1, 1.0 }));
                var botRight = new Vector2(H * new DenseVector(new double[3] { imageSize.X - 1, imageSize.Y - 1, 1.0 }));

                top = topLeft.DistanceTo(topRight);
                bot = botLeft.DistanceTo(botRight);
                left = topLeft.DistanceTo(botLeft);
                right = topRight.DistanceTo(botRight);

                ratioWidth = top / bot;
                ratioHeight = left / right;
            }
        }

        double leftTopBot;
        double leftLeftRight;
        double rightTopBot;
        double rightLeftRight;
        double sumRatio;

        public AspectError(Matrix<double> Hl, Matrix<double> Hr, Vector2 imageSize)
        {
            EdgeLengths left = new EdgeLengths(Hl, imageSize);
            EdgeLengths right = new EdgeLengths(Hr, imageSize);

            leftTopBot = left.ratioWidth;
            leftLeftRight = left.ratioHeight;
            rightTopBot = right.ratioWidth;
            rightLeftRight = right.ratioHeight;
            sumRatio = Math.Max(1 - leftTopBot, 1 - 1 / leftTopBot) +
                Math.Max(1 - leftLeftRight, 1 - 1 / leftLeftRight) +
                Math.Max(1 - rightTopBot, 1 - 1 / rightTopBot) +
                Math.Max(1 - rightLeftRight, 1 - 1 / rightLeftRight);
        }

        public void Store(Context context, string info = "", bool shortVer = false)
        {
            if(shortVer)
            {
                context.Output.AppendLine(leftTopBot.ToString("F3"));
                context.Output.AppendLine(leftLeftRight.ToString("F3"));
                context.Output.AppendLine(rightTopBot.ToString("F3"));
                context.Output.AppendLine(rightLeftRight.ToString("F3"));
                context.Output.AppendLine(sumRatio.ToString("F3"));
            }
            else
            {
                if(info.Length > 0)
                {
                    context.Output.AppendLine("Case: " + info);
                }
                context.Output.AppendLine("Left top/bot ratio: " + leftTopBot.ToString("F3"));
                context.Output.AppendLine("Left left/right ratio: " + leftLeftRight.ToString("F3"));
                context.Output.AppendLine("Right top/bot ratio: " + rightTopBot.ToString("F3"));
                context.Output.AppendLine("Right left/right ratio: " + rightLeftRight.ToString("F3"));
                context.Output.AppendLine("Sum of ratios: " + sumRatio.ToString("F3"));
            }
            context.Output.AppendLine();
        }
    }
}

