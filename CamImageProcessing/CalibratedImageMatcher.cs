using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamImageProcessing
{
    public abstract class CalibratedImageMatcher : ImagesMatcher
    {
        CamCore.CalibrationData CalibrationData { get { return CamCore.CalibrationData.Data; } }

        protected double _F1L, _F2L, _F3L;
        protected double _F1R, _F2R, _F3R;
        protected int _maxDisparity;

        protected int[,] _potentPoints; // Potential points to match ( lie near epi line witihin
                                        //  max disp ) dims:[count,2], in sec dim dy,dx
        
        protected void ComputeEpiGeometry()
        {
            // epiline (homogeus) -> ur = F*pl, ul = F^T*pr
            // First compute F1,F2,F3
            _F1L = CalibrationData.Fundamental[0, 0] + CalibrationData.Fundamental[0, 1] + CalibrationData.Fundamental[0, 2];
            _F2L = CalibrationData.Fundamental[1, 0] + CalibrationData.Fundamental[1, 1] + CalibrationData.Fundamental[1, 2];
            _F3L = CalibrationData.Fundamental[2, 0] + CalibrationData.Fundamental[2, 1] + CalibrationData.Fundamental[2, 2];

            _F1R = CalibrationData.Fundamental[0, 0] + CalibrationData.Fundamental[1, 0] + CalibrationData.Fundamental[2, 0];
            _F2R = CalibrationData.Fundamental[0, 1] + CalibrationData.Fundamental[1, 1] + CalibrationData.Fundamental[2, 1];
            _F3R = CalibrationData.Fundamental[0, 2] + CalibrationData.Fundamental[1, 2] + CalibrationData.Fundamental[2, 2];
        }

        // Finds potentPoints as (dy,dx) from specified (y,x)
        // forLeftImage -> ref image is left ( y,x from left y+dy,x+dx from right )
        protected void FindPotentialMatchPoints(int y, int x, bool forLeftImage)
        {
            double epiA, epiB, epiC;
            if (forLeftImage)
            {
                epiA = _F1L * x;
                epiB = _F2L * y;
                epiC = _F3L;
            }
            else
            {
                epiA = _F1R * x;
                epiB = _F2R * y;
                epiC = _F3R;
            }
        }
    }
}
