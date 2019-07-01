using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp.Drivers.Slcan
{
    static class CanFrameConverter
    {
        /// <summary>
        /// Create message with given message string.
        /// </summary>
        /// <remarks>
        /// The message string is parsed. On errors, the corresponding value is
        /// set to zero. 
        /// 
        /// Example message strings:
        /// t1230        id: 123h        dlc: 0      data: --
        /// t00121122    id: 001h        dlc: 2      data: 11 22
        /// T12345678197 id: 12345678h   dlc: 1      data: 97
        /// r0037        id: 003h        dlc: 7      RTR
        /// </remarks>
        /// <param name="msg">Message string</param>
        public static CanFrame Parse(string msg)
        {
            var rtr = false;
            uint id = 0;
            int index = 1;
            char type;
            if (msg.Length > 0) type = msg[0];
            else type = 't';

            switch (type)
            {
                case 'r':
                default:
                case 't':
                    rtr = type == 'r';
                    id = Convert.ToUInt32(msg.Substring(index, index + 3), 16);
                    index += 3;
                    break;
                case 'R':
                case 'T':
                    rtr = type == 'R';
                    id = Convert.ToUInt32(msg.Substring(index, index + 8), 16);
                    index += 8;
                    break;
            }

            var length = Convert.ToInt32(msg.Substring(index, index + 1), 16);
            if (length > 8) length = 8;
            index += 1;

            var data = new byte[length];
            if (!rtr)
            {
                for (int i = 0; i < length; i++)
                {
                    data[i] = Convert.ToByte(msg.Substring(index, index + 2), 16);
                    index += 2;
                }
            }

            return new CanFrame(id, data, 0, length);
        }

        /// <summary>
        /// Get string representation of CAN message.
        /// </summary>
        /// <returns>CAN message as string representation</returns>
        public static string ToString(CanFrame frame)
        {
            var flags = frame.Id.Flags;
            var rtr = (flags & CanIdFlags.ERR) != 0;
            var id = frame.Id.Value;

            if (id > 0x1fffffff)
                id = 0x1fffffff;

            var extended = id > 0x7ff;

            string s;
            if (extended)
            {
                if (rtr) s = "R";
                else s = "T";
                s = s + string.Format("{0:X8}", id);
            }
            else
            {
                if (rtr) s = "r";
                else s = "t";
                s = s + string.Format("{0:X3}", id);
            }
            s = s + string.Format("{0:X1}", frame.DataLength);

            if (!rtr)
            {
                for (int i = 0; i < frame.DataLength; i++)
                {
                    s = s + string.Format("{0:X2}", frame.Data[frame.DataOffset + i]);
                }
            }
            return s;
        }
    }
}
