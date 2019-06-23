using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using CanardSharp.Dsdl.DataTypes;
using CanardSharp.Dsdl.TypesInterop.Utilities;

namespace CanardSharp.Dsdl.TypesInterop
{
    public class ContractResolver
    {
        public ContractResolver(IUavcanTypeResolver schemeResolver)
        {
            _schemeResolver = schemeResolver;
        }

        readonly IUavcanTypeResolver _schemeResolver;

        readonly ConcurrentDictionary<Type, IContract> _contractCache = new ConcurrentDictionary<Type, IContract>();

        /// <summary>
        /// Gets a value indicating whether members are being get and set using dynamic code generation.
        /// This value is determined by the runtime permissions available.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if using dynamic code generation; otherwise, <c>false</c>.
        /// </value>
        public bool DynamicCodeGeneration { get; set; }
        public BindingFlags DefaultMembersSearchFlags { get; set; } = BindingFlags.Instance | BindingFlags.Public;

        protected virtual IValueProvider CreateMemberValueProvider(MemberInfo member)
        {
            IValueProvider valueProvider;

            if (DynamicCodeGeneration)
            {
                valueProvider = new DynamicValueProvider(member);
            }
            else
            {
                valueProvider = new ReflectionValueProvider(member);
            }

            return valueProvider;
        }

        /// <summary>
        /// Resolves the contract for a given type.
        /// </summary>
        /// <param name="type">The type to resolve a contract for.</param>
        /// <returns>The contract for a given type.</returns>
        public virtual IContract ResolveContract(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return _contractCache.GetOrAdd(type, CreateContract);
        }

        private static bool FilterMembers(MemberInfo member)
        {
            if (member is PropertyInfo property)
            {
                if (ReflectionUtils.IsIndexedProperty(property))
                {
                    return false;
                }

                return !ReflectionUtils.IsByRefLikeType(property.PropertyType);
            }
            else if (member is FieldInfo field)
            {
                return !ReflectionUtils.IsByRefLikeType(field.FieldType);
            }

            return true;
        }

