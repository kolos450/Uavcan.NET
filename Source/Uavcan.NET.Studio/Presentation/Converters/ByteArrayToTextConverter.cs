using System;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace Uavcan.NET.Studio.Presentation.Converters
{
    sealed class ByteArrayToTextConverter : IValueConverter
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

        public static string ToString(byte[] bytes)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < bytes.Length; i++)
            {
                var ch = bytes[i];

                if (IsPrintable(ch))
                    sb.Append((char)ch);
                else
                    sb.Append('.');
            }
            return sb.ToString();
        }

        static bool IsPrintable(byte c)
        {
            if (c >= 0x20 && c <= 0x7e)
                return true;
            return false;
        }
    }
}
