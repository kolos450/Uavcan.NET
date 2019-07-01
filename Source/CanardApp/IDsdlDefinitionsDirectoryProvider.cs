using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardApp
{
    public interface IDsdlDefinitionsDirectoryProvider
    {
        IEnumerable<string> Directories { get; }
    }
}
