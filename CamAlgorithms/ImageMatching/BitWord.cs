using System;
using System.Text;

namespace CamAlgorithms.ImageMatching
{
    public class HammingLookup
    {
        static int[] _wordBits;
        public static int OnesCount(UInt32 i)
        {
            return (_wordBits[i & 0xFFFF] + _wordBits[i >> 16]);
        }

        public static void ComputeWordBitsLookup()
        {
            _wordBits = new int[UInt16.MaxValue + 1];
            int x;
            int count = 0;
            for(int i = 0; i <= UInt16.MaxValue; ++i)
            {
                x = i;
                count = 0;
                for(count = 0; x > 0; ++count)
                    x &= x - 1;

                _wordBits[i] = count;
            }
        }
    }
    
    public interface IBitWord
    {
        int GetHammingDistance(IBitWord bw);
    }

    public static class BitWord
    {
        public delegate IBitWord CreateBitWordFunction(UInt32[] data);
        public static CreateBitWordFunction CreateBitWord;

        private static int _bitwordLength;
        public static int BitWordLength
        {
            get { return _bitwordLength; }
            set
            {
                _bitwordLength = value;
                if(_bitwordLength <= 32)
                {
                    CreateBitWord = BitWord_4.Create;
                }
                else if(_bitwordLength <= 32 * 2)
                {
                    CreateBitWord = BitWord_8.Create;
                }
                else if(_bitwordLength <= 32 * 3)
                {
                    CreateBitWord = BitWord_12.Create;
                }
                else if(_bitwordLength <= 32 * 4)
                {
                    CreateBitWord = BitWord_16.Create;
                }
                else if(_bitwordLength <= 32 * 5)
                {
                    CreateBitWord = BitWord_20.Create;
                }
                else if(_bitwordLength <= 32 * 6)
                {
                    CreateBitWord = BitWord_24.Create;
                }
                else if(_bitwordLength <= 32 * 7)
                {
                    CreateBitWord = BitWord_28.Create;
                }
                else if(_bitwordLength <= 32 * 8)
                {
                    CreateBitWord = BitWord_32.Create;
                }
                else
                    throw new ArgumentOutOfRangeException("BitWordLength", "Bitword larger than 32 bytes is not implemented");

                _byte4Length = _bitwordLength % 32 == 0 ? 
                    _bitwordLength / 32 : _bitwordLength / 32 + 1;
            }
        }
        
        private static int _byte4Length;
        public static int Byte4Length
        {
            get { return _byte4Length; }
        }
    }
    
    public class BitWord_4 : IBitWord
    {
        public UInt32 Byte1to4;

        public BitWord_4(UInt32[] bytes4)
        {
            Byte1to4 = bytes4[0];
        }
        
        public int GetHammingDistance(IBitWord bw)
        {
            return HammingLookup.OnesCount(Byte1to4 ^ ((BitWord_4)bw).Byte1to4);
        }

        public static IBitWord Create(UInt32[] bytes4)
        {
            return new BitWord_4(bytes4);
        }

        public override string ToString()
        {
            return "["+Convert.ToString(Byte1to4, 16)+"]";
        }
    }

    public class BitWord_8 : IBitWord
    {
        public UInt32 Byte1to4;
        public UInt32 Byte5to8;

        public BitWord_8(UInt32[] bytes4)
        {
            Byte1to4 = bytes4[0];
            Byte5to8 = bytes4[1];
        }

        public int GetHammingDistance(IBitWord bw)
        {
            return HammingLookup.OnesCount(Byte1to4 ^ ((BitWord_8)bw).Byte1to4) + 
                HammingLookup.OnesCount(Byte5to8 ^ ((BitWord_8)bw).Byte5to8);
        }

        public static IBitWord Create(UInt32[] bytes4)
        {
            return new BitWord_8(bytes4);
        }

        public override string ToString()
        {
            StringBuilder res = new StringBuilder();
            res.Append("[" + Convert.ToString(Byte1to4, 16) + "]");
            res.Append("[" + Convert.ToString(Byte5to8, 16) + "]");

            return res.ToString();
        }
    }

