using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamCore
{
    public static class FloatExtensions
    {
        public static int Round(this float num)
        {
            int r = (int)num;
            return num - r > 0.5f ? r + 1 : r;
        }
    }

    public static class DoubleExtensions
    {
        public static int Round(this double num)
        {
            int r = (int)num;
            return num - r > 0.5 ? r + 1 : r;
        }
    }
}
