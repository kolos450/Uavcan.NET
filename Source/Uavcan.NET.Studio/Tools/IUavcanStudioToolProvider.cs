using System.Windows;

namespace Uavcan.NET.Studio.Tools
{
    public interface IUavcanStudioToolProvider
    {
        string ToolTitle { get; }

        UIElement CreateUIElement(UavcanInstance uavcan);
    }
}
