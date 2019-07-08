using CanardSharp.Dsdl.DataTypes;
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
    class DataTypeToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            var dtFullName = value.ToString();
            var colorHash = Encoding.ASCII.GetBytes(dtFullName).Sum(x => x) & 0xF7;
            var (r, g, b) = ColorUtils.Map7BitToColor(colorHash);
            return new SolidColorBrush(Color.FromArgb(100, r, g, b));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
