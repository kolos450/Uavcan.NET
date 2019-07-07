using CanardSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace CanardApp.Tools.BusMonitor.Presentation.Converters
{
    class TransferIdToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var model = (FrameModel)value;
            if (model.Data.Length < 0)
                return null;

            // Making a rather haphazard hash using transfer ID and a part of CAN ID
            var x = (byte)(model.Data[model.Data.Length - 1] & 0b11111) | (((model.CanId.Value >> 16) & 0b1111) << 5);
            var red = ((x >> 6) & 0b111) * 25;
            var green = ((x >> 3) & 0b111) * 25;
            var blue = (x & 0b111) * 25;

            return new SolidColorBrush(Color.FromArgb(100, (byte)(0xFF - red), (byte)(0xFF - green), (byte)(0xFF - blue)));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
