using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using Uavcan.NET.IO.Can.Drivers;

namespace Uavcan.NET.Studio.Diagnostics
{
#if DEBUG
    [Export(typeof(ICanPortProvider))]
#endif
    sealed class DebugCanPortProvider : ICanPortProvider
    {
        public string Name => "Debug";

        public IEnumerable<ICanPort> GetDriverPorts()
        {
            yield return new DebugCanPort();
        }
    }
}
