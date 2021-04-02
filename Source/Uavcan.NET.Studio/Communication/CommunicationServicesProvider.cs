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
        private HeartbeatService _heartbeatService;
        private ParametersService _parametersService;

        [ImportingConstructor]
        internal CommunicationServicesProvider(UavcanService uavcanService)
        {
            if (uavcanService is null)
                throw new ArgumentNullException(nameof(uavcanService));

            _monitor = new NodeMonitor(uavcanService.Engine);
            _heartbeatService = new HeartbeatService(uavcanService.Engine);
            _parametersService = new ParametersService(uavcanService.Engine);
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

        internal HeartbeatService HeartbeatService
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(CommunicationServicesProvider));
                return _heartbeatService;
            }
        }

        internal IParametersService ParametersService
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(CommunicationServicesProvider));
                return _parametersService;
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

                if (_heartbeatService is not null)
                {
                    _heartbeatService.Dispose();
                    _heartbeatService = null;
                }

                if (_parametersService is not null)
                {
                    _parametersService.Dispose();
                    _parametersService = null;
                }
            }
        }
    }
}
