using Uavcan.NET.IO.Can.Drivers.Slcan.Collections;
using RJCP.IO.Ports;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Uavcan.NET.IO.Can.Drivers.Slcan
{
    /// <summary>
    /// Represents an USBtin device.
    /// </summary>
    public class UsbTin : ICanInterface, IDisposable
    {
        readonly Encoding _encoding = Encoding.ASCII;

        /// <summary>
        /// Serial port (virtual) to which USBtin is connected.
        /// </summary>
        protected SerialPortStream _serialPort;

        /// <summary>
        /// Characters coming from USBtin are collected in this StringBuilder.
        /// </summary>
        protected StringBuilder _incomingMessage = new();

        /// <summary>
        /// Listener for CAN messages.
        /// </summary>
        public event EventHandler<CanMessageEventArgs> MessageReceived;

        public event EventHandler<CanMessageEventArgs> MessageTransmitted;

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

        Task _readerTask;
        Task _eventsTask;
        CancellationTokenSource _backgroundTasksCancellationTokenSource;
        SemaphoreSlim _txSemaphore = new(0, 1);
        PriorityQueue<CanFrame> _txQueue = new();
        CanFrame _txCurrentMessage = null;

        SemaphoreSlim _eventsSemaphore = new(0, 1);
        ConcurrentQueue<CanFrame> _rxEventQueue = new();
        ConcurrentQueue<CanFrame> _txEventQueue = new();

        volatile TxState _txState = TxState.Initial;

        enum TxState
        {
            Initial,
            Pending,
            Accepted,
            Rejected
        }

        /// <summary>
        /// Connect to USBtin on given port.
        /// Opens the serial port, clears pending characters and send close command
        /// to make sure that we are in configuration mode.
        /// </summary>
        /// <param name="portName">Name of virtual serial port</param>
        public Task ConnectAsync(string portName, CancellationToken cancellationToken = default)
        {
            // Create serial port object.
            _serialPort = new SerialPortStream(portName, 115200, 8, Parity.None, StopBits.One)
            {
                ReadTimeout = SerialPortStream.InfiniteTimeout,
                WriteTimeout = TIMEOUT,
                RtsEnable = true,
                DtrEnable = true
            };

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

            return Task.CompletedTask;
        }

        async Task RaiseEvents(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await _eventsSemaphore.WaitAsync(ct).ConfigureAwait(false);
                if (ct.IsCancellationRequested)
                    return;

                while (_rxEventQueue.TryDequeue(out var message))
                {
                    MessageReceived?.Invoke(this, new CanMessageEventArgs { Message = message });
                }

                while (_txEventQueue.TryDequeue(out var message))
                {
                    MessageTransmitted?.Invoke(this, new CanMessageEventArgs { Message = message });
                }
            }
        }

        async Task ReadSerialBytesAsync(CancellationToken ct)
        {
            var bytesToRead = 1024;
            var receiveBuffer = new byte[bytesToRead];

            Task<int> rxTask = null;
            Task txTask = null;
            while ((!ct.IsCancellationRequested) && (_serialPort.IsOpen))
            {
                if (rxTask == null)
                    rxTask = _serialPort.ReadAsync(receiveBuffer, 0, bytesToRead, ct);
                if (txTask == null)
                    txTask = _txSemaphore.WaitAsync(ct);

                var completedTask = await Task.WhenAny(rxTask, txTask).ConfigureAwait(false);

                if (completedTask == rxTask)
                {
                    int numBytesRead = 0;
                    try
                    {
                        numBytesRead = await rxTask.ConfigureAwait(false);
                    }
                    catch (TimeoutException)
                    { }

                    if (numBytesRead > 0)
                        ProcessReceivedData(receiveBuffer, 0, numBytesRead);

                    rxTask = null;
                    continue;
                }
                else if (completedTask == txTask)
                {
                    switch (_txState)
                    {
                        case TxState.Initial:
                        case TxState.Accepted:
                            {
                                CanFrame message = null;
                                lock (_txQueue)
                                {
                                    var dequeueResult = _txQueue.TryDequeue(out message);
                                    Debug.Assert(!dequeueResult || message != null);
                                    Debug.Assert(dequeueResult || _txQueue.Count == 0);
                                }
                                if (message != null)
                                {
                                    _txCurrentMessage = message;
                                    await SendMessageAsync(message, ct).ConfigureAwait(false);
                                    _txState = TxState.Pending;
                                }
                                else
                                {
                                    _txState = TxState.Initial;
                                }
                            }
                            break;

                        case TxState.Rejected:
                            {
                                _txState = TxState.Pending;
                                var message = _txCurrentMessage;
                                if (message == null)
                                    throw new InvalidOperationException("Unexpected UsbTin response.");
                                await SendMessageAsync(message, ct).ConfigureAwait(false);
                            }
                            break;

                        case TxState.Pending:
                            break;

                        default:
                            throw new InvalidOperationException();
                    }

                    txTask = null;
                    continue;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        /// <summary>
        /// Disconnect, close serial port connection.
        /// </summary>
        public void Disconnect()
        {
            CloseCanChannel();

            if (_serialPort != null)
            {
                _serialPort.Close();
                _serialPort.Dispose();
                _serialPort = null;
            }
        }

        /// <summary>
        /// Open CAN channel.
        /// </summary>
        /// <param name="baudrate">Baudrate in bits/second</param>
        /// <param name="mode">CAN bus accessing mode</param>
        public Task OpenCanChannelAsync(int baudrate, UsbTinOpenMode mode, CancellationToken cancellationToken = default)
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

            _backgroundTasksCancellationTokenSource = new CancellationTokenSource();
            _readerTask = ReadSerialBytesAsync(_backgroundTasksCancellationTokenSource.Token);
            _eventsTask = RaiseEvents(_backgroundTasksCancellationTokenSource.Token);

            return Task.CompletedTask;
        }

        void ProcessReceivedData(byte[] buffer, int offset, int length)
        {
            for (int i = 0; i < length; i++)
            {
                var b = buffer[offset + i];

                if ((b == '\r') && _incomingMessage.Length > 0)
                {
                    var message = _incomingMessage.ToString();
                    char cmd = message[0];

                    // Check if this is a CAN message.
                    if (cmd == 't' || cmd == 'T' || cmd == 'r' || cmd == 'R')
                    {
                        // Create CAN message from message string.
                        var canmsg = CanFrameConverter.Parse(message);

                        // Give the CAN message to the listeners.
                        _rxEventQueue.Enqueue(canmsg);
                        SignalEvents();
                    }
                    else if ((cmd == 'z') || (cmd == 'Z'))
                    {
                        _txEventQueue.Enqueue(_txCurrentMessage);
                        _txCurrentMessage = null;
                        SignalEvents();

                        _txState = TxState.Accepted;
                        SignalTx();
                    }

                    _incomingMessage.Clear();

                }
                else if (b == 0x07)
                {
                    // Resend first element from tx fifo.
                    _txState = TxState.Rejected;
                    SignalTx();
                }
                else if (b != '\r')
                {
                    _incomingMessage.Append((char)b);
                }
            }
        }

        /// <summary>
        /// Close CAN channel.
        /// </summary>
        public void CloseCanChannel()
        {
            if (_backgroundTasksCancellationTokenSource != null)
            {
                _backgroundTasksCancellationTokenSource.Cancel();
                _backgroundTasksCancellationTokenSource.Dispose();
                _backgroundTasksCancellationTokenSource = null;
            }

            if (_readerTask != null)
            {
                try
                {
                    _readerTask.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException) { }
                _readerTask = null;
            }

            if (_eventsTask != null)
            {
                try
                {
                    _eventsTask.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException) { }
                _eventsTask = null;
            }

            if (_serialPort?.IsOpen == true)
            {
                _serialPort.Write("C\r");
            }

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
        protected string Transmit(string cmd)
        {
            string cmdline = cmd + "\r";
            _serialPort.Write(cmdline);

            return ReadResponse();
        }

        Task SendMessageAsync(CanFrame message, CancellationToken ct)
        {
            var bytes = _encoding.GetBytes(CanFrameConverter.ToString(message) + "\r");
            return _serialPort.WriteAsync(bytes, 0, bytes.Length, ct);
        }

        /// <summary>
        /// Send given can message.
        /// </summary>
        /// <param name="canmsg">Can message to send</param>
        public void Send(CanFrame canmsg)
        {
            lock (_txQueue)
            {
                _txQueue.Enqueue(canmsg);
            }

            SignalTx();
        }

        void SignalTx()
        {
            try
            {
                if (_txSemaphore.CurrentCount == 0)
                    _txSemaphore.Release();
            }
            catch (SemaphoreFullException) { }
        }

        void SignalEvents()
        {
            try
            {
                if (_eventsSemaphore.CurrentCount == 0)
                    _eventsSemaphore.Release();
            }
            catch (SemaphoreFullException) { }
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

        bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Disconnect();

                    if (_txSemaphore != null)
                    {
                        _txSemaphore.Dispose();
                        _txSemaphore = null;
                    }

                    if (_eventsSemaphore != null)
                    {
                        _eventsSemaphore.Dispose();
                        _eventsSemaphore = null;
                    }

                    if (_serialPort != null)
                    {
                        _serialPort.Dispose();
                        _serialPort = null;
                    }

                    if (_backgroundTasksCancellationTokenSource != null)
                    {
                        _backgroundTasksCancellationTokenSource.Dispose();
                        _backgroundTasksCancellationTokenSource = null;
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
