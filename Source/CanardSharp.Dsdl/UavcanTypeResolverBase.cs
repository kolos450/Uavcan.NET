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
        ConcurrentDictionary<string, IUavcanType> _fullNameToType = new ConcurrentDictionary<string, IUavcanType>(StringComparer.Ordinal);
        ConcurrentDictionary<int, IUavcanType> _dtidToType = new ConcurrentDictionary<int, IUavcanType>();

        public IUavcanType ResolveType(string ns, string typeName)
        {
            return TryResolveType(ns, typeName) ??
                 throw new Exception($"Type definition not found: {typeName}.");
        }

        public IUavcanType TryResolveType(string ns, string typeName)
        {
            var fullName = typeName.IndexOf('.') == -1 ?
                ns + "." + typeName :
                typeName;
            if (_fullNameToType.TryGetValue(fullName, out var type))
                return type;

            lock (_fullNameToType)
            {
                if (_fullNameToType.TryGetValue(fullName, out type))
                    return type;

                type = TryResolveTypeCore(ns, typeName);

                if (type != null &&
                    !string.Equals(fullName, type.Meta.FullName, StringComparison.Ordinal))
                {
                    fullName = type.Meta.FullName;

                    if (_fullNameToType.TryGetValue(fullName, out type))
                        return type;
                }

                _fullNameToType[fullName] = type;
                if (type != null)
                    DsdlParser.ResolveNestedTypes(type, this);
            }

            return type;
        }

        public IUavcanType ResolveType(int dtid, UavcanTypeKind kind)
        {
            return TryResolveType(dtid, kind) ??
                 throw new Exception($"Type definition not found, ID = {dtid}.");
        }

        public IUavcanType TryResolveType(int dtid, UavcanTypeKind kind)
        {
            foreach (var fullName in ResolveTypeNames(dtid))
            {
                var type = TryResolveType(fullName.Namespace, fullName.Name);
                if (type.Kind == kind)
                    return type;
            }
            return null;
        }

        protected abstract IUavcanType TryResolveTypeCore(string ns, string typeName);
        protected abstract IEnumerable<IUavcanTypeFullName> ResolveTypeNames(int dataTypeId);
    }
}
