using CamCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamImageProcessing.ImageMatching
{
    public class RankCostComputer : MatchingCostComputer
    {
        public int[,] RankBase { get; private set; }
        public int[,] RankMatched { get; private set; }

        public int RankMaskWidth { get; set; } // Actual width is equal to MaskWidth*2 + 1
        public int RankMaskHeight { get; set; } // Actual height is equal to MaskWidth*2 + 1
        public int CorrMaskWidth { get; set; } // Actual width is equal to MaskWidth*2 + 1
        public int CorrMaskHeight { get; set; } // Actual height is equal to MaskWidth*2 + 1
        private int _corrMaskSize;

        public override double GetCost(IntVector2 pixelBase, IntVector2 pixelMatched)
        {
            int cost = 0;
            for(int dx = -CorrMaskWidth; dx <= CorrMaskWidth; ++dx)
            {
                for(int dy = -CorrMaskHeight; dy <= CorrMaskHeight; ++dy)
                {
                    cost += Math.Abs(RankBase[pixelBase.Y + dy, pixelBase.X + dx] -
                        RankMatched[pixelMatched.Y + dy, pixelMatched.X + dx]);
                }
            }
            return cost;
        }

        public override double GetCost_Border(IntVector2 pixelBase, IntVector2 pixelMatched)
        {
            int cost = 0;
            int px_b, px_m, py_b, py_m;
            for(int dx = -CorrMaskWidth; dx <= CorrMaskWidth; ++dx)
            {
                for(int dy = -CorrMaskHeight; dy <= CorrMaskHeight; ++dy)
                {
                    px_b = Math.Max(0, Math.Min(ImageBase.ColumnCount - 1, pixelBase.X + dx));
                    py_b = Math.Max(0, Math.Min(ImageBase.RowCount - 1, pixelBase.Y + dy));
                    px_m = Math.Max(0, Math.Min(ImageMatched.ColumnCount - 1, pixelMatched.X + dx));
                    py_m = Math.Max(0, Math.Min(ImageMatched.RowCount - 1, pixelMatched.Y + dy));

                    cost += Math.Abs(RankBase[py_b, px_b] - RankMatched[py_m, px_m]);
                }
            }
            return cost;
        }

        public override void Init()
        {
            _corrMaskSize = (2 * CorrMaskHeight + 1) * (2 * RankMaskWidth + 1);

            // Transform images using census transform
            RankBase = new int[ImageBase.RowCount, ImageBase.ColumnCount];
            RankMatched = new int[ImageBase.RowCount, ImageBase.ColumnCount];

            // Compute max cost of rank :
            // - max_rank of any pixel can be rmask.h * rmask.w
            // - cost is correlation in some other mask, so max possible cost is if
            //   every cell in corellation mask differs by max_rank
            // - so max_cost = cmask.w * cmask.h * rmask.w * rmask.h
            MaxCost = (2.0 * RankMaskHeight + 1.0) * (2.0 * RankMaskWidth + 1.0) * 
                (2.0 * CorrMaskWidth + 1.0) * (2.0 * CorrMaskHeight + 1.0);

            BorderHeight = CorrMaskHeight;
            BorderWidth = CorrMaskWidth;

            // Compute rank transform for each pixel for which mask is within bounds
            int maxY = ImageBase.RowCount - RankMaskHeight, maxX = ImageBase.ColumnCount - RankMaskWidth;
            for(int y = RankMaskHeight; y < maxY; ++y)
            {
                for(int x = RankMaskWidth; x < maxX; ++x)
                {
                    RankTransform(y, x);
                }
            }

            // For the rest let pixels outside boundary have same value as symmetricaly placed one on image
            // 1) Top border
            for(int y = 0; y < RankMaskHeight; ++y)
                for(int x = 0; x < ImageBase.ColumnCount; ++x)
                    RankTransform_Border(y, x);
            // 2) Right border
            for(int y = RankMaskHeight; y < ImageBase.RowCount; ++y)
                for(int x = ImageBase.ColumnCount - RankMaskWidth; x < ImageBase.ColumnCount; ++x)
                    RankTransform_Border(y, x);
            // 3) Bottom border
            for(int y = ImageBase.RowCount - RankMaskHeight; y < ImageBase.RowCount; ++y)
                for(int x = 0; x < maxX; ++x)
                    RankTransform_Border(y, x);
            // 4) Left border
            for(int y = RankMaskHeight; y < maxY; ++y)
                for(int x = 0; x < RankMaskWidth; ++x)
                    RankTransform_Border(y, x);
        }


        public void RankTransform(int y, int x)
        {
            int rankBase = 0, rankMatch = 0;
            int dx, dy;
            for(dx = -RankMaskWidth; dx <= RankMaskWidth; ++dx)
            {
                for(dy = -RankMaskHeight; dy <= RankMaskHeight; ++dy)
                {
                    if(ImageBase.At(y + dy, x + dx) < ImageBase.At(y, x))
                        ++rankBase;
                    if(ImageMatched.At(y + dy, x + dx) < ImageMatched.At(y, x))
                        ++rankMatch;
                }
            }

            RankBase[y, x] = rankBase;
            RankMatched[y, x] = rankMatch;
        }

        public void RankTransform_Border(int y, int x)
        {
            int rankBase = 0, rankMatch = 0;
            int px, py, dx, dy;
            for(dx = -RankMaskWidth; dx <= RankMaskWidth; ++dx)
            {
                for(dy = -RankMaskHeight; dy <= RankMaskHeight; ++dy)
                {
                    px = Math.Max(0, Math.Min(ImageBase.ColumnCount - 1, x + dx));
                    py = Math.Max(0, Math.Min(ImageBase.RowCount - 1, y + dy));

                    if(ImageBase.At(py, px) < ImageBase.At(y, x))
                        ++rankBase;
                    if(ImageMatched.At(py, px) < ImageMatched.At(y, x))
                        ++rankMatch;
                }
            }

            RankBase[y, x] = rankBase;
            RankMatched[y, x] = rankMatch;
        }

        public override void Update()
        {
            // Rank transform need no updates
        }

        public override void InitParameters()
        {
            AlgorithmParameter maskRW = new IntParameter(
                "Rank-Compute Mask Width Radius", "RMWR", 3, 1, 10);
            _parameters.Add(maskRW);

            AlgorithmParameter maskRH = new IntParameter(
                "Rank-Compute Mask Height Radius", "RMHR", 3, 1, 10);
            _parameters.Add(maskRH);

            AlgorithmParameter maskCW = new IntParameter(
                "Correlation Mask Width Radius", "CMWR", 3, 1, 10);
            _parameters.Add(maskCW);

            AlgorithmParameter maskCH = new IntParameter(
                "Correlation Mask Height Radius", "CMHR", 3, 1, 10);
            _parameters.Add(maskCH);
        }

        public override void UpdateParameters()
        {
            RankMaskWidth = AlgorithmParameter.FindValue<int>("RMWR", Parameters);
            RankMaskHeight = AlgorithmParameter.FindValue<int>("RMHR", Parameters);
            CorrMaskWidth = AlgorithmParameter.FindValue<int>("CMWR", Parameters);
            CorrMaskHeight = AlgorithmParameter.FindValue<int>("CMHR", Parameters);
        }

        public override string ToString()
        {
            return "Rank Cost Computer";
        }
    }
}
