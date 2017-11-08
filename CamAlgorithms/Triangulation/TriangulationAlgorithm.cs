using CamAlgorithms.Calibration;
using CamCore;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamAlgorithms.Triangulation
{
    public abstract class TriangulationComputer
    {
        public CameraPair Cameras { get; set; }
        public bool Rectified { get; set; }

        public List<Vector<double>> PointsLeft { get; set; }
        public List<Vector<double>> PointsRight { get; set; }
        public List<Vector<double>> Points3D { get; protected set; }

        public bool Terminate { get; set; }
        public int CurrentPoint { get; protected set; }

        public abstract void Estimate3DPoints();
    }

    public class TriangulationAlgorithm : IParameterizable
    {
        public List<TriangulatedPoint> Points { get; set; }

        public CameraPair Cameras { get; set; }
        public bool Recitifed { get; set; }
        
        public enum TriangulationMethod
        {
            TwoPointsLinear,
            TwoPointsEpilineFit
        }

        public TriangulationComputer Algorithm { get; set; }

        private TriangulationMethod _method;
        public TriangulationMethod Method
        {
            get { return _method; }
            set
            {
                _method = value;
                switch(value)
                {
                    case TriangulationMethod.TwoPointsEpilineFit:
                        Algorithm = new TwoPointsTriangulation()
                        {
                            UseLinearEstimationOnly = false
                        };
                        break;
                    case TriangulationMethod.TwoPointsLinear:
                    default:
                        Algorithm = new TwoPointsTriangulation()
                        {
                            UseLinearEstimationOnly = true
                        };
                        break;
                }
            }
        }

        public int CurrentPoint { get { return Algorithm.CurrentPoint; } }

        public void Find3DPoints()
        {
            if(Points == null || Points.Count == 0)
            {
                return;
            }

            Algorithm.Terminate = false;
            Algorithm.PointsLeft = new List<Vector<double>>(Points.Count);
            Algorithm.PointsRight = new List<Vector<double>>(Points.Count);
            for(int i = 0; i < Points.Count; ++i)
            {
                Algorithm.PointsLeft.Add(Points[i].ImageLeft.ToMathNetVector3());
                Algorithm.PointsRight.Add(Points[i].ImageRight.ToMathNetVector3());
            }
            Algorithm.Cameras = Cameras;
            Algorithm.Rectified = false; // Recitifed;

            Algorithm.Estimate3DPoints();
            for(int i = 0; i < Points.Count; ++i)
            {
                Points[i].Real = new Vector3(Algorithm.Points3D[i]);
            }
        }

        public void Terminate()
        {
            Algorithm.Terminate = true;
        }

        public string Name { get { return "Triangulation Algorithm: " + Method.ToString(); } }

        public List<AlgorithmParameter> Parameters { get; protected set; }

        public void InitParameters()
        {
            Parameters = new List<AlgorithmParameter>();

            DictionaryParameter methodParam =
                new DictionaryParameter("Triangulation Method", "Method", TriangulationMethod.TwoPointsEpilineFit);

            methodParam.ValuesMap = new Dictionary<string, object>()
            {
                { "Two Point Algorithm With Epiline Fit Error Minimalization", TriangulationMethod.TwoPointsEpilineFit },
                { "Two Point Algorithm Linear", TriangulationMethod.TwoPointsLinear }
            };

            Parameters.Add(methodParam);
        }

        public void UpdateParameters()
        {
            Method = AlgorithmParameter.FindValue<TriangulationMethod>("Method", Parameters);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
