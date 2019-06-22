using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CanardSharp.Dsdl.TypesInterop.Utilities
{
    static class ReflectionUtils
    {
        public static readonly Type[] EmptyTypes;

        static ReflectionUtils()
        {
            EmptyTypes = Type.EmptyTypes;
        }

        public static bool IsNullable(Type t)
        {
            if (t == null)
                throw new ArgumentNullException(nameof(t));

            if (t.IsValueType())
            {
                return IsNullableType(t);
            }

            return true;
        }

        public static bool IsNullableType(Type t)
        {
            if (t == null)
                throw new ArgumentNullException(nameof(t));

            return (t.IsGenericType() && t.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        /// <summary>
        /// Gets the member's underlying type.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <returns>The underlying type of the member.</returns>
        public static Type GetMemberUnderlyingType(MemberInfo member)
        {
            if (member == null)
                throw new ArgumentNullException(nameof(member));

            switch (member.MemberType())
            {
                case MemberTypes.Field:
                    return ((FieldInfo)member).FieldType;
                case MemberTypes.Property:
                    return ((PropertyInfo)member).PropertyType;
                case MemberTypes.Event:
                    return ((EventInfo)member).EventHandlerType;
                case MemberTypes.Method:
                    return ((MethodInfo)member).ReturnType;
                default:
                    throw new ArgumentException("MemberInfo must be of type FieldInfo, PropertyInfo, EventInfo or MethodInfo", nameof(member));
            }
        }

        /// <summary>
        /// Sets the member's value on the target object.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <param name="target">The target.</param>
        /// <param name="value">The value.</param>
        public static void SetMemberValue(MemberInfo member, object target, object value)
        {
            if (member == null)
                throw new ArgumentNullException(nameof(member));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            switch (member.MemberType())
            {
                case MemberTypes.Field:
                    ((FieldInfo)member).SetValue(target, value);
                    break;
                case MemberTypes.Property:
                    ((PropertyInfo)member).SetValue(target, value, null);
                    break;
                default:
                    throw new ArgumentException("MemberInfo '{0}' must be of type FieldInfo or PropertyInfo".FormatWith(CultureInfo.InvariantCulture, member.Name), nameof(member));
            }
        }

        /// <summary>
        /// Gets the member's value on the object.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <param name="target">The target object.</param>
        /// <returns>The member's value on the object.</returns>
        public static object GetMemberValue(MemberInfo member, object target)
        {
            if (member == null)
                throw new ArgumentNullException(nameof(member));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            switch (member.MemberType())
            {
                case MemberTypes.Field:
                    return ((FieldInfo)member).GetValue(target);
                case MemberTypes.Property:
                    try
                    {
                        return ((PropertyInfo)member).GetValue(target, null);
                    }
                    catch (TargetParameterCountException e)
                    {
                        throw new ArgumentException("MemberInfo '{0}' has index parameters".FormatWith(CultureInfo.InvariantCulture, member.Name), e);
                    }
                default:
                    throw new ArgumentException("MemberInfo '{0}' is not of type FieldInfo or PropertyInfo".FormatWith(CultureInfo.InvariantCulture, member.Name), nameof(member));
            }
        }

        public static bool IsVirtual(this PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
                throw new ArgumentNullException(nameof(propertyInfo));

            MethodInfo m = propertyInfo.GetGetMethod(true);
            if (m != null && m.IsVirtual)
            {
                return true;
            }

            m = propertyInfo.GetSetMethod(true);
            if (m != null && m.IsVirtual)
            {
                return true;
            }

            return false;
        }

        public static MemberInfo GetMemberInfoFromType(Type targetType, MemberInfo memberInfo)
        {
            const BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            switch (memberInfo.MemberType())
            {
                case MemberTypes.Property:
                    PropertyInfo propertyInfo = (PropertyInfo)memberInfo;

                    Type[] types = propertyInfo.GetIndexParameters().Select(p => p.ParameterType).ToArray();

                    return targetType.GetProperty(propertyInfo.Name, bindingAttr, null, propertyInfo.PropertyType, types, null);
                default:
                    return targetType.GetMember(memberInfo.Name, memberInfo.MemberType(), bindingAttr).SingleOrDefault();
            }
        }

        public static T GetAttribute<T>(object attributeProvider) where T : Attribute
        {
            return GetAttribute<T>(attributeProvider, true);
        }

        public static T GetAttribute<T>(object attributeProvider, bool inherit) where T : Attribute
        {
            T[] attributes = GetAttributes<T>(attributeProvider, inherit);

            return attributes?.FirstOrDefault();
        }

        public static T[] GetAttributes<T>(object attributeProvider, bool inherit) where T : Attribute
        {
            Attribute[] a = GetAttributes(attributeProvider, typeof(T), inherit);

            if (a is T[] attributes)
            {
                return attributes;
            }

            return a.Cast<T>().ToArray();
        }

        public static Attribute[] GetAttributes(object attributeProvider, Type attributeType, bool inherit)
        {
            if (attributeProvider == null)
                throw new ArgumentNullException(nameof(attributeProvider));

            object provider = attributeProvider;

            // http://hyperthink.net/blog/getcustomattributes-gotcha/
            // ICustomAttributeProvider doesn't do inheritance

            switch (provider)
            {
                case Type t:
                    object[] array = attributeType != null ? t.GetCustomAttributes(attributeType, inherit) : t.GetCustomAttributes(inherit);
                    Attribute[] attributes = array.Cast<Attribute>().ToArray();
                    return attributes;
                case Assembly a:
                    return (attributeType != null) ? Attribute.GetCustomAttributes(a, attributeType) : Attribute.GetCustomAttributes(a);
                case MemberInfo mi:
                    return (attributeType != null) ? Attribute.GetCustomAttributes(mi, attributeType, inherit) : Attribute.GetCustomAttributes(mi, inherit);
                case Module m:
                    return (attributeType != null) ? Attribute.GetCustomAttributes(m, attributeType, inherit) : Attribute.GetCustomAttributes(m, inherit);
                case ParameterInfo p:
                    return (attributeType != null) ? Attribute.GetCustomAttributes(p, attributeType, inherit) : Attribute.GetCustomAttributes(p, inherit);
                default:
                    ICustomAttributeProvider customAttributeProvider = (ICustomAttributeProvider)attributeProvider;
                    object[] result = (attributeType != null) ? customAttributeProvider.GetCustomAttributes(attributeType, inherit) : customAttributeProvider.GetCustomAttributes(inherit);

                    return (Attribute[])result;
            }
        }

        public static Type EnsureNotByRefType(Type t)
        {
            return (t.IsByRef && t.HasElementType)
                ? t.GetElementType()
                : t;
        }

        public static BindingFlags RemoveFlag(this BindingFlags bindingAttr, BindingFlags flag)
        {
            return ((bindingAttr & flag) == flag)
                ? bindingAttr ^ flag
                : bindingAttr;
        }

        private static void GetChildPrivateFields(IList<MemberInfo> initialFields, Type targetType, BindingFlags bindingAttr)
        {
            // fix weirdness with private FieldInfos only being returned for the current Type
            // find base type fields and add them to result
            if ((bindingAttr & BindingFlags.NonPublic) != 0)
            {
                // modify flags to not search for public fields
                BindingFlags nonPublicBindingAttr = bindingAttr.RemoveFlag(BindingFlags.Public);

                while ((targetType = targetType.BaseType()) != null)
                {
                    // filter out protected fields
                    IEnumerable<FieldInfo> childPrivateFields =
                        targetType.GetFields(nonPublicBindingAttr).Where(f => f.IsPrivate);

                    initialFields.AddRange(childPrivateFields);
                }
            }
        }

        public static IEnumerable<FieldInfo> GetFields(Type targetType, BindingFlags bindingAttr)
        {
            if (targetType == null)
                throw new ArgumentNullException(nameof(targetType));

            List<MemberInfo> fieldInfos = new List<MemberInfo>(targetType.GetFields(bindingAttr));
            // Type.GetFields doesn't return inherited private fields
            // manually find private fields from base class
            GetChildPrivateFields(fieldInfos, targetType, bindingAttr);

            return fieldInfos.Cast<FieldInfo>();
        }

        public static bool IsPublic(PropertyInfo property)
        {
            if (property.GetGetMethod() != null && property.GetGetMethod().IsPublic)
            {
                return true;
            }
            if (property.GetSetMethod() != null && property.GetSetMethod().IsPublic)
            {
                return true;
            }

            return false;
        }

        public static MethodInfo GetBaseDefinition(this PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
                throw new ArgumentNullException(nameof(propertyInfo));

            MethodInfo m = propertyInfo.GetGetMethod(true);
            if (m != null)
            {
                return m.GetBaseDefinition();
            }

            return propertyInfo.GetSetMethod(true)?.GetBaseDefinition();
        }

        private static void GetChildPrivateProperties(IList<PropertyInfo> initialProperties, Type targetType, BindingFlags bindingAttr)
        {
            // fix weirdness with private PropertyInfos only being returned for the current Type
            // find base type properties and add them to result

            // also find base properties that have been hidden by subtype properties with the same name

            while ((targetType = targetType.BaseType()) != null)
            {
                foreach (PropertyInfo propertyInfo in targetType.GetProperties(bindingAttr))
                {
                    PropertyInfo subTypeProperty = propertyInfo;

                    if (!subTypeProperty.IsVirtual())
                    {
                        if (!IsPublic(subTypeProperty))
                        {
                            // have to test on name rather than reference because instances are different
                            // depending on the type that GetProperties was called on
                            int index = initialProperties.IndexOf(p => p.Name == subTypeProperty.Name);
                            if (index == -1)
                            {
                                initialProperties.Add(subTypeProperty);
                            }
                            else
                            {
                                PropertyInfo childProperty = initialProperties[index];
                                // don't replace public child with private base
                                if (!IsPublic(childProperty))
                                {
                                    // replace nonpublic properties for a child, but gotten from
                                    // the parent with the one from the child
                                    // the property gotten from the child will have access to private getter/setter
                                    initialProperties[index] = subTypeProperty;
                                }
                            }
                        }
                        else
                        {
                            int index = initialProperties.IndexOf(p => p.Name == subTypeProperty.Name
                                                                       && p.DeclaringType == subTypeProperty.DeclaringType);

                            if (index == -1)
                            {
                                initialProperties.Add(subTypeProperty);
                            }
                        }
                    }
                    else
                    {
                        Type subTypePropertyDeclaringType = subTypeProperty.GetBaseDefinition()?.DeclaringType ?? subTypeProperty.DeclaringType;

                        int index = initialProperties.IndexOf(p => p.Name == subTypeProperty.Name
                                                                   && p.IsVirtual()
                                                                   && (p.GetBaseDefinition()?.DeclaringType ?? p.DeclaringType).IsAssignableFrom(subTypePropertyDeclaringType));

                        // don't add a virtual property that has an override
                        if (index == -1)
                        {
                            initialProperties.Add(subTypeProperty);
                        }
                    }
                }
            }
        }

        public static IEnumerable<PropertyInfo> GetProperties(Type targetType, BindingFlags bindingAttr)
        {
            if (targetType == null)
                throw new ArgumentNullException(nameof(targetType));

            List<PropertyInfo> propertyInfos = new List<PropertyInfo>(targetType.GetProperties(bindingAttr));

            // GetProperties on an interface doesn't return properties from its interfaces
            if (targetType.IsInterface())
            {
                foreach (Type i in targetType.GetInterfaces())
                {
                    propertyInfos.AddRange(i.GetProperties(bindingAttr));
                }
            }

            GetChildPrivateProperties(propertyInfos, targetType, bindingAttr);

            // a base class private getter/setter will be inaccessible unless the property was gotten from the base class
            for (int i = 0; i < propertyInfos.Count; i++)
            {
                PropertyInfo member = propertyInfos[i];
                if (member.DeclaringType != targetType)
                {
                    PropertyInfo declaredMember = (PropertyInfo)GetMemberInfoFromType(member.DeclaringType, member);
                    propertyInfos[i] = declaredMember;
                }
            }

            return propertyInfos;
        }

        private static bool IsOverridenGenericMember(MemberInfo memberInfo, BindingFlags bindingAttr)
        {
            if (memberInfo.MemberType() != MemberTypes.Property)
            {
                return false;
            }

            PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
            if (!IsVirtual(propertyInfo))
            {
                return false;
            }

            Type declaringType = propertyInfo.DeclaringType;
            if (!declaringType.IsGenericType())
            {
                return false;
            }
            Type genericTypeDefinition = declaringType.GetGenericTypeDefinition();
            if (genericTypeDefinition == null)
            {
                return false;
            }
            MemberInfo[] members = genericTypeDefinition.GetMember(propertyInfo.Name, bindingAttr);
            if (members.Length == 0)
            {
                return false;
            }
            Type memberUnderlyingType = GetMemberUnderlyingType(members[0]);
            if (!memberUnderlyingType.IsGenericParameter)
            {
                return false;
            }

            return true;
        }

        public static List<MemberInfo> GetFieldsAndProperties(Type type, BindingFlags bindingAttr)
        {
            List<MemberInfo> targetMembers = new List<MemberInfo>();

            targetMembers.AddRange(GetFields(type, bindingAttr));
            targetMembers.AddRange(GetProperties(type, bindingAttr));

            // for some reason .NET returns multiple members when overriding a generic member on a base class
            // http://social.msdn.microsoft.com/Forums/en-US/b5abbfee-e292-4a64-8907-4e3f0fb90cd9/reflection-overriden-abstract-generic-properties?forum=netfxbcl
            // filter members to only return the override on the topmost class
            // update: I think this is fixed in .NET 3.5 SP1 - leave this in for now...
            List<MemberInfo> distinctMembers = new List<MemberInfo>(targetMembers.Count);

            foreach (IGrouping<string, MemberInfo> groupedMember in targetMembers.GroupBy(m => m.Name))
            {
                int count = groupedMember.Count();

                if (count == 1)
                {
                    distinctMembers.Add(groupedMember.First());
                }
                else
                {
                    List<MemberInfo> resolvedMembers = new List<MemberInfo>();
                    foreach (MemberInfo memberInfo in groupedMember)
                    {
                        // this is a bit hacky
                        // if the hiding property is hiding a base property and it is virtual
                        // then this ensures the derived property gets used
                        if (resolvedMembers.Count == 0)
                        {
                            resolvedMembers.Add(memberInfo);
                        }
                        else if (!IsOverridenGenericMember(memberInfo, bindingAttr) || memberInfo.Name == "Item")
                        {
                            // two members with the same name were declared on a type
                            // this can be done via IL emit, e.g. Moq
                            if (resolvedMembers.Any(m => m.DeclaringType == memberInfo.DeclaringType))
                            {
                                continue;
                            }

                            resolvedMembers.Add(memberInfo);
                        }
                    }

                    distinctMembers.AddRange(resolvedMembers);
                }
            }

            return distinctMembers;
        }

        /// <summary>
        /// Determines whether the property is an indexed property.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        /// 	<c>true</c> if the property is an indexed property; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsIndexedProperty(PropertyInfo property)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            return (property.GetIndexParameters().Length > 0);
        }

        public static bool IsByRefLikeType(Type type)
        {
            if (!type.IsValueType())
            {
                return false;
            }

            // IsByRefLike flag on type is not available in netstandard2.0
            Attribute[] attributes = GetAttributes(type, null, false);
            for (int i = 0; i < attributes.Length; i++)
            {
                if (string.Equals(attributes[i].GetType().FullName, "System.Runtime.CompilerServices.IsByRefLikeAttribute", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
