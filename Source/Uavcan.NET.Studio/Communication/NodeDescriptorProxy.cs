using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;

namespace Uavcan.NET.Studio.Communication
{
    sealed class NodeDescriptorProxy : INodeDescriptor
    {
        INodeDescriptor _descriptor;
        NodeMonitor _monitor;
        volatile bool _initialized;

        public NodeDescriptorProxy(NodeMonitor monitor, NodeHandle handle)
        {
            if (monitor is null)
                throw new ArgumentNullException(nameof(monitor));

            Handle = handle;

            if (monitor.TryGetRegisteredNodeDescriptor(Handle, out var descriptor))
            {
                _descriptor = descriptor;
                _initialized = true;
            }
            else
            {
                _descriptor = DefaultNodeDescriptor.Instance;
                _monitor = monitor;
                monitor.WaitForNode(handle, this, InitializeProxy);
            }
        }

        static void InitializeProxy(object context) =>
            ((NodeDescriptorProxy)context).InitializeProxy();

        private void InitializeProxy()
        {
            if (_initialized)
                return;

            if (!_monitor.TryGetRegisteredNodeDescriptor(Handle, out var descriptor))
                throw new InvalidOperationException();
            _monitor = null;

            var defaultDescriptor = _descriptor;
            _descriptor = descriptor;

            _initialized = true;
            Thread.MemoryBarrier();

            if (_propertyChangedProxy is not null)
            {
                foreach (var dlg in _propertyChangedProxy.GetInvocationList())
                    descriptor.PropertyChanged += (PropertyChangedEventHandler)dlg;

                if (defaultDescriptor.Registered != descriptor.Registered)
                    _propertyChangedProxy.Invoke(this, new PropertyChangedEventArgs(nameof(Registered)));
                if (defaultDescriptor.Updated != descriptor.Updated)
                    _propertyChangedProxy.Invoke(this, new PropertyChangedEventArgs(nameof(Updated)));
                if (defaultDescriptor.Info != descriptor.Info)
                    _propertyChangedProxy.Invoke(this, new PropertyChangedEventArgs(nameof(Info)));
                if (defaultDescriptor.Status != descriptor.Status)
                    _propertyChangedProxy.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));

                _propertyChangedProxy = null;
            }
        }

        public NodeHandle Handle { get; }

        public DateTimeOffset Registered => _descriptor.Registered;
        public DateTimeOffset Updated => _descriptor.Updated;
        public INodeInfo Info => _descriptor.Info;
        public INodeStatus Status => _descriptor.Status;

        event PropertyChangedEventHandler _propertyChangedProxy;

        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                if (_initialized)
                    _descriptor.PropertyChanged += value;
                else
                    _propertyChangedProxy += value;
            }
            remove
            {
                if (_initialized)
                    _descriptor.PropertyChanged -= value;
                else
                    _propertyChangedProxy -= value;
            }
        }

        sealed class DefaultNodeDescriptor : INodeDescriptor
        {
            public static DefaultNodeDescriptor Instance { get; } = new DefaultNodeDescriptor();
            public NodeHandle Handle => default;
            public DateTimeOffset Registered => default;
            public DateTimeOffset Updated => default;
            public INodeInfo Info => default;
            public INodeStatus Status => default;
            public event PropertyChangedEventHandler PropertyChanged;
        }
    }
}
