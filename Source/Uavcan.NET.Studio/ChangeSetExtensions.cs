using DynamicData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET.Studio
{
    static class ChangeSetExtensions
    {
        public static T GetFirstAddedItemOrDefault<T>(this IChangeSet<T> changeSet)
        {
            foreach (var change in changeSet)
            {
                switch (change.Reason)
                {
                    case ListChangeReason.Add:
                        return change.Item.Current;
                    case ListChangeReason.AddRange:
                        return change.Range.FirstOrDefault();
                }
            }

            return default;
        }

        public static bool Removes<T>(this IChangeSet<T> changeSet, T item, IEqualityComparer<T> equalityComparer = null)
        {
            if (changeSet.Count == 0)
                return false;

            if (equalityComparer == null)
                equalityComparer = EqualityComparer<T>.Default;

            foreach (var change in changeSet)
            {
                switch (change.Reason)
                {
                    case ListChangeReason.Clear:
                        return true;

                    case ListChangeReason.Remove:
                        if (equalityComparer.Equals(change.Item.Current, item))
                            return true;
                        break;

                    case ListChangeReason.RemoveRange:
                        if (change.Range.Any(i => equalityComparer.Equals(i, item)))
                            return true;
                        break;

                    case ListChangeReason.Replace:
                        if (equalityComparer.Equals(change.Item.Previous.Value, item))
                            return true;
                        break;
                }
            }

            return false;
        }
    }
}
