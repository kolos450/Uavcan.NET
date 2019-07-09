﻿using Uavcan.NET.Dsdl.TypesInterop.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET.Dsdl.TypesInterop
{
    /// <summary>
    /// A collection of <see cref="DsdlProperty"/> objects.
    /// </summary>
    public class DsdlPropertyCollection : KeyedCollection<string, DsdlProperty>
    {
        private readonly Type _type;
        private readonly List<DsdlProperty> _list;

        /// <summary>
        /// Initializes a new instance of the <see cref="DsdlPropertyCollection"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        public DsdlPropertyCollection(Type type)
            : base(StringComparer.Ordinal)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            _type = type;

            // foreach over List<T> to avoid boxing the Enumerator
            _list = (List<DsdlProperty>)Items;
        }

        /// <summary>
        /// When implemented in a derived class, extracts the key from the specified element.
        /// </summary>
        /// <param name="item">The element from which to extract the key.</param>
        /// <returns>The key for the specified element.</returns>
        protected override string GetKeyForItem(DsdlProperty item)
        {
            return item.PropertyName;
        }

        /// <summary>
        /// Adds a <see cref="DsdlProperty"/> object.
        /// </summary>
        /// <param name="property">The property to add to the collection.</param>
        public void AddProperty(DsdlProperty property)
        {
            if (Contains(property.PropertyName))
            {
                // don't overwrite existing property with ignored property
                if (property.Ignored)
                {
                    return;
                }

                DsdlProperty existingProperty = this[property.PropertyName];
                bool duplicateProperty = true;

                if (existingProperty.Ignored)
                {
                    // remove ignored property so it can be replaced in collection
                    Remove(existingProperty);
                    duplicateProperty = false;
                }
                else
                {
                    if (property.DeclaringType != null && existingProperty.DeclaringType != null)
                    {
                        if (property.DeclaringType.IsSubclassOf(existingProperty.DeclaringType)
                            || (existingProperty.DeclaringType.IsInterface && property.DeclaringType.ImplementInterface(existingProperty.DeclaringType)))
                        {
                            // current property is on a derived class and hides the existing
                            Remove(existingProperty);
                            duplicateProperty = false;
                        }
                        if (existingProperty.DeclaringType.IsSubclassOf(property.DeclaringType)
                            || (property.DeclaringType.IsInterface && existingProperty.DeclaringType.ImplementInterface(property.DeclaringType)))
                        {
                            // current property is hidden by the existing so don't add it
                            return;
                        }

                        if (_type.ImplementInterface(existingProperty.DeclaringType) && _type.ImplementInterface(property.DeclaringType))
                        {
                            // current property was already defined on another interface
                            return;
                        }
                    }
                }

                if (duplicateProperty)
                {
                    throw new SerializationException("A member with the name '{0}' already exists on '{1}'. Use the DsdlPropertyAttribute to specify another name.".FormatWith(CultureInfo.InvariantCulture, property.PropertyName, _type));
                }
            }

            Add(property);
        }

        /// <summary>
        /// Gets the closest matching <see cref="DsdlProperty"/> object.
        /// First attempts to get an exact case match of <paramref name="propertyName"/> and then
        /// a case insensitive match.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>A matching property if found.</returns>
        public DsdlProperty GetClosestMatchProperty(string propertyName)
        {
            DsdlProperty property = GetProperty(propertyName, StringComparison.Ordinal);
            if (property == null)
            {
                property = GetProperty(propertyName, StringComparison.OrdinalIgnoreCase);
            }

            return property;
        }

        private bool TryGetValue(string key, out DsdlProperty item)
        {
            if (Dictionary == null)
            {
                item = default;
                return false;
            }

            return Dictionary.TryGetValue(key, out item);
        }

        /// <summary>
        /// Gets a property by property name.
        /// </summary>
        /// <param name="propertyName">The name of the property to get.</param>
        /// <param name="comparisonType">Type property name string comparison.</param>
        /// <returns>A matching property if found.</returns>
        public DsdlProperty GetProperty(string propertyName, StringComparison comparisonType)
        {
            // KeyedCollection has an ordinal comparer
            if (comparisonType == StringComparison.Ordinal)
            {
                if (TryGetValue(propertyName, out DsdlProperty property))
                {
                    return property;
                }

                return null;
            }

            for (int i = 0; i < _list.Count; i++)
            {
                DsdlProperty property = _list[i];
                if (string.Equals(propertyName, property.PropertyName, comparisonType))
                {
                    return property;
                }
            }

            return null;
        }
    }
}
