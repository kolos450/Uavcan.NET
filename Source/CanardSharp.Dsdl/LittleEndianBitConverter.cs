using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp.Dsdl
{
    /// <summary>
    /// Converts base data types to an array of bytes, and an array of bytes to base data types in little-endian byte order.
    /// </summary>
    /// <remarks>
    /// <seealso cref="BigEndianBitConverter"/>
    /// </remarks>
    public sealed class LittleEndianBitConverter
    {
        private LittleEndianBitConverter()
        {
        }

        /// <summary>
        /// Returns the specified 32-bit signed integer value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 4.</returns>
        public static byte[] GetBytes(int value)
        {
            var buffer = new byte[4];
            FillBytes(value, buffer);
            return buffer;
        }

        /// <summary>
        /// Fills the array with four bytes of the specified 32-bit signed integer value beginning at <paramref name="startIndex"/>.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The array of bytes to store converted value at.</param>
        /// <param name="startIndex">The start index where converted value is be stored.</param>
        public static void FillBytes(int value, byte[] buffer, int startIndex)
        {
            ValidateFillArguments(buffer, startIndex, 4);

            buffer[startIndex++] = (byte)value;
            buffer[startIndex++] = (byte)(value >> 8);
            buffer[startIndex++] = (byte)(value >> 16);
            buffer[startIndex] = (byte)(value >> 24);
        }

        /// <summary>
        /// Fills the array with four bytes of the specified 32-bit signed integer value.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The array of bytes to store converted value at.</param>
        public static void FillBytes(int value, byte[] buffer) => FillBytes(value, buffer, 0);

        /// <summary>
        /// Returns the specified 32-bit unsigned integer value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 4.</returns>
        public static byte[] GetBytes(uint value)
        {
            var buffer = new byte[4];
            FillBytes(value, buffer);
            return buffer;
        }

        /// <summary>
        /// Fills the array with four bytes of the specified 32-bit unsigned integer value beginning at <paramref name="startIndex"/>.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The array of bytes to store converted value at.</param>
        /// <param name="startIndex">The start index where converted value is to be stored at <paramref name="buffer"/>.</param>
        public static void FillBytes(uint value, byte[] buffer, int startIndex) => FillBytes((int)value, buffer, startIndex);

        /// <summary>
        /// Fills the array with four bytes of the specified 32-bit unsigned integer value.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The array of bytes to store converted value at.</param>
        public static void FillBytes(uint value, byte[] buffer) => FillBytes(value, buffer, 0);

        /// <summary>
        /// Returns the specified 16-bit signed integer value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 2.</returns>
        public static byte[] GetBytes(short value)
        {
            var buffer = new byte[2];
            FillBytes(value, buffer);
            return buffer;
        }

        /// <summary>
        /// Fills the array with two bytes of the specified 16-bit signed integer value beginning at <paramref name="startIndex"/>.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The array of bytes to store converted value at.</param>
        /// <param name="startIndex">The start index where converted value is to be stored at <paramref name="buffer"/>.</param>
        public static void FillBytes(short value, byte[] buffer, int startIndex)
        {
            ValidateFillArguments(buffer, startIndex, 2);

            buffer[startIndex++] = (byte)value;
            buffer[startIndex] = (byte)(value >> 8);
        }

        /// <summary>
        /// Fills the array with two bytes of the specified 16-bit signed integer value.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The array of bytes to store converted value at.</param>
        public static void FillBytes(short value, byte[] buffer) => FillBytes(value, buffer, 0);

        /// <summary>
        /// Returns the specified 16-bit unsigned integer value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 2.</returns>
        public static byte[] GetBytes(ushort value)
        {
            var buffer = new byte[2];
            FillBytes(value, buffer);
            return buffer;
        }

        /// <summary>
        /// Fills the array with two bytes of the specified 16-bit unsigned integer value beginning at <paramref name="startIndex"/>.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The array of bytes to store converted value at.</param>
        /// <param name="startIndex">The start index where converted value is to be stored at <paramref name="buffer"/>.</param>
        public static void FillBytes(ushort value, byte[] buffer, int startIndex) => FillBytes((short)value, buffer, startIndex);

        /// <summary>
        /// Fills the array with two bytes of the specified 16-bit unsigned integer value.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The array of bytes to store converted value at.</param>
        public static void FillBytes(ushort value, byte[] buffer) => FillBytes(value, buffer, 0);

        /// <summary>
        /// Returns the specified 64-bit signed integer value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 8.</returns>
        public static byte[] GetBytes(long value)
        {
            var buffer = new byte[8];
            FillBytes(value, buffer);
            return buffer;
        }

        /// <summary>
        /// Fills the array with eight bytes of the specified 64-bit signed integer value beginning at <paramref name="startIndex"/>.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The array of bytes to store converted value at.</param>
        /// <param name="startIndex">The start index where converted value is to be stored at <paramref name="buffer"/>.</param>
        public static void FillBytes(long value, byte[] buffer, int startIndex)
        {
            ValidateFillArguments(buffer, startIndex, 8);

            FillBytes((int)value, buffer, startIndex);
            FillBytes((int)(value >> 32), buffer, startIndex + 4);
        }

        /// <summary>
        /// Fills the array with eight bytes of the specified 64-bit signed integer value.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The array of bytes to store converted value at.</param>
        public static void FillBytes(long value, byte[] buffer) => FillBytes(value, buffer, 0);

        /// <summary>
        /// Returns the specified 64-bit unsigned integer value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 8.</returns>
        public static byte[] GetBytes(ulong value)
        {
            var buffer = new byte[8];
            FillBytes(value, buffer);
            return buffer;
        }

        /// <summary>
        /// Fills the array with eight bytes of the specified 64-bit unsigned integer value beginning at <paramref name="startIndex"/>.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The array of bytes to store converted value at.</param>
        /// <param name="startIndex">The start index where converted value is to be stored at <paramref name="buffer"/>.</param>
        public static void FillBytes(ulong value, byte[] buffer, int startIndex) => FillBytes((long)value, buffer, startIndex);

        /// <summary>
        /// Fills the array with eight bytes of the specified 64-bit unsigned integer value.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The array of bytes to store converted value at.</param>
        public static void FillBytes(ulong value, byte[] buffer) => FillBytes(value, buffer, 0);

        /// <summary>
        /// Returns the specified single-precision floating point value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 4.</returns>
        public static byte[] GetBytes(float value)
        {
            var buffer = new byte[4];
            FillBytes(value, buffer);
            return buffer;
        }

        /// <summary>
        /// Fills the array with four bytes of the specified single-precision floating point value beginning at <paramref name="startIndex"/>.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The array of bytes to store converted value at.</param>
        /// <param name="startIndex">The start index where converted value is to be stored at <paramref name="buffer"/>.</param>
        public static void FillBytes(float value, byte[] buffer, int startIndex)
        {
            ValidateFillArguments(buffer, startIndex, 4);

            FillBytes(SingleToInt32Bits(value), buffer, startIndex);
        }

        /// <summary>
        /// Fills the array with four bytes of the specified single-precision floating point value.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The array of bytes to store converted value at.</param>
        public static void FillBytes(float value, byte[] buffer) => FillBytes(value, buffer, 0);

        /// <summary>
        /// Returns the specified double-precision floating point value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 8.</returns>
        public static byte[] GetBytes(double value)
        {
            var buffer = new byte[8];
            FillBytes(value, buffer);
            return buffer;
        }

        /// <summary>
        /// Fills the array with eight bytes of the specified double-precision floating point value beginning at <paramref name="startIndex"/>.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The array of bytes to store converted value at.</param>
        /// <param name="startIndex">The start index where converted value is to be stored at <paramref name="buffer"/>.</param>
        public static void FillBytes(double value, byte[] buffer, int startIndex)
        {
            ValidateFillArguments(buffer, startIndex, 8);

            FillBytes(BitConverter.DoubleToInt64Bits(value), buffer, startIndex);
        }

        /// <summary>
        /// Fills the array with eight bytes of the specified double-precision floating point value.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The array of bytes to store converted value at.</param>
        public static void FillBytes(double value, byte[] buffer) => FillBytes(value, buffer, 0);

        /// <summary>
        /// Returns the specified decimal value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 16.</returns>
        public static byte[] GetBytes(decimal value)
        {
            var buffer = new byte[16];
            FillBytes(value, buffer);
            return buffer;
        }

        /// <summary>
        /// Fills the array with sixteen bytes of the specified decimal value beginning at <paramref name="startIndex"/>.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The array of bytes to store converted value at.</param>
        /// <param name="startIndex">The start index where converted value is to be stored at <paramref name="buffer"/>.</param>
        public static void FillBytes(decimal value, byte[] buffer, int startIndex)
        {
            ValidateFillArguments(buffer, startIndex, 16);

            var bits = decimal.GetBits(value);
            for (int i = 0; i < bits.Length; ++i)
                FillBytes(bits[i], buffer, startIndex + i * sizeof(int));
        }

        /// <summary>
        /// Fills the array with sixteen bytes of the specified decimal value.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <param name="buffer">The array of bytes to store converted value at.</param>
        public static void FillBytes(decimal value, byte[] buffer) => FillBytes(value, buffer, 0);

        /// <summary>
        /// Returns the specified <see cref="bool"/> value as an array of bytes.
        /// </summary>
        /// <param name="value">A <see cref="bool"/> value.</param>
        /// <returns>An array of bytes with length 1.</returns>
        public static byte[] GetBytes(bool value)
        {
            var buffer = new byte[1];
            FillBytes(value, buffer, 0);
            return buffer;
        }

        /// <summary>
        /// Fills the array with one byte of the specified <see cref="bool"/> value beginning at <paramref name="startIndex"/>.
        /// </summary>
        /// <param name="value">A <see cref="bool"/> value.</param>
        /// <param name="buffer">The array of bytes to store converted value at.</param>
        /// <param name="startIndex">The start index where converted value is to be stored at <paramref name="buffer"/>.</param>
        public static void FillBytes(bool value, byte[] buffer, int startIndex) => FillBytesCore(value, buffer, startIndex);

        /// <summary>
        /// Fills the array with one byte of the specified <see cref="bool"/> value beginning.
        /// </summary>
        /// <param name="value">A <see cref="bool"/> value.</param>
        /// <param name="buffer">The array of bytes to store converted value at.</param>
        public static void FillBytes(bool value, byte[] buffer) => FillBytes(value, buffer, 0);

        /// <summary>
        /// Returns a 32-bit signed integer converted from four bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <param name="startIndex">The starting position within value.</param>
        /// <returns>A 32-bit signed integer formed by four bytes beginning at startIndex.</returns>
        public static int ToInt32(byte[] value, int startIndex) => (int)ToUInt32(value, startIndex);

        /// <summary>
        /// Returns a 32-bit signed integer converted from the first four bytes of a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <returns>A 32-bit signed integer formed by the first four bytes of a byte array.</returns>
        public static int ToInt32(byte[] value) => ToInt32(value, 0);

        /// <summary>
        /// Returns a 32-bit unsigned integer converted from four bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <param name="startIndex">The starting position within value.</param>
        /// <returns>A 32-bit unsigned integer formed by four bytes beginning at startIndex.</returns>
        public static uint ToUInt32(byte[] value, int startIndex)
        {
            ValidateToArguments(value, startIndex, 4);

            return
                value[startIndex] |
                (uint)value[startIndex + 1] << 8 |
                (uint)value[startIndex + 2] << 16 |
                (uint)value[startIndex + 3] << 24;
        }

        /// <summary>
        /// Returns a 32-bit unsigned integer converted from the first four bytes of a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <returns>A 32-bit unsigned integer formed by the first four bytes of a byte array.</returns>
        public static uint ToUInt32(byte[] value) => ToUInt32(value, 0);

        /// <summary>
        /// Returns a 16-bit signed integer converted from two bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <param name="startIndex">The starting position within value.</param>
        /// <returns>A 16-bit signed integer formed by two bytes beginning at startIndex.</returns>
        public static short ToInt16(byte[] value, int startIndex)
        {
            ValidateToArguments(value, startIndex, 2);

            byte b1 = value[startIndex++];
            byte b0 = value[startIndex];
            return (short)((b0 << 8) | b1);
        }

        /// <summary>
        /// Returns a 16-bit signed integer converted from the first two bytes of a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <returns>A 16-bit signed integer formed by the first two bytes of a byte array.</returns>
        public static short ToInt16(byte[] value) => ToInt16(value, 0);

        /// <summary>
        /// Returns a 16-bit unsigned integer converted from two bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <param name="startIndex">The starting position within value.</param>
        /// <returns>A 16-bit unsigned integer formed by two bytes beginning at startIndex.</returns>
        public static ushort ToUInt16(byte[] value, int startIndex) => (ushort)ToInt16(value, startIndex);

        /// <summary>
        /// Returns a 16-bit unsigned integer converted from the first two bytes of a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <returns>A 16-bit unsigned integer formed by the first two bytes of a byte array.</returns>
        public static ushort ToUInt16(byte[] value) => ToUInt16(value, 0);

        /// <summary>
        /// Returns a 64-bit signed integer converted from eight bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <param name="startIndex">The starting position within value.</param>
        /// <returns>A 64-bit signed integer formed by eight bytes beginning at startIndex.</returns>
        public static long ToInt64(byte[] value, int startIndex)
        {
            ValidateToArguments(value, startIndex, 8);

            uint h = ToUInt32(value, startIndex + 4);
            uint l = ToUInt32(value, startIndex);
            return (((long)h) << 32) | l;
        }

        /// <summary>
        /// Returns a 64-bit signed integer converted from the first eight bytes of a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <returns>A 64-bit signed integer formed by the first eight bytes of a byte array.</returns>
        public static long ToInt64(byte[] value) => ToInt64(value, 0);

        /// <summary>
        /// Returns a 64-bit unsigned integer converted from eight bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <param name="startIndex">The starting position within value.</param>
        /// <returns>A 64-bit unsigned integer formed by eight bytes beginning at startIndex.</returns>
        public static ulong ToUInt64(byte[] value, int startIndex) => (ulong)ToInt64(value, startIndex);

        /// <summary>
        /// Returns a 64-bit unsigned integer converted from the first eight bytes of a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <returns>A 64-bit unsigned integer formed by the first eight bytes of a byte array.</returns>
        public static ulong ToUInt64(byte[] value) => ToUInt64(value, 0);

        /// <summary>
        /// Returns a single-precision floating point number converted from four bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <param name="startIndex">The starting position within value.</param>
        /// <returns>A single-precision floating point number formed by four bytes beginning at <paramref name="startIndex"/>.</returns>
        public static float ToSingle(byte[] value, int startIndex)
        {
            ValidateToArguments(value, startIndex, 4);

            return Int32BitsToSingle(ToInt32(value, startIndex));
        }

        /// <summary>
        /// Returns a single-precision floating point number converted from four bytes of a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <returns>A single-precision floating point number formed by four bytes of a byte array.</returns>
        public static float ToSingle(byte[] value) => ToSingle(value, 0);

        /// <summary>
        /// Returns a double-precision floating point number converted from eight bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <param name="startIndex">The starting position within value.</param>
        /// <returns>A double-precision floating point number formed by eight bytes beginning at <paramref name="startIndex"/>.</returns>
        public static double ToDouble(byte[] value, int startIndex)
        {
            ValidateToArguments(value, startIndex, 8);

            return BitConverter.Int64BitsToDouble(ToInt64(value, startIndex));
        }

        /// <summary>
        /// Returns a double-precision floating point number converted from eight bytes of a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <returns>A double-precision floating point number formed by eight bytes of a byte array.</returns>
        public static double ToDouble(byte[] value) => ToDouble(value, 0);

        /// <summary>
        /// Returns a decimal number converted from sixteen bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <param name="startIndex">The starting position within value.</param>
        /// <returns>A decimal number formed by sixteen bytes beginning at <paramref name="startIndex"/>.</returns>
        public static decimal ToDecimal(byte[] value, int startIndex)
        {
            ValidateToArguments(value, startIndex, 16);

            var bits = new int[4];

            for (int i = 0; i < bits.Length; ++i)
                bits[i] = ToInt32(value, startIndex + i * sizeof(int));

            return new decimal(bits);
        }

        /// <summary>
        /// Returns a decimal number converted from sixteen bytes of a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <returns>A decimal number formed by sixteen of a byte array.</returns>
        public static decimal ToDecimal(byte[] value) => ToDecimal(value, 0);

        /// <summary>
        /// Returns a <see cref="Boolean"/> value converted from one byte at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <param name="startIndex">The starting position within value.</param>
        /// <returns><c>true</c> if the byte at startIndex in value is nonzero; otherwise, <c>false</c>.</returns>
        public static bool ToBoolean(byte[] value, int startIndex)
        {
            ValidateToArguments(value, startIndex, 1);

            return value[startIndex] != 0;
        }

        /// <summary>
        /// Returns a Boolean value converted from the first byte of a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <returns><c>true</c> if the first byte of a byte array is nonzero; otherwise, <c>false</c>.</returns>
        public static bool ToBoolean(byte[] value) => ToBoolean(value, 0);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        static volatile LittleEndianBitConverter _Instance;

        /// <summary>
        /// Returns a default bit converter instance for little-endian byte order.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static LittleEndianBitConverter Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new LittleEndianBitConverter();
                return _Instance;
            }
        }

        static void ValidateToArguments(byte[] value, int startIndex, int size)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            ValidateArguments(value, startIndex, size);
        }

        static void ValidateFillArguments(byte[] buffer, int startIndex, int size)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            ValidateArguments(buffer, startIndex, size);
        }

        static void ValidateArguments(byte[] array, int startIndex, int size)
        {
            if (startIndex >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex), "Index out of range.");
            if (startIndex > array.Length - size)
                throw new ArgumentException("Indexed array is too small.");
        }

        static void FillBytesCore(bool value, byte[] buffer, int startIndex)
        {
            ValidateFillArguments(buffer, startIndex, 1);

            buffer[startIndex] = value ? (byte)1 : (byte)0;
        }

#if NETCOREAPP
        public static int SingleToInt32Bits(float value) => BitConverter.SingleToInt32Bits(value);

        public static float Int32BitsToSingle(int value) => BitConverter.Int32BitsToSingle(value);
#else
        [StructLayout(LayoutKind.Explicit)]
        struct ReinterpretCastGround32
        {
            [FieldOffset(0)]
            public int Int32;

            [FieldOffset(0)]
            public float Single;
        }

        public static int SingleToInt32Bits(float value)
        {
            var rcg = new ReinterpretCastGround32();
            rcg.Single = value;
            return rcg.Int32;
        }

        public static float Int32BitsToSingle(int value)
        {
            var rcg = new ReinterpretCastGround32();
            rcg.Int32 = value;
            return rcg.Single;
        }
#endif
    }
}
