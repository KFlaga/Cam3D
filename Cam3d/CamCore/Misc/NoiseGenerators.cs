using System;
using System.Collections.Generic;
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
        public abstract void DisturbMatrix(Matrix<double> matToBeDisturbed);
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

        public override double GetSample()
        {
            return (double)_gauss.Sample();
        }

        public override void DisturbVector(Vector<double> vecToBeDisturbed)
        {
            double[] samples = new double[vecToBeDisturbed.Count];
            _gauss.Samples(samples);
            for(int i = 0; i < vecToBeDisturbed.Count; ++i)
                vecToBeDisturbed.At(i, vecToBeDisturbed.At(i) + (double)samples[i]);
        }

        public override void DisturbMatrix(Matrix<double> matToBeDisturbed)
        {
            double[] samples = new double[matToBeDisturbed.RowCount * matToBeDisturbed.ColumnCount];
            _gauss.Samples(samples);
            int i = 0;
            for(int c = 0; c < matToBeDisturbed.ColumnCount; ++c)
            { 
                for(int r = 0; r < matToBeDisturbed.RowCount; ++r)
                {
                    matToBeDisturbed.At(r, c,
                       matToBeDisturbed.At(r, c) + samples[i]);
                    ++i;
                }
            }
        }
    }
}
