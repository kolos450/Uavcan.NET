using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp.Dsdl.TypesInterop.Utilities
{
    static class EnumUtils
    {
        static readonly ConcurrentDictionary<Type, EnumInfo> ValuesAndNamesPerEnum = new ConcurrentDictionary<Type, EnumInfo>();
        private const string EnumSeparatorString = ", ";

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
    }
}
