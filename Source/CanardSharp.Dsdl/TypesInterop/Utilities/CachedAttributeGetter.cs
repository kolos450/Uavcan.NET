using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace CanardSharp.Dsdl.TypesInterop.Utilities
{
    static class CachedAttributeGetter<T> where T : Attribute
    {
        private static readonly ConcurrentDictionary<object, T> TypeAttributeCache = new ConcurrentDictionary<object, T>();

        public static T GetAttribute(object type)
        {
            return TypeAttributeCache.GetOrAdd(type, TypeReflector.GetAttribute<T>);
        }
    }
}
