using System;
using System.Collections.Generic;
using System.Text;

namespace Uavcan.NET.Studio
{
    record ApplicationOptions(
        string ConnectionString,
        string ToolName,
        byte? NodeId)
    {
        public static ApplicationOptions Default { get; } =
            new ApplicationOptions(null, null, null);
    }
}
