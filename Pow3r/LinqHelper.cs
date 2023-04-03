using System;
using System.Collections.Generic;

namespace Pow3r
{
    public static class LinqHelper
    {
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var item in enumerable)
            {
                action(item);
            }
        }
    }
}
