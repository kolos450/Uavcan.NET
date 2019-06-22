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
        UavcanType ResolveType(string ns, string typeName);
    }
}
