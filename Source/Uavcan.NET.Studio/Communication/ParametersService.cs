using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Uavcan.NET.Studio.Communication
{
    public sealed class ParametersService : IParametersService, IDisposable
    {
        private UavcanInstance _uavcan;

        public ParametersService(UavcanInstance uavcan)
        {
            _uavcan = uavcan;
        }

        public ParametersAccessor CreateAccessor(NodeHandle handle, CancellationToken ct)
        {
            return new ParametersAccessor(_uavcan, handle);
        }

        public void Dispose()
        {

        }
    }
}
