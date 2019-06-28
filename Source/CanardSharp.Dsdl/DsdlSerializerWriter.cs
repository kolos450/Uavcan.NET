using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using CanardSharp.Dsdl.DataTypes;
using CanardSharp.Dsdl.TypesInterop;
using CanardSharp.Dsdl.TypesInterop.Utilities;
using CanardSharp.IO;

namespace CanardSharp.Dsdl
{
    class DsdlSerializerWriter
    {
        readonly Stack<object> _serializeStack = new Stack<object>();
        readonly DsdlSerializer _serializer;

        public DsdlSerializerWriter(DsdlSerializer dsdlSerializer)
        {
            _serializer = dsdlSerializer;
        }

        public void Serialize(BitStreamWriter stream, object value, DsdlType dsdlScheme)
        {
            var contract = _serializer.ContractResolver.ResolveContract(value.GetType());
            SerializeValue(stream, value, contract, null, null, null, dsdlScheme, true);
        }

        private void SerializeValue(
            BitStreamWriter writer,
            object value,
            IContract valueContract,
            DsdlProperty member,
            ContainerContract containerContract,
            DsdlProperty containerProperty,
            DsdlType derivedDsdlType,
            bool tailArrayOptimization = false)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value), "Cannot serialize null value");

            switch (valueContract)
            {
                case ObjectContract contract:
                    SerializeObject(writer, value, contract, member, containerContract, containerProperty, derivedDsdlType, tailArrayOptimization);
                    break;
                case ArrayContract contract:
                    if (contract.IsMultidimensionalArray)
                        throw new NotSupportedException("Multidimensional arrays are not supported.");
                    SerializeList(writer, (IEnumerable)value, contract, member, containerContract, containerProperty, derivedDsdlType, tailArrayOptimization);
                    break;
                case PrimitiveContract contract:
                    SerializePrimitive(writer, value, contract, member, containerContract, containerProperty, derivedDsdlType);
                    break;
                case DictionaryContract contract:
                    SerializeDictionary(writer, (value is IDictionary dictionary) ? dictionary : contract.CreateWrapper(value), contract, member, containerContract, containerProperty, derivedDsdlType, tailArrayOptimization);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(valueContract));
            }
        }

        void SerializePrimitive(BitStreamWriter writer, object value, PrimitiveContract contract, DsdlProperty member, ContainerContract containerContract, DsdlProperty containerProperty, DsdlType derivedDsdlType)
        {
            if (!(derivedDsdlType is PrimitiveDsdlType t))
                throw new InvalidOperationException($"Primitive DSDL type expected for type '{contract.UnderlyingType.FullName}'.");

            switch (t)
            {
                case BooleanDsdlType _:
                    var boolValue = (bool)ConvertUtils.ConvertOrCast(value, CultureInfo.CurrentCulture, typeof(bool));
                    BitSerializer.Write(writer, boolValue, t.MaxBitlen);
                    break;
                case IntDsdlType idt:
                    var longValue = (long)ConvertUtils.ConvertOrCast(value, CultureInfo.CurrentCulture, typeof(long));
                    longValue = ApplyIntegerCastMode(longValue, idt);
                    BitSerializer.Write(writer, longValue, t.MaxBitlen);
                    break;
                case UIntDsdlType uidt:
                    var ulongValue = (ulong)ConvertUtils.ConvertOrCast(value, CultureInfo.CurrentCulture, typeof(ulong));
                    ulongValue = ApplyIntegerCastMode(ulongValue, uidt);
                    BitSerializer.Write(writer, ulongValue, t.MaxBitlen);
                    break;
                case FloatDsdlType fdt:
                    var doubleValue = (double)ConvertUtils.ConvertOrCast(value, CultureInfo.CurrentCulture, typeof(double));
                    WriteFloatPrimitive(writer, doubleValue, fdt);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(t));
            }
        }

        static void WriteFloatPrimitive(BitStreamWriter writer, double value, FloatDsdlType type)
        {
            var range = TypeLimits.GetFloatRange(type.MaxBitlen);

            switch (type.CastMode)
            {
                case CastMode.Saturated:
                    if (value > range.Maximum)
                        value = range.Maximum;
                    if (value < range.Minimum)
                        value = range.Minimum;
                    break;

                case CastMode.Truncated:
                    if (!double.IsNaN(value) && value > range.Maximum)
                        value = double.PositiveInfinity;
                    if (!double.IsNaN(value) && value < range.Maximum)
                        value = double.NegativeInfinity;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            switch (type.MaxBitlen)
            {
                case 16:
                    var val = BitSerializer.Float32ToUInt16((float)value);
                    BitSerializer.Write(writer, val, 16);
                    break;

                case 32:
                    BitSerializer.Write(writer, (float)value, 32);
                    break;

                case 64:
                    BitSerializer.Write(writer, value, 64);
                    break;

                default:
                    throw new InvalidOperationException($"Unexpected float bit lenght: {type.MaxBitlen}.");
            }
        }

        static long ApplyIntegerCastMode(long value, IntDsdlType type)
        {
            var maxValue = type.MaxBitlen == 64 ? unchecked((long)ulong.MaxValue) : (long)((1UL << type.MaxBitlen) - 1);

            switch (type.CastMode)
            {
                case CastMode.Truncated:
                    return value & maxValue;
                case CastMode.Saturated:
                    var range = TypeLimits.GetIntRange(type.MaxBitlen);
                    if (value < range.Minimum)
                        return range.Minimum;
                    else if (value > range.Maximum)
                        return range.Maximum;
                    return value;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type.CastMode));
            }
        }

        static ulong ApplyIntegerCastMode(ulong value, UIntDsdlType type)
        {
            var maxValue = (type.MaxBitlen == 64) ? ulong.MaxValue : (1UL << type.MaxBitlen) - 1;

            switch (type.CastMode)
            {
                case CastMode.Truncated:
                    return value & maxValue;
                case CastMode.Saturated:
                    if ((value & ~maxValue) == 0)
                        return value;
                    return maxValue;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type.CastMode));
            }
        }

        bool CheckForCircularReference(object value, DsdlProperty property, IContract contract)
        {
            if (value == null || contract is PrimitiveContract)
                return true;

            if (_serializeStack.Contains(value))
            {
                string message = "Self referencing loop detected";
                if (property != null)
                    message += " for property '{0}'".FormatWith(CultureInfo.InvariantCulture, property.PropertyName);
                message += " with type '{0}'.".FormatWith(CultureInfo.InvariantCulture, value.GetType());

                throw new SerializationException(message);
            }

            return true;
        }

        struct ResolvedProperty
        {
            public DsdlProperty Member;
            public IContract MemberContact;
            public object MemberValue;
        }

        ResolvedProperty? ResolveObjectProperty(object value, ObjectContract contract, string name)
        {
            var property = contract.Properties.GetProperty(name, StringComparison.Ordinal);
            if (property == null)
                return null;

            if (!CalculatePropertyValues(value, contract, property, out var memberContract, out var memberValue))
                return null;

            return new ResolvedProperty
            {
                Member = property,
                MemberContact = memberContract,
                MemberValue = memberValue
            };
        }

        void SerializeObject(BitStreamWriter writer, object value, ObjectContract contract, DsdlProperty member, ContainerContract collectionContract, DsdlProperty containerProperty, DsdlType derivedDsdlType, bool tailArrayOptimization)
        {
            var dsdlScheme = GetScheme<CompositeDsdlTypeBase>(contract, derivedDsdlType);

            _serializeStack.Push(value);

            SerializeObjectCore(
                writer,
                name => ResolveObjectProperty(value, contract, name),
                contract,
                member,
                derivedDsdlType,
                tailArrayOptimization);

            _serializeStack.Pop();
        }

        void WriteAlignment(BitStreamWriter writer, VoidDsdlType t)
        {
            var amount = t.MaxBitlen;

            while (amount > 8)
            {
                writer.Write(0, 8);
                amount -= 8;
            }

            if (amount > 0)
                writer.Write(0, amount);
        }

        static T GetScheme<T>(IContract contract, DsdlType derivedScheme) where T : DsdlType
        {
            T result = null;
            var schemeFromContract = contract.DsdlType;
            if (schemeFromContract != null)
            {
                result = schemeFromContract as T;
                if (result == null)
                    throw new InvalidOperationException($"Unexpected DSDL type for type '{contract.UnderlyingType.FullName}'.");
            }

            if (derivedScheme != null)
            {
                if (result != null && result != derivedScheme)
                    throw new InvalidOperationException($"DSDL scheme mismatch for type '{contract.UnderlyingType.FullName}'.");

                result = derivedScheme as T;
                if (result == null)
                    throw new InvalidOperationException($"Unexpected DSDL type for type '{contract.UnderlyingType.FullName}'.");
            }

            if (result == null)
                throw new InvalidOperationException($"Unexpected DSDL type for type '{contract.UnderlyingType.FullName}'.");

            return result;
        }

        bool CalculatePropertyValues(object value, ContainerContract contract, DsdlProperty property, out IContract memberContract, out object memberValue)
        {
            memberContract = null;
            memberValue = null;

            if (property.Ignored || !property.Readable)
                return false;

            memberValue = property.ValueProvider.GetValue(value);
            if (memberValue == null)
            {
                throw new SerializationException(
                    $"Cannot write a null value for property '{contract.UnderlyingType.FullName}.{property.PropertyName}'.");
            }

            if (property.PropertyContract == null)
                property.PropertyContract = _serializer.ContractResolver.ResolveContract(property.PropertyType);

            memberContract = (property.PropertyContract.IsSealed) ?
                property.PropertyContract :
                _serializer.ContractResolver.ResolveContract(memberValue.GetType());

            if (!CheckForCircularReference(memberValue, property, memberContract))
                return false;

            if (!CheckDsdlTypeCompatibility(property.DsdlType, memberContract))
                throw new InvalidOperationException(
                    $"DSDL type mismatch for property '{contract.UnderlyingType.FullName}.{property.UnderlyingName}'.");

            return true;
        }

        void SerializeList(
            BitStreamWriter writer,
            IEnumerable values,
            ArrayContract contract,
            DsdlProperty member,
            ContainerContract collectionContract,
            DsdlProperty containerProperty,
            DsdlType derivedDsdlType,
            bool tailArrayOptimization)
        {
            if (!(derivedDsdlType is ArrayDsdlType arrayDsdlType))
                throw new InvalidOperationException($"Array DSDL type expected for type '{contract.UnderlyingType.FullName}'.");

            object underlyingList = values is IWrappedCollection wrappedCollection ? wrappedCollection.UnderlyingCollection : values;

            _serializeStack.Push(underlyingList);

            var arrayCount = values.Count();
            switch (arrayDsdlType.Mode)
            {
                case ArrayDsdlTypeMode.Dynamic:
                    {
                        if (arrayCount > arrayDsdlType.MaxSize)
                            throw new SerializationException($"'{contract.UnderlyingType.FullName}' is too big. MaxSize is {arrayDsdlType.MaxSize}.");
                        if (!tailArrayOptimization || arrayDsdlType.ElementType.MinBitlen < 8)
                            WriteDynamicArraySize(writer, arrayCount, arrayDsdlType);
                        else
                            tailArrayOptimization = false;
                        break;
                    }
                case ArrayDsdlTypeMode.Static:
                    {
                        if (arrayCount != arrayDsdlType.MaxSize)
                            throw new SerializationException($"'{contract.UnderlyingType.FullName}' expected size is {arrayDsdlType.MaxSize}.");
                        break;
                    }
            }

            // Note: an exception from the IEnumerable won't be caught.
            int i = 0;
            foreach (object value in values)
            {
                i++;

                var valueContract = contract.FinalItemContract ??
                    (value == null ? null : _serializer.ContractResolver.ResolveContract(value.GetType()));

                if (!CheckForCircularReference(value, null, valueContract))
                    continue;

                if (!CheckDsdlTypeCompatibility(arrayDsdlType.ElementType, valueContract))
                    throw new InvalidOperationException(
                        $"DSDL type mismatch for enumerated item '{contract.UnderlyingType.FullName}.{valueContract.UnderlyingType}'.");

                var tao = i == arrayCount ? tailArrayOptimization : false;
                SerializeValue(writer, value, valueContract, null, contract, member, arrayDsdlType.ElementType, tao);
            }

            //writer.WriteEndArray();

            _serializeStack.Pop();
        }

        void WriteDynamicArraySize(BitStreamWriter writer, int count, ArrayDsdlType arrayDsdlType)
        {
            var bitLen = BitSerializer.IntBitLength(arrayDsdlType.MaxSize + 1);
            BitSerializer.Write(writer, count, bitLen);
        }

        static bool CheckDsdlTypeCompatibility(DsdlType schemeType, IContract actualContract)
        {
            switch (schemeType)
            {
                case PrimitiveDsdlType _:
                    if (!(actualContract is PrimitiveContract))
                        return false;
                    break;
                case ArrayDsdlType _:
                    if (!(actualContract is ArrayContract))
                        return false;
                    break;
                case CompositeDsdlTypeBase dt:
                    if (dt != actualContract.DsdlType)
                        return false;
                    break;
            }

            return true;
        }

        ResolvedProperty? ResolveDictionaryProperty(IDictionary<string, object> dictionary, DictionaryContract contract, string name)
        {
            if (!dictionary.TryGetValue(name, out var value))
                return null;

            var valueContract = contract.FinalItemContract ??
                (value == null ? null : _serializer.ContractResolver.ResolveContract(value.GetType()));

            if (!CheckForCircularReference(value, null, valueContract))
                return null;

            return new ResolvedProperty
            {
                Member = null,
                MemberContact = valueContract,
                MemberValue = value
            };
        }

        void SerializeDictionary(BitStreamWriter writer, IDictionary values, DictionaryContract contract, DsdlProperty member, ContainerContract collectionContract, DsdlProperty containerProperty, DsdlType derivedDsdlType, bool tailArrayOptimization)
        {
            object underlyingDictionary = values is IWrappedDictionary wrappedDictionary ? wrappedDictionary.UnderlyingDictionary : values;

            _serializeStack.Push(underlyingDictionary);

            if (contract.ItemContract == null)
                contract.ItemContract = _serializer.ContractResolver.ResolveContract(contract.DictionaryValueType ?? typeof(object));

            if (contract.KeyContract == null)
                contract.KeyContract = _serializer.ContractResolver.ResolveContract(contract.DictionaryKeyType ?? typeof(object));

            var dictionaryNormalized = PreprocessDictionary(values, contract);

            SerializeObjectCore(
                writer,
                name => ResolveDictionaryProperty(dictionaryNormalized, contract, name),
                contract,
                member,
                derivedDsdlType,
                tailArrayOptimization);

            _serializeStack.Pop();
        }

        void SerializeObjectCore(
            BitStreamWriter writer,
            Func<string, ResolvedProperty?> propertyResolver,
            ContainerContract containerContract,
            DsdlProperty containerProperty,
            DsdlType derivedDsdlType,
            bool tailArrayOptimization)
        {
            var dsdlScheme = GetScheme<CompositeDsdlTypeBase>(containerContract, derivedDsdlType);

            //WriteObjectStart(writer, value, contract, member, collectionContract, containerProperty);

            VoidDsdlType voidDsdlType = null;
            int voidDsdlTypeIndex = -1;
            var isUnion = dsdlScheme.IsUnion;
            var unionMemberFound = false;

            for (int i = 0; i < dsdlScheme.Fields.Count; i++)
            {
                var dsdlMember = dsdlScheme.Fields[i];
                var isLastMember = i == dsdlScheme.Fields.Count - 1;

                if ((voidDsdlType = (dsdlMember.Type as VoidDsdlType)) != null)
                {
                    voidDsdlTypeIndex = i;
                    if (!isUnion)
                        WriteAlignment(writer, voidDsdlType);
                    continue;
                }

                var resolvedProp = propertyResolver(dsdlMember.Name);

                if (isUnion)
                {
                    if (resolvedProp == null)
                        continue;
                    if (unionMemberFound)
                        throw new InvalidOperationException($"Cannot find single union value for type '{containerContract.UnderlyingType.FullName}'.");
                    unionMemberFound = true;
                    WriteUnionFieldIndex(writer, i, dsdlScheme);
                    var rp = resolvedProp.Value;
                    SerializeValue(writer, rp.MemberValue, rp.MemberContact, rp.Member, containerContract, containerProperty, dsdlMember.Type);
                }
                else
                {
                    if (resolvedProp == null)
                        throw new InvalidOperationException($"Cannot resove member '{containerContract.UnderlyingType.FullName}.{dsdlMember.Name}'.");
                    var rp = resolvedProp.Value;
                    var tao = tailArrayOptimization &&
                        isLastMember &&
                        dsdlMember.Type is ArrayDsdlType adt;
                    SerializeValue(writer, rp.MemberValue, rp.MemberContact, rp.Member, containerContract, containerProperty, dsdlMember.Type, tao);
                }
            }

            if (isUnion && !unionMemberFound)
            {
                if (voidDsdlType != null)
                {
                    WriteUnionFieldIndex(writer, voidDsdlTypeIndex, dsdlScheme);
                    WriteAlignment(writer, voidDsdlType);
                }
                else
                {
                    throw new InvalidOperationException($"Cannot find union value for '{containerContract.UnderlyingType.FullName}' type.");
                }
            }
        }

        void WriteUnionFieldIndex(BitStreamWriter writer, int index, CompositeDsdlTypeBase t)
        {
            var bitLen = BitSerializer.IntBitLength(t.Fields.Count);
            BitSerializer.Write(writer, index, bitLen);
        }

        IDictionary<string, object> PreprocessDictionary(IDictionary values, DictionaryContract contract)
        {
            var result = new Dictionary<string, object>(StringComparer.Ordinal);

            // Manual use of IDictionaryEnumerator instead of foreach to avoid DictionaryEntry box allocations.
            IDictionaryEnumerator e = values.GetEnumerator();
            try
            {
                while (e.MoveNext())
                {
                    DictionaryEntry entry = e.Entry;

                    string propertyName = GetPropertyName(entry.Key, contract.KeyContract, out bool escape);

                    propertyName = (contract.DictionaryKeyResolver != null)
                        ? contract.DictionaryKeyResolver(propertyName)
                        : propertyName;

                    result[propertyName] = entry.Value;
                }
            }
            finally
            {
                (e as IDisposable)?.Dispose();
            }

            return result;
        }

        string GetPropertyName(object name, IContract contract, out bool escape)
        {
            if (contract is PrimitiveContract primitiveContract)
            {
                switch (primitiveContract.TypeCode)
                {
                    default:
                        {
                            escape = true;

                            if (primitiveContract.IsEnum && EnumUtils.TryToString(primitiveContract.NonNullableUnderlyingType, name, out string enumName))
                            {
                                return enumName;
                            }

                            return Convert.ToString(name, CultureInfo.InvariantCulture);
                        }
                }
            }
            else
            {
                escape = true;
                return name.ToString();
            }
        }
    }
}
