using System.ComponentModel.Composition;
using System.Windows;

namespace Uavcan.NET.Studio.Tools.BusMonitor
{
    [Export(typeof(IUavcanStudioToolProvider))]
    sealed class BusMonitorToolProvider : IUavcanStudioToolProvider
    {
        public string ToolTitle => "Bus monitor";

        public UIElement CreateUIElement(UavcanInstance uavcan)
        {
            return new BusMonitorControl(uavcan);
        }
    }
}
