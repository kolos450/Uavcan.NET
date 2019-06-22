using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CanardSharp
{
    public class CanardRxState
    {
        public ulong TimestampUsec;

        public TransferDescriptor TransferDescriptor;

        public byte TransferId;
        public bool NextToggle;

        public ushort PayloadCrc;

        public void prepareForNextTransfer()
        {
            _singlePayloadBuffer = null;
            _payloadBuffers = null;
            TransferId++;
            NextToggle = false;
        }

        byte[] _singlePayloadBuffer;
        List<byte[]> _payloadBuffers;

        public byte[] Payload
        {
            get
            {
                if (_singlePayloadBuffer != null)
                {
                    Debug.Assert(_payloadBuffers == null);
                    return _singlePayloadBuffer;
                }
                else if (_payloadBuffers != null)
                {
                    Debug.Assert(_singlePayloadBuffer == null);

                    var singleBufferLen = 0;
                    foreach (var buf in _payloadBuffers)
                        singleBufferLen += buf.Length;

                    var singleBuffer = new byte[singleBufferLen];
                    var offset = 0;
                    foreach (var buf in _payloadBuffers)
                    {
                        Buffer.BlockCopy(buf, 0, singleBuffer, offset, buf.Length);
                        offset += buf.Length;
                    }

                    _singlePayloadBuffer = null;
                    _singlePayloadBuffer = singleBuffer;
                    return singleBuffer;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                _payloadBuffers = null;
                _singlePayloadBuffer = value;
            }
        }

        public DataTypeDescriptor DataTypeDescriptor { get; set; }

        public void SetPayload(byte[] source, int offset, int length)
        {
            _payloadBuffers = null;
            var buffer = new byte[length];
            Buffer.BlockCopy(source, offset, buffer, 0, length);
            _singlePayloadBuffer = buffer;
        }

        public void AddPayload(byte[] source, int offset, int length)
        {
            if (_payloadBuffers == null)
                _payloadBuffers = new List<byte[]>();

            if (_singlePayloadBuffer != null)
            {
                Debug.Assert(_payloadBuffers.Count == 0);
                _payloadBuffers.Add(_singlePayloadBuffer);
                _singlePayloadBuffer = null;
            }

            var buffer = new byte[length];
            Buffer.BlockCopy(source, offset, buffer, 0, length);
            _payloadBuffers.Add(buffer);
        }

        public ushort CalculateCrc()
        {
            var crc = CRC.AddSignature(CRC.InitialValue, DataTypeDescriptor.Signature);
            var payload = Payload;
            CRC.Add(crc, payload, 0, payload.Length);
            return crc;
        }
    };
}
