using CanardSharp.Dsdl.TypesInterop.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp.Dsdl.TypesInterop
{
    class ContractResolverEx : ContractResolver
    {
        public ContractResolverEx(IUavcanTypeResolver schemeResolver) : base(schemeResolver)
        {
        }

        protected override IContract CreateContract(Type objectType)
        {
            Type t = ReflectionUtils.EnsureNotByRefType(objectType);
            t = ReflectionUtils.EnsureNotNullableType(t);

            if (CollectionUtils.IsDictionaryType(t))
            {
                return CreateDictionaryContract(objectType);
            }

            return base.CreateContract(objectType);
        }

        /// <summary>
        /// Creates a <see cref="JsonDictionaryContract"/> for the given type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>A <see cref="JsonDictionaryContract"/> for the given type.</returns>
        protected virtual DictionaryContract CreateDictionaryContract(Type objectType)
        {
            DictionaryContract contract = new DictionaryContract(objectType);
            InitializeContract(contract);
            contract.DictionaryKeyResolver = ResolveDictionaryKey;
            return contract;
        }

        /// <summary>
        /// Resolves the key of the dictionary. By default <see cref="ResolvePropertyName"/> is used to resolve dictionary keys.
        /// </summary>
        /// <param name="dictionaryKey">Key of the dictionary.</param>
        /// <returns>Resolved key of the dictionary.</returns>
        protected virtual string ResolveDictionaryKey(string dictionaryKey)
        {
            return dictionaryKey;
        }
    }
}
