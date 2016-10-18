using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamImageProcessing.ImageMatching
{
    public class MutualInformationCostComputer
    {
        int[,] _discreteImageBase;
        int[,] _discreteImageMatched;

        int[] _probabilityMapBase;
        int[] _probabilityMapMatched;
        int[,] _probabilityMapMutual;

        double[] _entropyBase;
        double[] _entropyMatched;
        double[,] _entropyMutual;

        // Kernel
        //0.003386	0.014235	0.022945	0.014235	0.003386
        //0.014235	0.059853	0.096472	0.059853	0.014235
        //0.022945	0.096472	0.155494	0.096472	0.022945
        //0.014235	0.059853	0.096472	0.059853	0.014235
        //0.003386	0.014235	0.022945	0.014235	0.003386
        double[,] _gauss55 =
        {
           { 0.003386, 0.014235, 0.022945, 0.014235, 0.003386 },
           { 0.014235, 0.059853, 0.096472, 0.059853, 0.014235 },
           { 0.022945, 0.096472, 0.155494, 0.096472, 0.022945 },
           { 0.014235, 0.059853, 0.096472, 0.059853, 0.014235 },
           { 0.003386, 0.014235, 0.022945, 0.014235, 0.003386 }
        };

        public void ComputeDiscreteImages()
        {

        }

        public void ComputeProbabilities()
        {

        }

        public void ComputeEntropyImages()
        {

        }
    }
}