    public struct BitWord_12 : IBitWord
    {
        public UInt32 Byte1to4;
        public UInt32 Byte5to8;
        public UInt32 Byte9to12;

        public BitWord_12(UInt32[] bytes4)
        {
            Byte1to4 = bytes4[0];
            Byte5to8 = bytes4[1];
            Byte9to12 = bytes4[2];
        }

        public int GetHammingDistance(IBitWord bw)
        {
            return HammingLookup.OnesCount(Byte1to4 ^ ((BitWord_12)bw).Byte1to4) +
                HammingLookup.OnesCount(Byte5to8 ^ ((BitWord_12)bw).Byte5to8) +
                HammingLookup.OnesCount(Byte9to12 ^ ((BitWord_12)bw).Byte9to12);
        }

        public static IBitWord Create(UInt32[] bytes4)
        {
            return new BitWord_12(bytes4);
        }

        public override string ToString()
        {
            StringBuilder res = new StringBuilder();
            res.Append("[" + Convert.ToString(Byte1to4, 16) + "]");
            res.Append("[" + Convert.ToString(Byte5to8, 16) + "]");
            res.Append("[" + Convert.ToString(Byte9to12, 16) + "]");

            return res.ToString();
        }
    }

    public struct BitWord_16 : IBitWord
    {
        public UInt32 Byte1to4;
        public UInt32 Byte5to8;
        public UInt32 Byte9to12;
        public UInt32 Byte13to16;

        public BitWord_16(UInt32[] bytes4)
        {
            Byte1to4 = bytes4[0];
            Byte5to8 = bytes4[1];
            Byte9to12 = bytes4[2];
            Byte13to16 = bytes4[3];
        }

        public int GetHammingDistance(IBitWord bw)
        {
            return HammingLookup.OnesCount(Byte1to4 ^ ((BitWord_16)bw).Byte1to4) +
                HammingLookup.OnesCount(Byte5to8 ^ ((BitWord_16)bw).Byte5to8) +
                HammingLookup.OnesCount(Byte9to12 ^ ((BitWord_16)bw).Byte9to12) +
                HammingLookup.OnesCount(Byte13to16 ^ ((BitWord_16)bw).Byte13to16);
        }

        public static IBitWord Create(UInt32[] bytes4)
        {
            return new BitWord_16(bytes4);
        }

        public override string ToString()
        {
            StringBuilder res = new StringBuilder();
            res.Append("[" + Convert.ToString(Byte1to4, 16) + "]");
            res.Append("[" + Convert.ToString(Byte5to8, 16) + "]");
            res.Append("[" + Convert.ToString(Byte9to12, 16) + "]");
            res.Append("[" + Convert.ToString(Byte13to16, 16) + "]");

            return res.ToString();
        }
    }

    public struct BitWord_20 : IBitWord
    {
        public UInt32 Byte1to4;
        public UInt32 Byte5to8;
        public UInt32 Byte9to12;
        public UInt32 Byte13to16;
        public UInt32 Byte17to20;

        public BitWord_20(UInt32[] bytes4)
        {
            Byte1to4 = bytes4[0];
            Byte5to8 = bytes4[1];
            Byte9to12 = bytes4[2];
            Byte13to16 = bytes4[3];
            Byte17to20 = bytes4[4];
        }

        public int GetHammingDistance(IBitWord bw)
        {
            return HammingLookup.OnesCount(Byte1to4 ^ ((BitWord_20)bw).Byte1to4) +
                HammingLookup.OnesCount(Byte5to8 ^ ((BitWord_20)bw).Byte5to8) +
                HammingLookup.OnesCount(Byte9to12 ^ ((BitWord_20)bw).Byte9to12) +
                HammingLookup.OnesCount(Byte17to20 ^ ((BitWord_20)bw).Byte13to16) +
                HammingLookup.OnesCount(Byte13to16 ^ ((BitWord_20)bw).Byte17to20);
        }

        public static IBitWord Create(UInt32[] bytes4)
        {
            return new BitWord_20(bytes4);
        }

