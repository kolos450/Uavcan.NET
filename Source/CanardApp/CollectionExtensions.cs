using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardApp
{
    static class CollectionExtensions
    {
        public static T GetOrAdd<K, T>(this IDictionary<K, T> dict, K key, T defaultValue)
        {
            if (!dict.TryGetValue(key, out var result))
            {
                result = defaultValue;
                dict[key] = result;
            }

            return result;
        }
    }
}
