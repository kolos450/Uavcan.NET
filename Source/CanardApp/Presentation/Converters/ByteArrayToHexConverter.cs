using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CanardApp.Presentation.Converters
{
    sealed class ByteArrayToHexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var ba = value as byte[];
            if (ba == null)
                return null;
            return ToString(ba);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        static string ToString(byte[] bytes)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                if (i != 0)
                    sb.Append(' ');

                var s = bytes[i].ToString("X", CultureInfo.InvariantCulture);
                if (s.Length == 1)
                    sb.Append('0');
                sb.Append(s);
            }
            return sb.ToString();
        }
    }
}
