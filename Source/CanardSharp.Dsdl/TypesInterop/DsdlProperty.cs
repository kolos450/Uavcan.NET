using CanardSharp.Dsdl.DataTypes;
using CanardSharp.Dsdl.TypesInterop.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp.Dsdl.TypesInterop
{
    /// <summary>
    /// Maps a JSON property to a .NET member or constructor parameter.
    /// </summary>
    public class DsdlProperty
    {
        internal bool _hasExplicitDefaultValue;

        private object _defaultValue;
        private bool _hasGeneratedDefaultValue;
        private string _propertyName;
        private Type _propertyType;

        // use to cache contract during deserialization
        internal IContract PropertyContract { get; set; }

        /// <summary>
        /// Gets or sets the name of the property.
        /// </summary>
        /// <value>The name of the property.</value>
        public string PropertyName
        {
            get => _propertyName;
            set
            {
                _propertyName = value;
            }
        }

        /// <summary>
        /// Gets or sets the type that declared this property.
        /// </summary>
        /// <value>The type that declared this property.</value>
        public Type DeclaringType { get; set; }

        /// <summary>
        /// Gets or sets the order of serialization of a member.
        /// </summary>
        /// <value>The numeric order of serialization.</value>
        public int? Order { get; set; }

        /// <summary>
        /// Gets or sets the name of the underlying member or parameter.
        /// </summary>
        /// <value>The name of the underlying member or parameter.</value>
        public string UnderlyingName { get; set; }

        /// <summary>
        /// Gets the <see cref="IValueProvider"/> that will get and set the <see cref="JsonProperty"/> during serialization.
        /// </summary>
        /// <value>The <see cref="IValueProvider"/> that will get and set the <see cref="JsonProperty"/> during serialization.</value>
        public IValueProvider ValueProvider { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IAttributeProvider"/> for this property.
        /// </summary>
        /// <value>The <see cref="IAttributeProvider"/> for this property.</value>
        public IAttributeProvider AttributeProvider { get; set; }

        /// <summary>
        /// Gets or sets the type of the property.
        /// </summary>
        /// <value>The type of the property.</value>
        public Type PropertyType
        {
            get => _propertyType;
            set
            {
                if (_propertyType != value)
                {
                    _propertyType = value;
                    _hasGeneratedDefaultValue = false;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="JsonProperty"/> is ignored.
        /// </summary>
        /// <value><c>true</c> if ignored; otherwise, <c>false</c>.</value>
        public bool Ignored { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="JsonProperty"/> is readable.
        /// </summary>
        /// <value><c>true</c> if readable; otherwise, <c>false</c>.</value>
        public bool Readable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="JsonProperty"/> is writable.
        /// </summary>
        /// <value><c>true</c> if writable; otherwise, <c>false</c>.</value>
        public bool Writable { get; set; }

        /// <summary>
        /// Gets the default value.
        /// </summary>
        /// <value>The default value.</value>
        public object DefaultValue
        {
            get
            {
                if (!_hasExplicitDefaultValue)
                {
                    return null;
                }

                return _defaultValue;
            }
            set
            {
                _hasExplicitDefaultValue = true;
                _defaultValue = value;
            }
        }

        internal object GetResolvedDefaultValue()
        {
            if (_propertyType == null)
            {
                return null;
            }

            if (!_hasExplicitDefaultValue && !_hasGeneratedDefaultValue)
            {
                _defaultValue = ReflectionUtils.GetDefaultValue(PropertyType);
                _hasGeneratedDefaultValue = true;
            }

            return _defaultValue;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this property preserves object references.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is reference; otherwise, <c>false</c>.
        /// </value>
        public bool? IsReference { get; set; }

        /// <summary>
        /// Returns a <see cref="String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return PropertyName;
        }

        /// <summary>
        /// Gets or sets whether this property's collection items are serialized as a reference.
        /// </summary>
        /// <value>Whether this property's collection items are serialized as a reference.</value>
        public bool? ItemIsReference { get; set; }
        public DsdlType DsdlType { get; set; }
    }
}
