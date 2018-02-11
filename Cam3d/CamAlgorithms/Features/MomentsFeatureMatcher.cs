using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;

namespace CamAlgorithms
{
    public class MomentsFeatureMatcher : FeaturesMatcher
    {
        public int WindowRadius { get; set; }
        public bool UseMahalanobis { get; set; }
        public bool UseCenteredMoments { get; set; }
        public bool UseScaledMoments { get; set; }
        
        int[] _ybounds;

        List<Vector<double>> _leftMoments; // Contains moment invariants of image feature points
        List<Vector<double>> _rightMoments;
        
        public override void Match()
        {
            int r21 = 2 * WindowRadius + 1;
            _leftMoments = new List<Vector<double>>();
            _rightMoments = new List<Vector<double>>();

            // Find circural mask bounds:
            // for x = [-r,r] -> y = [ -sqrt(r^2 - x^2), sqrt(r^2 - x^2) ]
            _ybounds = new int[r21];
            for(int x = -WindowRadius; x <= WindowRadius; ++x)
            {
                _ybounds[x + WindowRadius] = (int)Math.Sqrt(WindowRadius * WindowRadius - x * x);
            }

            // Find moment vectors for each feature point
            for(int i = 0; i < LeftFeaturePoints.Count; ++i)
            {
                IntVector2 pixel = LeftFeaturePoints[i];
                if(pixel.X < WindowRadius || pixel.Y < WindowRadius ||
                    pixel.X >= LeftImage.ColumnCount - WindowRadius ||
                    pixel.Y >= LeftImage.RowCount - WindowRadius)
                {
                    _leftMoments.Add(null);
                }
                else
                {
                    Vector<double> mom = ComputeMomentVectorForPatch(pixel, LeftImage);
                    if(UseCenteredMoments)
                        mom = ComputeCenteredMomentVector(mom);
                    if(UseScaledMoments)
                        ScaleMomentVector(mom);
                    Vector<double> invMom = ComputeInvariantMomentVector(mom);
                    _leftMoments.Add(invMom);
                }
            }

            for(int i = 0; i < RightFeaturePoints.Count; ++i)
            {
                IntVector2 pixel = RightFeaturePoints[i];
                if(pixel.X < WindowRadius || pixel.Y < WindowRadius ||
                    pixel.X >= RightImage.ColumnCount - WindowRadius ||
                    pixel.Y >= RightImage.RowCount - WindowRadius)
                {
                    _rightMoments.Add(null);
                }
                else
                {
                    Vector<double> mom = ComputeMomentVectorForPatch(pixel, RightImage);
                    if(UseCenteredMoments)
                        mom = ComputeCenteredMomentVector(mom);
                    if(UseScaledMoments)
                        ScaleMomentVector(mom);
                    Vector<double> invMom = ComputeInvariantMomentVector(mom);
                    _rightMoments.Add(invMom);
                }
            }

            // We need to find covariance matrix of invariants as they have 
            // different magnitudes, so simple ||Il - Ir|| may not be best choice
            // E = 1/(n-1) sum( (xi-m)(xi-m)^T ) (x is column vector, m = 1/n sum(xi)
            // 1) Find mean
            int n = 0;
            Vector<double> meanInv = new DenseVector(_invCount);
            for(int i = 0; i < _leftMoments.Count; ++i)
            {
                if(_leftMoments[i] != null)
                {
                    meanInv.PointwiseAddThis(_leftMoments[i]);
                    ++n;
                }
            }
            for(int i = 0; i < _rightMoments.Count; ++i)
            {
                if(_rightMoments[i] != null)
                {
                    meanInv.PointwiseAddThis(_rightMoments[i]);
                    ++n;
                }
            }
            meanInv.MultiplyThis(1.0 / n);
            // 2) Find E
            Matrix<double> cov = new DenseMatrix(_invCount, _invCount);
            for(int i = 0; i < _leftMoments.Count; ++i)
            {
                if(_leftMoments[i] != null)
                {
                    cov.PointwiseAddThis(CamCore.MatrixExtensions.FromVectorProduct(_leftMoments[i] - meanInv));
                }
            }
            for(int i = 0; i < _rightMoments.Count; ++i)
            {
                if(_rightMoments[i] != null)
                {
                    cov.PointwiseAddThis(CamCore.MatrixExtensions.FromVectorProduct(_rightMoments[i] - meanInv));
                }
            }
            cov.MultiplyThis(1.0 / (n - 1));
            var covInv = cov.Inverse();

            // Match each point pair and find ||Il - Ir||E
            List<MatchedPair> costs;
            var matchLeft = new List<MatchedPair>();
            var matchRight = new List<MatchedPair>();
            for(int l = 0; l < LeftFeaturePoints.Count; ++l)
            {
                costs = new List<MatchedPair>(LeftFeaturePoints.Count);
                if(_leftMoments[l] != null)
                {
                    for(int r = 0; r < RightFeaturePoints.Count; ++r)
                    {
                        if(_rightMoments[r] != null)
                        {
                            var d = _leftMoments[l] - _rightMoments[r];
                            costs.Add(new MatchedPair()
                            {
                                LeftPoint = new Vector2(LeftFeaturePoints[l]),
                                RightPoint = new Vector2(RightFeaturePoints[r]),
                                Cost = UseMahalanobis ? d * covInv * d : // cost = d^T * E^-1 * d
                                    d.DotProduct(d)
                            });
                        }
                    }
                    costs.Sort((c1, c2) => { return c1.Cost > c2.Cost ? 1 : (c1.Cost < c2.Cost ? -1 : 0); });
                    // Confidence will be (c2-c1)/(c1+c2)
                    MatchedPair match = costs[0];
                    match.Confidence = (costs[1].Cost - costs[0].Cost) / (costs[1].Cost + costs[0].Cost);
                    matchLeft.Add(match);
                }
            }

            for(int r = 0; r < RightFeaturePoints.Count; ++r)
            {
                costs = new List<MatchedPair>(RightFeaturePoints.Count);
                if(_rightMoments[r] != null)
                {
                    for(int l = 0; l < LeftFeaturePoints.Count; ++l)
                    {
                        if(_leftMoments[l] != null)
                        {
                            var d = _leftMoments[l] - _rightMoments[r];
                            costs.Add(new MatchedPair()
                            {
                                LeftPoint = new Vector2(LeftFeaturePoints[l]),
                                RightPoint = new Vector2(RightFeaturePoints[r]),
                                Cost = UseMahalanobis ? d * covInv * d : // cost = d^T * E^-1 * d
                                    d.DotProduct(d)
                            });
                        }
                    }
                    costs.Sort((c1, c2) => { return c1.Cost > c2.Cost ? 1 : (c1.Cost < c2.Cost ? -1 : 0); });
                    // Confidence will be (c2-c1)/(c1+c2)
                    MatchedPair match = costs[0];
                    match.Confidence = (costs[1].Cost - costs[0].Cost) / (costs[1].Cost + costs[0].Cost);
                    matchRight.Add(match);
                }
            }

            Matches = new List<MatchedPair>();
            foreach(var ml in matchLeft)
            {
                MatchedPair mr = matchRight.Find((m) => { return ml.LeftPoint.DistanceTo(m.LeftPoint) < 0.01; });
                // We have both sides matches
                if(mr != null && ml.RightPoint.DistanceTo(mr.RightPoint) < 0.01)
                {
                    // Cross check sucessful
                    Matches.Add(mr);
                }
            }

        }

