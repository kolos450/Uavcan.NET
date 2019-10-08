using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Uavcan.NET.Studio.Presentation.Converters
{
    sealed class LogicalNotConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var v = (bool)value;
            return !v;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
