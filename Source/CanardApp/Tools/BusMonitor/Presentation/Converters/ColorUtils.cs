using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CanardApp.Tools.BusMonitor.Presentation.Converters
{
    static class ColorUtils
    {
        public static (byte r, byte g, byte b) Map7BitToColor(int value)
        {
            value &= 0x7f;

            var red = ((value >> 5) & 0b11) * 48;         // 2 bits to red
            var green = ((value >> 2) & 0b111) * 12;     // 3 bits to green, because human eye is more sensitive in this wavelength
            var blue = (value & 0b11) * 48;              // 2 bits to blue

            return ((byte)(0xFF - red), (byte)(0xFF - green), (byte)(0xFF - blue));
        }
    }
}