        const int m00 = 0; const int m01 = 1;
        const int m10 = 2; const int m11 = 3;
        const int m02 = 4; const int m20 = 5;
        const int m12 = 6; const int m21 = 7;
        const int m03 = 8; const int m30 = 9;
        // Moments vector : [m00, m01, m10, m11, m02, m20, m12, m21, m03, m30]
        Vector<double> ComputeMomentVectorForPatch(IntVector2 centerPixel, IImage image)
        {
            Vector<double> moments = new DenseVector(10);
            // M00 = total_intensity
            // Mpq = sum{x,y}( x^p*y^q*I(x,y) )
            for(int dx = -WindowRadius; dx <= WindowRadius; ++dx)
            {
                for(int dy = -_ybounds[dx + WindowRadius]; dy <= _ybounds[dx + WindowRadius]; ++dy)
                {
                    if(image.HaveValueAt(centerPixel.Y + dy, centerPixel.X + dx))
                    {
                        double val = image[centerPixel.Y + dy, centerPixel.X + dx];
                        moments[m00] += val;
                        moments[m01] += dy * val;
                        moments[m10] += dx * val;

                        moments[m11] += dx * dy * val;
                        moments[m02] += dy * dy * val;
                        moments[m20] += dx * dx * val;

                        moments[m12] += dx * dy * dy * val;
                        moments[m21] += dx * dx * dy * val;
                        moments[m03] += dy * dy * dy * val;
                        moments[m30] += dx * dx * dx * val;
                    }
                }
            }
            return moments;
        }

