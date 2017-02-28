using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamImageProcessing
{
    public class WatershedSegmentation : ImageSegmentation
    {
        public enum WatershedTechnique
        {
            Immersion = 0,
            TopologicalDistance
        }

        WatershedTechnique _watershedTechnique;
        public WatershedTechnique UsedWatershedTechnique
        {
            get { return _watershedTechnique; }
            set
            {
                _watershedTechnique = value;
                switch(value)
                {
                    case WatershedTechnique.Immersion:
                        // _kernelSpatial = KernelGauss;
                        break;
                    case WatershedTechnique.TopologicalDistance:
                        //_kernelSpatial = KernelEpanechnikov;
                        break;
                }
            }
        }

        //public delegate double KernelDelegate(double distSq);
        //KernelDelegate _kernelSpatial;

        public static Matrix<double> ComputeLowerCompletion(Matrix<double> imageMatrix)
        {
            Matrix<double> lowerComp = new DenseMatrix(imageMatrix.RowCount, imageMatrix.ColumnCount);
            // Let Paths(p) be a set of all descending paths starting at p and ending
            // in some q with I(p) > I(q)
            // let d(p) be : 
            // - 0 if P(p) is empty (so p is in local minimum)
            // - otherwise min{for n in Paths(p)} len(n) (so it is length of shortest path from 
            //         p to some pixel with gray value lower than p
            // Let L be max(d(p))
            // Lower completion I_LC(p) is defined as follows:
            // - L * I(p) if d(p) == 0
            // - L * I(p) + d(p) - 1 otherwise

            List<IntVector2> pixQueue = new List<IntVector2>(imageMatrix.RowCount * imageMatrix.ColumnCount);
            // 1) Initialize pixels queue with pixels that have some lower neighbour
            for(int c = 0; c < imageMatrix.ColumnCount; ++c)
            {
                for(int r = 0; r < imageMatrix.RowCount; ++r)
                {
                    IntVector2 pixel = new IntVector2(x: c, y: r);
                    if(CheckPixelHaveLowerNeighbour(imageMatrix, pixel))
                    {
                        pixQueue.Add(pixel);
                    }
                }
            }

            int dist = 1; // Pixel length of path 
            int idx = 0;
            IntVector2 equalDistFinishedPixel = new IntVector2(-1, -1); // When this pixel is met, all paths with length = dist are finished
            pixQueue.Add(equalDistFinishedPixel);
            while(idx < pixQueue.Count)
            {
                IntVector2 pixel = pixQueue[idx];
                ++idx;

                // All pixels with d(p) = dist are finished : increment dist and push next break-pixel
                if(pixel == equalDistFinishedPixel)
                {
                    if(idx < pixQueue.Count)
                    {
                        pixQueue.Add(equalDistFinishedPixel);
                        ++dist;
                    }
                }
                else
                {
                    lowerComp.At(pixel.Y, pixel.X, dist); // Store d(p) in LC
                    // Push equal neighbours : their d(p) will be one higher than this pixel d(p)
                    PushEqualNeighbours(imageMatrix, lowerComp, pixQueue, pixel);
                }
            }

            for(int c = 0; c < imageMatrix.ColumnCount; ++c)
            {
                for(int r = 0; r < imageMatrix.RowCount; ++r)
                {
                    // Compute final LC from definition
                    lowerComp.At(r, c, GetLowerCompletionValue(
                        imageMatrix.At(r, c), dist, lowerComp.At(r, c)));
                }
            }

            // Scale image so that it is in range [0,1]
            lowerComp.MultiplyThis(1.0 / lowerComp.AbsoulteMaximum().Item3);

            return lowerComp;
        }

        protected static bool CheckPixelHaveLowerNeighbour(Matrix<double> imageMatrix, IntVector2 pixel)
        {
            int maxDy = Math.Min(1, imageMatrix.RowCount - 1 - pixel.Y);
            int minDy = Math.Max(-1, -pixel.Y);
            int maxDx = Math.Min(1, imageMatrix.ColumnCount - 1 - pixel.X);
            int minDx = Math.Max(-1, -pixel.X);

            double Ip = imageMatrix.At(pixel.Y, pixel.X);
            for(int dx = minDx; dx <= maxDx; ++dx)
            {
                for(int dy = minDy; dy <= maxDy; ++dy)
                {
                    if(imageMatrix.At(pixel.Y + dy, pixel.X + dx) < Ip)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        protected static void PushEqualNeighbours(Matrix<double> imageMatrix,
            Matrix<double> lowerComp, List<IntVector2> pixQueue,
            IntVector2 pixel)
        {
            int maxDy = Math.Min(1, imageMatrix.RowCount - 1 - pixel.Y);
            int minDy = Math.Max(-1, -pixel.Y);
            int maxDx = Math.Min(1, imageMatrix.ColumnCount - 1 - pixel.X);
            int minDx = Math.Max(-1, -pixel.X);

            double Ip = imageMatrix.At(pixel.Y, pixel.X);
            for(int dx = minDx; dx <= maxDx; ++dx)
            {
                for(int dy = minDy; dy <= maxDy; ++dy)
                {
                    double Iq = imageMatrix.At(pixel.Y + dy, pixel.X + dx);
                    double lc = lowerComp.At(pixel.Y + dy, pixel.X + dx);
                    if(Iq == Ip && lc == 0.0)
                    {
                        IntVector2 q = new IntVector2(y: pixel.Y + dy, x: pixel.X + dx);
                        pixQueue.Add(q);
                        lowerComp.At(q.Y, q.X, -1.0);
                    }
                }
            }
        }

        protected static double GetLowerCompletionValue(double imgVal, double maxDist, double pDist)
        {
            if(pDist != 0.0)
            {
                return maxDist * imgVal + pDist - 1.0;
            }
            else
            {
                return maxDist * imgVal;
            }
        }

        public override void SegmentGray(Matrix<double> imageMatrix)
        {

        }

        public override void SegmentColor(ColorImage image)
        {

        }

        public override void SegmentDisparity(DisparityMap dispMap)
        {

        }

        public override void InitParameters()
        {
            base.InitParameters();
        }

        public override void UpdateParameters()
        {
            base.UpdateParameters();
        }

        public override string Name { get { return "Watershed Segmentation"; } }
    }
}
