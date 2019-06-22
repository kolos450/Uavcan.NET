using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using CanardSharp.Dsdl.TypesInterop.Utilities;

namespace CanardSharp.Dsdl.TypesInterop
{
    class ContractResolver
    {
        ConcurrentDictionary<Type, ResolvedContract> _contractCache = new ConcurrentDictionary<Type, ResolvedContract>();

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
        public virtual ResolvedContract ResolveContract(Type type)
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
        protected virtual ResolvedContract CreateContract(Type objectType)
        {
            Type t = ReflectionUtils.EnsureNotByRefType(objectType);

            throw new NotImplementedException();
        }
    }
}
