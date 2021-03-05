using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET.IO.Can.Drivers.Slcan
{
    /// <summary>
    /// Represents the CAN filter registers.
    /// </summary>
    public class FilterValue : FilterMask
    {
        /// <summary>
        /// Create filter for extended CAN messages.
        /// </summary>
        /// <param name="extid">Filter for extended identifier</param>
        public FilterValue(int extid)
            : base(extid)
        {
            registers[1] |= 0x08;
        }

        /// <summary>
        /// Create filter for standard CAN message.
        /// </summary>
        /// <param name="sid">Filter for standard identifier</param>
        /// <param name="d0">Filter for first data byte</param>
        /// <param name="d1">Filter for second data byte</param>
        public FilterValue(int sid, byte d0, byte d1)
            : base(sid, d0, d1)
        {
        }
    }
}
