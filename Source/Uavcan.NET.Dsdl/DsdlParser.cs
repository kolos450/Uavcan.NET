﻿using Uavcan.NET.Dsdl.DataTypes;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Uavcan.NET.Dsdl
{
    public static class DsdlParser
    {
        const int MAX_FULL_TYPE_NAME_LEN = 80;
        const int SERVICE_DATA_TYPE_ID_MAX = 255;
        const int MESSAGE_DATA_TYPE_ID_MAX = 65535;

        static Regex _fullNameRegex = new Regex(
                @"^([a-z][a-z0-9_]*\.)+[A-Z][A-Za-z0-9_]*$",
                RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        static Regex _attributeRegex = new Regex(
                @"^[A-Z][A-Za-z0-9_]*$",
                RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        static void ValidateAttributeName(string value)
        {
            if (!_attributeRegex.IsMatch(value))
                throw new Exception($"Invalid attribute: '{value}'.");
        }

        static void ValidateTypeFullName(string value)
        {
            if (value.Length > MAX_FULL_TYPE_NAME_LEN)
                throw new Exception($"Type name is too long: '{value}'");
            if (!_fullNameRegex.IsMatch(value))
                throw new Exception($"Invalid type full name: '{value}'.");
        }

        static void ValidateDTID(IUavcanType type)
        {
            if (type.Meta.DefaultDTID == null)
                return;
            if (type.Meta.DefaultDTID < 0)
                throw new Exception("Invalid data type ID.");
            switch (type)
            {
                case MessageType _ when (type.Meta.DefaultDTID > MESSAGE_DATA_TYPE_ID_MAX):
                case ServiceType _ when (type.Meta.DefaultDTID > SERVICE_DATA_TYPE_ID_MAX):
                    throw new Exception("Invalid data type ID.");
            }
        }

        static void ValidateUnion(IUavcanType type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            void ValidateUnionCore(CompositeDsdlTypeBase ct)
            {
                if (ct.IsUnion)
                {
                    if (ct.Fields == null || ct.Fields.Count < 2)
                        throw new Exception("Union contains less than 2 fields.");
                    if (ct.Fields.OfType<VoidDsdlType>().Any())
                        throw new Exception("Union must not contain void fields.");
                }
            }

            switch (type)
            {
                case MessageType mt:
                    ValidateUnionCore(mt);
                    break;
                case ServiceType st:
                    ValidateUnionCore(st.Request);
                    ValidateUnionCore(st.Response);
                    break;
                default:
                    throw new ArgumentException();
            }
        }

        sealed class DsdlTypeReference : DsdlType
        {
            public string Namespace { get; }
            public string Name { get; }

            public DsdlTypeReference(string ns, string attrTypeName)
            {
                Namespace = ns;
                Name = attrTypeName;
            }

            public override int MaxBitlen => throw new InvalidOperationException();
            public override int MinBitlen => throw new InvalidOperationException();
            public override ulong? GetDataTypeSignature() => throw new InvalidOperationException();
            public override string GetNormalizedMemberDefinition() => throw new InvalidOperationException();
        }

        public static void ResolveNestedTypes(IUavcanType type, IUavcanTypeResolver typeResolver)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (typeResolver == null)
                throw new ArgumentNullException(nameof(typeResolver));

            switch (type)
            {
                case MessageType t:
                    ResolveNestedTypesCore(t.UnderlyingCompositeDsdlType, typeResolver);
                    break;
                case ServiceType t:
                    ResolveNestedTypesCore(t.Request, typeResolver);
                    ResolveNestedTypesCore(t.Response, typeResolver);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        static void ResolveNestedTypesCore(CompositeDsdlTypeBase t, IUavcanTypeResolver typeResolver)
        {
            foreach (var f in t.Fields)
            {
                if (f.Type is DsdlTypeReference reference)
                {
                    var nestedType = typeResolver.ResolveType(reference.Namespace, reference.Name);
                    f.Type = CastNestedType(nestedType);
                }
                else if (f.Type is ArrayDsdlType adt && adt.ElementType is DsdlTypeReference elementTypeReference)
                {
                    var nestedType = typeResolver.ResolveType(elementTypeReference.Namespace, elementTypeReference.Name);
                    adt.SetElementType(CastNestedType(nestedType));
                }
            }
        }

        static DsdlType CastNestedType(IUavcanType nestedType)
        {
            switch (nestedType)
            {
                case ServiceType _:
                    throw new Exception("A service type can not be nested into another compound type.");
                case MessageType messageType:
                    return messageType;
                default:
                    throw new InvalidOperationException($"Unknown DSDL type: '{nestedType}'.");
            }
        }

        public static IUavcanType Parse(TextReader reader, UavcanTypeMeta meta)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));
            if (meta == null)
                throw new ArgumentNullException(nameof(meta));

            ValidateTypeFullName(meta.FullName);

            IUavcanType result = null;
            var compoundType = new CompositeDsdlType();

            int lineCounter = 0;
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                lineCounter++;

                line = SkipComment(line).Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                try
                {
                    if (line == "---")
                    {
                        if (result != null)
                            throw new Exception("Duplicate response mark.");

                        result = new ServiceType
                        {
                            Meta = meta,
                            Request = compoundType,
                        };

                        compoundType = new CompositeDsdlType();
                    }
                    else if (line == "@union")
                    {
                        if (compoundType.IsUnion)
                            throw new Exception("Data structure has already been declared as union.");
                        compoundType.SetIsUnion(true);
                    }
                    else
                    {
                        var tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        ProcessLineTokens(meta, compoundType, tokens);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Cannot parse line #{lineCounter}: '{line}'.", ex);
                }
            }

            if (result == null)
            {
                result = new MessageType(compoundType)
                {
                    Meta = meta,
                };
            }
            else if (result is ServiceType st)
            {
                st.Response = compoundType;
            }
            else
            {
                throw new InvalidOperationException();
            }

            ValidateDTID(result);
            ValidateUnion(result);

            return result;
        }

        static void ProcessLineTokens(UavcanTypeMeta meta, CompositeDsdlType type, string[] tokens)
        {
            if (tokens.Length < 1)
                throw new Exception("Invalid attribute definition.");

            int offset = 0;
            var castMode = CastMode.Saturated;
            switch (tokens[0])
            {
                case "saturated":
                    offset++;
                    break;
                case "truncated":
                    offset++;
                    castMode = CastMode.Truncated;
                    break;
            }

            string attrTypeName, attrName;

            switch (tokens.Length - offset)
            {
                case 0:
                case 1 when (!tokens[0].StartsWith("void")):
                    throw new Exception("Invalid attribute definition.");
                case 1:
                    attrTypeName = tokens[offset];
                    attrName = null;
                    offset += 1;
                    break;
                default:
                    attrTypeName = tokens[offset];
                    attrName = tokens[offset + 1];
                    offset += 2;
                    break;
            }

            var attrType = ParseType(meta.Namespace, attrTypeName, castMode);

            switch (attrType)
            {
                case VoidDsdlType _:
                    break;
                default:
                    ValidateAttributeName(attrName);
                    break;
            }

            switch (tokens.Length - offset)
            {
                case 0:
                    var field = new DsdlField
                    {
                        Name = attrName,
                        Type = attrType,
                    };
                    type.AddMember(field);
                    if (attrName != null)
                        type.Members.Add(attrName, field);
                    break;
                case 1:
                    throw new Exception("Constant assignment expected.");
                default:
                    if (tokens[offset] != "=")
                        throw new Exception("Constant assignment expected.");
                    var expression = string.Join(" ", tokens, offset + 1, tokens.Length - offset - 1);
                    var constant = CreateConstant(attrType, attrName, expression);
                    type.AddMember(constant);
                    if (attrName != null)
                        type.Members.Add(attrName, constant);
                    break;
            }
        }

        static DsdlConstant CreateConstant(DsdlType attrType, string attrName, string expression)
        {
            if (!(attrType is PrimitiveDsdlType primitive))
                throw new Exception($"Invalid type for constant {attrName}.");

            var value = Evaluate(expression, primitive);

            if (!primitive.IsInRange(value))
                throw new Exception($"Value {value} is out of range.");

            return new DsdlConstant
            {
                Name = attrName,
                Type = attrType,
                Value = value,
            };
        }

        static object Evaluate(string expression, PrimitiveDsdlType expectedReturnType)
        {
            var evaluator = new DsdlConstExprEvaluator(expectedReturnType);
            var result = evaluator.Evaluate(expression);
            if (result is string stringResult && bool.TryParse(stringResult, out var boolResult))
                return boolResult;
            return result;
        }

        static Regex _typeVoidRegex = new Regex(
                @"^void(?<SIZE>\d{1,2})$",
                RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        static Regex _typeArrayRegex = new Regex(
                @"^(?<NAME>.+?)\[(?<SIZE>[^\]]*)\]$",
                RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        static Regex _typePrimitiveRegex = new Regex(
                @"^(?<NAME>[a-z]+)(?<SIZE>\d{1,2})$",
                RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        static DsdlType ParseType(string ns, string attrTypeName, CastMode castMode)
        {
            Match match;
            if ((match = _typeVoidRegex.Match(attrTypeName)).Success)
            {
                var size = int.Parse(match.Groups["SIZE"].Value);
                if (size < 1 || size > 64)
                    throw new Exception($"Invalid void bit length: {size}.");
                return new VoidDsdlType(size);
            }
            else if ((match = _typeArrayRegex.Match(attrTypeName)).Success)
            {
                var size = match.Groups["SIZE"].Value;
                var name = match.Groups["NAME"].Value;

                var valueType = ParseType(ns, name, castMode);
                if (valueType is ArrayDsdlType)
                    throw new Exception("Multidimensional arrays are not allowed (protip: use nested types).");

                try
                {
                    if (size.StartsWith("<="))
                        return new ArrayDsdlType(valueType, ArrayDsdlTypeMode.Dynamic, int.Parse(size.Substring(2)));
                    else if (size.StartsWith("<"))
                        return new ArrayDsdlType(valueType, ArrayDsdlTypeMode.Dynamic, int.Parse(size.Substring(1)) - 1);
                    else
                        return new ArrayDsdlType(valueType, ArrayDsdlTypeMode.Static, int.Parse(size));
                }
                catch (Exception ex)
                {
                    throw new Exception($"Invalid array size specifier [{size}] (valid patterns: [<=X], [<X], [X]).", ex);
                }
            }
            else if (attrTypeName == "bool")
            {
                return new BooleanDsdlType(castMode);
            }
            else if ((match = _typePrimitiveRegex.Match(attrTypeName)).Success)
            {
                var size = int.Parse(match.Groups["SIZE"].Value);
                var name = match.Groups["NAME"].Value;

                switch (name)
                {
                    case "uint":
                        return new UIntDsdlType(size, castMode);
                    case "int":
                        return new IntDsdlType(size, castMode);
                    case "float":
                        return new FloatDsdlType(size, castMode);
                }
            }

            if (castMode != CastMode.Saturated)
                throw new Exception("Cast mode specifier is not applicable for compound types.");

            return new DsdlTypeReference(ns, attrTypeName);
        }

        static string SkipComment(string line)
        {
            var commentIndex = line.IndexOf("#");
            if (commentIndex >= 0)
            {
                line = line.Substring(0, commentIndex);
            }

            return line;
        }
    }
}
