﻿using CanardSharp.IO;
using System;
using System.Buffers;

namespace CanardSharp
{
    class UavcanSerializer
    {
        public bool DeserializeBoolean(BitStreamReader bitStream, byte bitLength) =>
            Deserialize(bitStream, b => LittleEndianBitConverter.ToBoolean(b), bitLength);
        public long DeserializeInt(BitStreamReader bitStream, byte bitLength) =>
            Deserialize(bitStream, b => ExtendSignBit(LittleEndianBitConverter.ToInt64(b), bitLength), bitLength);
        public ulong DeserializeUInt(BitStreamReader bitStream, byte bitLength) =>
            Deserialize(bitStream, b => LittleEndianBitConverter.ToUInt64(b), bitLength);
        public double DeserializeFloat(BitStreamReader bitStream, byte bitLength) =>
            Deserialize(bitStream, b => LittleEndianBitConverter.ToDouble(b), bitLength);

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
        T Deserialize<T>(BitStreamReader reader, Func<byte[], T> func, byte bitLength)
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

        ArrayPool<byte> _arrayPool = ArrayPool<byte>.Create();

        void Serialize(BitStreamWriter destination, Action<byte[]> bytesFiller, byte bitLength)
        {
            var buffer = _arrayPool.Rent(8);
            try
            {
                bytesFiller(buffer);
                Serialize(destination, buffer, 0, bitLength);
            }
            finally
            {
                _arrayPool.Return(buffer);
            }
        }

        public void Serialize(BitStreamWriter destination, bool value, byte bitLength) =>
            Serialize(destination, b => LittleEndianBitConverter.FillBytes(value, b), bitLength);
        //public void Serialize(BitStreamWriter destination, sbyte value, byte bitLength) =>
        //    Serialize(destination, b => LittleEndianBitConverter.FillBytes(value, b), bitLength);
        //public void Serialize(BitStreamWriter destination, byte value, byte bitLength) =>
        //    Serialize(destination, b => LittleEndianBitConverter.FillBytes(value, b), bitLength);
        //public void Serialize(BitStreamWriter destination, short value, byte bitLength) =>
        //    Serialize(destination, b => LittleEndianBitConverter.FillBytes(value, b), bitLength);
        //public void Serialize(BitStreamWriter destination, ushort value, byte bitLength) =>
        //    Serialize(destination, b => LittleEndianBitConverter.FillBytes(value, b), bitLength);
        //public void Serialize(BitStreamWriter destination, int value, byte bitLength) =>
        //    Serialize(destination, b => LittleEndianBitConverter.FillBytes(value, b), bitLength);
        //public void Serialize(BitStreamWriter destination, uint value, byte bitLength) =>
        //    Serialize(destination, b => LittleEndianBitConverter.FillBytes(value, b), bitLength);
        public void Serialize(BitStreamWriter destination, long value, byte bitLength) =>
            Serialize(destination, b => LittleEndianBitConverter.FillBytes(value, b), bitLength);
        public void Serialize(BitStreamWriter destination, ulong value, byte bitLength) =>
            Serialize(destination, b => LittleEndianBitConverter.FillBytes(value, b), bitLength);
        //public void Serialize(BitStreamWriter destination, float value, byte bitLength) =>
        //    Serialize(destination, b => LittleEndianBitConverter.FillBytes(value, b), bitLength);
        public void Serialize(BitStreamWriter destination, double value, byte bitLength) =>
            Serialize(destination, b => LittleEndianBitConverter.FillBytes(value, b), bitLength);

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
        void Serialize(BitStreamWriter destination,
                        byte[] source,
                        int sourceOffset,
                        byte bitLength)
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