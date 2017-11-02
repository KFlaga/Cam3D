using System;
using System.Collections.Generic;
using System.Linq;

namespace CamCore
{
    public static class ListExtensions
    {
        public static void RemoveIf<T>(this List<T> list, Func<T, bool> predicate)
        {
            for(int i = 0; i < list.Count; ++i)
            {
                if(predicate(list[i]))
                {
                    list.RemoveAt(i);
                    continue;
                }
            }
        }

        public static void RemoveOneIf<T>(this List<T> list, Func<T, bool> predicate)
        {
            for(int i = 0; i < list.Count; ++i)
            {
                if(predicate(list[i]))
                {
                    list.RemoveAt(i);
                    return;
                }
            }
        }
    }
}
