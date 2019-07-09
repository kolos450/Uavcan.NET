using System;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace Uavcan.NET.Studio.Presentation.Converters
{
    sealed class ByteArrayToHexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var ba = value as byte[];
            if (ba == null)
                return null;
            return ToString(ba, true);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public static string ToString(byte[] bytes, bool addSpaces = false)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                if (i != 0 && addSpaces)
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
