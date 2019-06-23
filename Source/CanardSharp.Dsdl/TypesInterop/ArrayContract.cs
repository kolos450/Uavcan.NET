using CanardSharp.Dsdl.TypesInterop.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp.Dsdl.TypesInterop
{
    /// <summary>
    /// Contract details for a <see cref="System.Type"/> used by the <see cref="JsonSerializer"/>.
    /// </summary>
    public class ArrayContract : ContainerContract
    {
        /// <summary>
        /// Gets the <see cref="System.Type"/> of the collection items.
        /// </summary>
        /// <value>The <see cref="System.Type"/> of the collection items.</value>
        public Type CollectionItemType { get; }

        /// <summary>
        /// Gets a value indicating whether the collection type is a multidimensional array.
        /// </summary>
        /// <value><c>true</c> if the collection type is a multidimensional array; otherwise, <c>false</c>.</value>
        public bool IsMultidimensionalArray { get; }

        private readonly Type _genericCollectionDefinitionType;

        private Type _genericWrapperType;
        private ObjectConstructor<object> _genericWrapperCreator;
        private Func<object> _genericTemporaryCollectionCreator;

        internal bool IsArray { get; }
        internal bool ShouldCreateWrapper { get; }
        internal bool CanDeserialize { get; private set; }

        private readonly ConstructorInfo _parameterizedConstructor;

        private ObjectConstructor<object> _parameterizedCreator;
        private ObjectConstructor<object> _overrideCreator;

        internal ObjectConstructor<object> ParameterizedCreator
        {
            get
            {
                if (_parameterizedCreator == null)
                {
                    _parameterizedCreator = TypeReflector.ReflectionDelegateFactory.CreateParameterizedConstructor(_parameterizedConstructor);
                }

                return _parameterizedCreator;
            }
        }

        /// <summary>
        /// Gets or sets the function used to create the object. When set this function will override <see cref="JsonContract.DefaultCreator"/>.
        /// </summary>
        /// <value>The function used to create the object.</value>
        public ObjectConstructor<object> OverrideCreator
        {
            get => _overrideCreator;
            set
            {
                _overrideCreator = value;
                // hacky
                CanDeserialize = true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the creator has a parameter with the collection values.
        /// </summary>
        /// <value><c>true</c> if the creator has a parameter with the collection values; otherwise, <c>false</c>.</value>
        public bool HasParameterizedCreator { get; set; }

        internal bool HasParameterizedCreatorInternal => (HasParameterizedCreator || _parameterizedCreator != null || _parameterizedConstructor != null);

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonArrayContract"/> class.
        /// </summary>
        /// <param name="underlyingType">The underlying type for the contract.</param>
        public ArrayContract(Type underlyingType)
            : base(underlyingType)
        {
            IsArray = CreatedType.IsArray;

            bool canDeserialize;

            Type tempCollectionType;
            if (IsArray)
            {
                CollectionItemType = ReflectionUtils.GetCollectionItemType(UnderlyingType);
                IsReadOnlyOrFixedSize = true;
                _genericCollectionDefinitionType = typeof(List<>).MakeGenericType(CollectionItemType);

                canDeserialize = true;
                IsMultidimensionalArray = (IsArray && UnderlyingType.GetArrayRank() > 1);
            }
            else if (typeof(IList).IsAssignableFrom(NonNullableUnderlyingType))
            {
                if (ReflectionUtils.ImplementsGenericDefinition(NonNullableUnderlyingType, typeof(ICollection<>), out _genericCollectionDefinitionType))
                {
                    CollectionItemType = _genericCollectionDefinitionType.GetGenericArguments()[0];
                }
                else
                {
                    CollectionItemType = ReflectionUtils.GetCollectionItemType(NonNullableUnderlyingType);
                }

                if (NonNullableUnderlyingType == typeof(IList))
                {
                    CreatedType = typeof(List<object>);
                }

                if (CollectionItemType != null)
                {
                    _parameterizedConstructor = CollectionUtils.ResolveEnumerableCollectionConstructor(NonNullableUnderlyingType, CollectionItemType);
                }

                IsReadOnlyOrFixedSize = ReflectionUtils.InheritsGenericDefinition(NonNullableUnderlyingType, typeof(ReadOnlyCollection<>));
                canDeserialize = true;
            }
            else if (ReflectionUtils.ImplementsGenericDefinition(NonNullableUnderlyingType, typeof(ICollection<>), out _genericCollectionDefinitionType))
            {
                CollectionItemType = _genericCollectionDefinitionType.GetGenericArguments()[0];

                if (ReflectionUtils.IsGenericDefinition(NonNullableUnderlyingType, typeof(ICollection<>))
                    || ReflectionUtils.IsGenericDefinition(NonNullableUnderlyingType, typeof(IList<>)))
                {
                    CreatedType = typeof(List<>).MakeGenericType(CollectionItemType);
                }

                if (ReflectionUtils.IsGenericDefinition(NonNullableUnderlyingType, typeof(ISet<>)))
                {
                    CreatedType = typeof(HashSet<>).MakeGenericType(CollectionItemType);
                }

                _parameterizedConstructor = CollectionUtils.ResolveEnumerableCollectionConstructor(NonNullableUnderlyingType, CollectionItemType);
                canDeserialize = true;
                ShouldCreateWrapper = true;
            }
            else if (ReflectionUtils.ImplementsGenericDefinition(NonNullableUnderlyingType, typeof(IReadOnlyCollection<>), out tempCollectionType))
            {
                CollectionItemType = tempCollectionType.GetGenericArguments()[0];

                if (ReflectionUtils.IsGenericDefinition(NonNullableUnderlyingType, typeof(IReadOnlyCollection<>))
                    || ReflectionUtils.IsGenericDefinition(NonNullableUnderlyingType, typeof(IReadOnlyList<>)))
                {
                    CreatedType = typeof(ReadOnlyCollection<>).MakeGenericType(CollectionItemType);
                }

                _genericCollectionDefinitionType = typeof(List<>).MakeGenericType(CollectionItemType);
                _parameterizedConstructor = CollectionUtils.ResolveEnumerableCollectionConstructor(CreatedType, CollectionItemType);

                IsReadOnlyOrFixedSize = true;
                canDeserialize = HasParameterizedCreatorInternal;
            }
            else if (ReflectionUtils.ImplementsGenericDefinition(NonNullableUnderlyingType, typeof(IEnumerable<>), out tempCollectionType))
            {
                CollectionItemType = tempCollectionType.GetGenericArguments()[0];

                if (ReflectionUtils.IsGenericDefinition(UnderlyingType, typeof(IEnumerable<>)))
                {
                    CreatedType = typeof(List<>).MakeGenericType(CollectionItemType);
                }

                _parameterizedConstructor = CollectionUtils.ResolveEnumerableCollectionConstructor(NonNullableUnderlyingType, CollectionItemType);

                if (NonNullableUnderlyingType.IsGenericType() && NonNullableUnderlyingType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    _genericCollectionDefinitionType = tempCollectionType;

                    IsReadOnlyOrFixedSize = false;
                    ShouldCreateWrapper = false;
                    canDeserialize = true;
                }
                else
                {
                    _genericCollectionDefinitionType = typeof(List<>).MakeGenericType(CollectionItemType);

                    IsReadOnlyOrFixedSize = true;
                    ShouldCreateWrapper = true;
                    canDeserialize = HasParameterizedCreatorInternal;
                }
            }
            else
            {
                // types that implement IEnumerable and nothing else
                canDeserialize = false;
                ShouldCreateWrapper = true;
            }

            CanDeserialize = canDeserialize;
        }

        internal IWrappedCollection CreateWrapper(object list)
        {
            if (_genericWrapperCreator == null)
            {
                _genericWrapperType = typeof(CollectionWrapper<>).MakeGenericType(CollectionItemType);

                Type constructorArgument;

                if (ReflectionUtils.InheritsGenericDefinition(_genericCollectionDefinitionType, typeof(List<>))
                    || _genericCollectionDefinitionType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    constructorArgument = typeof(ICollection<>).MakeGenericType(CollectionItemType);
                }
                else
                {
                    constructorArgument = _genericCollectionDefinitionType;
                }

                ConstructorInfo genericWrapperConstructor = _genericWrapperType.GetConstructor(new[] { constructorArgument });
                _genericWrapperCreator = TypeReflector.ReflectionDelegateFactory.CreateParameterizedConstructor(genericWrapperConstructor);
            }

            return (IWrappedCollection)_genericWrapperCreator(list);
        }

        internal IList CreateTemporaryCollection()
        {
            if (_genericTemporaryCollectionCreator == null)
            {
                // multidimensional array will also have array instances in it
                Type collectionItemType = (IsMultidimensionalArray || CollectionItemType == null)
                    ? typeof(object)
                    : CollectionItemType;

                Type temporaryListType = typeof(List<>).MakeGenericType(collectionItemType);
                _genericTemporaryCollectionCreator = TypeReflector.ReflectionDelegateFactory.CreateDefaultConstructor<object>(temporaryListType);
            }

            return (IList)_genericTemporaryCollectionCreator();
        }
    }
}
