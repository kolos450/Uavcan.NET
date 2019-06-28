using CanardSharp.IO;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
        public static float ReadSingle(BitStreamReader bitStream, int bitLength) =>
            Read(bitStream, b => LittleEndianBitConverter.ToSingle(b), bitLength);
        public static double ReadDouble(BitStreamReader bitStream, int bitLength) =>
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

                int i = 0;
                while (bitLength > 0)
                {
                    var currentBitLen = bitLength;
                    if (currentBitLen > 8)
                        currentBitLen = 8;
                    bytes[i++] = reader.ReadByte(currentBitLen);
                    bitLength -= currentBitLen;
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
            if (bitLength < 64 && (value & (1L << (bitLength - 1))) != 0)
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
        public static void Write(BitStreamWriter destination, float value, int bitLength) =>
            Write(destination, b => LittleEndianBitConverter.FillBytes(value, b), bitLength);
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

            while (bitLength > 0)
            {
                var currentBitLen = bitLength;
                if (currentBitLen > 8)
                    currentBitLen = 8;
                destination.Write(source[sourceOffset++], currentBitLen);
                bitLength -= currentBitLen;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        struct Float32IntegerUnion
        {
            [FieldOffset(0)] public uint I;
            [FieldOffset(0)] public float F;
        }

        public static float UInt16ToFloat32(ushort value)
        {
            var magic = new Float32IntegerUnion { I = (254 - 15) << 23 };
            var was_inf_nan = new Float32IntegerUnion { I = (127 + 16) << 23 };

            var union = new Float32IntegerUnion { I = ((uint)value & 0x7FFF) << 13 }; // exponent/mantissa bits
            union.F *= magic.F;                                                       // exponent adjust
            if (union.F >= was_inf_nan.F)                                             // make sure Inf/NaN survive
                union.I |= 255 << 23;
            union.I |= ((uint)value & 0x8000) << 16;                                  // sign bit

            return union.F;
        }

        public static ushort Float32ToUInt16(float value)
        {
            var f32infty = new Float32IntegerUnion { I = 255 << 23 };
            var f16infty = new Float32IntegerUnion { I = 31 << 23 };
            var magic = new Float32IntegerUnion { I = 15 << 23 };
            var inval = new Float32IntegerUnion { F = value };
            var sign_mask = 0x80000000;
            var round_mask = ~0xFFFU;

            var sign = inval.I & sign_mask;
            inval.I ^= sign;

            ushort @out;
            if (inval.I >= f32infty.I)                           // Inf or NaN (all exponent bits set)
            {
                @out = (ushort)(inval.I > f32infty.I ? 0x7FFFu : 0x7C00u);
            }
            else
            {
                inval.I &= round_mask;
                inval.F *= magic.F;
                inval.I -= round_mask;
                if (inval.I > f16infty.I)
                    inval.I = f16infty.I;                        // Clamp to signed infinity if overflowed
                @out = (ushort)((inval.I >> 13) & 0xFFFF);       // Take the bits!
            }
            return (ushort)(@out | (sign >> 16) & 0xFFFF);
        }
    }
}
