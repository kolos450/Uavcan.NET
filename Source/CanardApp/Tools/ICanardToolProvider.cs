using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CanardApp.Tools
{
    public interface ICanardToolProvider
    {
        string ToolTitle { get; }

        UIElement GetUIElement();
    }
}
