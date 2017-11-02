using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Collections.Generic;

namespace CamAlgorithms.ImageMatching
{
    public class InterpolationDisparityComputer : DisparityComputer
    {
      //  public bool InterpolateFinal { get; set; } = true;
      //  public bool InterpolatePixelWise { get; set; } = false;

        List<Disparity> _dispForPixel;
        int _minIdx;
        int _min2Idx;
        double _minCost;
        double _min2Cost;

        public InterpolationDisparityComputer()
        {
            ConfidenceComp.UsedConfidenceMethod = ConfidenceMethod.TwoAgainstMax;
        }

        public override void Init()
        {
            _dispForPixel = new List<Disparity>(ImageBase.RowCount + ImageBase.ColumnCount);
            _minIdx = 0;
            _min2Idx = 0;
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

        Matrix<double> _D = DenseMatrix.Create(3, 3, 1.0);
        Vector<double> _a = new DenseVector(3);
        Vector<double> _d = new DenseVector(3);

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

            if(_minIdx > 0 && _minIdx < _dispForPixel.Count - 1)
            {
                // D'[d-1] = D[d] + c[d-1]/c[d] * (D[d-1] - D[d])
                // D'[d+1] = D[d] + c[d+1]/c[d] * (D[d+1] - D[d])
                // Fit quadratic func. to d,d-1,d+1
                double c_n = _dispForPixel[_minIdx - 1].Cost / _minCost;
                double c_p = _dispForPixel[_minIdx + 1].Cost / _minCost;
                double dx = _dispForPixel[_minIdx].DX;
                double dy = _dispForPixel[_minIdx].DY;
                double dx_n = dx + c_n * (_dispForPixel[_minIdx - 1].DX - dx);
                double dy_n = dy + c_n * (_dispForPixel[_minIdx - 1].DY - dy);
                double dx_p = dx + c_p * (_dispForPixel[_minIdx + 1].DX - dx);
                double dy_p = dy + c_p * (_dispForPixel[_minIdx + 1].DY - dy);

                _D.At(0, 0, (_minIdx - 1) * (_minIdx - 1));
                _D.At(0, 1, _minIdx - 1);
                _D.At(1, 0, _minIdx * _minIdx);
                _D.At(1, 1, _minIdx);
                _D.At(2, 0, (_minIdx + 1) * (_minIdx + 1));
                _D.At(2, 1, (_minIdx + 1));

                _d.At(0, dx_n);
                _d.At(1, dx);
                _d.At(2, dx_p);

                _a = SvdSolver.Solve(_D, _d);
                bestDisp.SubDX = _a.At(0) * _minIdx * _minIdx + _a.At(1) * _minIdx + _a.At(2);

                _d.At(0, dy_n);
                _d.At(1, dy);
                _d.At(2, dy_p);

                _a = SvdSolver.Solve(_D, _d);
                bestDisp.SubDY = _a.At(0) * _minIdx * _minIdx + _a.At(1) * _minIdx + _a.At(2);
            }

            IntVector2 pm = bestDisp.GetMatchedPixel(pixelBase);

            DisparityMap.Set(pm.Y, pm.X, bestDisp);

            _dispForPixel = new List<Disparity>(2 * _dispForPixel.Count);
            _minIdx = 0;
            _min2Idx = 0;
            _minCost = double.PositiveInfinity;
            _min2Cost = double.PositiveInfinity;
        }

        public override void FinalizeMap()
        {

        }

        //public override void InitParameters()
        //{
        //    base.InitParameters();

        //    BooleanParameter intFinParam =
        //        new BooleanParameter("Interpolate On Final Map", "IFIN", false);
        //    Parameters.Add(intFinParam);

        //    BooleanParameter intPixParam =
        //        new BooleanParameter("Interpolate Pixel-Wise", "IPIX", true);
        //    Parameters.Add(intPixParam);
        //}

        //public override void UpdateParameters()
        //{ 
        //    base.UpdateParameters();
        //    InterpolateFinal = AlgorithmParameter.FindValue<bool>("IFIN", Parameters);
        //    InterpolatePixelWise = AlgorithmParameter.FindValue<bool>("IPIX", Parameters);
        //}

        public override string Name
        {
            get
            {
                return "Interpolation Disparity Computer";
            }
        }
    }
}
