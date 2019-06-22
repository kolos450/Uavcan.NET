using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp.Drivers.Slcan
{
    /// <summary>
    /// Represents the CAN filter mask registers.
    /// </summary>
    public class FilterMask
    {
        /// <summary>
        /// Registers in MCP2515 style.
        /// </summary>
        protected byte[] registers = new byte[4];

        /// <summary>
        /// Create filer mask for extended CAN messages.
        /// </summary>
        /// <remarks>
        /// Bitmask:
        /// 0 - accept (accept regardless of filter)
        /// 1 - check (accept only if filter matches)
        /// 
        /// Examples:
        /// fm1 = new FilterMask(0x1fffffff); // Check whole extended id
        /// fm2 = new FilterMask(0x1fffff00); // Check extended id except last 8 bits
        /// </remarks>
        /// <param name="extid">Filter mask for CAN identifier</param>
        public FilterMask(int extid)
        {
            registers[0] = (byte)((extid >> 21) & 0xff);
            registers[1] = (byte)(((extid >> 16) & 0x03) | ((extid >> 13) & 0xe0));
            registers[2] = (byte)((extid >> 8) & 0xff);
            registers[3] = (byte)(extid & 0xff);
        }

        /// <summary>
        /// Create filter mask for standard CAN messages.
        /// </summary>
        /// <remarks>
        /// Bitmask:
        /// 0 - accept (accept regardless of filter)
        /// 1 - check (accept only if filter matches)
        /// 
        /// Examples:
        /// fm1 = new FilterMask(0x7ff, (byte)0x00, (byte)0x00); // check whole id, data bytes are irrelevant
        /// fm2 = new FilterMask(0x7f0, (byte)0x00, (byte)0x00); // check whole id except last 4 bits, data bytes are irrelevant
        /// fm2 = new FilterMask(0x7f0, (byte)0xff, (byte)0x00); // check whole id except last 4 bits, check first data byte, second is irrelevant
        /// </remarks>
        /// <param name="sid">Filter mask for CAN identifier</param>
        /// <param name="d0">Filter mask for first data byte</param>
        /// <param name="d1">Filter mask for second data byte</param>
        public FilterMask(int sid, byte d0, byte d1)
        {

            registers[0] = (byte)((sid >> 3) & 0xff);
            registers[1] = (byte)((sid & 0x7) << 5);
            registers[2] = d0;
            registers[3] = d1;
        }

        /// <summary>
        /// Get register values in MCP2515 style.
        /// </summary>
        public byte[] Registers => registers;
    }
}