        public override string ToString()
        {
            StringBuilder res = new StringBuilder();
            res.Append("[" + Convert.ToString(Byte1to4, 16) + "]");
            res.Append("[" + Convert.ToString(Byte5to8, 16) + "]");
            res.Append("[" + Convert.ToString(Byte9to12, 16) + "]");
            res.Append("[" + Convert.ToString(Byte13to16, 16) + "]");
            res.Append("[" + Convert.ToString(Byte17to20, 16) + "]");

            return res.ToString();
        }
    }

    public struct BitWord_24 : IBitWord
    {
        public UInt32 Byte1to4;
        public UInt32 Byte5to8;
        public UInt32 Byte9to12;
        public UInt32 Byte13to16;
        public UInt32 Byte17to20;
        public UInt32 Byte21to24;

        public BitWord_24(UInt32[] bytes4)
        {
            Byte1to4 = bytes4[0];
            Byte5to8 = bytes4[1];
            Byte9to12 = bytes4[2];
            Byte13to16 = bytes4[3];
            Byte17to20 = bytes4[4];
            Byte21to24 = bytes4[5];
        }

        public int GetHammingDistance(IBitWord bw)
        {
            return HammingLookup.OnesCount(Byte1to4 ^ ((BitWord_24)bw).Byte1to4) +
                HammingLookup.OnesCount(Byte5to8 ^ ((BitWord_24)bw).Byte5to8) +
                HammingLookup.OnesCount(Byte9to12 ^ ((BitWord_24)bw).Byte9to12) +
                HammingLookup.OnesCount(Byte13to16 ^ ((BitWord_24)bw).Byte13to16) +
                HammingLookup.OnesCount(Byte17to20 ^ ((BitWord_24)bw).Byte17to20) +
                HammingLookup.OnesCount(Byte21to24 ^ ((BitWord_24)bw).Byte21to24);
        }

        public static IBitWord Create(UInt32[] bytes4)
        {
            return new BitWord_24(bytes4);
        }

        public override string ToString()
        {
            StringBuilder res = new StringBuilder();
            res.Append("[" + Convert.ToString(Byte1to4, 16) + "]");
            res.Append("[" + Convert.ToString(Byte5to8, 16) + "]");
            res.Append("[" + Convert.ToString(Byte9to12, 16) + "]");
            res.Append("[" + Convert.ToString(Byte13to16, 16) + "]");
            res.Append("[" + Convert.ToString(Byte17to20, 16) + "]");
            res.Append("[" + Convert.ToString(Byte21to24, 16) + "]");

            return res.ToString();
        }
    }

    public struct BitWord_28 : IBitWord
    {
        public UInt32 Byte1to4;
        public UInt32 Byte5to8;
        public UInt32 Byte9to12;
        public UInt32 Byte13to16;
        public UInt32 Byte17to20;
        public UInt32 Byte21to24;
        public UInt32 Byte25to28;

        public BitWord_28(UInt32[] bytes4)
        {
            Byte1to4 = bytes4[0];
            Byte5to8 = bytes4[1];
            Byte9to12 = bytes4[2];
            Byte13to16 = bytes4[3];
            Byte17to20 = bytes4[4];
            Byte21to24 = bytes4[5];
            Byte25to28 = bytes4[6];
        }

        public int GetHammingDistance(IBitWord bw)
        {
            return HammingLookup.OnesCount(Byte1to4 ^ ((BitWord_28)bw).Byte1to4) +
                HammingLookup.OnesCount(Byte5to8 ^ ((BitWord_28)bw).Byte5to8) +
                HammingLookup.OnesCount(Byte9to12 ^ ((BitWord_28)bw).Byte9to12) +
                HammingLookup.OnesCount(Byte13to16 ^ ((BitWord_28)bw).Byte13to16) +
                HammingLookup.OnesCount(Byte17to20 ^ ((BitWord_28)bw).Byte17to20) +
                HammingLookup.OnesCount(Byte21to24 ^ ((BitWord_28)bw).Byte21to24) +
                HammingLookup.OnesCount(Byte25to28 ^ ((BitWord_28)bw).Byte25to28);
        }

        public static IBitWord Create(UInt32[] bytes4)
        {
            return new BitWord_28(bytes4);
        }

