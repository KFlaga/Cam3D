using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace CamCore
{
    public abstract class NoiseGenerator
    {
        public int Seed { get; set; }
        public bool RandomSeed { get; set; }

        public abstract void UpdateDistribution();

        public abstract double GetSample();
        public abstract void DisturbVector(Vector<double> vecToBeDisturbed);
    }

    public class GaussianNoiseGenerator : NoiseGenerator
    {
        protected MathNet.Numerics.Distributions.Normal _gauss;

        private double _mean;
        public double Mean
        {
            get { return _mean; }
            set { _mean = value; }
        }

        private double _variance;
        public double Variance
        {
            get { return _variance; }
            set
            {
                _variance = value;
                _deviation = (double)Math.Sqrt(_variance);
            }
        }

        private double _deviation;
        public double Deviation
        {
            get { return _deviation; }
            set
            {
                _deviation = value;
                _variance = _deviation * _deviation;
            }
        }

        public GaussianNoiseGenerator()
        {
            Seed = 0;
            RandomSeed = false;
            _mean = 0.0f;
            _deviation = 1.0f;
        }

        public override void UpdateDistribution()
        {
            MathNet.Numerics.Random.RandomSource rand;
            if(RandomSeed)
            {
                rand = new MathNet.Numerics.Random.MersenneTwister();
            }
            else
            {
                rand = new MathNet.Numerics.Random.MersenneTwister(Seed);
            }
            _gauss = new MathNet.Numerics.Distributions.Normal(_mean, _deviation, rand);
        }
       
        public override void DisturbVector(Vector<double> vecToBeDisturbed)
        {
            double[] samples = new double[vecToBeDisturbed.Count];
            _gauss.Samples(samples);
            for(int i = 0; i < vecToBeDisturbed.Count; ++i)
                vecToBeDisturbed.At(i, vecToBeDisturbed.At(i) + (double)samples[i]);
        }

        public override double GetSample()
        {
            return (double)_gauss.Sample();
        }
    }
}
