﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardApp.Tools.BusMonitor
{
    [Export(typeof(ICanardToolProvider))]
    sealed class BusMonitorToolProvider : ICanardToolProvider
    {
        public string ToolTitle => "Bus monitor";

        public void Execute()
        {
        }
    }
}