        // Moments vector : [m00, m01, m10, m11, m02, m20, m12, m21, m03, m30]
        Vector<double> ComputeCenteredMomentVector(Vector<double> moments)
        {
            Vector<double> centralMoments = new DenseVector(10);
            double xmean = moments[m10] / moments[m00];
            double ymean = moments[m01] / moments[m00];
            centralMoments[m00] = moments[m00]; // m00
            centralMoments[m01] = 0.0; // m01
            centralMoments[m10] = 0.0; // m10
            centralMoments[m11] = (moments[m11] - xmean * moments[m01]); // m11
            centralMoments[m02] = (moments[m02] - xmean * moments[m10]); // m02
            centralMoments[m20] = (moments[m20] - ymean * moments[m01]); // m20
            centralMoments[m12] = (moments[m12] - 2 * ymean * moments[m11] - xmean * moments[m02] + 2 * ymean * ymean * moments[m10]); // m12
            centralMoments[m21] = (moments[m21] - 2 * xmean * moments[m11] - ymean * moments[m20] + 2 * xmean * xmean * moments[m01]); // m21
            centralMoments[m03] = (moments[m03] - 3 * ymean * moments[m02] + 2 * ymean * ymean * moments[m01]); // m03
            centralMoments[m30] = (moments[m30] - 3 * xmean * moments[m20] + 2 * xmean * xmean * moments[m10]); // m30
            return centralMoments;
        }

        void ScaleMomentVector(Vector<double> moments)
        {
            double scale1 = 1.0 / (moments[m00]);
            double scale2 = 1.0 / (moments[m00] * moments[m00]);
            double scale3 = 1.0 / (moments[m00] * moments[m00] * moments[m00]);
            moments[m01] = moments[m01] * scale1; // m01
            moments[m10] = moments[m10] * scale1; // m10
            moments[m11] = moments[m11] * scale2; // m11
            moments[m02] = moments[m02] * scale2; // m02
            moments[m20] = moments[m20] * scale2; // m20
            moments[m12] = moments[m12] * scale3; // m12
            moments[m21] = moments[m21] * scale3; // m21
            moments[m03] = moments[m03] * scale3; // m03
            moments[m30] = moments[m30] * scale3; // m30
        }

