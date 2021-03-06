﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uavcan.NET.Dsdl.DataTypes;

namespace Uavcan.NET.Dsdl.Testing
{
    sealed class StringUavcanTypeResolver : UavcanTypeResolverBase
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

        protected override IUavcanType TryResolveTypeCore(string ns, string typeName)
        {
            (UavcanTypeMeta Meta, string Definition) pair;

            if (!_lookup.TryGetValue(typeName, out pair) &&
                !_lookup.TryGetValue(ns + "." + typeName, out pair))
                return null;

            using (var reader = new StringReader(pair.Definition))
            {
                return DsdlParser.Parse(reader, pair.Meta);
            }
        }

        protected override IEnumerable<IUavcanTypeFullName> ResolveTypeNames(int dataTypeId)
        {
            yield break;
        }
    }
}
