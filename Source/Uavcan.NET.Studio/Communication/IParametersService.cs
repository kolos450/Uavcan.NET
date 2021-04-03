using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Uavcan.NET.Studio.Communication
{
    public interface IParametersService
    {
        ParametersAccessor CreateAccessor(NodeHandle handle, CancellationToken cancellationToken = default);
    }
}
