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

        public void Serialize<T>(BitStreamWriter stream, T value, IContract contract)
        {
            SerializeValue(stream, value, contract, null, null, null, null);
        }

        private void SerializeValue(BitStreamWriter writer, object value, IContract valueContract, DsdlProperty member, ContainerContract containerContract, DsdlProperty containerProperty, DsdlType derivedDsdlType)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value), "Cannot serialize null value");

            switch (valueContract)
            {
                case ObjectContract contract:
                    SerializeObject(writer, value, contract, member, containerContract, containerProperty);
                    break;
                case ArrayContract contract:
                    if (contract.IsMultidimensionalArray)
                        throw new NotSupportedException("Multidimensional arrays are not supported.");
                    SerializeList(writer, (IEnumerable)value, contract, member, containerContract, containerProperty, derivedDsdlType);
                    break;
                case PrimitiveContract contract:
                    SerializePrimitive(writer, value, contract, member, containerContract, containerProperty, derivedDsdlType);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void SerializePrimitive(BitStreamWriter writer, object value, PrimitiveContract contract, DsdlProperty member, ContainerContract containerContract, DsdlProperty containerProperty, DsdlType derivedDsdlType)
        {
            if (!(derivedDsdlType is PrimitiveDsdlType))
                throw new InvalidOperationException($"Primitive DSDL type expected for type '{contract.UnderlyingType.FullName}'.");



            //JsonWriter.WriteValue(writer, contract.TypeCode, value);
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

        void SerializeObject(BitStreamWriter writer, object value, ObjectContract contract, DsdlProperty member, ContainerContract collectionContract, DsdlProperty containerProperty)
        {
            if (!(contract.DsdlType is CompositeDsdlType compositeDsdlType))
                throw new InvalidOperationException($"Composite DSDL type expected for type '{contract.UnderlyingType.FullName}'.");

            _serializeStack.Push(value);

            //WriteObjectStart(writer, value, contract, member, collectionContract, containerProperty);

            foreach (var dsdlMember in compositeDsdlType.Fields)
            {
                var property = contract.Properties.GetProperty(dsdlMember.Name, StringComparison.Ordinal);

                if (!CalculatePropertyValues(value, contract, member, property, out var memberContract, out var memberValue, out var derivedDsdlType))
                    continue;

                //property.WritePropertyName(writer);
                SerializeValue(writer, memberValue, memberContract, property, contract, member, derivedDsdlType);
            }

            //writer.WriteEndObject();

            _serializeStack.Pop();
        }

        bool CalculatePropertyValues(object value, ContainerContract contract, DsdlProperty propertyOwner, DsdlProperty property, out IContract memberContract, out object memberValue, out DsdlType derivedDsdlType)
        {
            memberContract = null;
            memberValue = null;
            derivedDsdlType = null;

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
            derivedDsdlType = property.DsdlType;

            return true;
        }

        void SerializeList(BitStreamWriter writer, IEnumerable values, ArrayContract contract, DsdlProperty member, ContainerContract collectionContract, DsdlProperty containerProperty, DsdlType derivedDsdlType)
        {
            if (!(derivedDsdlType is ArrayDsdlType arrayDsdlType))
                throw new InvalidOperationException($"Array DSDL type expected for type '{contract.UnderlyingType.FullName}'.");

            object underlyingList = values is IWrappedCollection wrappedCollection ? wrappedCollection.UnderlyingCollection : values;

            _serializeStack.Push(underlyingList);

            //writer.WriteStartArray();

            // Note: an exception from the IEnumerable won't be caught.
            foreach (object value in values)
            {
                var valueContract = contract.FinalItemContract ??
                    _serializer.ContractResolver.ResolveContract(value.GetType());

                if (!CheckForCircularReference(value, null, valueContract))
                    continue;

                if (!CheckDsdlTypeCompatibility(arrayDsdlType.ItemType, valueContract))
                    throw new InvalidOperationException(
                        $"DSDL type mismatch for enumerated item '{contract.UnderlyingType.FullName}.{valueContract.UnderlyingType}'.");

                SerializeValue(writer, value, valueContract, null, contract, member, arrayDsdlType.ItemType);
            }

            //writer.WriteEndArray();

            _serializeStack.Pop();
        }

        static bool CheckDsdlTypeCompatibility(DsdlType schemeType, IContract actualContract)
        {
            switch (schemeType)
            {
                case PrimitiveDsdlType dt:
                    if (!(actualContract is PrimitiveContract))
                        return false;
                    break;
                case ArrayDsdlType dt:
                    if (!(actualContract is ArrayContract))
                        return false;
                    break;
                case CompositeDsdlType dt:
                    if (dt != actualContract.DsdlType)
                        return false;
                    break;
            }

            return true;
        }
    }
}
