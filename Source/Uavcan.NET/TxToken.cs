using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Uavcan.NET.IO.Can;
using Uavcan.NET.IO.Can.Drivers;

namespace Uavcan.NET
{
    public class TxToken : IDisposable
    {
        IEnumerable<ICanInterface> _drivers;

        public TxToken(IEnumerable<ICanInterface> drivers)
        {
            _drivers = drivers ?? throw new ArgumentNullException(nameof(drivers));

            foreach (var driver in drivers)
            {
                driver.MessageTransmitted += Driver_MessageTransmitted;
            }
        }

        private void Driver_MessageTransmitted(object sender, CanMessageEventArgs e)
        {
            var items = _items;
            if (items?.Contains((e.Message, sender as ICanInterface)) == true)
            {
                _counter++;

                if (_counter == items.Count)
                {
                    Dispose();
                }
            }
        }

        int _counter = 0;
        HashSet<(CanFrame, ICanInterface)> _items = new();

        public void Add(CanFrame frame, ICanInterface driver)
        {
            var items = _items;
            if (items is null)
                throw new InvalidOperationException();
            items.Add((frame, driver));
        }

        public void Dispose()
        {
            _items = null;

            var drivers = _drivers;
            if (drivers is not null)
            {
                foreach (var driver in drivers)
                {
                    driver.MessageTransmitted -= Driver_MessageTransmitted;
                }

                _drivers = null;
            }

            if (_semaphore is not null)
            {
                _semaphore.Release();
                _semaphore.Dispose();
                _semaphore = null;
            }
        }

        SemaphoreSlim _semaphore = new(0, 1);

        public void Wait(CancellationToken cancellationToken = default)
        {
            if (_items?.Count > 0)
            {
                _semaphore?.Wait(cancellationToken);
            }
        }

        public void WaitAsync(CancellationToken cancellationToken = default)
        {
            if (_items?.Count > 0)
            {
                _semaphore?.WaitAsync(cancellationToken);
            }
        }
    }
}
