using System;
using System.Collections.Generic;
using System.Text;
using Uavcan.NET.Dsdl.DataTypes;
using Uavcan.NET.Dsdl.TypesInterop.Utilities;

namespace Uavcan.NET.Dsdl.TypesInterop
{
    public abstract class IContract
    {
        internal bool IsNullable;
        internal bool IsConvertable;
        internal bool IsEnum;
        internal Type NonNullableUnderlyingType;
        internal bool IsReadOnlyOrFixedSize;
        internal bool IsSealed;
        internal bool IsInstantiable;

        private Type _createdType;

        /// <summary>
        /// Gets the underlying type for the contract.
        /// </summary>
        /// <value>The underlying type for the contract.</value>
        public Type UnderlyingType { get; }

        /// <summary>
        /// Gets or sets the type created during deserialization.
        /// </summary>
        /// <value>The type created during deserialization.</value>
        public Type CreatedType
        {
            get => _createdType;
            set
            {
                _createdType = value;

                IsSealed = _createdType.IsSealed;
                IsInstantiable = !(_createdType.IsInterface || _createdType.IsAbstract);
            }
        }

        /// <summary>
        /// Gets or sets whether this type contract is serialized as a reference.
        /// </summary>
        /// <value>Whether this type contract is serialized as a reference.</value>
        public bool? IsReference { get; set; }

        /// <summary>
        /// Gets or sets the default creator method used to create the object.
        /// </summary>
        /// <value>The default creator method used to create the object.</value>
        public Func<object> DefaultCreator { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the default creator is non-public.
        /// </summary>
        /// <value><c>true</c> if the default object creator is non-public; otherwise, <c>false</c>.</value>
        public bool DefaultCreatorNonPublic { get; set; }

        public DsdlType DsdlType { get; set; }
        public IUavcanType UavcanType { get; set; }

        internal IContract(Type underlyingType)
        {
            if (underlyingType == null)
                throw new ArgumentNullException(nameof(underlyingType));

            UnderlyingType = underlyingType;

            // resolve ByRef types
            // typically comes from in and ref parameters on methods/ctors
            underlyingType = ReflectionUtils.EnsureNotByRefType(underlyingType);

            IsNullable = ReflectionUtils.IsNullable(underlyingType);

            NonNullableUnderlyingType = (IsNullable && ReflectionUtils.IsNullableType(underlyingType)) ? Nullable.GetUnderlyingType(underlyingType) : underlyingType;

            CreatedType = NonNullableUnderlyingType;

            IsConvertable = ConvertUtils.IsConvertible(NonNullableUnderlyingType);
            IsEnum = NonNullableUnderlyingType.IsEnum;
        }
    }
}
