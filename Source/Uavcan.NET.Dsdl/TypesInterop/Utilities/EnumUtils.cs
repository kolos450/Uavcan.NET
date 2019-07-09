using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET.Dsdl.TypesInterop.Utilities
{
    static class EnumUtils
    {
        static readonly ConcurrentDictionary<Type, EnumInfo> ValuesAndNamesPerEnum = new ConcurrentDictionary<Type, EnumInfo>();
        private const string EnumSeparatorString = ", ";
        private const char EnumSeparatorChar = ',';

        static EnumInfo InitializeValuesAndNames(Type enumType)
        {
            string[] names = Enum.GetNames(enumType);
            string[] resolvedNames = new string[names.Length];
            ulong[] values = new ulong[names.Length];

            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i];
                FieldInfo f = enumType.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                values[i] = ToUInt64(f.GetValue(null));

                string resolvedName;
                resolvedName = f.GetCustomAttributes(typeof(EnumMemberAttribute), true)
                         .Cast<EnumMemberAttribute>()
                         .Select(a => a.Value)
                         .SingleOrDefault() ?? f.Name;

                if (Array.IndexOf(resolvedNames, resolvedName, 0, i) != -1)
                {
                    throw new InvalidOperationException("Enum name '{0}' already exists on enum '{1}'.".FormatWith(CultureInfo.InvariantCulture, resolvedName, enumType.Name));
                }

                resolvedNames[i] = resolvedName;
            }

            bool isFlags = enumType.IsDefined(typeof(FlagsAttribute), false);

            return new EnumInfo(isFlags, values, names, resolvedNames);
        }

        static ulong ToUInt64(object value)
        {
            PrimitiveTypeCode typeCode = ConvertUtils.GetTypeCode(value.GetType(), out bool _);

            switch (typeCode)
            {
                case PrimitiveTypeCode.SByte:
                    return (ulong)(sbyte)value;
                case PrimitiveTypeCode.Byte:
                    return (byte)value;
                case PrimitiveTypeCode.Boolean:
                    // direct cast from bool to byte is not allowed
                    return Convert.ToByte((bool)value);
                case PrimitiveTypeCode.Int16:
                    return (ulong)(short)value;
                case PrimitiveTypeCode.UInt16:
                    return (ushort)value;
                case PrimitiveTypeCode.Char:
                    return (char)value;
                case PrimitiveTypeCode.UInt32:
                    return (uint)value;
                case PrimitiveTypeCode.Int32:
                    return (ulong)(int)value;
                case PrimitiveTypeCode.UInt64:
                    return (ulong)value;
                case PrimitiveTypeCode.Int64:
                    return (ulong)(long)value;
                // All unsigned types will be directly cast
                default:
                    throw new InvalidOperationException("Unknown enum type.");
            }
        }

        public static bool TryToString(Type enumType, object value, out string name)
        {
            EnumInfo enumInfo = ValuesAndNamesPerEnum.GetOrAdd(enumType, InitializeValuesAndNames);
            ulong v = ToUInt64(value);

            if (!enumInfo.IsFlags)
            {
                int index = Array.BinarySearch(enumInfo.Values, v);
                if (index >= 0)
                {
                    name = enumInfo.ResolvedNames[index];
                    return true;
                }

                // is number value
                name = null;
                return false;
            }
            else // These are flags OR'ed together (We treat everything as unsigned types)
            {
                name = InternalFlagsFormat(enumInfo, v);
                return name != null;
            }
        }

        static string InternalFlagsFormat(EnumInfo entry, ulong result)
        {
            string[] resolvedNames = entry.ResolvedNames;
            ulong[] values = entry.Values;

            int index = values.Length - 1;
            StringBuilder sb = new StringBuilder();
            bool firstTime = true;
            ulong saveResult = result;

            // We will not optimize this code further to keep it maintainable. There are some boundary checks that can be applied
            // to minimize the comparsions required. This code works the same for the best/worst case. In general the number of
            // items in an enum are sufficiently small and not worth the optimization.
            while (index >= 0)
            {
                if (index == 0 && values[index] == 0)
                {
                    break;
                }

                if ((result & values[index]) == values[index])
                {
                    result -= values[index];
                    if (!firstTime)
                    {
                        sb.Insert(0, EnumSeparatorString);
                    }

                    string resolvedName = resolvedNames[index];
                    sb.Insert(0, resolvedName);
                    firstTime = false;
                }

                index--;
            }

            string returnString;
            if (result != 0)
            {
                // We were unable to represent this number as a bitwise or of valid flags
                returnString = null; // return null so the caller knows to .ToString() the input
            }
            else if (saveResult == 0)
            {
                // For the cases when we have zero
                if (values.Length > 0 && values[0] == 0)
                {
                    returnString = resolvedNames[0]; // Zero was one of the enum values.
                }
                else
                {
                    returnString = null;
                }
            }
            else
            {
                returnString = sb.ToString(); // Return the string representation
            }

            return returnString;
        }

        public static object ParseEnum(Type enumType, string value, bool disallowNumber)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (!enumType.IsEnum)
            {
                throw new ArgumentException("Type provided must be an Enum.", nameof(enumType));
            }

            EnumInfo entry = ValuesAndNamesPerEnum.GetOrAdd(enumType, InitializeValuesAndNames);
            string[] enumNames = entry.Names;
            string[] resolvedNames = entry.ResolvedNames;
            ulong[] enumValues = entry.Values;

            // first check if the entire text (including commas) matches a resolved name
            int? matchingIndex = FindIndexByName(resolvedNames, value, 0, value.Length, StringComparison.Ordinal);
            if (matchingIndex != null)
            {
                return Enum.ToObject(enumType, enumValues[matchingIndex.Value]);
            }

            int firstNonWhitespaceIndex = -1;
            for (int i = 0; i < value.Length; i++)
            {
                if (!char.IsWhiteSpace(value[i]))
                {
                    firstNonWhitespaceIndex = i;
                    break;
                }
            }
            if (firstNonWhitespaceIndex == -1)
            {
                throw new ArgumentException("Must specify valid information for parsing in the string.");
            }

            // check whether string is a number and parse as a number value
            char firstNonWhitespaceChar = value[firstNonWhitespaceIndex];
            if (char.IsDigit(firstNonWhitespaceChar) || firstNonWhitespaceChar == '-' || firstNonWhitespaceChar == '+')
            {
                Type underlyingType = Enum.GetUnderlyingType(enumType);

                value = value.Trim();
                object temp = null;

                try
                {
                    temp = Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
                }
                catch (FormatException)
                {
                    // We need to Parse this as a String instead. There are cases
                    // when you tlbimp enums that can have values of the form "3D".
                    // Don't fix this code.
                }

                if (temp != null)
                {
                    if (disallowNumber)
                    {
                        throw new FormatException("Integer string '{0}' is not allowed.".FormatWith(CultureInfo.InvariantCulture, value));
                    }

                    return Enum.ToObject(enumType, temp);
                }
            }

            ulong result = 0;

            int valueIndex = firstNonWhitespaceIndex;
            while (valueIndex <= value.Length) // '=' is to handle invalid case of an ending comma
            {
                // Find the next separator, if there is one, otherwise the end of the string.
                int endIndex = value.IndexOf(EnumSeparatorChar, valueIndex);
                if (endIndex == -1)
                {
                    endIndex = value.Length;
                }

                // Shift the starting and ending indices to eliminate whitespace
                int endIndexNoWhitespace = endIndex;
                while (valueIndex < endIndex && char.IsWhiteSpace(value[valueIndex]))
                {
                    valueIndex++;
                }

                while (endIndexNoWhitespace > valueIndex && char.IsWhiteSpace(value[endIndexNoWhitespace - 1]))
                {
                    endIndexNoWhitespace--;
                }
                int valueSubstringLength = endIndexNoWhitespace - valueIndex;

                // match with case sensitivity
                matchingIndex = MatchName(value, enumNames, resolvedNames, valueIndex, valueSubstringLength, StringComparison.Ordinal);

                // if no match found, attempt case insensitive search
                if (matchingIndex == null)
                {
                    matchingIndex = MatchName(value, enumNames, resolvedNames, valueIndex, valueSubstringLength, StringComparison.OrdinalIgnoreCase);
                }

                if (matchingIndex == null)
                {
                    // still can't find a match
                    // before we throw an error, check whether the entire string has a case insensitive match against resolve names
                    matchingIndex = FindIndexByName(resolvedNames, value, 0, value.Length, StringComparison.OrdinalIgnoreCase);
                    if (matchingIndex != null)
                    {
                        return Enum.ToObject(enumType, enumValues[matchingIndex.Value]);
                    }

                    // no match so error
                    throw new ArgumentException("Requested value '{0}' was not found.".FormatWith(CultureInfo.InvariantCulture, value));
                }

                result |= enumValues[matchingIndex.Value];

                // Move our pointer to the ending index to go again.
                valueIndex = endIndex + 1;
            }

            return Enum.ToObject(enumType, result);
        }

        static int? MatchName(string value, string[] enumNames, string[] resolvedNames, int valueIndex, int valueSubstringLength, StringComparison comparison)
        {
            int? matchingIndex = FindIndexByName(resolvedNames, value, valueIndex, valueSubstringLength, comparison);
            if (matchingIndex == null)
            {
                matchingIndex = FindIndexByName(enumNames, value, valueIndex, valueSubstringLength, comparison);
            }

            return matchingIndex;
        }

        static int? FindIndexByName(string[] enumNames, string value, int valueIndex, int valueSubstringLength, StringComparison comparison)
        {
            for (int i = 0; i < enumNames.Length; i++)
            {
                if (enumNames[i].Length == valueSubstringLength &&
                    string.Compare(enumNames[i], 0, value, valueIndex, valueSubstringLength, comparison) == 0)
                {
                    return i;
                }
            }

            return null;
        }
    }
}
