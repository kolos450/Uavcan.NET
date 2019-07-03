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

                if (!string.Equals(fullName, type.Meta.FullName, StringComparison.Ordinal))
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

        public IUavcanType ResolveType(int dtid)
        {
            return TryResolveType(dtid) ??
                 throw new Exception($"Type definition not found, ID = {dtid}.");
        }

        public IUavcanType TryResolveType(int dtid)
        {
            var name = TryResolveTypeName(dtid);
            if (name == default)
                return null;
            return TryResolveType(name.Namespace, name.Name);
        }

        protected abstract IUavcanType TryResolveTypeCore(string ns, string typeName);
        protected abstract (string Namespace, string Name) TryResolveTypeName(int dataTypeId);
    }
}
