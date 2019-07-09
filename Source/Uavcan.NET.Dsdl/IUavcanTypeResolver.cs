using Uavcan.NET.Dsdl.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET.Dsdl
{
    public interface IUavcanTypeResolver
    {
        IUavcanType ResolveType(string ns, string typeName);

        IUavcanType TryResolveType(string ns, string typeName);

        IUavcanType ResolveType(int dataTypeId, UavcanTypeKind kind);

        IUavcanType TryResolveType(int dataTypeId, UavcanTypeKind kind);
    }
}
