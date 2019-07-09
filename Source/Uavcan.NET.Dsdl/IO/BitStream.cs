using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET.IO
{
    static class Native
    {
        /// <summary>
        /// Initialize the constants
        /// </summary>
        /// <SecurityNote>
        ///     Critical: Critical as this code invokes Marshal.SizeOf which uses LinkDemand for UnmanagedCode permission.
        ///     TreatAsSafe: The method doesn't take any user inputs. It only pre-computes the size of our internal types.
        /// </SecurityNote>
        static Native()
        {
            // NOTICE-2005/10/14-WAYNEZEN,
            // Make sure those lengths are indepentent from the 32bit or 64bit platform. Otherwise it could 
            // break the ISF format.
            SizeOfInt = (uint)Marshal.SizeOf(typeof(int));
            SizeOfUInt = (uint)Marshal.SizeOf(typeof(uint));
            SizeOfUShort = (uint)Marshal.SizeOf(typeof(ushort));
            SizeOfByte = (uint)Marshal.SizeOf(typeof(byte));
            SizeOfFloat = (uint)Marshal.SizeOf(typeof(float));
            SizeOfDouble = (uint)Marshal.SizeOf(typeof(double));
            SizeOfGuid = (uint)Marshal.SizeOf(typeof(Guid));
            SizeOfDecimal = (uint)Marshal.SizeOf(typeof(decimal));
        }

        internal static readonly uint SizeOfInt;      // Size of an int
        internal static readonly uint SizeOfUInt;     // Size of an unsigned int
        internal static readonly uint SizeOfUShort;   // Size of an unsigned short
        internal static readonly uint SizeOfByte;     // Size of a byte
        internal static readonly uint SizeOfFloat;    // Size of a float
        internal static readonly uint SizeOfDouble;   // Size of a double
        internal static readonly uint SizeOfGuid;    // Size of a GUID
        internal static readonly uint SizeOfDecimal; // Size of a VB-style Decimal

        internal const int BitsPerByte = 8;    // number of bits in a byte
        internal const int BitsPerShort = 16;    // number of bits in one short - 2 bytes
        internal const int BitsPerInt = 32;    // number of bits in one integer - 4 bytes
        internal const int BitsPerLong = 64;    // number of bits in one long - 8 bytes


        // since casting from floats have mantisaa components,
        //      casts from float to int are not constrained by
        //      Int32.MaxValue, but by the maximum float value
        //      whose mantissa component is still within range
        //      of an integer. Anything larger will cause an overflow.
        internal const int MaxFloatToIntValue = 2147483584 - 1; // 2.14748e+009
    }

    /// <summary>
    /// A stream-style reader for retrieving packed bits from a byte array
    /// </summary>
    /// <remarks>This bits should packed into the leftmost position in each byte.
    /// For compatibility purposes with the v1 ISF encoder and decoder, the order of the
    /// packing must not be changed. This code is a from-scratch rewrite of the BitStream
    /// natice C++ class in the v1 Ink code, but still maintaining the same packing
    /// behavior.</remarks>
    public class BitStreamReader
    {
        /// <summary>
        /// Create a new BitStreamReader to unpack the bits in a buffer of bytes
        /// </summary>
        /// <param name="buffer">Buffer of bytes</param>
        public BitStreamReader(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            _byteArray = buffer;
            _bufferLengthInBits = (uint)buffer.Length * Native.BitsPerByte;

            LengthInBytes = buffer.Length;
        }

        /// <summary>
        /// Create a new BitStreamReader to unpack the bits in a buffer of bytes
        /// </summary>
        /// <param name="buffer">Buffer of bytes</param>
        /// <param name="startIndex">The index to start reading at</param>
        public BitStreamReader(byte[] buffer, int startIndex, int length)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (startIndex < 0 || startIndex + length > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            _byteArray = buffer;
            _byteArrayIndex = startIndex;
            _bufferLengthInBits = (uint)length * Native.BitsPerByte;

            LengthInBytes = buffer.Length;
        }

        /// <summary>
        /// Create a new BitStreamReader to unpack the bits in a buffer of bytes
        /// and enforce a maximum buffer read length
        /// </summary>
        /// <param name="buffer">Buffer of bytes</param>
        /// <param name="bufferLengthInBits">Maximum number of bytes to read from the buffer</param>
        public BitStreamReader(byte[] buffer, uint bufferLengthInBits)
            : this(buffer)
        {
            if (bufferLengthInBits > (buffer.Length * Native.BitsPerByte))
                throw new ArgumentOutOfRangeException("bufferLengthInBits");

            _bufferLengthInBits = bufferLengthInBits;
        }

        /// <summary>
        /// Reads a single bit from the buffer
        /// </summary>
        /// <returns></returns>
        public bool ReadBit()
        {
            byte b = ReadByte(1);
            return (b & 1) == 1;
        }

        /// <summary>
        /// Read a specified number of bits from the stream into a single byte
        /// </summary>
        /// <param name="countOfBits">The number of bits to unpack</param>
        /// <returns>A single byte that contains up to 8 packed bits</returns>
        /// <remarks>For example, if 2 bits are read from the stream, then a full byte
        /// will be created with the least significant bits set to the 2 unpacked bits
        /// from the stream</remarks>
        public byte ReadByte(int countOfBits)
        {
            // if the end of the stream has been reached, then throw an exception
            if (EndOfStream)
                throw new EndOfStreamException();

            // we only support 1-8 bits currently, not multiple bytes, and not 0 bits
            if (countOfBits > Native.BitsPerByte || countOfBits <= 0)
                throw new ArgumentOutOfRangeException("countOfBits");

            if (countOfBits > _bufferLengthInBits)
                throw new ArgumentOutOfRangeException("countOfBits");

            _bufferLengthInBits -= (uint)countOfBits;

            // initialize return byte to 0 before reading from the cache
            byte returnByte = 0;

            // if the partial bit cache contains more bits than requested, then read the
            //      cache only
            if (_cbitsInPartialByte >= countOfBits)
            {
                // retrieve the requested count of most significant bits from the cache
                //      and store them in the least significant positions in the return byte
                int rightShiftPartialByteBy = Native.BitsPerByte - countOfBits;
                returnByte = (byte)(_partialByte >> rightShiftPartialByteBy);

                // reposition any unused portion of the cache in the most significant part of the bit cache
                unchecked // disable overflow checking since we are intentionally throwing away
                          //  the significant bits
                {
                    _partialByte <<= countOfBits;
                }
                // update the bit count in the cache
                _cbitsInPartialByte -= countOfBits;
            }
            // otherwise, we need to retrieve more full bytes from the stream
            else
            {
                // retrieve the next full byte from the stream
                byte nextByte = _byteArray[_byteArrayIndex];
                _byteArrayIndex++;

                //right shift partial byte to get it ready to or with the partial next byte
                int rightShiftPartialByteBy = Native.BitsPerByte - countOfBits;
                returnByte = (byte)(_partialByte >> rightShiftPartialByteBy);

                // now copy the remaining chunk of the newly retrieved full byte
                int rightShiftNextByteBy = Math.Abs((countOfBits - _cbitsInPartialByte) - Native.BitsPerByte);
                returnByte |= (byte)(nextByte >> rightShiftNextByteBy);

                // update the partial bit cache with the remainder of the newly retrieved full byte
                unchecked // disable overflow checking since we are intentionally throwing away
                          //  the significant bits
                {
                    _partialByte = (byte)(nextByte << (countOfBits - _cbitsInPartialByte));
                }

                _cbitsInPartialByte = Native.BitsPerByte - (countOfBits - _cbitsInPartialByte);
            }
            return returnByte;
        }

        /// <summary>
        /// Since the return value of Read cannot distinguish between valid and invalid
        /// data (e.g. 8 bits set), the EndOfStream property detects when there is no more
        /// data to read.
        /// </summary>
        /// <value>True if stream end has been reached</value>
        public bool EndOfStream
        {
            get
            {
                return 0 == _bufferLengthInBits;
            }
        }

        /// <summary>
        /// The current read index in the array
        /// </summary>
        public int CurrentIndex
        {
            get
            {
                //_byteArrayIndex is always advanced to the next index
                // so we always decrement before returning
                return _byteArrayIndex - 1;
            }
        }


        // Privates
        // reference to the source byte buffer to read from
        private byte[] _byteArray = null;

        // maximum length of buffer to read in bits
        private uint _bufferLengthInBits = 0;

        // the index in the source buffer for the next byte to be read
        private int _byteArrayIndex = 0;

        // since the bits from multiple inputs can be packed into a single byte
        //  (e.g. 2 bits per input fits 4 per byte), we use this field as a cache
        //  of the remaining partial bits.
        private byte _partialByte = 0;

        // the number of bits (partial byte) left to read in the overlapped byte field
        private int _cbitsInPartialByte = 0;

        public int LengthInBytes { get; }
    }

    /// <summary>
    /// A stream-like writer for packing bits into a byte buffer
    /// </summary>
    /// <remarks>This class is to be used with the BitStreamReader for reading
    /// and writing bytes. Note that the bytes should be read in the same order
    /// and lengths as they were written to retrieve the same values.
    /// See remarks in BitStreamReader regarding compatibility with the native C++
    /// BitStream class.</remarks>
    public class BitStreamWriter
    {
        /// <summary>
        /// Create a new bit writer that writes to the target buffer
        /// </summary>
        /// <param name="bufferToWriteTo"></param>
        public BitStreamWriter(List<byte> bufferToWriteTo)
        {
            if (bufferToWriteTo == null)
                throw new ArgumentNullException("bufferToWriteTo");

            _targetBuffer = bufferToWriteTo;
            _targetBufferOffset = bufferToWriteTo.Count;
        }

        /// <summary>
        /// Write a specific number of bits from byte input into the stream
        /// </summary>
        /// <param name="bits">The byte to read the bits from</param>
        /// <param name="countOfBits">The number of bits to read</param>
        public void Write(byte bits, int countOfBits)
        {
            // validate that a subset of the bits in a single byte are being written
            if (countOfBits <= 0 || countOfBits > Native.BitsPerByte)
                throw new ArgumentOutOfRangeException("countOfBits");

            byte buffer;
            // if there is remaining bits in the last byte in the stream
            //      then use those first
            if (_remaining > 0)
            {
                // retrieve the last byte from the stream, update it, and then replace it
                buffer = _targetBuffer[_targetBuffer.Count - 1];
                // if the remaining bits aren't enough then just copy the significant bits
                //      of the input into the remainder
                if (countOfBits > _remaining)
                {
                    buffer |= (byte)((bits & (0xFF >> (Native.BitsPerByte - countOfBits))) >> (countOfBits - _remaining));
                }
                // otherwise, copy the entire set of input bits into the remainder
                else
                {
                    buffer |= (byte)((bits & (0xFF >> (Native.BitsPerByte - countOfBits))) << (_remaining - countOfBits));
                }
                _targetBuffer[_targetBuffer.Count - 1] = buffer;
            }

            // if the remainder wasn't large enough to hold the entire input set
            if (countOfBits > _remaining)
            {
                // then copy the uncontained portion of the input set into a temporary byte
                _remaining = Native.BitsPerByte - (countOfBits - _remaining);
                unchecked // disable overflow checking since we are intentionally throwing away
                          //  the significant bits
                {
                    buffer = (byte)(bits << _remaining);
                }
                // and add it to the target buffer
                _targetBuffer.Add(buffer);
            }
            else
            {
                // otherwise, simply update the amount of remaining bits we have to spare
                _remaining -= countOfBits;
            }
        }


        // the buffer that the bits are written into
        readonly List<byte> _targetBuffer;
        readonly int _targetBufferOffset;

        // number of free bits remaining in the last byte added to the target buffer
        private int _remaining = 0;

        public int Position => _targetBuffer.Count - _targetBufferOffset;
    }
}
