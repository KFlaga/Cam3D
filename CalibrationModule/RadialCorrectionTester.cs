using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalibrationModule
{
    // Generates set of lines, applies known correction, computes specified model parameters and corrects points
    public class RadialCorrectionTester
    {
        public RadialDistortionModel RealModel { get; set; } = new Rational3TestModel();
        public RadialDistortionModel TestedModel { get; set; } = new Rational3RDModel();
        public List<List<Vector2>> RealLines { get; set; }
        public List<List<Vector2>> RealDistortedLines { get; set; }
        public List<List<Vector2>> CorrectedLines { get; set; }
        public LMDistortionBasicLineFitMinimalisation Minimalisation { get; set; } = new LMDistortionBasicLineFitMinimalisation();

        public RadialCorrectionTester()
        {
            RealModel.InitialCenterEstimation = new Vector2(x: 300, y: 280);
            RealModel.InitialAspectEstimation = 1;

            TestedModel.InitialCenterEstimation = new Vector2(x: 320, y: 240);
            TestedModel.InitialAspectEstimation = 1;

            Minimalisation.MaximumResidiual = 20.0;
            Minimalisation.MaximumIterations = 100;
            Minimalisation.DistortionModel = TestedModel;
        }

        public void Test()
        {
            RealModel.InitParameters();
            TestedModel.InitParameters();
            GetTestLines();

            TestCorrectionInverse();

            //_minimalisation.LinePoints = _realDistortedLines;
            //_minimalisation.MeasurementsVector = new DenseVector(_realDistortedLines.Count);

            //Vector<double> parVec = new DenseVector(_testedModel.ParametersCount);
            //_testedModel.Parameters.CopyTo(parVec);
            //_minimalisation.ParametersVector = parVec;
            //_minimalisation.UseMultiplicativeDumping = true;

            //_minimalisation.Process();

            //_minimalisation.ResultsVector.CopyTo(_testedModel.Parameters);
        }

        public void TestCorrectionInverse()
        {
            RealModel.InitParameters();
            TestedModel.InitParameters();
            RealModel.Parameters.CopyTo(TestedModel.Parameters);
            GetTestLines();

            CorrectedLines = new List<List<Vector2>>();
            double error = 0.0;
            for(int l = 0; l < RealLines.Count; ++l)
            {
                var dline = RealDistortedLines[l];
                List<Vector2> cline = new List<Vector2>(dline.Count);

                for(int p = 0; p < dline.Count; ++p)
                {
                    TestedModel.P = dline[p];
                    TestedModel.Undistort();
                    cline.Add(new Vector2(TestedModel.Pf));

                    error += RealLines[l][p].DistanceTo(TestedModel.Pf);
                }
                CorrectedLines.Add(cline);
            }
        }

        public void GetTestLines()
        {
            RealLines = GenerateLines();
            RealDistortedLines = new List<List<Vector2>>(RealLines.Count);
            for(int i = 0; i < RealLines.Count; ++i)
            {
                var line = RealLines[i];
                List<Vector2> dline = new List<Vector2>(line.Count);
                for(int p = 0; p < line.Count; ++p)
                {
                    RealModel.P = line[p];
                    RealModel.Distort();
                    dline.Add(new Vector2(RealModel.Pf));
                }
                RealDistortedLines.Add(dline);
            }
        }

        public List<List<Vector2>> GenerateLines()
        {
            double img_w = 640.0;
            double img_h = 480.0;
            int linesCount = 20;
            int pointsCount = 10;
            Random rand = new Random();
            List<List<Vector2>> realLines = new List<List<Vector2>>();

            for(int i = 0; i < linesCount; ++i)
            {
                List<Vector2> line = new List<Vector2>();

                // Generate 2 random points on image
                Vector2 x1 = new Vector2(x: rand.NextDouble() * img_w, y: rand.NextDouble() * img_h);
                Vector2 x2 = new Vector2(x: rand.NextDouble() * img_w, y: rand.NextDouble() * img_h);
                // Generate points on line between those points : 
                // points on line : x1 + k(x2-x1), where k = [0,1]

                for(int p = 0; p < pointsCount; ++p)
                {
                    Vector2 point = new Vector2();
                    double k = rand.NextDouble();
                    point.X = x1.X + k * (x2.X - x1.X);
                    point.Y = x1.Y + k * (x2.Y - x1.Y);
                    line.Add(point);
                }

                realLines.Add(line);
            }
            return realLines;
        }


    }

    // Models rd=R(ru) = ru(1 + k1ru^2 + k2ru^4)
    // Undistort function actually distorts points applying xf = (xi-cx)*R(ru)/ru + cx
    class Polynomial2TestModel : RadialDistortionModel
    {
        public override int ParametersCount
        {
            get
            {
                return 4;
            }
        }

        private int _k1Idx { get { return 0; } }
        private int _k2Idx { get { return 1; } }
        private int _cxIdx { get { return 2; } }
        private int _cyIdx { get { return 3; } }
        private double _k1 { get { return Parameters[_k1Idx]; } }
        private double _k2 { get { return Parameters[_k2Idx]; } }
        private double _cx { get { return Parameters[_cxIdx]; } }
        private double _cy { get { return Parameters[_cyIdx]; } }

        public Polynomial2TestModel()
        {
            Parameters = new DenseVector(ParametersCount);

            Pu = new Vector2();
            Pd = new Vector2();
            Pf = new Vector2();
        }

        public override void InitParameters()
        {
            Parameters[_k1Idx] = 1e-6;
            Parameters[_k2Idx] = 1e-14;
            Parameters[_cxIdx] = InitialCenterEstimation.X;
            Parameters[_cyIdx] = InitialCenterEstimation.Y;
        }

        public override void FullUpdate()
        {
            throw new NotImplementedException();
        }

        public override void Undistort()
        {
            throw new NotImplementedException();
        }

        public override void Distort()
        {
            Pu.X = (P.X - _cx);
            Pu.Y = (P.Y - _cy);

            Ru = Math.Sqrt(Pu.X * Pu.X + Pu.Y * Pu.Y);
            Pf.X = Pu.X * (1 + _k1 * Ru * Ru + _k2 * Ru * Ru * Ru * Ru) + _cx;
            Pf.Y = Pu.Y * (1 + _k1 * Ru * Ru + _k2 * Ru * Ru * Ru * Ru) + _cy;
        }
    }

    // Rational model of distortion function :
    //                        1 + k1*ru
    // rd = R(ru) = ru * -------------------
    //                   1 + k2*ru + k3*ru^2
    // 
    class Rational3TestModel : RadialDistortionModel
    {
        public override int ParametersCount
        {
            get
            {
                return 6;
            }
        }

        private int _k1Idx { get { return 0; } }
        private int _k2Idx { get { return 1; } }
        private int _k3Idx { get { return 2; } }
        private int _cxIdx { get { return 3; } }
        private int _cyIdx { get { return 4; } }
        private int _sxIdx { get { return 5; } }
        private double _k1 { get { return Parameters[_k1Idx]; } }
        private double _k2 { get { return Parameters[_k2Idx]; } }
        private double _k3 { get { return Parameters[_k2Idx]; } }
        private double _cx { get { return Parameters[_cxIdx]; } }
        private double _cy { get { return Parameters[_cyIdx]; } }
        private double _sx { get { return Parameters[_sxIdx]; } }

        public Rational3TestModel()
        {
            Parameters = new DenseVector(ParametersCount);

            Pu = new Vector2();
            Pd = new Vector2();
            Pf = new Vector2();
        }

        public override void InitParameters()
        {
            Parameters[_k1Idx] = 1e-4;
            Parameters[_k2Idx] = -1e-6;
            Parameters[_k3Idx] = 1e-8;
            Parameters[_cxIdx] = InitialCenterEstimation.X;
            Parameters[_cyIdx] = InitialCenterEstimation.Y;
            Parameters[_sxIdx] = 1;
        }

        public override void FullUpdate()
        {
            throw new NotImplementedException();
        }

        public override void Undistort()
        {
            throw new NotImplementedException();
        }

        public override void Distort()
        {
            Pu.X = (P.X - _cx);
            Pu.Y = (P.Y - _cy);

            Ru = Math.Sqrt(Pu.X * Pu.X + Pu.Y * Pu.Y);
            Pf.X = Pu.X * (1 + _k1 * Ru) / (1 + _k2 * Ru + _k3 * Ru * Ru) + _cx;
            Pf.Y = Pu.Y * (1 + _k1 * Ru) / (1 + _k2 * Ru + _k3 * Ru * Ru) + _cy;
        }
    }
}