        public override string ToString()
        {
            StringBuilder res = new StringBuilder();
            res.Append("[" + Convert.ToString(Byte1to4, 16) + "]");
            res.Append("[" + Convert.ToString(Byte5to8, 16) + "]");
            res.Append("[" + Convert.ToString(Byte9to12, 16) + "]");
            res.Append("[" + Convert.ToString(Byte13to16, 16) + "]");
            res.Append("[" + Convert.ToString(Byte17to20, 16) + "]");
            res.Append("[" + Convert.ToString(Byte21to24, 16) + "]");
            res.Append("[" + Convert.ToString(Byte25to28, 16) + "]");

            return res.ToString();
        }
    }

    public struct BitWord_32 : IBitWord
    {
        public UInt32 Byte1to4;
        public UInt32 Byte5to8;
        public UInt32 Byte9to12;
        public UInt32 Byte13to16;
        public UInt32 Byte17to20;
        public UInt32 Byte21to24;
        public UInt32 Byte25to28;
        public UInt32 Byte29to32;

        public BitWord_32(UInt32[] bytes4)
        {
            Byte1to4 = bytes4[0];
            Byte5to8 = bytes4[1];
            Byte9to12 = bytes4[2];
            Byte13to16 = bytes4[3];
            Byte17to20 = bytes4[4];
            Byte21to24 = bytes4[5];
            Byte25to28 = bytes4[6];
            Byte29to32 = bytes4[7];
        }

        public int GetHammingDistance(IBitWord bw)
        {
            return HammingLookup.OnesCount(Byte1to4 ^ ((BitWord_32)bw).Byte1to4) +
                HammingLookup.OnesCount(Byte5to8 ^ ((BitWord_32)bw).Byte5to8) +
                HammingLookup.OnesCount(Byte9to12 ^ ((BitWord_32)bw).Byte9to12) +
                HammingLookup.OnesCount(Byte13to16 ^ ((BitWord_32)bw).Byte13to16) +
                HammingLookup.OnesCount(Byte17to20 ^ ((BitWord_32)bw).Byte17to20) +
                HammingLookup.OnesCount(Byte21to24 ^ ((BitWord_32)bw).Byte21to24) +
                HammingLookup.OnesCount(Byte25to28 ^ ((BitWord_32)bw).Byte25to28) +
                HammingLookup.OnesCount(Byte29to32 ^ ((BitWord_32)bw).Byte29to32);
        }

        public int GetHammingDistance(BitWord_32 bw)
        {
            return HammingLookup.OnesCount(Byte1to4 ^ bw.Byte1to4) +
                HammingLookup.OnesCount(Byte5to8 ^ bw.Byte5to8) +
                HammingLookup.OnesCount(Byte9to12 ^ bw.Byte9to12) +
                HammingLookup.OnesCount(Byte13to16 ^ bw.Byte13to16) +
                HammingLookup.OnesCount(Byte17to20 ^ bw.Byte17to20) +
                HammingLookup.OnesCount(Byte21to24 ^ bw.Byte21to24) +
                HammingLookup.OnesCount(Byte25to28 ^ bw.Byte25to28) +
                HammingLookup.OnesCount(Byte29to32 ^ bw.Byte29to32);
        }

        public static IBitWord Create(UInt32[] bytes4)
        {
            return new BitWord_32(bytes4);
        }

        public override string ToString()
        {
            StringBuilder res = new StringBuilder();
            res.Append("[" + Convert.ToString(Byte1to4, 16) + "]");
            res.Append("[" + Convert.ToString(Byte5to8, 16) + "]");
            res.Append("[" + Convert.ToString(Byte9to12, 16) + "]");
            res.Append("[" + Convert.ToString(Byte13to16, 16) + "]");
            res.Append("[" + Convert.ToString(Byte17to20, 16) + "]");
            res.Append("[" + Convert.ToString(Byte21to24, 16) + "]");
            res.Append("[" + Convert.ToString(Byte25to28, 16) + "]");
            res.Append("[" + Convert.ToString(Byte29to32, 16) + "]");

            return res.ToString();
        }
    }
}
