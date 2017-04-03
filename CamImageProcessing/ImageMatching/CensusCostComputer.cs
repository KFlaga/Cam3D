using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CamCore;
using MathNet.Numerics.LinearAlgebra;

namespace CamImageProcessing.ImageMatching
{
    public class CensusCostComputer : MatchingCostComputer
    {
        public IBitWord[,] CensusBase { get; set; } // [y,x]
        public IBitWord[,] CensusMatched { get; set; } // [y,x]

        public int MaskWidth { get; set; } // Actual width is equal to MaskWidth*2 + 1
        public int MaskHeight { get; set; } // Actual height is equal to MaskWidth*2 + 1
        public int WordLength { get; set; }

        public override double GetCost(IntVector2 pixelBase, IntVector2 pixelMatched)
        {
            return CensusBase[pixelBase.Y, pixelBase.X].GetHammingDistance(
                CensusMatched[pixelMatched.Y, pixelMatched.X]);
        }

        public override double GetCost_Border(IntVector2 pixelBase, IntVector2 pixelMatched)
        {
            return CensusBase[pixelBase.Y, pixelBase.X].GetHammingDistance(
                CensusMatched[pixelMatched.Y, pixelMatched.X]);
        }

        public override void Init()
        {
            HammingLookup.ComputeWordBitsLookup();

            // Transform images using census transform
            CensusBase = new IBitWord[ImageBase.RowCount, ImageBase.ColumnCount];
            CensusMatched = new IBitWord[ImageBase.RowCount, ImageBase.ColumnCount];

            WordLength = (2 * MaskHeight + 1) * (2 * MaskWidth + 1);
            BitWord.BitWordLength = WordLength;
            uint[] maskWordBase = new uint[BitWord.Byte4Length];
            uint[] maskWordMatched = new uint[BitWord.Byte4Length];

            BorderHeight = MaskHeight;
            BorderWidth = MaskWidth;

            // Compute max cost of census :
            // - max cost if all bits in mask differs (except center pixel itself), so its equal to WordLength - 1
            MaxCost = WordLength - 1;

            // Compute census transfor for each pixel for which mask is within bounds
            int maxY = ImageBase.RowCount - MaskHeight, maxX = ImageBase.ColumnCount - MaskWidth;

            BorderFunction<CensusCostComputer>.DoBorderFunction(this,
                (thisObj, y, x) => { CensusTransform(y, x, maskWordBase, maskWordMatched); },
                (thisObj, y, x) => { CensusTransform_Border(y, x, maskWordBase, maskWordMatched); },
                MaskWidth, MaskHeight, ImageBase.RowCount, ImageBase.ColumnCount); 
        }

        public void CensusTransform(int y, int x, uint[] maskBase, uint[] maskMatch)
        {
            Array.Clear(maskBase, 0, BitWord.Byte4Length);
            Array.Clear(maskMatch, 0, BitWord.Byte4Length);
            int maskPos = 0, dx, dy;
            for(dx = -MaskWidth; dx <= MaskWidth; ++dx)
            {
                for(dy = -MaskHeight; dy <= MaskHeight; ++dy)
                {
                    if(ImageBase[y + dy, x + dx] < ImageBase[y, x])
                        maskBase[maskPos / 32] |= (1u << (maskPos % 32));
                    if(ImageMatched[y + dy, x + dx] < ImageMatched[y, x])
                        maskMatch[maskPos / 32] |= (1u << (maskPos % 32));
                    ++maskPos;
                }
            }

            CensusBase[y, x] = BitWord.CreateBitWord(maskBase);
            CensusMatched[y, x] = BitWord.CreateBitWord(maskMatch);
        }

        public void CensusTransform_Border(int y, int x, uint[] maskBase, uint[] maskMatch)
        {
            Array.Clear(maskBase, 0, BitWord.Byte4Length);
            Array.Clear(maskMatch, 0, BitWord.Byte4Length);
            int maskPos = 0, dx, dy, px, py;
            for(dx = -MaskWidth; dx <= MaskWidth; ++dx)
            {
                for(dy = -MaskHeight; dy <= MaskHeight; ++dy)
                {
                    px = x + dx;
                    px = px > ImageBase.ColumnCount - 1 ? 2 * ImageBase.ColumnCount - px - 2 : px;
                    px = px < 0 ? -px : px;

                    py = y + dy;
                    py = py > ImageBase.RowCount - 1 ? 2 * ImageBase.RowCount - py - 2 : py;
                    py = py < 0 ? -py : py;

                    if(ImageBase[py, px] < ImageBase[y, x])
                        maskBase[maskPos / 32] |= (1u << (maskPos % 32));
                    if(ImageMatched[py, px] < ImageMatched[y, x])
                        maskMatch[maskPos / 32] |= (1u << (maskPos % 32));
                    ++maskPos;
                }
            }

            CensusBase[y, x] = BitWord.CreateBitWord(maskBase);
            CensusMatched[y, x] = BitWord.CreateBitWord(maskMatch);
        }

        public override void Update()
        {
            // Census transform need no updates
        }

        public override void InitParameters()
        {
            base.InitParameters();
            AlgorithmParameter maskW = new IntParameter(
                "Mask Width Radius", "MWR", 6, 1, 7);
            _parameters.Add(maskW);

            AlgorithmParameter maskH = new IntParameter(
                "Mask Height Radius", "MHR", 6, 1, 7);
            _parameters.Add(maskH);
        }

        public override void UpdateParameters()
        {
            base.UpdateParameters();
            MaskWidth = AlgorithmParameter.FindValue<int>("MWR", Parameters);
            MaskHeight = AlgorithmParameter.FindValue<int>("MHR", Parameters);
        }

        public override string Name
        {
            get
            {
                return "Census Cost Computer";
            }
        }
    }
}
