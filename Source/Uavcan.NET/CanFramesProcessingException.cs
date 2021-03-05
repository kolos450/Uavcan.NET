using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using Uavcan.NET.IO.Can;

namespace Uavcan.NET
{
    [Serializable]
    public sealed class CanFramesProcessingException : Exception
    {
        public IEnumerable<CanFrame> SourceFrames { get; }

        public CanFramesProcessingException() :
            this(string.Empty, Enumerable.Empty<CanFrame>())
        { }

        public CanFramesProcessingException(string message, params CanFrame[] sourceFrames)
            : this(message, (IEnumerable<CanFrame>)sourceFrames)
        { }

        public CanFramesProcessingException(string message, IEnumerable<CanFrame> sourceFrames)
            : base(message)
        {
            SourceFrames = sourceFrames;
        }

        public CanFramesProcessingException(string message, IEnumerable<CanFrame> sourceFrames, Exception innerException)
            : base(message, innerException)
        {
            SourceFrames = sourceFrames;
        }


        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            info.AddValue(nameof(SourceFrames), SourceFrames);

            base.GetObjectData(info, context);
        }
    }
}
