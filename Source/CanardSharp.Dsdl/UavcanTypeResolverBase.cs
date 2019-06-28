using CanardSharp.Dsdl.DataTypes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp.Dsdl
{
    public abstract class UavcanTypeResolverBase : IUavcanTypeResolver
    {
        ConcurrentDictionary<string, IUavcanType> _lookup = new ConcurrentDictionary<string, IUavcanType>(StringComparer.Ordinal);

        public IUavcanType ResolveType(string ns, string typeName)
        {
            return TryResolveType(ns, typeName) ??
                 throw new Exception($"Type definition not found: {typeName}.");
        }

        public IUavcanType TryResolveType(string ns, string typeName)
        {
            var key = ns + "." + typeName;
            if (_lookup.TryGetValue(key, out var type))
                return type;

            lock (_lookup)
            {
                var fullName = ns + "." + typeName;
                if (_lookup.TryGetValue(fullName, out type))
                    return type;

                type = TryResolveTypeCore(ns, typeName);

                _lookup[fullName] = type;
                if(type != null)
                    DsdlParser.ResolveNestedTypes(type, this);
            }

            return type;
        }

        protected abstract IUavcanType TryResolveTypeCore(string ns, string typeName);
    }
}
