using CamCore;
using CamAlgorithms;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RectificationModule
{
    public class FeatureMatcher_OpenCV : FeaturesMatcher
    {
        class MatchedPair_Idx
        {
            public int Idx1, Idx2;
            public double Cost, Confidence;
        }

        public int NumberOfFeatures { get; set; }
        public float ImagePyramidScaleFactor { get; set; }
        public int ImagePyramidLevels { get; set; }
        public int ImagePyramidFirstLevel { get; set; } = 0;
        public int EdgeTreshold { get; set; }
        public int PatchSize { get; set; }
        public int FastTreshold { get; set; }

        public override void Match()
        {
            Mat imgLeft = EmguCVUtils.ImageToMat_Gray(LeftImage);
            Mat imgRight = EmguCVUtils.ImageToMat_Gray(RightImage);

            Mat imgMaskLeft = null;
            Mat imgMaskRight = null;
            if(LeftImage is MaskedImage)
            {
                imgMaskLeft = EmguCVUtils.ImageToMat_Mask(LeftImage as MaskedImage);
            }
            if(RightImage is MaskedImage)
            {
                imgMaskRight = EmguCVUtils.ImageToMat_Mask(RightImage as MaskedImage);
            }

            Emgu.CV.Features2D.ORBDetector detORB = new Emgu.CV.Features2D.ORBDetector(
                NumberOfFeatures, ImagePyramidScaleFactor, ImagePyramidLevels,
                EdgeTreshold, ImagePyramidFirstLevel, 2,
                Emgu.CV.Features2D.ORBDetector.ScoreType.Harris, PatchSize, FastTreshold);

            var kpLeft = detORB.Detect(imgLeft, imgMaskLeft);
            var kpRight = detORB.Detect(imgRight, imgMaskRight);
            Mat descLeft = new Mat(), descRight = new Mat();

            detORB.Compute(imgLeft, new Emgu.CV.Util.VectorOfKeyPoint(kpLeft), descLeft);
            detORB.Compute(imgRight, new Emgu.CV.Util.VectorOfKeyPoint(kpRight), descRight);

            //Emgu.CV.Features2D.BFMatcher matcher = new Emgu.CV.Features2D.BFMatcher(
            //    Emgu.CV.Features2D.DistanceType.Hamming, true);

            //matcher.Add(descLeft);

            //int k = 1;
            //Emgu.CV.Util.VectorOfVectorOfDMatch matches = new Emgu.CV.Util.VectorOfVectorOfDMatch();

            //matcher.KnnMatch(descRight, matches, k, null);

            //Matches = new List<MatchedPair>();
            //var matchArr = matches.ToArrayOfArray();
            //for(int m = 0; m < matchArr.Length; ++m)
            //{
            //    var singleArr = matchArr[m];
            //    for(int i = 0; i < singleArr.Length; ++i)
            //    {
            //        var match = singleArr[i];
            //        MatchedPair pair = new MatchedPair()
            //        {
            //            Cost = match.Distance,
            //            RightPoint = new Vector2(kpRight[match.QueryIdx].Point.X, kpRight[match.QueryIdx].Point.Y),
            //            LeftPoint = new Vector2(kpLeft[match.TrainIdx].Point.X, kpLeft[match.TrainIdx].Point.Y),
            //        };
            //        Matches.Add(pair);
            //    }
            //}
            CamAlgorithms.ImageMatching.HammingLookup.ComputeWordBitsLookup();
            var matchesLeft = MatchDesriptors(descLeft, descRight);
            var matchesRight = MatchDesriptors(descRight, descLeft);

            Matches = new List<MatchedPair>();
            foreach(var ml in matchesLeft)
            {
                MatchedPair_Idx mr = matchesRight.Find((m) => { return ml.Idx1 == m.Idx2; });
                // We have both sides matches
                if(mr != null && ml.Idx2 == mr.Idx1)
                {
                    if(mr.Confidence > 0.2 && ml.Confidence > 0.2 &&
                        (ml.Confidence + mr.Confidence) > 0.5)
                    {
                        // Cross check sucessful
                        MatchedPair mp = new MatchedPair()
                        {
                            Confidence = 0.6 * (ml.Confidence + mr.Confidence),
                            Cost = ml.Cost,
                            LeftPoint = new Vector2(kpLeft[ml.Idx1].Point.X, kpLeft[ml.Idx1].Point.Y),
                            RightPoint = new Vector2(kpRight[ml.Idx2].Point.X, kpRight[ml.Idx2].Point.Y)
                        };
                        Matches.Add(mp);
                    }
                }
            }
        }

        private List<MatchedPair_Idx> MatchDesriptors(Mat desc1, Mat desc2)
        {
            List<MatchedPair_Idx> costs;
            var matches = new List<MatchedPair_Idx>();
            for(int l = 0; l < desc1.Rows; ++l)
            {
                costs = new List<MatchedPair_Idx>(desc2.Rows);
                for(int r = 0; r < desc2.Rows; ++r)
                {
                    // Get hamming distance:
                    int ones = 0;
                    for(int i = 0; i < desc1.Cols; ++i)
                    {
                        uint b1 = (uint)desc1.GetByteValue(l, i);
                        uint b2 = (uint)desc2.GetByteValue(r, i);
                        uint b =  b1 ^ b2;
                        ones += CamAlgorithms.ImageMatching.HammingLookup.OnesCount(b);
                    }

                    costs.Add(new MatchedPair_Idx()
                    {
                        Idx1 = l,
                        Idx2 = r,
                        Cost = ones
                    });
                }
                costs.Sort((c1, c2) => { return c1.Cost > c2.Cost ? 1 : (c1.Cost < c2.Cost ? -1 : 0); });
                // Confidence will be (c2-c1)/(c1+c2)
                MatchedPair_Idx match = costs[0];
                match.Confidence = (costs[1].Cost - costs[0].Cost) / (costs[1].Cost + costs[0].Cost);
                matches.Add(match);
            }
            return matches;
        }

        public override string Name
        {
            get
            {
                return "OpenCV ORB Detector/Matcher";
            }
        }

        public override void InitParameters()
        {
            base.InitParameters();

            IntParameter nfeaturesParam = new IntParameter(
                "Number of Features", "NFET", 500, 1, 5000);
            Parameters.Add(nfeaturesParam);

            IntParameter fastTreshParam = new IntParameter(
                "Fast Treshold", "FAST_TR", 10, 1, 100);
            Parameters.Add(fastTreshParam);

            IntParameter edgeTreshParam = new IntParameter(
                "Edge Treshold", "EDGE_TR", 15, 1, 300);
            Parameters.Add(edgeTreshParam);

            IntParameter patchParam = new IntParameter(
                "Patch Size", "PATCH", 15, 1, 300);
            Parameters.Add(patchParam);

            FloatParameter scaleParam = new FloatParameter(
                "Image Pyramid Scale Factor", "IP_SCALE", 1.2f, 1.1f, 2.0f);
            Parameters.Add(scaleParam);

            IntParameter plevelsParam = new IntParameter(
                "Image Pyramid Levels", "IP_LEVEL", 8, 1, 100);
            Parameters.Add(plevelsParam);
        }

        public override void UpdateParameters()
        {
            base.UpdateParameters();

            NumberOfFeatures = AlgorithmParameter.FindValue<int>("NFET", Parameters);
            FastTreshold = AlgorithmParameter.FindValue<int>("FAST_TR", Parameters);
            EdgeTreshold = (AlgorithmParameter.FindValue<int>("EDGE_TR", Parameters) * 2) + 1;
            PatchSize = AlgorithmParameter.FindValue<int>("PATCH", Parameters) * 2 + 1;
            ImagePyramidScaleFactor = AlgorithmParameter.FindValue<float>("IP_SCALE", Parameters);
            ImagePyramidLevels = AlgorithmParameter.FindValue<int>("IP_LEVEL", Parameters);
        }
    }
}
