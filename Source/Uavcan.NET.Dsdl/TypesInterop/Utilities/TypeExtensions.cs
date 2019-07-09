using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Uavcan.NET.Dsdl.TypesInterop.Utilities
{
    static class TypeExtensions
    {
        public static bool IsInterface(this Type type)
        {
            return type.IsInterface;
        }

        public static bool IsValueType(this Type type)
        {
            return type.IsValueType;
        }

        public static bool IsPrimitive(this Type type)
        {
            return type.IsPrimitive;
        }

        public static bool IsGenericType(this Type type)
        {
            return type.IsGenericType;
        }

        public static MemberTypes MemberType(this MemberInfo memberInfo)
        {
            return memberInfo.MemberType;
        }

        public static Type BaseType(this Type type)
        {
            return type.BaseType;
        }

        public static bool ImplementInterface(this Type type, Type interfaceType)
        {
            for (Type currentType = type; currentType != null; currentType = currentType.BaseType())
            {
                IEnumerable<Type> interfaces = currentType.GetInterfaces();
                foreach (Type i in interfaces)
                {
                    if (i == interfaceType || (i != null && i.ImplementInterface(interfaceType)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
