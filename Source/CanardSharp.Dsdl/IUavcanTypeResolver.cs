using CanardSharp.Dsdl.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp.Dsdl
{
    public interface IUavcanTypeResolver
    {
        IUavcanType ResolveType(string ns, string typeName);

        IUavcanType TryResolveType(string ns, string typeName);
    }
}
