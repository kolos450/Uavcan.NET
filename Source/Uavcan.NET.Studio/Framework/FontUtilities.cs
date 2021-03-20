using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Uavcan.NET.Studio.Framework
{
    static class FontUtilities
    {
        public static Font GetFont(string[] fontNames, float size)
        {
            foreach (var fontName in fontNames)
            {
                var font = new Font(fontName, size);
                if (font.Name == fontName)
                    return font;
            }

            throw new ArgumentException("Cannot get a font.", nameof(fontNames));
        }
    }
}
