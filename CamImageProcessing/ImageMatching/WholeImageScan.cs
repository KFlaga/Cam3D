using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamImageProcessing.ImageMatching
{
    public class WholeImageScan : CostAggregator
    {
        public int MaxDisp_PosX { get; set; }
        public int MaxDisp_NegX { get; set; }
        public int MaxDisp_PosY { get; set; }
        public int MaxDisp_NegY { get; set; }
        
        public override void ComputeMatchingCosts()
        {
            IntVector2 pm = new IntVector2();

            int mindx = IsLeftImageBase ? -MaxDisp_NegX : -MaxDisp_PosX;
            int maxdx = IsLeftImageBase ? MaxDisp_PosX : MaxDisp_NegX;
            int mindy = IsLeftImageBase ? -MaxDisp_NegY : -MaxDisp_PosY;
            int maxdy = IsLeftImageBase ? MaxDisp_PosY : MaxDisp_NegY;

            for(int c = 0; c < ImageBase.ColumnCount; ++c)
            {
                int xmin = Math.Max(0, c + mindx);
                int xmax = Math.Min(ImageBase.ColumnCount, c + maxdx);

                for(int r = 0; r < ImageBase.RowCount; ++r)
                {
                    Vector2 pb_d = new Vector2(x: c, y: r);
                    CurrentPixel = new IntVector2(x: c, y: r);

                    int ymin = Math.Max(0, r + mindx);
                    int ymax = Math.Min(ImageBase.RowCount, r + maxdy);

                    for(int xm = xmin; xm < xmax; ++xm)
                    {
                        for(int ym = ymin; ym < ymax; ++ym)
                        {
                            pm.X = xm;
                            pm.Y = ym;
                            double cost = CostComp.GetCost_Border(CurrentPixel, pm);
                            DispComp.StoreDisparity(CurrentPixel, pm, cost);
                        }
                    }

                    DispComp.FinalizeForPixel(CurrentPixel);
                }
            }
        }

        public override void ComputeMatchingCosts_Rectified()
        {
            IntVector2 pm = new IntVector2();
            for(int c = 0; c < ImageBase.ColumnCount; ++c)
            {
                for(int r = 0; r < ImageBase.RowCount; ++r)
                {
                    Vector2 pb_d = new Vector2(x: c, y: r);
                    CurrentPixel = new IntVector2(x: c, y: r);

                    for(int xm = 0; xm < ImageMatched.ColumnCount; ++xm)
                    {
                        pm.X = xm;
                        pm.Y = r;
                        double cost = CostComp.GetCost_Border(CurrentPixel, pm);
                        DispComp.StoreDisparity(CurrentPixel, pm, cost);
                    }

                    DispComp.FinalizeForPixel(CurrentPixel);
                }
            }
        }

        public override void InitParameters()
        {
            base.InitParameters();

            IntParameter maxDXParam =
                new IntParameter("Max Disparity In X (Positive)", "DXPOS", 1, -100000, 100000);
            Parameters.Add(maxDXParam);

            IntParameter minDXParam =
                new IntParameter("Max Disparity In X (Negative)", "DXNEG", 1, -100000, 100000);
            Parameters.Add(minDXParam);

            IntParameter maxDYParam =
                new IntParameter("Max Disparity In Y (Positive)", "DYPOS", 1, -100000, 100000);
            Parameters.Add(maxDYParam);

            IntParameter minDYParam =
                new IntParameter("Max Disparity In Y (Negative)", "DYNEG", 1, -100000, 100000);
            Parameters.Add(minDYParam);

            ParametrizedObjectParameter disparityParam = new ParametrizedObjectParameter(
                "Disparity Computer", "DISP_COMP");

            disparityParam.Parameterizables = new List<IParameterizable>();
            var dcWTA = new WTADisparityComputer();
            dcWTA.InitParameters();
            disparityParam.Parameterizables.Add(dcWTA);

            var intWTA = new InterpolationDisparityComputer();
            intWTA.InitParameters();
            disparityParam.Parameterizables.Add(intWTA);

            Parameters.Add(disparityParam);

        }

        public override void UpdateParameters()
        {
            base.UpdateParameters();
            MaxDisp_PosX = AlgorithmParameter.FindValue<int>("DXPOS", Parameters);
            MaxDisp_NegX = AlgorithmParameter.FindValue<int>("DXNEG", Parameters);
            MaxDisp_PosY = AlgorithmParameter.FindValue<int>("DYPOS", Parameters);
            MaxDisp_NegY = AlgorithmParameter.FindValue<int>("DYNEG", Parameters);

            DispComp = AlgorithmParameter.FindValue<DisparityComputer>("DISP_COMP", Parameters);
            DispComp.UpdateParameters();
        }

        public override string ToString()
        {
            return "Whole Image Scan Cost Aggregator";
        }
    }
}
