using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CanardSharp.Dsdl.DataTypes;

namespace CanardSharp.Dsdl.Testing
{
    sealed class StringUavcanTypeResolver : IUavcanTypeResolver
    {
        Dictionary<string, (UavcanTypeMeta Meta, string Definition)> _lookup =
            new Dictionary<string, (UavcanTypeMeta Meta, string Definition)>(StringComparer.Ordinal);

        public StringUavcanTypeResolver(IEnumerable<(UavcanTypeMeta Meta, string Definition)> types)
        {
            foreach (var (meta, def) in types)
            {
                _lookup[meta.FullName] = (meta, def);
            }
        }

        public UavcanType ResolveType(string ns, string typeName)
        {
            return TryResolveType(ns, typeName) ??
                 throw new Exception($"Type definition not found: {typeName}.");
        }
        public UavcanType TryResolveType(string ns, string typeName)
        {
            (UavcanTypeMeta Meta, string Definition) pair;

            if (!_lookup.TryGetValue(typeName, out pair) &&
                !_lookup.TryGetValue(ns + "." + typeName, out pair))
                return null;

            using (var reader = new StringReader(pair.Definition))
            {
                return DsdlParser.Parse(reader, pair.Meta, this);
            }
        }
    }
}
