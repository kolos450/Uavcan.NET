using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Uavcan.NET.Studio.Tools.EfeDebugger
{
    [Export(typeof(IUavcanStudioToolProvider))]
    sealed class EfeMemoryToolProvider : IUavcanStudioToolProvider
    {
        public string ToolTitle => "EFE Memory";

        public UIElement CreateUIElement(UavcanInstance uavcan)
        {
            return new EfeMemoryControl(uavcan);
        }
    }
}
