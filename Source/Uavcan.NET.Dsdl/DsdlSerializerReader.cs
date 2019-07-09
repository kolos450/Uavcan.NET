using Uavcan.NET.Dsdl.DataTypes;
using Uavcan.NET.Dsdl.TypesInterop;
using Uavcan.NET.Dsdl.TypesInterop.Utilities;
using Uavcan.NET.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET.Dsdl
{
    class DsdlSerializerReader
    {
        readonly DsdlSerializer _dsdlSerializer;
        readonly Encoding _encoding = Encoding.ASCII;

        public DsdlSerializerReader(DsdlSerializer dsdlSerializer)
        {
            _dsdlSerializer = dsdlSerializer;
        }

        private IContract GetContractSafe(Type type)
        {
            if (type == null)
                return null;

            return _dsdlSerializer.ContractResolver.ResolveContract(type);
        }

        public object Deserialize(BitStreamReader reader, DsdlType scheme, IContract contract)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));
            if (scheme == null)
                throw new ArgumentNullException(nameof(scheme));

            if (HasNoDefinedType(contract))
            {
                return CreateUnknownObject(reader, scheme, true);
            }

            return CreateValueInternal(reader, contract, null, null, null, null, scheme, null, true);
        }

        IContract _stringContract = null;
        IContract StringContract
        {
            get
            {
                if (_stringContract == null)
                    _stringContract = _dsdlSerializer.ContractResolver.ResolveContract(typeof(byte[]));
                return _stringContract;
            }
        }

        object CreateValueInternal(
            BitStreamReader reader,
            IContract contract,
            DsdlProperty member,
            ContainerContract containerContract,
            DsdlProperty containerMember,
            object existingValue,
            DsdlType scheme,
            Type objectType,
            bool tailArrayOptimization = false)
        {
            switch (scheme)
            {
                case VoidDsdlType t:
                    ReadAlignment(reader, t);
                    return null;

                case PrimitiveDsdlType t:
                    var primitive = ReadPrimitiveType(reader, t);
                    return EnsureType(reader, primitive, CultureInfo.InvariantCulture, contract, objectType);

                case ArrayDsdlType t when t.IsStringLike && contract.UnderlyingType == typeof(string):
                    var list = CreateList(reader, StringContract, member, null, t, tailArrayOptimization) as IEnumerable<byte>;
                    return _encoding.GetString(list.ToArray());

                case ArrayDsdlType t:
                    return CreateList(reader, contract, member, existingValue, t, tailArrayOptimization);

                case CompositeDsdlTypeBase t:
                    return CreateObject(reader, contract, member, containerContract, containerMember, existingValue, objectType, t, tailArrayOptimization);

                default:
                    throw new ArgumentOutOfRangeException(nameof(scheme));
            }
        }

        object CreateUnknownObject(BitStreamReader reader, DsdlType scheme, bool tailArrayOptimization = false)
        {
            switch (scheme)
            {
                case VoidDsdlType t:
                    ReadAlignment(reader, t);
                    return null;

                case PrimitiveDsdlType t:
                    return ReadPrimitiveType(reader, t);

                case ArrayDsdlType t:
                    switch (t.Mode)
                    {
                        case ArrayDsdlTypeMode.Static:
                            return ReadStaticArray(reader, t);

                        case ArrayDsdlTypeMode.Dynamic:
                            return ReadDynamicArray(reader, t, tailArrayOptimization);

                        default:
                            throw new ArgumentOutOfRangeException(nameof(ArrayDsdlTypeMode));
                    }

                case CompositeDsdlTypeBase t:
                    var dictionary = new Dictionary<string, object>(StringComparer.Ordinal);

                    if (t.IsUnion)
                    {
                        var unionFieldIndex = ReadUnionFieldIndex(reader, t);
                        var field = t.Fields[unionFieldIndex];
                        if (!(field.Type is VoidDsdlType))
                            dictionary[field.Name] = CreateUnknownObject(reader, field.Type);
                    }
                    else
                    {
                        for (int i = 0; i < t.Fields.Count - 1; i++)
                        {
                            var field = t.Fields[i];
                            if (!(field.Type is VoidDsdlType))
                                dictionary[field.Name] = CreateUnknownObject(reader, field.Type);
                        }

                        if (t.Fields.Count > 0)
                        {
                            var field = t.Fields[t.Fields.Count - 1];
                            switch (field.Type)
                            {
                                case VoidDsdlType _:
                                    break;
                                case ArrayDsdlType _:
                                    dictionary[field.Name] = CreateUnknownObject(reader, field.Type, tailArrayOptimization);
                                    break;
                                default:
                                    dictionary[field.Name] = CreateUnknownObject(reader, field.Type);
                                    break;
                            }
                        }
                    }

                    return dictionary;

                default:
                    throw new ArgumentOutOfRangeException(nameof(scheme));
            }
        }

        IEnumerable ReadDynamicArray(BitStreamReader reader, ArrayDsdlType t, bool tailArrayOptimization)
        {
            if (tailArrayOptimization && t.ElementType.MinBitlen >= 8)
            {
                var result = new ArrayList();
                while (reader.LengthInBytes - reader.CurrentIndex > 1)
                {
                    result.Add(CreateUnknownObject(reader, t.ElementType));
                }
                return result;
            }
            else
            {
                var arraySize = ReadDynamicArraySize(reader, t);
                var result = new object[arraySize];
                for (int i = 0; i < arraySize; i++)
                {
                    var tao = i == arraySize - 1 ? tailArrayOptimization : false;
                    result[i] = CreateUnknownObject(reader, t.ElementType, tao);
                }
                return result;
            }
        }

        IEnumerable ReadStaticArray(BitStreamReader reader, ArrayDsdlType t)
        {
            var result = new object[t.MaxSize];
            for (int i = 0; i < t.MaxSize; i++)
                result[i] = CreateUnknownObject(reader, t.ElementType);
            return result;
        }

        int ReadUnionFieldIndex(BitStreamReader reader, CompositeDsdlTypeBase t)
        {
            var bitLen = BitSerializer.IntBitLength(t.Fields.Count);
            return (int)BitSerializer.ReadUInt(reader, bitLen);
        }

        int ReadDynamicArraySize(BitStreamReader reader, ArrayDsdlType t)
        {
            var bitLen = BitSerializer.IntBitLength(t.MaxSize + 1);
            return (int)BitSerializer.ReadUInt(reader, bitLen);
        }

        object ReadPrimitiveType(BitStreamReader reader, PrimitiveDsdlType t)
        {
            switch (t)
            {
                case BooleanDsdlType _:
                    return BitSerializer.ReadBoolean(reader, t.MaxBitlen);
                case IntDsdlType _:
                    return BitSerializer.ReadIntTyped(reader, t.MaxBitlen);
                case UIntDsdlType _:
                    return BitSerializer.ReadUIntTyped(reader, t.MaxBitlen);
                case FloatDsdlType fdt:
                    return ReadFloatPrimitiveType(reader, fdt);
                default:
                    throw new ArgumentOutOfRangeException(nameof(t));
            }
        }

        static object ReadFloatPrimitiveType(BitStreamReader reader, FloatDsdlType t)
        {
            switch (t.MaxBitlen)
            {
                case 16:
                    var value = (ushort)BitSerializer.ReadUInt(reader, 16);
                    return BitSerializer.UInt16ToFloat32(value);

                case 32:
                    return BitSerializer.ReadSingle(reader, 32);

                case 64:
                    return BitSerializer.ReadDouble(reader, 64);

                default:
                    throw new InvalidOperationException($"Unexpected float bit lenght: {t.MaxBitlen}.");
            }
        }

        void ReadAlignment(BitStreamReader reader, VoidDsdlType t)
        {
            var amount = t.MaxBitlen;

            while (amount > 8)
            {
                reader.ReadByte(8);
                amount -= 8;
            }

            if (amount > 0)
                reader.ReadByte(amount);
        }

        object CreateObject(
            BitStreamReader reader,
            IContract contract,
            DsdlProperty member,
            ContainerContract containerContract,
            DsdlProperty containerMember,
            object existingValue,
            Type objectType,
            CompositeDsdlTypeBase scheme,
            bool tailArrayOptimization)
        {
            if (HasNoDefinedType(contract))
            {
                return CreateUnknownObject(reader, scheme);
            }

            switch (contract)
            {
                case ObjectContract objectContract:
                    {
                        bool createdFromNonDefaultCreator = false;
                        object targetObject;
                        // check that if type name handling is being used that the existing value is compatible with the specified type
                        if (existingValue != null && objectType != null && objectType.IsAssignableFrom(existingValue.GetType()))
                        {
                            targetObject = existingValue;
                        }
                        else
                        {
                            targetObject = CreateNewObject(reader, objectContract, member, containerMember, out createdFromNonDefaultCreator);
                        }

                        // don't populate if read from non-default creator because the object has already been read
                        if (createdFromNonDefaultCreator)
                        {
                            return targetObject;
                        }

                        return PopulateObject(targetObject, reader, objectContract, member, scheme, tailArrayOptimization);
                    }
                case DictionaryContract dictionaryContract:
                    {
                        object targetDictionary;

                        if (existingValue == null)
                        {
                            var dictionary = CreateNewDictionary(reader, dictionaryContract, out bool createdFromNonDefaultCreator);

                            if (createdFromNonDefaultCreator && !dictionaryContract.HasParameterizedCreatorInternal)
                                throw new SerializationException("Cannot deserialize readonly or fixed size dictionary: {0}.".FormatWith(CultureInfo.InvariantCulture, contract.UnderlyingType));

                            PopulateDictionary(dictionary, reader, dictionaryContract, member, scheme, tailArrayOptimization);

                            if (createdFromNonDefaultCreator)
                            {
                                ObjectConstructor<object> creator = dictionaryContract.OverrideCreator ?? dictionaryContract.ParameterizedCreator;

                                return creator(dictionary);
                            }
                            else if (dictionary is IWrappedDictionary wrappedDictionary)
                            {
                                return wrappedDictionary.UnderlyingDictionary;
                            }

                            targetDictionary = dictionary;
                        }
                        else
                        {
                            targetDictionary = PopulateDictionary(
                                dictionaryContract.ShouldCreateWrapper || !(existingValue is IDictionary) ? dictionaryContract.CreateWrapper(existingValue) : (IDictionary)existingValue,
                                reader,
                                dictionaryContract,
                                member,
                                scheme,
                                tailArrayOptimization);
                        }

                        return targetDictionary;
                    }
            }

            throw new SerializationException($"Cannot deserialize the current object.");
        }

        object CreateList(
            BitStreamReader reader,
            IContract contract,
            DsdlProperty member,
            object existingValue,
            ArrayDsdlType scheme,
            bool tailArrayOptimization)
        {
            if (HasNoDefinedType(contract))
                return CreateUnknownObject(reader, scheme, tailArrayOptimization);

            if (!(contract is ArrayContract arrayContract))
                throw new SerializationException("Could not resolve type to IContract.");

            if (existingValue == null)
            {
                var list = CreateNewList(reader, arrayContract, out bool createdFromNonDefaultCreator);

                if (createdFromNonDefaultCreator && !arrayContract.HasParameterizedCreatorInternal && !arrayContract.IsArray)
                    throw new SerializationException($"Cannot deserialize readonly or fixed size list: {contract.UnderlyingType}.");
                if (arrayContract.IsMultidimensionalArray)
                    throw new NotSupportedException("Multidimensional arrays are not supported.");

                PopulateList(list, reader, arrayContract, member, scheme, tailArrayOptimization);

                if (createdFromNonDefaultCreator)
                {
                    if (arrayContract.IsArray)
                    {
                        Array a = Array.CreateInstance(arrayContract.CollectionItemType, list.Count);
                        list.CopyTo(a, 0);
                        list = a;
                    }
                    else
                    {
                        ObjectConstructor<object> creator = arrayContract.OverrideCreator ?? arrayContract.ParameterizedCreator;

                        return creator(list);
                    }
                }
                else if (list is IWrappedCollection wrappedCollection)
                {
                    return wrappedCollection.UnderlyingCollection;
                }

                return list;
            }
            else
            {
                if (!arrayContract.CanDeserialize)
                    throw new SerializationException($"Cannot populate list type {contract.CreatedType}.");

                return PopulateList(
                    (arrayContract.ShouldCreateWrapper || !(existingValue is IList list)) ? arrayContract.CreateWrapper(existingValue) : list,
                    reader,
                    arrayContract,
                    member,
                    scheme,
                    tailArrayOptimization);
            }
        }

        private bool HasNoDefinedType(IContract contract)
        {
            return (contract == null || contract.UnderlyingType == typeof(object));
        }

        private object EnsureType(BitStreamReader reader, object value, CultureInfo culture, IContract contract, Type targetType)
        {
            if (targetType == null)
            {
                return value;
            }

            Type valueType = value?.GetType();

            // type of value and type of target don't match
            // attempt to convert value's type to target's type
            if (valueType != targetType)
            {
                if (value == null && contract.IsNullable)
                {
                    return null;
                }

                if (contract.IsConvertable)
                {
                    PrimitiveContract primitiveContract = (PrimitiveContract)contract;

                    if (contract.IsEnum)
                    {
                        if (value is string s)
                        {
                            return EnumUtils.ParseEnum(contract.NonNullableUnderlyingType, s, false);
                        }
                        if (ConvertUtils.IsInteger(primitiveContract.TypeCode))
                        {
                            return Enum.ToObject(contract.NonNullableUnderlyingType, value);
                        }
                    }

                    // this won't work when converting to a custom IConvertible
                    return Convert.ChangeType(value, contract.NonNullableUnderlyingType, culture);
                }

                return ConvertUtils.ConvertOrCast(value, culture, contract.NonNullableUnderlyingType);
            }

            return value;
        }

        bool SetPropertyValue(
            DsdlProperty property,
            ContainerContract containerContract,
            DsdlProperty containerProperty,
            BitStreamReader reader,
            object target,
            DsdlType scheme,
            bool tailArrayOptimization)
        {
            if (property.Ignored)
                return true;

            if (property.PropertyContract == null)
                property.PropertyContract = GetContractSafe(property.PropertyType);

            bool useExistingValue = false;
            object currentValue = null;
            if (property.Readable)
            {
                currentValue = property.ValueProvider.GetValue(target);
            }

            IContract propertyContract;
            if (currentValue == null)
            {
                propertyContract = property.PropertyContract;
            }
            else
            {
                propertyContract = GetContractSafe(currentValue.GetType());
                useExistingValue = (!propertyContract.IsReadOnlyOrFixedSize && !propertyContract.UnderlyingType.IsValueType());
            }

            var value = CreateValueInternal(
                reader,
                propertyContract,
                property,
                containerContract,
                containerProperty,
                (useExistingValue) ? currentValue : null,
                scheme,
                property.PropertyType,
                tailArrayOptimization);

            // always set the value if useExistingValue is false,
            // otherwise also set it if CreateValue returns a new value compared to the currentValue
            // this could happen because of a JsonConverter against the type
            if ((!useExistingValue || value != currentValue)
                && ShouldSetPropertyValue(property, containerContract as ObjectContract, value))
            {
                property.ValueProvider.SetValue(target, value);

                return true;
            }

            // the value wasn't set be JSON was populated onto the existing value
            return useExistingValue;
        }

        private bool ShouldSetPropertyValue(DsdlProperty property, ObjectContract contract, object value)
        {
            if (!property.Writable)
            {
                return false;
            }

            return true;
        }

        IList CreateNewList(BitStreamReader reader, ArrayContract contract, out bool createdFromNonDefaultCreator)
        {
            // some types like non-generic IEnumerable can be serialized but not deserialized
            if (!contract.CanDeserialize)
                throw new SerializationException("Cannot create and populate list type {0}.".FormatWith(CultureInfo.InvariantCulture, contract.CreatedType));

            if (contract.OverrideCreator != null)
            {
                if (contract.HasParameterizedCreator)
                {
                    createdFromNonDefaultCreator = true;
                    return contract.CreateTemporaryCollection();
                }
                else
                {
                    object list = contract.OverrideCreator();

                    if (contract.ShouldCreateWrapper)
                    {
                        list = contract.CreateWrapper(list);
                    }

                    createdFromNonDefaultCreator = false;
                    return (IList)list;
                }
            }
            else if (contract.IsReadOnlyOrFixedSize)
            {
                createdFromNonDefaultCreator = true;
                IList list = contract.CreateTemporaryCollection();

                if (contract.ShouldCreateWrapper)
                {
                    list = contract.CreateWrapper(list);
                }

                return list;
            }
            else if (contract.DefaultCreator != null && !contract.DefaultCreatorNonPublic)
            {
                object list = contract.DefaultCreator();

                if (contract.ShouldCreateWrapper)
                {
                    list = contract.CreateWrapper(list);
                }

                createdFromNonDefaultCreator = false;
                return (IList)list;
            }
            else if (contract.HasParameterizedCreatorInternal)
            {
                createdFromNonDefaultCreator = true;
                return contract.CreateTemporaryCollection();
            }
            else
            {
                if (!contract.IsInstantiable)
                    throw new SerializationException("Could not create an instance of type {0}. Type is an interface or abstract class and cannot be instantiated.".FormatWith(CultureInfo.InvariantCulture, contract.UnderlyingType));

                throw new SerializationException("Unable to find a constructor to use for type {0}.".FormatWith(CultureInfo.InvariantCulture, contract.UnderlyingType));
            }
        }

        private IDictionary CreateNewDictionary(BitStreamReader reader, DictionaryContract contract, out bool createdFromNonDefaultCreator)
        {
            if (contract.OverrideCreator != null)
            {
                if (contract.HasParameterizedCreator)
                {
                    createdFromNonDefaultCreator = true;
                    return contract.CreateTemporaryDictionary();
                }
                else
                {
                    createdFromNonDefaultCreator = false;
                    return (IDictionary)contract.OverrideCreator();
                }
            }
            else if (contract.IsReadOnlyOrFixedSize)
            {
                createdFromNonDefaultCreator = true;
                return contract.CreateTemporaryDictionary();
            }
            else if (contract.DefaultCreator != null && !contract.DefaultCreatorNonPublic)
            {
                object dictionary = contract.DefaultCreator();

                if (contract.ShouldCreateWrapper)
                {
                    dictionary = contract.CreateWrapper(dictionary);
                }

                createdFromNonDefaultCreator = false;
                return (IDictionary)dictionary;
            }
            else if (contract.HasParameterizedCreatorInternal)
            {
                createdFromNonDefaultCreator = true;
                return contract.CreateTemporaryDictionary();
            }
            else
            {
                if (!contract.IsInstantiable)
                {
                    throw new SerializationException("Could not create an instance of type {0}. Type is an interface or abstract class and cannot be instantiated.".FormatWith(CultureInfo.InvariantCulture, contract.UnderlyingType));
                }

                throw new SerializationException("Unable to find a default constructor to use for type {0}.".FormatWith(CultureInfo.InvariantCulture, contract.UnderlyingType));
            }
        }

        object PopulateDictionary(
            IDictionary dictionary,
            BitStreamReader reader,
            DictionaryContract contract,
            DsdlProperty containerProperty,
            CompositeDsdlTypeBase scheme,
            bool tailArrayOptimization)
        {
            object underlyingDictionary = dictionary is IWrappedDictionary wrappedDictionary ? wrappedDictionary.UnderlyingDictionary : dictionary;

            if (contract.KeyContract == null)
                contract.KeyContract = GetContractSafe(contract.DictionaryKeyType);

            if (contract.ItemContract == null)
                contract.ItemContract = GetContractSafe(contract.DictionaryValueType);

            foreach (var (field, tao) in EnumerateSchemeFields(reader, scheme, tailArrayOptimization))
            {
                if (field.Type is VoidDsdlType t)
                {
                    ReadAlignment(reader, t);
                    continue;
                }

                var keyValue = EnsureType(reader, field.Name, CultureInfo.InvariantCulture, contract.KeyContract, contract.DictionaryKeyType);

                var itemValue = CreateValueInternal(reader,
                    contract.ItemContract,
                    null,
                    contract,
                    containerProperty,
                    null,
                    field.Type,
                    null,
                    tao);

                dictionary[keyValue] = itemValue;
            }

            return underlyingDictionary;
        }

        object PopulateList(
            IList list,
            BitStreamReader reader,
            ArrayContract contract,
            DsdlProperty containerProperty,
            ArrayDsdlType scheme,
            bool tailArrayOptimization)
        {
            object underlyingList = list is IWrappedCollection wrappedCollection ? wrappedCollection.UnderlyingCollection : list;

            if (contract.ItemContract == null)
                contract.ItemContract = GetContractSafe(contract.CollectionItemType);

            object ReadArrayItem(bool tao = false)
            {
                return CreateValueInternal(reader, contract.ItemContract, null, contract, containerProperty, null, scheme.ElementType, contract.CollectionItemType, tao);
            }

            switch (scheme.Mode)
            {
                case ArrayDsdlTypeMode.Static:
                    {
                        for (int i = 0; i < scheme.MaxSize; i++)
                            list.Add(ReadArrayItem());
                        break;
                    }

                case ArrayDsdlTypeMode.Dynamic:
                    {
                        if (tailArrayOptimization && scheme.ElementType.MinBitlen >= 8)
                        {
                            while (reader.LengthInBytes - reader.CurrentIndex > 1)
                                list.Add(ReadArrayItem());
                        }
                        else
                        {
                            var arraySize = ReadDynamicArraySize(reader, scheme);
                            for (int i = 0; i < arraySize; i++)
                            {
                                var tao = i == arraySize - 1 ? tailArrayOptimization : false;
                                list.Add(ReadArrayItem(tao));
                            }
                        }
                        break;
                    }

                default:
                    throw new ArgumentOutOfRangeException(nameof(ArrayDsdlTypeMode));
            }

            return underlyingList;
        }

        public object CreateNewObject(BitStreamReader reader, ObjectContract objectContract, DsdlProperty containerMember, DsdlProperty containerProperty, out bool createdFromNonDefaultCreator)
        {
            object newObject = null;

            if (objectContract.DefaultCreator != null && !objectContract.DefaultCreatorNonPublic)
            {
                // use the default constructor if it is...
                // public
                // non-public and the user has change constructor handling settings
                // non-public and there is no other creator
                newObject = objectContract.DefaultCreator();
            }

            if (newObject == null)
            {
                if (!objectContract.IsInstantiable)
                {
                    throw new SerializationException("Could not create an instance of type {0}. Type is an interface or abstract class and cannot be instantiated.".FormatWith(CultureInfo.InvariantCulture, objectContract.UnderlyingType));
                }

                throw new SerializationException("Unable to find a constructor to use for type {0}. A class should either have a default constructor, one constructor with arguments or a constructor marked with the JsonConstructor attribute.".FormatWith(CultureInfo.InvariantCulture, objectContract.UnderlyingType));
            }

            createdFromNonDefaultCreator = false;
            return newObject;
        }

        private object PopulateObject(
            object newObject,
            BitStreamReader
            reader,
            ObjectContract contract,
            DsdlProperty member,
            CompositeDsdlTypeBase scheme,
            bool tailArrayOptimization)
        {
            foreach (var (field, tao) in EnumerateSchemeFields(reader, scheme, tailArrayOptimization))
            {
                if (field.Type is VoidDsdlType t)
                {
                    ReadAlignment(reader, t);
                    continue;
                }

                // attempt exact case match first
                // then try match ignoring case
                DsdlProperty property = contract.Properties.GetClosestMatchProperty(field.Name);

                if (property == null)
                    throw new SerializationException($"Could not find member '{field.Name}' on object of type '{contract.UnderlyingType.FullName}'");

                if (property.PropertyContract == null)
                    property.PropertyContract = GetContractSafe(property.PropertyType);

                SetPropertyValue(property, contract, member, reader, newObject, field.Type, tao);
            }

            return newObject;
        }

        IEnumerable<(DsdlField Field, bool TAO)> EnumerateSchemeFields(
            BitStreamReader reader,
            CompositeDsdlTypeBase scheme,
            bool tailArrayOptimization)
        {
            if (scheme.IsUnion)
            {
                var unionFieldIndex = ReadUnionFieldIndex(reader, scheme);
                var field = scheme.Fields[unionFieldIndex];
                yield return (field, false);
            }
            else
            {
                for (int i = 0; i < scheme.Fields.Count - 1; i++)
                {
                    var field = scheme.Fields[i];
                    yield return (field, false);
                }

                if (scheme.Fields.Count > 0)
                {
                    var field = scheme.Fields[scheme.Fields.Count - 1];
                    switch (field.Type)
                    {
                        case ArrayDsdlType _:
                            yield return (field, tailArrayOptimization);
                            break;

                        default:
                            yield return (field, false);
                            break;
                    }
                }
            }
        }
    }
}
