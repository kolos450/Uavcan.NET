using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET
{
    public sealed class CanFramesProcessingException : Exception
    {
        public CanFramesProcessingException(string message, params CanFrame[] sourceFrames)
            : this(message, (IEnumerable<CanFrame>)sourceFrames)
        { }

        public CanFramesProcessingException(string message, IEnumerable<CanFrame> sourceFrames)
            : base(message)
        {
            SourceFrames = sourceFrames;
        }

        public IEnumerable<CanFrame> SourceFrames { get; }
    }
}
