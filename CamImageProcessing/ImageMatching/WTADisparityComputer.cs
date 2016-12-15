using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CamCore;

namespace CamImageProcessing.ImageMatching
{
    // Just set disparity with lowest cost
    public class WTADisparityComputer : DisparityComputer
    {
        List<Disparity> _dispForPixel;
        int _minIdx;
        int _min2Idx;
        double _minCost;
        double _min2Cost;

        public WTADisparityComputer()
        {
            ConfidenceComp.UsedConfidenceMethod = ConfidenceMethod.TwoAgainstMax;
        }

        public override void Init()
        {
            _dispForPixel = new List<Disparity>(ImageBase.RowCount + ImageBase.ColumnCount);
             _minIdx = -1;
             _min2Idx = -1;
             _minCost = double.PositiveInfinity;
             _min2Cost = double.PositiveInfinity;
        }

        public override void StoreDisparity(IntVector2 pixelBase, IntVector2 pixelMatched, double cost)
        {
            StoreDisparity(new Disparity(pixelBase, pixelMatched, cost, 0.0, (int)DisparityFlags.Valid));
        }

        public override void StoreDisparity(Disparity disp)
        {
            _dispForPixel.Add(disp);

            if(_minCost > disp.Cost)
            {
                _min2Cost = _minCost;
                _min2Idx = _minIdx;
                _minCost = disp.Cost;
                _minIdx = _dispForPixel.Count - 1;
            }
            else if(_min2Cost > disp.Cost)
            {
                _min2Cost = disp.Cost;
                _min2Idx = _dispForPixel.Count - 1;
            }
        }

        public override void FinalizeForPixel(IntVector2 pixelBase)
        {
            if(_minIdx == -1)
            {
                // There was no disparity for pixel : set as invalid
                DisparityMap.Set(pixelBase.Y, pixelBase.X, 
                    new Disparity(pixelBase, pixelBase, double.PositiveInfinity, 0.0, (int)DisparityFlags.Invalid));
                return;
            }

            Disparity bestDisp = _dispForPixel[_minIdx];
            bestDisp.Confidence = ConfidenceComp.ComputeConfidence(_dispForPixel, _minIdx, _min2Idx);

           // IntVector2 pm = bestDisp.GetMatchedPixel(pixelBase);

            DisparityMap.Set(pixelBase.Y, pixelBase.X, bestDisp);
            
            _dispForPixel = new List<Disparity>(2 * _dispForPixel.Count);
            _minIdx = -1;
            _min2Idx = -1;
            _minCost = double.PositiveInfinity;
            _min2Cost = double.PositiveInfinity;
        }

        public override void FinalizeMap()
        {
            // Look over whole map, and :
        }

        public override string Name
        {
            get
            {
                return "WTA Disparity Computer";
            }
        }
    }
}
