using System.Windows;
using System.Windows.Data;

namespace Uavcan.NET.Studio.Diagnostics.Terminal
{
    public interface ITerminalEx : ITerminal
    {
        /// <summary>
        ///     The error color for the bound items.
        /// </summary>
        IValueConverter LineColorConverter { get; set; }

        /// <summary>
        ///     The individual line height for the bound items.
        /// </summary>
        int ItemHeight { get; set; }

        /// <summary>
        ///     The margin around the bound items.
        /// </summary>
        Thickness ItemsMargin { get; set; }
    }
}