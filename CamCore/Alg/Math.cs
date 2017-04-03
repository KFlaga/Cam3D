using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamCore
{
    public static class MathExtensions
    {
        public static T MaxInRange<T>(T[] range) where T : IComparable
        {
            T max = range[0];
            foreach(T el in range)
            {
                max = (dynamic)max < el ? el : max;
            }
            return max;
        }

        public static T MinInRange<T>(T[] range) where T : IComparable
        {
            T min = range[0];
            foreach(T el in range)
            {
                min = (dynamic)min > el ? el : min;
            }
            return min;
        }
    }
}
