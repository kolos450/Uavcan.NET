using CanardSharp.IO;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp.Dsdl
{
    static class BitSerializer
    {
        public static int IntBitLength(int value) => Math.Max((int)Math.Ceiling(Math.Log(value, 2)), 1);

        public static bool ReadBoolean(BitStreamReader bitStream, int bitLength) =>
            Read(bitStream, b => LittleEndianBitConverter.ToBoolean(b), bitLength);
        public static long ReadInt(BitStreamReader bitStream, int bitLength) =>
            Read(bitStream, b => ExtendSignBit(LittleEndianBitConverter.ToInt64(b), bitLength), bitLength);
        public static ulong ReadUInt(BitStreamReader bitStream, int bitLength) =>
            Read(bitStream, b => LittleEndianBitConverter.ToUInt64(b), bitLength);
        public static double ReadFloat(BitStreamReader bitStream, int bitLength) =>
            Read(bitStream, b => LittleEndianBitConverter.ToDouble(b), bitLength);

        public static object ReadIntTyped(BitStreamReader bitStream, int bitLength)
        {
            var value = ReadInt(bitStream, bitLength);
            if (bitLength <= 8)
                return (sbyte)value;
            if (bitLength <= 16)
                return (short)value;
            if (bitLength <= 32)
                return (int)value;
            return value;
        }

        public static object ReadUIntTyped(BitStreamReader bitStream, int bitLength)
        {
            var value = ReadUInt(bitStream, bitLength);
            if (bitLength <= 8)
                return (byte)value;
            if (bitLength <= 16)
                return (ushort)value;
            if (bitLength <= 32)
                return (uint)value;
            return value;
        }

        public static object ReadFloatTyped(BitStreamReader bitStream, int bitLength)
        {
            var value = ReadFloat(bitStream, bitLength);
            if (bitLength <= 32)
                return (float)value;
            return value;
        }

        /**
         * This function can be used to extract values from received UAVCAN transfers. It decodes a scalar value -
         * boolean, integer, character, or floating point - from the specified bit position in the RX transfer buffer.
         * Simple single-frame transfers can also be parsed manually.
         *
         * Returns the number of bits successfully decoded, which may be less than requested if operation ran out of
         * buffer boundaries.
         *
         * Caveat:  This function works correctly only on platforms that use two's complement signed integer representation.
         *          I am not aware of any modern microarchitecture that uses anything else than two's complement, so it should
         *          not affect portability in any way.
         *
         * The type of value pointed to by 'out_value' is defined as follows:
         *
         *  | bit_length | value_is_signed | out_value points to                   |
         *  |------------|-----------------|---------------------------------------|
         *  | 1          | false           | bool (may be incompatible with byte!) |
         *  | 1          | true            | N/A                                   |
         *  | [2, 8]     | false           | byte, or char                         |
         *  | [2, 8]     | true            | byte, or char                         |
         *  | [9, 16]    | false           | ushort                                |
         *  | [9, 16]    | true            | short                                 |
         *  | [17, 32]   | false           | uint                                  |
         *  | [17, 32]   | true            | int, or 32-bit float                  |
         *  | [33, 64]   | false           | ulong                                 |
         *  | [33, 64]   | true            | long, or 64-bit float                 |
         */
        static T Read<T>(BitStreamReader reader, Func<byte[], T> func, int bitLength)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));
            if (bitLength < 1 || bitLength > 64)
                throw new ArgumentOutOfRangeException(nameof(bitLength));

            var bytes = _arrayPool.Rent(8);
            try
            {
                Array.Clear(bytes, 0, bytes.Length);

                var bytesToRead = (bitLength + 7) / 8;
                for (int i = 0; i < bytesToRead; i++)
                {
                    bytes[i] = reader.ReadByte(i == bytesToRead - 1 ? bitLength % 8 : 8);
                }

                return func(bytes);
            }
            finally
            {
                _arrayPool.Return(bytes);
            }

            throw new InvalidOperationException();
        }

        static sbyte ExtendSignBit(sbyte value, int bitLength)
        {
            if ((value & (1 << (bitLength - 1))) != 0)
                value |= (sbyte)(0xFF & ~((1 << bitLength) - 1));

            return value;
        }

        static short ExtendSignBit(short value, int bitLength)
        {
            if ((value & (1 << (bitLength - 1))) != 0)
                value |= (short)(0xFFFF & ~((1 << bitLength) - 1));

            return value;
        }

        static int ExtendSignBit(int value, int bitLength)
        {
            if ((value & (1 << (bitLength - 1))) != 0)
                value |= (int)(0xFFFFFFFFU & ~((1 << bitLength) - 1));

            return value;
        }

        static long ExtendSignBit(long value, int bitLength)
        {
            if ((value & (1L << (bitLength - 1))) != 0)
                value |= (long)(0xFFFFFFFFFFFFFFFFUL & ~((1UL << bitLength) - 1));

            return value;
        }

        static ArrayPool<byte> _arrayPool = ArrayPool<byte>.Create();

        static void Write(BitStreamWriter destination, Action<byte[]> bytesFiller, int bitLength)
        {
            var buffer = _arrayPool.Rent(8);
            try
            {
                bytesFiller(buffer);
                Write(destination, buffer, 0, bitLength);
            }
            finally
            {
                _arrayPool.Return(buffer);
            }
        }

        public static void Write(BitStreamWriter destination, bool value, int bitLength) =>
            Write(destination, b => LittleEndianBitConverter.FillBytes(value, b), bitLength);
        //public void Write(BitStreamWriter destination, sbyte value, int bitLength) =>
        //    Write(destination, b => LittleEndianBitConverter.FillBytes(value, b), bitLength);
        //public void Write(BitStreamWriter destination, byte value, int bitLength) =>
        //    Write(destination, b => LittleEndianBitConverter.FillBytes(value, b), bitLength);
        //public void Write(BitStreamWriter destination, short value, int bitLength) =>
        //    Write(destination, b => LittleEndianBitConverter.FillBytes(value, b), bitLength);
        //public void Write(BitStreamWriter destination, ushort value, int bitLength) =>
        //    Write(destination, b => LittleEndianBitConverter.FillBytes(value, b), bitLength);
        //public void Write(BitStreamWriter destination, int value, int bitLength) =>
        //    Write(destination, b => LittleEndianBitConverter.FillBytes(value, b), bitLength);
        //public void Write(BitStreamWriter destination, uint value, int bitLength) =>
        //    Write(destination, b => LittleEndianBitConverter.FillBytes(value, b), bitLength);
        public static void Write(BitStreamWriter destination, long value, int bitLength) =>
            Write(destination, b => LittleEndianBitConverter.FillBytes(value, b), bitLength);
        public static void Write(BitStreamWriter destination, ulong value, int bitLength) =>
            Write(destination, b => LittleEndianBitConverter.FillBytes(value, b), bitLength);
        //public void Write(BitStreamWriter destination, float value, byte bitLength) =>
        //    Write(destination, b => LittleEndianBitConverter.FillBytes(value, b), bitLength);
        public static void Write(BitStreamWriter destination, double value, int bitLength) =>
            Write(destination, b => LittleEndianBitConverter.FillBytes(value, b), bitLength);

        /**
         * This function can be used to encode values for later transmission in a UAVCAN transfer. It encodes a scalar value -
         * boolean, integer, character, or floating point - and puts it to the specified bit position in the specified
         * contiguous buffer.
         * Simple single-frame transfers can also be encoded manually.
         *
         * Caveat:  This function works correctly only on platforms that use two's complement signed integer representation.
         *          I am not aware of any modern microarchitecture that uses anything else than two's complement, so it should
         *          not affect portability in any way.
         *
         * The type of value pointed to by 'value' is defined as follows:
         *
         *  | bit_length | value points to                       |
         *  |------------|---------------------------------------|
         *  | 1          | bool (may be incompatible with byte!) |
         *  | [2, 8]     | byte, byte, or char                   |
         *  | [9, 16]    | ushort, short                         |
         *  | [17, 32]   | uint, int, or 32-bit float            |
         *  | [33, 64]   | ulong, long, or 64-bit float          |
         */
        static void Write(BitStreamWriter destination,
                        byte[] source,
                        int sourceOffset,
                        int bitLength)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (bitLength < 1 || bitLength > 64)
                throw new ArgumentOutOfRangeException(nameof(bitLength));

            var bytesToWrite = (bitLength + 7) / 8;
            for (int i = 0; i < bytesToWrite; i++)
            {
                destination.Write(source[sourceOffset + i], i == bytesToWrite - 1 ? bitLength % 8 : 8);
            }
        }
    }
}