        /// <summary>
        /// Gets the serializable members for the type.
        /// </summary>
        /// <param name="objectType">The type to get serializable members for.</param>
        /// <returns>The serializable members for the type.</returns>
        protected virtual List<MemberInfo> GetSerializableMembers(Type objectType)
        {
            var allMembers = ReflectionUtils.GetFieldsAndProperties(objectType, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Where(FilterMembers);

            var serializableMembers = new List<MemberInfo>();

            var dataContractAttribute = TypeReflector.GetDataContractAttribute(objectType);
            var defaultMembers = ReflectionUtils.GetFieldsAndProperties(objectType, DefaultMembersSearchFlags)
                .Where(FilterMembers).ToList();

            foreach (var member in allMembers)
            {
                if (defaultMembers.Contains(member))
                {
                    // add all members that are found by default member search
                    serializableMembers.Add(member);
                }
                else
                {
                    // add members that are explicitly marked with JsonProperty/DataMember attribute
                    // or are a field if serializing just fields
                    if (dataContractAttribute != null && TypeReflector.GetAttribute<DataMemberAttribute>(member) != null)
                    {
                        serializableMembers.Add(member);
                    }
                }
            }

            // don't include TargetSite on non-serializable exceptions
            // MemberBase is problematic to serialize. Large, self referencing instances, etc
            if (typeof(Exception).IsAssignableFrom(objectType))
            {
                serializableMembers = serializableMembers.Where(m => !string.Equals(m.Name, "TargetSite", StringComparison.Ordinal)).ToList();
            }

            return serializableMembers;
        }

        /// <summary>
        /// Determines which contract type is created for the given type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>A <see cref="JsonContract"/> for the given type.</returns>
        protected virtual IContract CreateContract(Type objectType)
        {
            Type t = ReflectionUtils.EnsureNotByRefType(objectType);

            if (IsSupportedPrimitiveType(t))
            {
                return CreatePrimitiveContract(objectType);
            }

            t = ReflectionUtils.EnsureNotNullableType(t);

            if (CollectionUtils.IsDictionaryType(t))
            {
                return CreateDictionaryContract(objectType);
            }

            if (typeof(IEnumerable).IsAssignableFrom(t))
            {
                return CreateArrayContract(objectType);
            }

            return CreateObjectContract(objectType);
        }

        /// <summary>
        /// Creates a <see cref="JsonDictionaryContract"/> for the given type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>A <see cref="JsonDictionaryContract"/> for the given type.</returns>
        protected virtual DictionaryContract CreateDictionaryContract(Type objectType)
        {
            DictionaryContract contract = new DictionaryContract(objectType);
            InitializeContract(contract);
            contract.DictionaryKeyResolver = ResolveDictionaryKey;
            return contract;
        }

        /// <summary>
        /// Resolves the key of the dictionary. By default <see cref="ResolvePropertyName"/> is used to resolve dictionary keys.
        /// </summary>
        /// <param name="dictionaryKey">Key of the dictionary.</param>
        /// <returns>Resolved key of the dictionary.</returns>
        protected virtual string ResolveDictionaryKey(string dictionaryKey)
        {
            return dictionaryKey;
        }

        static bool IsSupportedPrimitiveType(Type t)
        {
            switch (ConvertUtils.GetTypeCode(t))
            {
                case PrimitiveTypeCode.Boolean:
                case PrimitiveTypeCode.Char:
                case PrimitiveTypeCode.Byte:
                case PrimitiveTypeCode.SByte:
                case PrimitiveTypeCode.Int16:
                case PrimitiveTypeCode.UInt16:
                case PrimitiveTypeCode.Int32:
                case PrimitiveTypeCode.UInt32:
                case PrimitiveTypeCode.Int64:
                case PrimitiveTypeCode.UInt64:
                case PrimitiveTypeCode.Single:
                case PrimitiveTypeCode.Double:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Creates a <see cref="JsonPrimitiveContract"/> for the given type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>A <see cref="JsonPrimitiveContract"/> for the given type.</returns>
        protected virtual PrimitiveContract CreatePrimitiveContract(Type objectType)
        {
            PrimitiveContract contract = new PrimitiveContract(objectType);
            InitializeContract(contract);

            return contract;
        }

        /// <summary>
        /// Creates a <see cref="JsonArrayContract"/> for the given type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>A <see cref="JsonArrayContract"/> for the given type.</returns>
        protected virtual ArrayContract CreateArrayContract(Type objectType)
        {
            ArrayContract contract = new ArrayContract(objectType);
            InitializeContract(contract);
            return contract;
        }

        /// <summary>
        /// Creates a <see cref="JsonObjectContract"/> for the given type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>A <see cref="JsonObjectContract"/> for the given type.</returns>
        protected virtual ObjectContract CreateObjectContract(Type objectType)
        {
            ObjectContract contract = new ObjectContract(objectType);
            InitializeContract(contract);

            var scheme = TryResolveDsdlType(objectType);
            contract.DsdlType = scheme;

            contract.Properties.AddRange(CreateProperties(contract.NonNullableUnderlyingType, scheme));

            if (contract.IsInstantiable)
            {
                if (contract.DefaultCreator == null || contract.DefaultCreatorNonPublic)
                {
                    ConstructorInfo constructor = GetParameterizedConstructor(contract.NonNullableUnderlyingType);
                    if (constructor != null)
                    {
                        contract.ParameterizedCreator = TypeReflector.ReflectionDelegateFactory.CreateParameterizedConstructor(constructor);
                        contract.CreatorParameters.AddRange(CreateConstructorParameters(constructor, contract.Properties));
                    }
                }
                else if (contract.NonNullableUnderlyingType.IsValueType())
                {
                    // value types always have default constructor
                    // check whether there is a constructor that matches with non-writable properties on value type
                    ConstructorInfo constructor = GetImmutableConstructor(contract.NonNullableUnderlyingType, contract.Properties);
                    if (constructor != null)
                    {
                        contract.OverrideCreator = TypeReflector.ReflectionDelegateFactory.CreateParameterizedConstructor(constructor);
                        contract.CreatorParameters.AddRange(CreateConstructorParameters(constructor, contract.Properties));
                    }
                }
            }

            ValidateDsdlTypeCompatibility(contract, scheme);

            return contract;
        }

        void ValidateDsdlTypeCompatibility(ObjectContract contract, CompositeDsdlType scheme)
        {
            if (scheme == null)
                return;

            foreach (var member in scheme.Fields)
            {
                if (member.Type is VoidDsdlType)
                    continue;

                if (contract.Properties.GetProperty(member.Name, StringComparison.Ordinal) == null)
                    throw new InvalidOperationException($"Cannot find {member.Name} member for type {contract.UnderlyingType.FullName}.");
            }
        }

        CompositeDsdlType TryResolveDsdlType(Type type)
        {
            string ns = null, name = null, suffix = null;

            var dataContractAttribute = TypeReflector.GetDataContractAttribute(type);
            if (dataContractAttribute != null)
            {
                if (dataContractAttribute.IsNamespaceSetExplicitly)
                {
                    ns = dataContractAttribute.Namespace;
                }
                if (dataContractAttribute.IsNameSetExplicitly)
                {
                    name = dataContractAttribute.Name;
                    var colonIndex = name.LastIndexOf('.');
                    if (colonIndex != -1)
                    {
                        suffix = name.Substring(colonIndex + 1);
                        name = name.Substring(0, colonIndex);
                    }
                }
            }

            if (ns == null)
                ns = type.Namespace;
            if (name == null)
                name = type.Name;

            var uavcanType = _schemeResolver.TryResolveType(ns, name);
            switch (uavcanType)
            {
                case MessageType t:
                    return t.Message;
                case ServiceType t:
                    switch (suffix?.ToLower())
                    {
                        case "request":
                            return t.Request;
                        case "response":
                            return t.Response;
                        default:
                            throw new InvalidOperationException($"{ns}.{name} is service. Please specify :request or :response suffix explicitly.");
                    }
                default:
                    return null;
            }
        }

        ConstructorInfo GetImmutableConstructor(Type objectType, DsdlPropertyCollection memberProperties)
        {
            IEnumerable<ConstructorInfo> constructors = objectType.GetConstructors();
            var en = constructors.GetEnumerator();
            if (en.MoveNext())
            {
                var constructor = en.Current;
                if (!en.MoveNext())
                {
                    var parameters = constructor.GetParameters();
                    if (parameters.Length > 0)
                    {
                        foreach (ParameterInfo parameterInfo in parameters)
                        {
                            var memberProperty = MatchProperty(memberProperties, parameterInfo.Name, parameterInfo.ParameterType);
                            if (memberProperty == null || memberProperty.Writable)
                            {
                                return null;
                            }
                        }

                        return constructor;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Creates the constructor parameters.
        /// </summary>
        /// <param name="constructor">The constructor to create properties for.</param>
        /// <param name="memberProperties">The type's member properties.</param>
        /// <returns>Properties for the given <see cref="ConstructorInfo"/>.</returns>
        protected virtual IList<DsdlProperty> CreateConstructorParameters(ConstructorInfo constructor, DsdlPropertyCollection memberProperties)
        {
            ParameterInfo[] constructorParameters = constructor.GetParameters();

            var parameterCollection = new DsdlPropertyCollection(constructor.DeclaringType);

            foreach (ParameterInfo parameterInfo in constructorParameters)
            {
                DsdlProperty matchingMemberProperty = MatchProperty(memberProperties, parameterInfo.Name, parameterInfo.ParameterType);

                // ensure that property will have a name from matching property or from parameterinfo
                // parameterinfo could have no name if generated by a proxy (I'm looking at you Castle)
                if (matchingMemberProperty != null || parameterInfo.Name != null)
                {
                    DsdlProperty property = CreatePropertyFromConstructorParameter(matchingMemberProperty, parameterInfo);

                    if (property != null)
                    {
                        parameterCollection.AddProperty(property);
                    }
                }
            }

            return parameterCollection;
        }

        /// <summary>
        /// Creates a <see cref="JsonProperty"/> for the given <see cref="ParameterInfo"/>.
        /// </summary>
        /// <param name="matchingMemberProperty">The matching member property.</param>
        /// <param name="parameterInfo">The constructor parameter.</param>
        /// <returns>A created <see cref="JsonProperty"/> for the given <see cref="ParameterInfo"/>.</returns>
        protected virtual DsdlProperty CreatePropertyFromConstructorParameter(DsdlProperty matchingMemberProperty, ParameterInfo parameterInfo)
        {
            var property = new DsdlProperty
            {
                PropertyType = parameterInfo.ParameterType,
                AttributeProvider = new ReflectionAttributeProvider(parameterInfo)
            };

            SetPropertySettingsFromAttributes(property, parameterInfo, parameterInfo.Name, parameterInfo.Member.DeclaringType, out _);

            property.Readable = false;
            property.Writable = true;

            // "inherit" values from matching member property if unset on parameter
            if (matchingMemberProperty != null)
            {
                property.PropertyName = (property.PropertyName != parameterInfo.Name) ? property.PropertyName : matchingMemberProperty.PropertyName;

                if (!property._hasExplicitDefaultValue && matchingMemberProperty._hasExplicitDefaultValue)
                {
                    property.DefaultValue = matchingMemberProperty.DefaultValue;
                }

                property.IsReference = property.IsReference ?? matchingMemberProperty.IsReference;
            }

            return property;
        }

        DsdlProperty MatchProperty(DsdlPropertyCollection properties, string name, Type type)
        {
            // it is possible to generate a member with a null name using Reflection.Emit
            // protect against an ArgumentNullException from GetClosestMatchProperty by testing for null here
            if (name == null)
            {
                return null;
            }

            var property = properties.GetClosestMatchProperty(name);
            // must match type as well as name
            if (property == null || property.PropertyType != type)
            {
                return null;
            }

            return property;
        }

        /// <summary>
        /// Creates properties for the given <see cref="JsonContract"/>.
        /// </summary>
        /// <param name="type">The type to create properties for.</param>
        /// /// <param name="memberSerialization">The member serialization mode for the type.</param>
        /// <returns>Properties for the given <see cref="JsonContract"/>.</returns>
        protected virtual IList<DsdlProperty> CreateProperties(Type type, CompositeDsdlType scheme)
        {
            List<MemberInfo> members = GetSerializableMembers(type);
            if (members == null)
            {
                throw new SerializationException("Null collection of serializable members returned.");
            }

            var properties = new DsdlPropertyCollection(type);

            foreach (MemberInfo member in members)
            {
                var property = CreateProperty(member);

                if (property != null)
                {
                    if (scheme != null)
                        property.DsdlType = scheme.TryGetField(property.PropertyName)?.Type;

                    properties.AddProperty(property);
                }
            }

            return properties.OrderBy(p => p.Order ?? -1).ToList();
        }

        /// <summary>
        /// Creates a <see cref="JsonProperty"/> for the given <see cref="MemberInfo"/>.
        /// </summary>
        /// <param name="memberSerialization">The member's parent <see cref="MemberSerialization"/>.</param>
        /// <param name="member">The member to create a <see cref="JsonProperty"/> for.</param>
        /// <returns>A created <see cref="JsonProperty"/> for the given <see cref="MemberInfo"/>.</returns>
        protected virtual DsdlProperty CreateProperty(MemberInfo member)
        {
            var property = new DsdlProperty
            {
                PropertyType = ReflectionUtils.GetMemberUnderlyingType(member),
                DeclaringType = member.DeclaringType,
                ValueProvider = CreateMemberValueProvider(member),
                AttributeProvider = new ReflectionAttributeProvider(member)
            };

            SetPropertySettingsFromAttributes(property, member, member.Name, member.DeclaringType, out bool allowNonPublicAccess);

            property.Readable = ReflectionUtils.CanReadMemberValue(member, allowNonPublicAccess);
            property.Writable = ReflectionUtils.CanSetMemberValue(member, allowNonPublicAccess, false);

            return property;
        }

        void SetPropertySettingsFromAttributes(DsdlProperty property, object attributeProvider, string name, Type declaringType, out bool allowNonPublicAccess)
        {
            var dataContractAttribute = TypeReflector.GetDataContractAttribute(declaringType);

            DataMemberAttribute dataMemberAttribute = null;
            if (dataContractAttribute != null && attributeProvider is MemberInfo memberInfo)
            {
                dataMemberAttribute = TypeReflector.GetDataMemberAttribute(memberInfo);
            }

            string mappedName;
            if (dataMemberAttribute?.Name != null)
            {
                mappedName = dataMemberAttribute.Name;
            }
            else
            {
                mappedName = name;
            }

            property.PropertyName = mappedName;
            property.UnderlyingName = name;
            property.IsReference = null;
            property.ItemIsReference = null;
            if (dataMemberAttribute != null)
            {
                property.Order = (dataMemberAttribute.Order != -1) ? (int?)dataMemberAttribute.Order : null;
            }

            allowNonPublicAccess = false;
            if ((DefaultMembersSearchFlags & BindingFlags.NonPublic) == BindingFlags.NonPublic)
            {
                allowNonPublicAccess = true;
            }
        }

        private ConstructorInfo GetParameterizedConstructor(Type objectType)
        {
            ConstructorInfo[] constructors = objectType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            if (constructors.Length == 1)
            {
                return constructors[0];
            }
            return null;
        }

        protected void InitializeContract(IContract contract)
        {
            var dataContractAttribute = TypeReflector.GetDataContractAttribute(contract.NonNullableUnderlyingType);
            // doesn't have a null value
            if (dataContractAttribute != null && dataContractAttribute.IsReference)
            {
                contract.IsReference = true;
            }

            if (contract.IsInstantiable
                && (ReflectionUtils.HasDefaultConstructor(contract.CreatedType, true) || contract.CreatedType.IsValueType()))
            {
                contract.DefaultCreator = GetDefaultCreator(contract.CreatedType);

                contract.DefaultCreatorNonPublic = (!contract.CreatedType.IsValueType() &&
                                                    ReflectionUtils.GetDefaultConstructor(contract.CreatedType) == null);
            }
        }

        private Func<object> GetDefaultCreator(Type createdType)
        {
            return TypeReflector.ReflectionDelegateFactory.CreateDefaultConstructor<object>(createdType);
        }
    }
}
