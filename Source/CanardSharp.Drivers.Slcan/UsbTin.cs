using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CanardSharp.Drivers.Slcan
{
    /// <summary>
    /// Represents an USBtin device.
    /// </summary>
    public class UsbTin : IDisposable
    {
        /// <summary>
        /// Serial port (virtual) to which USBtin is connected.
        /// </summary>
        protected SerialPort _serialPort;

        /// <summary>
        /// Characters coming from USBtin are collected in this StringBuilder.
        /// </summary>
        protected StringBuilder _incomingMessage = new StringBuilder();

        /// <summary>
        /// Listener for CAN messages.
        /// </summary>
        public event EventHandler<CanMessageReceivedEventArgs> MessageReceived;

        /// <summary>
        /// Transmit fifo.
        /// </summary>
        protected ConcurrentQueue<CanMessage> _fifoTX = new ConcurrentQueue<CanMessage>();

        /// <summary>
        /// USBtin firmware version.
        /// </summary>
        protected string _firmwareVersion;

        /// <summary>
        /// USBtin hardware version.
        /// </summary>
        protected string _hardwareVersion;

        /// <summary>
        /// USBtin serial number.
        /// </summary>
        protected string _serialNumber;

        /// <summary>
        /// Timeout for response from USBtin.
        /// </summary>
        protected const int TIMEOUT = 1000;

        /// <summary>
        /// Get firmware version string.
        /// </summary>
        /// <remarks>
        /// During connect() the firmware version is requested from USBtin.
        /// </remarks>
        public string FirmwareVersion => _firmwareVersion;

        /// <summary>
        /// Get hardware version string.
        /// </summary>
        /// <remarks>
        /// During connect() the hardware version is requested from USBtin.
        /// </remarks>
        public string HardwareVersion => _hardwareVersion;

        /// <summary>
        /// Get serial number string.
        /// </summary>
        /// <remarks>
        /// During connect() the serial number is requested from USBtin.
        /// </remarks>
        public string SerialNumber => _serialNumber;

        /// <summary>
        /// Connect to USBtin on given port.
        /// Opens the serial port, clears pending characters and send close command
        /// to make sure that we are in configuration mode.
        /// </summary>
        /// <param name="portName">Name of virtual serial port</param>
        public void Connect(string portName)
        {
            // Create serial port object.
            _serialPort = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One);

            _serialPort.ReadTimeout = TIMEOUT;
            _serialPort.WriteTimeout = TIMEOUT;

            _serialPort.Open();

            // Clear port and make sure we are in configuration mode (close cmd).
            _serialPort.Write("\rC\r");
            Thread.Sleep(100);
            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();
            _serialPort.Write("C\r");

            int b;
            do
            {
                b = _serialPort.ReadByte();
                if (b == -1)
                    throw new IOException("USBTin communication error.");
            } while ((b != '\r') && (b != 7));

            // Get version strings.
            _firmwareVersion = Transmit("v").Substring(1);
            _hardwareVersion = Transmit("V").Substring(1);
            _serialNumber = Transmit("N").Substring(1);

            // Reset overflow error flags.
            Transmit("W2D00");
        }

        /// <summary>
        /// Disconnect, close serial port connection.
        /// </summary>
        public void Disconnect()
        {
            _serialPort?.Close();
        }

        /// <summary>
        /// Open CAN channel.
        /// </summary>
        /// <param name="baudrate">Baudrate in bits/second</param>
        /// <param name="mode">CAN bus accessing mode</param>
        public void OpenCanChannel(int baudrate, UsbTinOpenMode mode)
        {
            // Set baudrate.
            char baudCh = ' ';
            switch (baudrate)
            {
                case 10000: baudCh = '0'; break;
                case 20000: baudCh = '1'; break;
                case 50000: baudCh = '2'; break;
                case 100000: baudCh = '3'; break;
                case 125000: baudCh = '4'; break;
                case 250000: baudCh = '5'; break;
                case 500000: baudCh = '6'; break;
                case 800000: baudCh = '7'; break;
                case 1000000: baudCh = '8'; break;
            }

            if (baudCh != ' ')
            {
                // Use preset baudrate.
                Transmit("S" + baudCh);
            }
            else
            {
                // Calculate baudrate register settings.
                const int FOSC = 24000000;
                int xdesired = FOSC / baudrate;
                int xopt = 0;
                int diffopt = 0;
                int brpopt = 0;

                // Walk through possible can bit length (in TQ).
                for (int x = 11; x <= 23; x++)
                {
                    // Get next even value for baudrate factor.
                    int xbrp = (xdesired * 10) / x;
                    int m = xbrp % 20;
                    if (m >= 10) xbrp += 20;
                    xbrp -= m;
                    xbrp /= 10;

                    // Check bounds.
                    if (xbrp < 2) xbrp = 2;
                    if (xbrp > 128) xbrp = 128;

                    // Calculate diff.
                    int xist = x * xbrp;
                    int diff = xdesired - xist;
                    if (diff < 0) diff = -diff;

                    // Use this clock option if it is better than previous.
                    if ((xopt == 0) || (diff <= diffopt)) { xopt = x; diffopt = diff; brpopt = xbrp / 2 - 1; };
                }

                // Mapping for CNF register values.
                var cnfvalues = new int[] { 0x9203, 0x9303, 0x9B03, 0x9B04, 0x9C04, 0xA404, 0xA405, 0xAC05, 0xAC06, 0xAD06, 0xB506, 0xB507, 0xBD07 };

                Transmit("s" + string.Format("{0:X2}", brpopt | 0xC0) + string.Format("{0:X4}", cnfvalues[xopt - 11]));
            }

            // Open can channel.
            char modeCh;
            switch (mode)
            {
                case UsbTinOpenMode.Listenonly:
                    modeCh = 'L';
                    break;
                case UsbTinOpenMode.Loopback:
                    modeCh = 'l';
                    break;
                case UsbTinOpenMode.Active:
                    modeCh = 'O';
                    break;
                default:
                    throw new ArgumentException(nameof(mode));
            }
            Transmit(modeCh + "");

            // Register serial port event listener.
            _serialPort.DataReceived += SerialPort_DataReceived;
        }

        void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var buffer = _serialPort.ReadExisting();
            foreach (var b in buffer)
            {
                if ((b == '\r') && _incomingMessage.Length > 0)
                {
                    var message = _incomingMessage.ToString();
                    char cmd = message[0];

                    // Check if this is a CAN message.
                    if (cmd == 't' || cmd == 'T' || cmd == 'r' || cmd == 'R')
                    {
                        // Create CAN message from message string.
                        var canmsg = new CanMessage(message);

                        // Give the CAN message to the listeners.
                        MessageReceived?.Invoke(this, new CanMessageReceivedEventArgs { Message = canmsg });
                    }
                    else if ((cmd == 'z') || (cmd == 'Z'))
                    {
                        // Remove first message from transmit fifo and send next one.
                        _fifoTX.TryDequeue(out _);

                        SendFirstTXFifoMessage();
                    }

                    _incomingMessage.Clear();

                }
                else if (b == 0x07)
                {
                    // Resend first element from tx fifo.
                    SendFirstTXFifoMessage();
                }
                else if (b != '\r')
                {
                    _incomingMessage.Append(b);
                }
            }
        }

        /**
         * Close CAN channel.
         */
        public void CloseCanChannel()
        {
            _serialPort.DataReceived -= SerialPort_DataReceived;
            _serialPort.Write("C\r");

            _firmwareVersion = null;
            _hardwareVersion = null;
        }

        /// <summary>
        /// Read response from USBtin.
        /// </summary>
        /// <returns>Response from USBtin</returns>
        protected string ReadResponse()
        {
            StringBuilder response = new StringBuilder();
            while (true)
            {
                int b = _serialPort.ReadByte();
                if (b == -1)
                {
                    throw new IOException("USBTin communication error.");
                }
                if (b == '\r')
                {
                    return response.ToString();
                }
                else if (b == 7)
                {
                    throw new IOException($"{_serialPort.PortName}, transmit, BELL signal");
                }
                else
                {
                    response.Append((char)b);
                }
            }
        }

        /// <summary>
        /// Transmit given command to USBtin.
        /// </summary>
        /// <param name="cmd">Command</param>
        /// <returns>Response from USBtin</returns>
        public string Transmit(string cmd)
        {
            string cmdline = cmd + "\r";
            _serialPort.Write(cmdline);

            return ReadResponse();
        }

        public void Dispose()
        {
            if (_serialPort != null)
            {
                _serialPort.Dispose();
                _serialPort = null;
            }
        }

        /// <summary>
        /// Send first message in tx fifo.
        /// </summary>
        protected void SendFirstTXFifoMessage()
        {
            if (_fifoTX.TryPeek(out var message))
            {
                _serialPort.Write(message.ToString() + "\r");
            }
        }

        /// <summary>
        /// Send given can message.
        /// </summary>
        /// <param name="canmsg">Can message to send</param>
        public void Send(CanMessage canmsg)
        {
            _fifoTX.Enqueue(canmsg);

            if (_fifoTX.Count() == 1)
                SendFirstTXFifoMessage();
        }

        /// <summary>
        /// Write given register of MCP2515.
        /// </summary>
        /// <param name="register">Register address</param>
        /// <param name="value">Value to write</param>
        public void WriteMCPRegister(int register, byte value)
        {
            var cmd = "W" + string.Format("{0:X2}", register) + string.Format("{0:X2}", value);
            Transmit(cmd);
        }

        /// <summary>
        /// Write given mask registers to MCP2515.
        /// </summary>
        /// <param name="maskid">Mask identifier (0 = RXM0, 1 = RXM1)</param>
        /// <param name="registers">Register values to write</param>
        protected void WriteMCPFilterMaskRegisters(int maskid, byte[] registers)
        {
            for (int i = 0; i < 4; i++)
            {
                WriteMCPRegister(0x20 + maskid * 4 + i, registers[i]);
            }
        }

        /// <summary>
        /// Write given filter registers to MCP2515.
        /// </summary>
        /// <param name="filterid">Filter identifier (0 = RXF0, ... 5 = RXF5)</param>
        /// <param name="registers">Register values to write</param>
        protected void WriteMCPFilterRegisters(int filterid, byte[] registers)
        {
            var startregister = new int[] { 0x00, 0x04, 0x08, 0x10, 0x14, 0x18 };

            for (int i = 0; i < 4; i++)
            {
                WriteMCPRegister(startregister[filterid] + i, registers[i]);
            }
        }

        /// <summary>
        /// Set hardware filters.
        /// </summary>
        /// <remarks>
        /// Call this function after connect() and before openCANChannel().
        /// </remarks>
        /// <param name="fc">Filter chains (USBtin supports maximum 2 hardware filter chains)</param>
        public void SetFilter(FilterChain[] fc)
        {

            /*
             * The MCP2515 offers two filter chains. Each chain consists of one mask
             * and a set of filters:
             * 
             * RXM0         RXM1
             *   |            |
             * RXF0         RXF2
             * RXF1         RXF3
             *              RXF4
             *              RXF5
             */

            // If no filter chain given, accept all messages.
            if ((fc == null) || (fc.Length == 0))
            {

                byte[] registers = { 0, 0, 0, 0 };
                WriteMCPFilterMaskRegisters(0, registers);
                WriteMCPFilterMaskRegisters(1, registers);

                return;
            }

            // Check maximum filter channels.
            if (fc.Length > 2)
            {
                throw new ArgumentException("Too many filter chains: " + fc.Length + " (maximum is 2)!");
            }

            // Swap channels if necessary and check filter chain length.
            if (fc.Length == 2)
            {

                if (fc[0].Filters.Length > fc[1].Filters.Length)
                {
                    FilterChain temp = fc[0];
                    fc[0] = fc[1];
                    fc[1] = temp;
                }

                if ((fc[0].Filters.Length > 2) || (fc[1].Filters.Length > 4))
                {
                    throw new ArgumentException("Filter chain too long: " + fc[0].Filters.Length + "/" + fc[1].Filters.Length + " (maximum is 2/4)!");
                }

            }
            else if (fc.Length == 1)
            {

                if ((fc[0].Filters.Length > 4))
                {
                    throw new ArgumentException("Filter chain too long: " + fc[0].Filters.Length + " (maximum is 4)!");
                }
            }

            // Set MCP2515 filter/mask registers; walk through filter channels.
            int filterid = 0;
            int fcidx = 0;
            for (int channel = 0; channel < 2; channel++)
            {

                // Set mask.
                WriteMCPFilterMaskRegisters(channel, fc[fcidx].Mask.Registers);

                // Set filters.
                byte[] registers = { 0, 0, 0, 0 };
                for (int i = 0; i < (channel == 0 ? 2 : 4); i++)
                {

                    if (fc[fcidx].Filters.Length > i)
                    {
                        registers = fc[fcidx].Filters[i].Registers;
                    }

                    WriteMCPFilterRegisters(filterid, registers);
                    filterid++;
                }

                // Go to next filter chain if available.
                if (fc.Length - 1 > fcidx)
                {
                    fcidx++;
                }
            }
        }
    }
}
