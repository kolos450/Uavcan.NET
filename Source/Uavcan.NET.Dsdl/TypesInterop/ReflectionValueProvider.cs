using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Uavcan.NET.Dsdl.TypesInterop.Utilities;

namespace Uavcan.NET.Dsdl.TypesInterop
{
    /// <summary>
    /// Get and set values for a <see cref="MemberInfo"/> using reflection.
    /// </summary>
    public class ReflectionValueProvider : IValueProvider
    {
        private readonly MemberInfo _memberInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReflectionValueProvider"/> class.
        /// </summary>
        /// <param name="memberInfo">The member info.</param>
        public ReflectionValueProvider(MemberInfo memberInfo)
        {
            if (memberInfo == null)
                throw new ArgumentNullException(nameof(memberInfo));
            _memberInfo = memberInfo;
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="target">The target to set the value on.</param>
        /// <param name="value">The value to set on the target.</param>
        public void SetValue(object target, object value)
        {
            try
            {
                ReflectionUtils.SetMemberValue(_memberInfo, target, value);
            }
            catch (Exception ex)
            {
                throw new SerializationException("Error setting value to '{0}' on '{1}'.".FormatWith(CultureInfo.InvariantCulture, _memberInfo.Name, target.GetType()), ex);
            }
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="target">The target to get the value from.</param>
        /// <returns>The value.</returns>
        public object GetValue(object target)
        {
            try
            {
                // https://github.com/dotnet/corefx/issues/26053
                if (_memberInfo is PropertyInfo propertyInfo && propertyInfo.PropertyType.IsByRef)
                {
                    throw new InvalidOperationException("Could not create getter for {0}. ByRef return values are not supported.".FormatWith(CultureInfo.InvariantCulture, propertyInfo));
                }

                return ReflectionUtils.GetMemberValue(_memberInfo, target);
            }
            catch (Exception ex)
            {
                throw new SerializationException("Error getting value from '{0}' on '{1}'.".FormatWith(CultureInfo.InvariantCulture, _memberInfo.Name, target.GetType()), ex);
            }
        }
    }
}
