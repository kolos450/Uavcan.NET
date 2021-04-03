using System;
using System.Collections.Generic;
using System.Text;
using Uavcan.NET.Studio.DataTypes.Protocol.Param;

namespace Uavcan.NET.Studio.Communication
{
    public sealed class ParameterSetException : Exception
    {
        public ParameterSetException()
        {
        }

        public ParameterSetException(string message)
            : base(message)
        {
        }

        public ParameterSetException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public Value LocalValue { get; set; }
        public Value RemoteValue { get; set; }
    }
}
