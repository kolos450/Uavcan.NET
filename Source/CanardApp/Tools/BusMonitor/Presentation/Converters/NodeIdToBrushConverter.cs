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
    class NodeIdToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var nodeId = System.Convert.ToInt32(value);
            var (r, g, b) = ColorUtils.Map7BitToColor(nodeId);
            return new SolidColorBrush(Color.FromArgb(100, r, g, b));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
