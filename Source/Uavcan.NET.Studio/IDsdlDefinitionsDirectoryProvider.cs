using System.Collections.Generic;

namespace Uavcan.NET.Studio
{
    public interface IDsdlDefinitionsDirectoryProvider
    {
        IEnumerable<string> Directories { get; }
    }
}
