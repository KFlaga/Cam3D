using CamAlgorithms;
using CamAlgorithms.Calibration;
using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CamUnitTest.TestsForThesis
{
    public class RadialDistortionTestUtils
    {
        public static void StoreModelInfo(Context context,
            RadialDistortionModel idealModel,
            RadialDistortionModel usedModel,
            MinimalisationAlgorithm minimalization, bool shortVer = false)
        {
            if(shortVer)
            {
                StoreModelParameters(context, idealModel.Coeffs);
                StoreModelParameters(context, minimalization.InitialParameters);
                StoreModelParameters(context, minimalization.BestResultVector);
            }
            else
            {
                context.Output.Append("Ideal parameters: ");
                StoreModelParameters(context, idealModel.Coeffs);
                context.Output.Append("Initial parameters: ");
                StoreModelParameters(context, minimalization.InitialParameters);
                context.Output.Append("Final parameters: ");
                StoreModelParameters(context, minimalization.BestResultVector);
            }
            context.Output.AppendLine();
        }

        public static void StoreModelParameters(Context context, IEnumerable<double> par)
        {
            foreach(var p in par)
            {
                context.Output.Append(p.ToString("E3") + ", ");
            }
            context.Output.AppendLine();
        }

        public static void StoreTestInfo(Context context, int pointCount, double meanRadius, double deviation)
        {
            context.Output.AppendLine("Number of Points: " + pointCount);
            context.Output.AppendLine("Mean Radius: " + meanRadius.ToString("F3"));
            context.Output.AppendLine("Noise Deviation: " + deviation.ToString("F4"));
            context.Output.AppendLine();
        }

        public static void StoreMinimalizationInfo(Context context, MinimalisationAlgorithm minimalization, bool shortVer = false)
        {
            if(shortVer)
            {
                context.Output.AppendLine(minimalization.BaseResidiual.ToString("E3"));
                context.Output.AppendLine(minimalization.MinimumResidiual.ToString("E3"));
            }
            else
            {
                context.Output.AppendLine("Minimalization results:");
                context.Output.AppendLine("Initial residual: " + minimalization.BaseResidiual.ToString("E3"));
                context.Output.AppendLine("Final residual: " + minimalization.MinimumResidiual.ToString("E3"));
                context.Output.AppendLine("Iterations: " + minimalization.CurrentIteration);
            }
            context.Output.AppendLine();
        }

        public static RadialDistortionQuadricFitMinimalisation PrepareMinimalizationAlgorithm(
            RadialDistortionModel model, List<List<Vector2>> distortedLines, double maxError = 0.001, int iters = 50, bool findInitialParameters = false)
        {
            model.UseNumericDerivative = true;
            model.NumericDerivativeStep = 1e-4;
            
            var minimalization = new RadialDistortionQuadricFitMinimalisation();

            minimalization.DistortionModel = model;
            minimalization.LinePoints = distortedLines;
            minimalization.ParametersVector = new DenseVector(model.Coeffs.Count);
            model.Coeffs.CopyTo(minimalization.ParametersVector);

            minimalization.MaximumResidiual = maxError;  
            minimalization.MaximumIterations = iters;
            minimalization.UseCovarianceMatrix = false;
            minimalization.DoComputeJacobianNumerically = true;
            minimalization.NumericalDerivativeStep = 1e-4;
            minimalization.FindInitialModelParameters = findInitialParameters;

            return minimalization;
        }

        // Default coeffs:
        // L1 : A = 0, B = 1, C = -0.5 (horizontal)
        // L2 : A = -1, B = 1, C = 0
        // L3 : A = 1, B = 0, C = 1 (vertical)
        public static List<List<Vector2>> GeneratePointLines(int lineLength = 10, List<Line2D> coeffs = null, double min = 0.0, double max = 1.0)
        {
            List<List<Vector2>> newLines = new List<List<Vector2>>();
            
            if(coeffs == null)
            {
                coeffs = new List<Line2D>()
                {
                    new Line2D(0, -1, -0.5), new Line2D(-1, 1, 0), new Line2D(1, 0, 1)
                };
            }

            foreach(var line in coeffs)
            {
                var newLine = new List<Vector2>(lineLength);
                double dx = (max-min) / lineLength;
                for(int p = 0; p < lineLength; ++p)
                {
                    if(line.Direction == Line2D.LineDirection.Vertical)
                    {
                        newLine.Add(line.GetPointForY(min + p * dx));
                    }
                    else
                    {
                        newLine.Add(line.GetPointForX(min + p * dx));
                    }
                }
                newLines.Add(newLine);
            }

            return newLines;
        }
        
        public static List<List<Vector2>> AddNoiseToLines(List<List<Vector2>> lines, double variance = 0.02, int seed = 0)
        {
            List<List<Vector2>> noised = new List<List<Vector2>>();
            for(int l = 0; l < lines.Count; ++l)
            {
                noised.Add(TestUtils.AddNoise(lines[l], variance, seed));
            }
            return noised;
        }

        public static List<List<Vector2>> DistortLines(RadialDistortionModel model, List<List<Vector2>> realLines)
        {
            List<List<Vector2>> dlines;
            dlines = new List<List<Vector2>>(realLines.Count);

            for(int l = 0; l < realLines.Count; ++l)
            {
                List<Vector2> rline = realLines[l];
                List<Vector2> dline = new List<Vector2>(rline.Count);
                for(int p = 0; p < rline.Count; ++p)
                {
                    dline.Add(model.Distort(rline[p]));
                }

                dlines.Add(dline);
            }

            return dlines;
        }

        public static List<List<Vector2>> UndistortLines(RadialDistortionModel model, List<List<Vector2>> realLines)
        {
            List<List<Vector2>> dlines;
            dlines = new List<List<Vector2>>(realLines.Count);

            for(int l = 0; l < realLines.Count; ++l)
            {
                List<Vector2> rline = realLines[l];
                List<Vector2> dline = new List<Vector2>(rline.Count);
                for(int p = 0; p < rline.Count; ++p)
                {
                    dline.Add(model.Undistort(rline[p]));
                }

                dlines.Add(dline);
            }

            return dlines;
        }

        public static int GetPointsCount(List<List<Vector2>> lines)
        {
            int c = 0;
            for(int l = 0; l < lines.Count; ++l)
            {
                c += lines[l].Count;
            }
            return c;
        }

        public static double GetMeanRadius(List<List<Vector2>> lines, Vector2 center)
        {
            double r = 0;
            for(int l = 0; l < lines.Count; ++l)
            {
                for(int i = 0; i < lines[l].Count; ++i)
                {
                    r += lines[l][i].DistanceToSquared(center);
                }
            }
            r /= GetPointsCount(lines);
            return r;
        }

        // Generate 12 lines in range [0,1]
        public List<List<Vector2>> GenerateTestLines_Many()
        {
            int linesCount = 12;
            int pointsInLine = 10;
            Random rand = new Random();
            List<List<Vector2>> realLines = new List<List<Vector2>>();
            // Predefined starts/ends of lines
            Vector2[] pi = {
                new Vector2(0.0, 0.0),
                new Vector2(0.0, 0.3),
                new Vector2(0.0, 0.7),
                new Vector2(0.0, 1.0),

                new Vector2(0.0, 0.0),
                new Vector2(0.3, 0.0),
                new Vector2(0.7, 0.0),
                new Vector2(1.0, 0.0),

                new Vector2(0.1, 0.5),
                new Vector2(0.5, 0.9),
                new Vector2(0.9, 0.5),
                new Vector2(0.5, 0.1)
            };

            Vector2[] pf = {
                new Vector2(1.0, 0.0),
                new Vector2(1.0, 0.3),
                new Vector2(1.0, 0.7),
                new Vector2(1.0, 1.0),

                new Vector2(0.0, 1.0),
                new Vector2(0.3, 1.0),
                new Vector2(0.7, 1.0),
                new Vector2(1.0, 1.0),

                new Vector2(0.5, 0.9),
                new Vector2(0.9, 0.5),
                new Vector2(0.5, 0.1),
                new Vector2(0.1, 0.5)
            };

            for(int i = 0; i < linesCount; ++i)
            {
                List<Vector2> line = new List<Vector2>();
                // Create 10 points between pi and pf (inclusive)

                for(int p = 0; p < pointsInLine; ++p)
                {
                    Vector2 point = new Vector2();
                    point.X = pi[i].X + (double)p / (double)(pointsInLine - 1) * (pf[i].X - pi[i].X);
                    point.Y = pi[i].Y + (double)p / (double)(pointsInLine - 1) * (pf[i].Y - pi[i].Y);

                    line.Add(point);
                }

                realLines.Add(line);
            }
            return realLines;
        }
    }

    public class PolynomialModel : RadialDistortionModel
    {
        public override string Name => "Polynomial24";
        public override int ParametersCount => 4;
        
        private double _k1 => Coeffs[0];
        private double _k2 => Coeffs[1];
        private double _cx => Coeffs[2];
        private double _cy => Coeffs[3];

        public PolynomialModel()
        {
            AllocateWhatNeeded();
        }

        public PolynomialModel(double k1, double k2, double cx, double cy)
        {
            AllocateWhatNeeded();
            Coeffs[0] = k1;
            Coeffs[1] = k2;
            Coeffs[2] = cx;
            Coeffs[3] = cy;
            InitialCenterEstimation = new Vector2(_cx, _cy);
            DistortionCenter = new Vector2(_cx, _cy);
        }

        public PolynomialModel(Vector<double> par)
        {
            AllocateWhatNeeded();
            par.CopyTo(Coeffs);
            InitialCenterEstimation = new Vector2(_cx, _cy);
            DistortionCenter = new Vector2(_cx, _cy);
        }

        void AllocateWhatNeeded()
        {
            Coeffs = new DenseVector(ParametersCount);
            Pu = new Vector2();
            Pd = new Vector2();
            Pf = new Vector2();
            InitialCenterEstimation = new Vector2();
            DistortionCenter = new Vector2();
        }

        public override void FullUpdate()
        {
        }

        public override void InitCoeffs()
        {
        }

        public override void SetInitialParametersFromQuadrics(List<Quadric> quadrics, List<List<Vector2>> linePoints, List<int> fitPoints)
        {
            InitCoeffs();
        }

        public override void Distort()
        {
            Pu.X = (P.X - _cx);
            Pu.Y = (P.Y - _cy);

            Ru = Math.Sqrt(Pu.X * Pu.X + Pu.Y * Pu.Y);
            double r2 = Ru * Ru, r4 = r2 * r2;
            Pf.X = Pu.X * (1 + _k1 * r2 + _k2 * r4) + _cx;
            Pf.Y = Pu.Y * (1 + _k1 * r2 + _k2 * r4) + _cy;
        }

        public override void Undistort()
        {
            throw new NotImplementedException();
        }
    }
}
