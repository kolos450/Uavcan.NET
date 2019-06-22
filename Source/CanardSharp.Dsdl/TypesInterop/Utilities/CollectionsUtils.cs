using System;
using System.Collections.Generic;
using System.Text;

namespace CanardSharp.Dsdl.TypesInterop.Utilities
{
    static class CollectionsUtils
    {
        /// <summary>
        /// Adds the elements of the specified collection to the specified generic <see cref="IList{T}"/>.
        /// </summary>
        /// <param name="initial">The list to add to.</param>
        /// <param name="collection">The collection of elements to add.</param>
        public static void AddRange<T>(this IList<T> initial, IEnumerable<T> collection)
        {
            if (initial == null)
            {
                throw new ArgumentNullException(nameof(initial));
            }

            if (collection == null)
            {
                return;
            }

            foreach (T value in collection)
            {
                initial.Add(value);
            }
        }

        public static int IndexOf<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
        {
            int index = 0;
            foreach (T value in collection)
            {
                if (predicate(value))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }
    }
}
