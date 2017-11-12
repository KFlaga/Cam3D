using CamCore;
using System.Collections.Generic;

namespace CamAlgorithms
{
    public class CensusFeatureMatcher : FeaturesMatcher
    {
        public int WindowRadius { get; set; }
        ImageMatching.CensusCostComputer _census = new ImageMatching.CensusCostComputer();

        public override void Match()
        {
            int r21 = 2 * WindowRadius + 1;
            _census.MaskHeight = WindowRadius;
            _census.MaskWidth = WindowRadius;

            _census.ImageBase = LeftImage;
            _census.ImageMatched = RightImage;
            _census.Init();


            // Match each point pair and find ||Il - Ir||E
            List<MatchedPair> costs;
            var matchLeft = new List<MatchedPair>();
            var matchRight = new List<MatchedPair>();
            for(int l = 0; l < LeftFeaturePoints.Count; ++l)
            {
                costs = new List<MatchedPair>(LeftFeaturePoints.Count);

                for(int r = 0; r < RightFeaturePoints.Count; ++r)
                {
                    costs.Add(new MatchedPair()
                    {
                        LeftPoint = new Vector2(LeftFeaturePoints[l]),
                        RightPoint = new Vector2(RightFeaturePoints[r]),
                        Cost = _census.GetCost(LeftFeaturePoints[l], RightFeaturePoints[r])
                    });
                }
                costs.Sort((c1, c2) => { return c1.Cost > c2.Cost ? 1 : (c1.Cost < c2.Cost ? -1 : 0); });
                // Confidence will be (c2-c1)/(c1+c2)
                MatchedPair match = costs[0];
                match.Confidence = (costs[1].Cost - costs[0].Cost) / (costs[1].Cost + costs[0].Cost);
                matchLeft.Add(match);
            }

            for(int r = 0; r < RightFeaturePoints.Count; ++r)
            {
                costs = new List<MatchedPair>(RightFeaturePoints.Count);
                for(int l = 0; l < LeftFeaturePoints.Count; ++l)
                {
                    costs.Add(new MatchedPair()
                    {
                        LeftPoint = new Vector2(LeftFeaturePoints[l]),
                        RightPoint = new Vector2(RightFeaturePoints[r]),
                        Cost = _census.GetCost(LeftFeaturePoints[l], RightFeaturePoints[r])
                    });
                }
                costs.Sort((c1, c2) => { return c1.Cost > c2.Cost ? 1 : (c1.Cost < c2.Cost ? -1 : 0); });
                // Confidence will be (c2-c1)/(c1+c2)
                MatchedPair match = costs[0];
                match.Confidence = costs[1].Cost + costs[0].Cost > 0.0 ? 
                    (costs[1].Cost - costs[0].Cost) / (costs[1].Cost + costs[0].Cost) : 0.0;
                matchRight.Add(match);
            }

            Matches = new List<MatchedPair>();
            foreach(var ml in matchLeft)
            {
                MatchedPair mr = matchRight.Find((m) => 
                {
                    return ml.LeftPoint.DistanceTo(m.LeftPoint) < 0.01 &&
                        m.RightPoint.DistanceTo(ml.RightPoint) < 0.01; }
                );
                // We have both sides matches
                if(mr != null)
                {
                    mr.Confidence = 0.5 * (mr.Confidence + ml.Confidence);
                    // Cross check sucessful
                    Matches.Add(mr);
                }
            }
        }

        public override string Name { get { return "Census Matcher"; } }

        public override void InitParameters()
        {
            base.InitParameters();

            IAlgorithmParameter windowRadiusParam = new IntParameter(
                "Window Radius", "WRAD", 4, 1, 20);
            Parameters.Add(windowRadiusParam);
        }

        public override void UpdateParameters()
        {
            base.UpdateParameters();
            WindowRadius = IAlgorithmParameter.FindValue<int>("WRAD", Parameters);
        }
    }
}
