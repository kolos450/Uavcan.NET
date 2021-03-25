using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;

namespace Uavcan.NET.Studio.Communication
{
    [Export]
    public sealed class CommunicationServicesProvider : IDisposable
    {
        private bool _disposed;
        private NodeMonitor _monitor;

        [ImportingConstructor]
        internal CommunicationServicesProvider(UavcanService uavcanService)
        {
            if (uavcanService is null)
                throw new ArgumentNullException(nameof(uavcanService));

            _monitor = new NodeMonitor(uavcanService.Engine);
        }

        public INodeMonitor Monitor
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(CommunicationServicesProvider));
                return _monitor;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                if (_monitor is not null)
                {
                    _monitor.Dispose();
                    _monitor = null;
                }
            }
        }
    }
}
