using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Uavcan.NET.Studio.Framework
{
    /// <summary>
    /// A MEF export attribute that defines an export of type <see cref="FrameworkElement"/> with
    /// <see cref="ViewModelType"/> metadata.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    [SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments",
        Justification = "Store string rather than Type as metadata")]
    public sealed class ExportViewForAttribute : ExportAttribute
    {
        public ExportViewForAttribute(Type viewModelType)
            : base(typeof(FrameworkElement))
        {
            ViewModelType = viewModelType.FullName;
        }

        public string ViewModelType { get; }
    }
}