        int _invCount = 7;
        Vector<double> ComputeInvariantMomentVector(Vector<double> m)
        {
            Vector<double> invMoments = new DenseVector(_invCount);
            // I1 = m20 + m02
            invMoments[0] = m[m20] + m[m02];
            // I2 = (m20 + m02)^2 + 4m11^2
            invMoments[1] = (m[m20] + m[m02]) * (m[m20] + m[m02]) + 4 * m[m11] * m[m11];
            // I3 = (m30 + m12)^2 + (m21 + m03)^2
            invMoments[2] = (m[m30] + m[m12]) * (m[m30] + m[m12]) + (m[m21] + m[m03]) * (m[m21] + m[m03]);
            // I4 = (m30 - 3m12)*(m30 + m12)[(m30+m12)^2 - 3(m21+m03)^2] + (3m21-m03)(m21+m03)[3(m30+m12)^2-(m21+m03)^2]
            invMoments[3] = (m[m30] - 3 * m[m12]) * (m[m30] + m[m12]) *
                ((m[m30] + m[m12]) * (m[m30] + m[m12]) - 3 * (m[m21] + m[03]) * (m[m21] + m[03])) +
                (3 * m[m21] - m[m03]) * (m[m21] + m[m03]) *
                (3 * (m[m30] + m[m12]) * (m[m30] + m[m12]) - (m[m21] + m[03]) * (m[m21] + m[03]));
            // I5 = (m20-m02)[(m30+m12)^2-(m21+m03)^2]+4m11(m30+m12)(m21+m03)
            invMoments[4] = (m[m20] - m[m02]) *
                ((m[m30] + m[m12]) * (m[m30] + m[m12]) - (m[m21] + m[03]) * (m[m21] + m[03])) +
                4 * m[m11] * (m[m30] + m[m12]) * (m[m21] + m[03]);
            // I6 = (3m21-m03)(m30+m12)[(m30+m12)^2-3(m21+m03)^2]-(m30-3m12)(m21+m03)[3(m30+m12)^2-(m21+m03)^2]
            invMoments[5] = (3 * m[m21] - m[m03]) * (m[m30] + m[m12]) *
                ((m[m30] + m[m12]) * (m[m30] + m[m12]) - 3 * (m[m21] + m[03]) * (m[m21] + m[03])) -
                (m[m30] - 3 * m[m12]) * (m[m21] + m[m03]) *
                (3 * (m[m30] + m[m12]) * (m[m30] + m[m12]) - (m[m21] + m[03]) * (m[m21] + m[03]));
            // I7 = m11[(m30+m12)^2-(m03+m21)^2]-(m20-m02)(m30+m12)(m03+m21)
            invMoments[6] = m[m11] * ((m[m30] + m[m12]) * (m[m30] + m[m12]) - (m[m21] + m[03]) * (m[m21] + m[03])) -
                (m[m20] - m[m02]) * (m[m30] + m[m12]) * (m[m21] + m[03]);

             return invMoments;
            //return m;
        }

        public override string Name { get { return "Rotation-Invariant Moments Matcher"; } }

        public override void InitParameters()
        {
            base.InitParameters();

            IAlgorithmParameter windowRadiusParam = new IntParameter(
                "Patch Radius", "WRAD", 4, 1, 20);
            Parameters.Add(windowRadiusParam);

            IAlgorithmParameter mahalParam = new BooleanParameter(
                "Use Mahalanobis Distance", "MAHAL", true);
            Parameters.Add(mahalParam);

            IAlgorithmParameter centerParam = new BooleanParameter(
                "Use Centered Moments", "CENTER", false);
            Parameters.Add(centerParam);

            IAlgorithmParameter scaleParam = new BooleanParameter(
                "Use Scaled Moments", "SCALE", true);
            Parameters.Add(scaleParam);
        }

        public override void UpdateParameters()
        {
            base.UpdateParameters();
            WindowRadius = IAlgorithmParameter.FindValue<int>("WRAD", Parameters);
            UseMahalanobis = IAlgorithmParameter.FindValue<bool>("MAHAL", Parameters);
            UseCenteredMoments = IAlgorithmParameter.FindValue<bool>("CENTER", Parameters);
            UseScaledMoments = IAlgorithmParameter.FindValue<bool>("SCALE", Parameters);
        }
    }
}
