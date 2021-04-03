using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using Uavcan.NET.Studio.DataTypes.Protocol;

namespace Uavcan.NET.Studio.Communication
{
    sealed class HeartbeatService : IDisposable, IHeartbeatService
    {
        private UavcanInstance _uavcan;
        private Timer _timer;
        private readonly DateTimeOffset _initializedTime;

        public HeartbeatService(UavcanInstance uavcan)
        {
            _uavcan = uavcan ?? throw new ArgumentNullException(nameof(uavcan));

            _timer = new Timer
            {
                Interval = Interval.TotalMilliseconds,
                AutoReset = false,
            };

            _timer.Elapsed += Timer_Elapsed;

            _initializedTime = DateTimeOffset.Now;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var message = new NodeStatus
            {
                Health = Health,
                Mode = Mode,
                SubMode = SubMode,
                UptimeSec = (uint)(DateTimeOffset.Now - _initializedTime).TotalSeconds,
                VendorSpecificStatusCode = VendorSpecificStatusCode,
            };
            _uavcan.SendBroadcastMessage(message);

            if (_running)
            {
                _timer.Stop();
                _timer.Start();
            }
        }

        TimeSpan _interval = TimeSpan.FromMilliseconds(1175);
        public TimeSpan Interval
        {
            get => _interval;
            set
            {
                _timer.Interval = value.Milliseconds;
                _interval = value;
            }
        }

        public NodeHealth Health { get; set; } = NodeHealth.Ok;
        public NodeMode Mode { get; set; } = NodeMode.Operational;
        public byte SubMode { get; set; }
        public ushort VendorSpecificStatusCode { get; set; }

        volatile bool _running;

        public void Start()
        {
            _running = true;
            _timer.Start();
        }

        public void Stop()
        {
            _running = false;
            _timer.Stop();
        }

        public void Dispose()
        {
            if (_timer is not null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }
    }
}
