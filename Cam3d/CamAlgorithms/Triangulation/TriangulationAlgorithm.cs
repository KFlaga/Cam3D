using CamAlgorithms.Calibration;
using CamCore;
using MathNet.Numerics.LinearAlgebra;
using System.Collections.Generic;

namespace CamAlgorithms.Triangulation
{
    public class TriangulationAlgorithm : IParameterizable
    {
        public List<TriangulatedPoint> Points { get; set; } // Input / ouput (changes after run)
        public CameraPair Cameras { get; set; }
        
        public enum TriangulationMethod
        {
            TwoPointsLinear,
            TwoPointsRectified,
            TwoPointsEpilineFit
        }

        public TwoPointsTriangulation Algorithm { get; set; }

        private TriangulationMethod _method;
        public TriangulationMethod Method
        {
            get { return _method; }
            set
            {
                _method = value;
                switch(value)
                {
                    case TriangulationMethod.TwoPointsRectified:
                        Algorithm = new TwoPointsTriangulation()
                        {
                            UseLinearEstimationOnly = false,
                            Rectified = true
                        };
                        break;
                    case TriangulationMethod.TwoPointsEpilineFit:
                        Algorithm = new TwoPointsTriangulation()
                        {
                            UseLinearEstimationOnly = false,
                            Rectified = false
                        };
                        break;
                    case TriangulationMethod.TwoPointsLinear:
                    default:
                        Algorithm = new TwoPointsTriangulation()
                        {
                            UseLinearEstimationOnly = true,
                            Rectified = false
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

            Algorithm.Estimate3DPoints();
            for(int i = 0; i < Points.Count; ++i)
            {
                Points[i].Real = new Vector3(Algorithm.Points3D[i]);
                Points[i].ImageLeft = new Vector2(Algorithm.PointsLeftOut[i]);
                Points[i].ImageRight = new Vector2(Algorithm.PointsRightOut[i]);
            }
        }

        public void Terminate()
        {
            Algorithm.Terminate = true;
        }

        public string Name { get { return "Triangulation Algorithm: " + Method.ToString(); } }

        public List<IAlgorithmParameter> Parameters { get; protected set; }

        public void InitParameters()
        {
            Parameters = new List<IAlgorithmParameter>();

            DictionaryParameter methodParam =
                new DictionaryParameter("Triangulation Method", "Method", TriangulationMethod.TwoPointsEpilineFit);

            methodParam.ValuesMap = new Dictionary<string, object>()
            {
                { "Two Point Algorithm With Epiline Fit Error Minimalization", TriangulationMethod.TwoPointsEpilineFit },
                { "Two Point Algorithm Linear", TriangulationMethod.TwoPointsLinear },
                { "Two Point Algorithm Ideal Rectified", TriangulationMethod.TwoPointsRectified }
            };

            Parameters.Add(methodParam);
        }

        public void UpdateParameters()
        {
            Method = IAlgorithmParameter.FindValue<TriangulationMethod>("Method", Parameters);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
