using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Uavcan.NET.Studio.Tools.BusMonitor.Presentation.Converters
{
    class CanIdToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var canId = ((CanId)value).Value;
            var mask = 0x1F;
            var priority = (canId >> 24) & mask;
            return new SolidColorBrush(Color.FromArgb(100, 0xFF, (byte)(0xFF - (mask - priority) * 6), 0xFF));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
