using CamCore;

namespace CamAlgorithms.ImageMatching
{
    public class SmoothSegmentsRefiner : DisparityRefinement
    {
        public int MaxIterations { get; set; } = 10;
        public double StepCoeff { get; set; } = 0.5; // Actual coeff used is StepCoeff * (1/dirs)
        public double MaxDisparityDiff { get; set; } = 1.1;
        public bool UseEightDirections { get; set; } = false;

        IntVector2[] _dirs4 = new IntVector2[4]
        {
            new IntVector2(0, -1),
            new IntVector2(-1, 0),
            new IntVector2(1, 0),
            new IntVector2(0, 1),
        };

        IntVector2[] _dirs8 = new IntVector2[8]
        {
            new IntVector2(-1, -1),
            new IntVector2(0, -1),
            new IntVector2(1, -1),
            new IntVector2(-1, 0),
            new IntVector2(1, 0),
            new IntVector2(-1, 1),
            new IntVector2(0, 1),
            new IntVector2(1, 1),
        };

        double[] _dinv4 = new double[4]
        {
            1.0, 1.0, 1.0, 1.0
        };

        double[] _dinv8 = new double[8]
        {
            0.5, 1.0, 0.5, 1.0, 1.0, 0.5, 1.0, 0.5
        };

        public DisparityMap RefineMap(DisparityMap map)
        {
            // D(t+1) = D(t) + r/|dirs| * sum{dirs}(∇D_dir} for each dir if ∇D_dir < MaxDisp

            DisparityMap next = (DisparityMap)map.Clone();
            DisparityMap last = (DisparityMap)map.Clone();
            IntVector2[] dirs = UseEightDirections ? _dirs8 : _dirs4;
            double[] dinv = UseEightDirections ? _dinv8 : _dinv4;
            double r = UseEightDirections ? StepCoeff * 0.5 : StepCoeff;

            for(int t = 0; t < MaxIterations; ++t)
            {
                for(int x = 1; x < map.ColumnCount - 1; ++x)
                {
                    for(int y = 1; y < map.RowCount - 1; ++y)
                    {
                        double dispX = 0.0;
                        double n = 0;

                        for(int i = 0; i < dirs.Length; ++i)
                        {
                            double gradDispX = last[y + dirs[i].Y, x + dirs[i].X].SubDX - last[y, x].SubDX;
                            if(gradDispX < MaxDisparityDiff)
                            {
                                dispX += gradDispX;
                                n += 1.0;
                            }
                        }

                        if(n > 0 && dispX < 10000 && dispX > -10000)
                        {
                            dispX *= r / n;
                            dispX += last[y, x].SubDX;

                            next[y, x].SubDX = dispX;
                            next[y, x].DX = dispX.Round();
                        }
                        else if(dispX > 10000 || dispX < -10000)
                        {
                            // Detect invalid disparitiess
                            next[y, x].SubDX = 0;
                            next[y, x].DX = 0;
                            next[y, x].Flags = (int)DisparityFlags.Invalid;
                        }
                    }
                }

                // Exchange last and next (I(t) will be overriden by I(t+2), I(t+1) in last)
                var temp = last;
                last = next;
                next = temp;
            }

            for(int x = 1; x < map.ColumnCount - 1; ++x)
            {
                for(int y = 1; y < map.RowCount - 1; ++y)
                {
                    last[y, x].Flags = (int)DisparityFlags.Valid;
                }
            }
            return last;
        }

        public override void RefineMaps()
        {
            if(MapLeft != null)
            {
                MapLeft = RefineMap(MapLeft);
            }

            if(MapRight != null)
            {
                MapRight = RefineMap(MapRight);
            }
        }

        public override string Name
        {
            get
            {
                return "Segment Smoothing";
            }
        }

        public override void InitParameters()
        {
            base.InitParameters();
            
            Parameters.Add(new IntParameter(
                "Interations", "MaxIterations", 10, 1, 1000));
            Parameters.Add(new DoubleParameter(
                "Max disparitiy difference in segment", "MaxDisparityDiff", 1.1, 0.001, 100.0));
            Parameters.Add(new DoubleParameter(
                "Step Coeff", "StepCoeff", 0.5, -10.0, 10.0));
            Parameters.Add(new BooleanParameter(
                "Use 8 Gradient Directions", "UseEightDirections", false));
        }

        public override void UpdateParameters()
        {
            base.UpdateParameters();
            MaxIterations = AlgorithmParameter.FindValue<int>("MaxIterations", Parameters);
            MaxDisparityDiff = AlgorithmParameter.FindValue<double>("MaxDisparityDiff", Parameters);
            StepCoeff = AlgorithmParameter.FindValue<double>("StepCoeff", Parameters);
            UseEightDirections = AlgorithmParameter.FindValue<bool>("UseEightDirections", Parameters);
        }

        public override string ToString()
        {
            return "Segment Smoothing";
        }
    }
}
