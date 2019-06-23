using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using CanardSharp.Dsdl.TypesInterop.Utilities;

namespace CanardSharp.Dsdl.TypesInterop
{
    static class TypeReflector
    {
        public const string ConcurrentDictionaryTypeName = "System.Collections.Concurrent.ConcurrentDictionary`2";

        public static T GetCachedAttribute<T>(object attributeProvider) where T : Attribute
        {
            return CachedAttributeGetter<T>.GetAttribute(attributeProvider);
        }

        public static DataContractAttribute GetDataContractAttribute(Type type)
        {
            // DataContractAttribute does not have inheritance
            Type currentType = type;

            while (currentType != null)
            {
                DataContractAttribute result = CachedAttributeGetter<DataContractAttribute>.GetAttribute(currentType);
                if (result != null)
                {
                    return result;
                }

                currentType = currentType.BaseType();
            }

            return null;
        }

        public static DataMemberAttribute GetDataMemberAttribute(MemberInfo memberInfo)
        {
            // DataMemberAttribute does not have inheritance

            // can't override a field
            if (memberInfo.MemberType() == MemberTypes.Field)
            {
                return CachedAttributeGetter<DataMemberAttribute>.GetAttribute(memberInfo);
            }

            // search property and then search base properties if nothing is returned and the property is virtual
            PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
            DataMemberAttribute result = CachedAttributeGetter<DataMemberAttribute>.GetAttribute(propertyInfo);
            if (result == null)
            {
                if (propertyInfo.IsVirtual())
                {
                    Type currentType = propertyInfo.DeclaringType;

                    while (result == null && currentType != null)
                    {
                        PropertyInfo baseProperty = (PropertyInfo)ReflectionUtils.GetMemberInfoFromType(currentType, propertyInfo);
                        if (baseProperty != null && baseProperty.IsVirtual())
                        {
                            result = CachedAttributeGetter<DataMemberAttribute>.GetAttribute(baseProperty);
                        }

                        currentType = currentType.BaseType();
                    }
                }
            }

            return result;
        }

        private static T GetAttribute<T>(Type type) where T : Attribute
        {
            T attribute;

            attribute = ReflectionUtils.GetAttribute<T>(type, true);
            if (attribute != null)
            {
                return attribute;
            }

            foreach (Type typeInterface in type.GetInterfaces())
            {
                attribute = ReflectionUtils.GetAttribute<T>(typeInterface, true);
                if (attribute != null)
                {
                    return attribute;
                }
            }

            return null;
        }

        private static T GetAttribute<T>(MemberInfo memberInfo) where T : Attribute
        {
            T attribute;

            attribute = ReflectionUtils.GetAttribute<T>(memberInfo, true);
            if (attribute != null)
            {
                return attribute;
            }

            if (memberInfo.DeclaringType != null)
            {
                foreach (Type typeInterface in memberInfo.DeclaringType.GetInterfaces())
                {
                    MemberInfo interfaceTypeMemberInfo = ReflectionUtils.GetMemberInfoFromType(typeInterface, memberInfo);

                    if (interfaceTypeMemberInfo != null)
                    {
                        attribute = ReflectionUtils.GetAttribute<T>(interfaceTypeMemberInfo, true);
                        if (attribute != null)
                        {
                            return attribute;
                        }
                    }
                }
            }

            return null;
        }

        public static bool IsNonSerializable(object provider)
        {
            // no inheritance
            return (ReflectionUtils.GetAttribute<NonSerializedAttribute>(provider, false) != null);
        }

        public static bool IsSerializable(object provider)
        {
            // no inheritance
            return (ReflectionUtils.GetAttribute<SerializableAttribute>(provider, false) != null);
        }

        public static T GetAttribute<T>(object provider) where T : Attribute
        {
            if (provider is Type type)
            {
                return GetAttribute<T>(type);
            }

            if (provider is MemberInfo memberInfo)
            {
                return GetAttribute<T>(memberInfo);
            }

            return ReflectionUtils.GetAttribute<T>(provider, true);
        }

        public static ReflectionDelegateFactory ReflectionDelegateFactory
        {
            get
            {
                return DynamicReflectionDelegateFactory.Instance;
            }
        }
    }
}
