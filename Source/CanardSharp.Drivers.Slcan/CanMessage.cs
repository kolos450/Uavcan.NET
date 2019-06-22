using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp.Drivers.Slcan
{
    /// <summary>
    /// Represents a CAN message.
    /// </summary>
    public class CanMessage
    {
        /// <summary>
        /// CAN message ID.
        /// </summary>
        protected int _id;

        /// <summary>
        /// Marks frames with extended message id.
        /// </summary>
        protected bool _extended;

        /// <summary>
        /// Marks request for transmition frames.
        /// </summary>
        protected bool _rtr;

        /// <summary>
        /// CAN message identifier.
        /// </summary>
        public int Id
        {
            get => _id;
            set
            {
                if (value > (0x1fffffff))
                    _id = 0x1fffffff;

                if (value > 0x7ff)
                    _extended = true;

                _id = value;
            }
        }

        /// <summary>
        /// CAN message payload data.
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Determine if CAN message id is extended
        /// </summary>
        public bool IsExtended => _extended;

        /// <summary>
        /// Determine if CAN message is a request for transmission.
        /// </summary>
        public bool IsRtr => _rtr;

        /// <summary>
        /// Create message with given id and data. Depending on Id, the extended flag is set.
        /// </summary>
        /// <param name="id">Message identifier</param>
        /// <param name="data">Payload data</param>
        public CanMessage(int id, byte[] data)
        {
            Data = data;
            _extended = false;
            Id = id;
            _rtr = false;
        }

        /// <summary>
        /// Create message with given message properties.
        /// </summary>
        /// <param name="id">Message identifier</param>
        /// <param name="data">Payload data</param>
        /// <param name="extended">Marks messages with extended identifier</param>
        /// <param name="rtr">Marks RTR messages</param>
        public CanMessage(int id, byte[] data, bool extended, bool rtr)
        {
            Id = id;
            Data = data;
            _extended = extended;
            _rtr = rtr;
        }

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
        public CanMessage(string msg)
        {
            _rtr = false;
            int index = 1;
            char type;
            if (msg.Length > 0) type = msg[0];
            else type = 't';

            switch (type)
            {
                case 'r':
                default:
                case 't':
                    _rtr = type == 'r';
                    _id = Convert.ToInt32(msg.Substring(index, index + 3), 16);
                    _extended = false;
                    index += 3;
                    break;
                case 'R':
                case 'T':
                    _rtr = type == 'R';
                    _id = Convert.ToInt32(msg.Substring(index, index + 8), 16);
                    _extended = true;
                    index += 8;
                    break;
            }

            var length = Convert.ToInt32(msg.Substring(index, index + 1), 16);
            if (length > 8) length = 8;
            index += 1;

            Data = new byte[length];
            if (!_rtr)
            {
                for (int i = 0; i < length; i++)
                {
                    Data[i] = Convert.ToByte(msg.Substring(index, index + 2), 16);
                    index += 2;
                }
            }
        }

        /// <summary>
        /// Get string representation of CAN message.
        /// </summary>
        /// <returns>CAN message as string representation</returns>
        public override string ToString()
        {
            string s;
            if (_extended)
            {
                if (_rtr) s = "R";
                else s = "T";
                s = s + string.Format("{0:X8}", _id);
            }
            else
            {
                if (_rtr) s = "r";
                else s = "t";
                s = s + string.Format("{0:X3}", _id);
            }
            s = s + string.Format("{0:X1}", Data.Length);

            if (!_rtr)
            {
                for (int i = 0; i < Data.Length; i++)
                {
                    s = s + string.Format("{0:X2}", Data[i]);
                }
            }
            return s;
        }
    }
}
