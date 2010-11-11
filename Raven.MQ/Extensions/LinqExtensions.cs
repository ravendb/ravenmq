using System;
using System.Collections.Generic;

namespace RavenMQ.Extensions
{
    public static class LinqExtensions
    {
        public static void Apply<T>(this IEnumerable<T> self, Action<T> action)
        {
            foreach (var t in self)
            {
                action(t);
            }
        }    
    }
}