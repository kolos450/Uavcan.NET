using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CanardSharp.Dsdl.TypesInterop.Utilities
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
    }
}
