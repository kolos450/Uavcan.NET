using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uavcan.NET.IO.Can.Drivers;
using Uavcan.NET.Studio.Engine;

namespace Uavcan.NET.Studio
{
    [Export]
    sealed class UavcanService : IDisposable
    {
        [ImportingConstructor]
        public UavcanService(TypeResolvingService typeResolvingService)
        {
            Engine = new UavcanInstance(typeResolvingService);
        }

        public void AddDriver(ICanInterface driver)
        {
            Engine.AddDriver(driver);
        }

        public void Dispose()
        {
            if (Engine != null)
            {
                Engine.Dispose();
                Engine = null;
            }
        }

        public UavcanInstance Engine { get; private set; }
    }
}
