using Uavcan.NET.Dsdl.DataTypes;
using System;
using System.Collections;
using System.Linq;
using System.Text;

namespace Uavcan.NET.Studio.Tools.BusMonitor
{
    static class ObjectPrinter
    {
        public static string PrintToString(object obj, DsdlType scheme)
        {
            var sb = new StringBuilder();
            Print(obj, sb, 0, scheme);
            return sb.ToString();
        }

        static void Print(object obj, StringBuilder sb, int tabs, DsdlType scheme)
        {
            if (obj is null)
                return;

            switch (scheme)
            {
                case CompositeDsdlTypeBase t:
                    PrintObject(obj, sb, tabs, t);
                    break;
                case ArrayDsdlType t when t.IsStringLike:
                    PrintString(obj, sb);
                    break;
                case ArrayDsdlType t:
                    PrintArray(obj, sb, tabs, t);
                    break;
                default:
                    PrintPrimitive(obj, sb);
                    break;
            }
        }

        static void PrintPrimitive(object obj, StringBuilder sb)
        {
            if (obj == null)
                return;
            sb.Append(obj.ToString());
        }

        static void PrintString(object obj, StringBuilder sb)
        {
            var enumerable = obj as IEnumerable;
            if (enumerable == null)
                throw new ArgumentException("Cannot cast object to enumerable.", nameof(obj));
            var bytes = enumerable.Cast<byte>().ToArray();
            var str = Encoding.ASCII.GetString(bytes);
            sb.Append('\"');
            sb.Append(str);
            sb.Append('\"');
            sb.Append(" (");
            PrintHex(bytes, sb);
            sb.Append(")");
        }

        static void PrintHex(byte[] bytes, StringBuilder sb)
        {
            foreach (byte b in bytes)
                sb.AppendFormat("{0:X2}", b);
        }

        static void PrintArray(object obj, StringBuilder sb, int tabs, ArrayDsdlType t)
        {
            var enumerable = obj as IEnumerable;
            if (enumerable == null)
                throw new ArgumentException("Cannot cast object to enumerable.", nameof(obj));

            var arrayType = t.ElementType;
            var isSLType = IsSingleLineType(arrayType);

            sb.Append("[");
            if (!isSLType)
                sb.AppendLine();

            bool isFirst = true;
            foreach (var i in enumerable)
            {
                if (!isFirst)
                {
                    sb.Append(", ");

                    if (!isSLType)
                    {
                        sb.AppendLine();
                        Tab(sb, tabs);
                    }
                }
                isFirst = false;

                Print(i, sb, tabs + 1, arrayType);
            }

            if (!isSLType)
                Tab(sb, tabs - 1);
            sb.Append("]");
        }

        static bool IsSingleLineType(DsdlType type)
        {
            switch (type)
            {
                case ArrayDsdlType t when t.IsStringLike:
                    return true;
                case PrimitiveDsdlType _:
                    return true;
                default:
                    return false;
            }
        }

        static void PrintObject(object obj, StringBuilder sb, int tabs, CompositeDsdlTypeBase t)
        {
            var dict = obj as IDictionary;
            if (dict == null)
                throw new ArgumentException("Cannot cast object to dictionary.", nameof(obj));

            if (tabs != 0)
            {
                sb.AppendLine("{");
            }

            bool isFirst = true;
            foreach (var field in t.Fields)
            {
                if (!isFirst)
                {
                    sb.Append(",");
                    sb.AppendLine();
                }
                isFirst = false;

                Tab(sb, tabs);
                sb.Append(field.Name);
                sb.Append(": ");

                if (dict.Contains(field.Name))
                {
                    var value = dict[field.Name];
                    Print(value, sb, tabs + 1, field.Type);
                }
            }

            if (tabs != 0)
            {
                sb.AppendLine();
                Tab(sb, tabs - 1);
                sb.Append("}");
            }
        }

        static void Tab(StringBuilder sb, int tabs)
        {
            sb.Append(' ', tabs * 2);
        }
    }
}
