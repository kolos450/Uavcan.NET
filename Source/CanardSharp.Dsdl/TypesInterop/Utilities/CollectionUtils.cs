﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CanardSharp.Dsdl.TypesInterop.Utilities
{
    static class CollectionUtils
    {
        /// <summary>
        /// Adds the elements of the specified collection to the specified generic <see cref="IList{T}"/>.
        /// </summary>
        /// <param name="initial">The list to add to.</param>
        /// <param name="collection">The collection of elements to add.</param>
        public static void AddRange<T>(this IList<T> initial, IEnumerable<T> collection)
        {
            if (initial == null)
            {
                throw new ArgumentNullException(nameof(initial));
            }

            if (collection == null)
            {
                return;
            }

            foreach (T value in collection)
            {
                initial.Add(value);
            }
        }

        public static int IndexOf<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
        {
            int index = 0;
            foreach (T value in collection)
            {
                if (predicate(value))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        public static bool IsDictionaryType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                return true;
            }
            if (ReflectionUtils.ImplementsGenericDefinition(type, typeof(IDictionary<,>)))
            {
                return true;
            }

            if (ReflectionUtils.ImplementsGenericDefinition(type, typeof(IReadOnlyDictionary<,>)))
            {
                return true;
            }

            return false;
        }

        public static ConstructorInfo ResolveEnumerableCollectionConstructor(Type collectionType, Type collectionItemType)
        {
            Type genericConstructorArgument = typeof(IList<>).MakeGenericType(collectionItemType);

            return ResolveEnumerableCollectionConstructor(collectionType, collectionItemType, genericConstructorArgument);
        }

        public static ConstructorInfo ResolveEnumerableCollectionConstructor(Type collectionType, Type collectionItemType, Type constructorArgumentType)
        {
            Type genericEnumerable = typeof(IEnumerable<>).MakeGenericType(collectionItemType);
            ConstructorInfo match = null;

            foreach (ConstructorInfo constructor in collectionType.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                IList<ParameterInfo> parameters = constructor.GetParameters();

                if (parameters.Count == 1)
                {
                    Type parameterType = parameters[0].ParameterType;

                    if (genericEnumerable == parameterType)
                    {
                        // exact match
                        match = constructor;
                        break;
                    }

                    // in case we can't find an exact match, use first inexact
                    if (match == null)
                    {
                        if (parameterType.IsAssignableFrom(constructorArgumentType))
                        {
                            match = constructor;
                        }
                    }
                }
            }

            return match;
        }
    }
}
