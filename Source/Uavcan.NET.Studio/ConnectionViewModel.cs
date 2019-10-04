using DynamicData;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Uavcan.NET.Studio
{
    sealed class ConnectionViewModel : ReactiveObject, IDisposable
    {
        readonly IDisposable _cleanUp;
        readonly ChangesetHelper<string> _changesetHelper;

        public ConnectionViewModel()
        {
            _changesetHelper = new ChangesetHelper<string>(
                SerialPort.GetPortNames(),
                StringComparer.Ordinal);

            var bindingDisposable = Observable.Interval(TimeSpan.FromMilliseconds(500))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(_ => _changesetHelper.GetChanges(SerialPort.GetPortNames()))
                .Bind(out _interfaces)
                .Subscribe();

            Connect = ReactiveCommand.Create<Window>(window =>
            {
                var portName = InterfaceName as string;
                if (string.IsNullOrEmpty(portName))
                    return;

                if (BitRate == null)
                    return;

                window.DialogResult = true;
                window.Close();
            });

            _cleanUp = new CompositeDisposable(bindingDisposable);
        }

        readonly ReadOnlyObservableCollection<string> _interfaces;
        public ReadOnlyObservableCollection<string> Interfaces => _interfaces;

        bool _busy;
        public bool Busy { get => _busy; set => this.RaiseAndSetIfChanged(ref _busy, value); }

        string _interfaceName;
        public string InterfaceName { get => _interfaceName; set => this.RaiseAndSetIfChanged(ref _interfaceName, value); }

        int? _bitRate;
        public int? BitRate { get => _bitRate; set => this.RaiseAndSetIfChanged(ref _bitRate, value); }

        public ReactiveCommand<Window, Unit> Connect { get; }

        public void Dispose()
        {
            if (_cleanUp != null)
                _cleanUp.Dispose();
        }

        sealed class ChangesetHelper<T>
        {
            IEnumerable<T> _items;
            IEqualityComparer<T> _equalityComparer;

            public ChangesetHelper(IEqualityComparer<T> equalityComparer = null)
                : this(Enumerable.Empty<T>(), equalityComparer)
            { }

            public ChangesetHelper(IEnumerable<T> items, IEqualityComparer<T> equalityComparer = null)
            {
                if (items == null)
                    throw new ArgumentNullException(nameof(items));
                _items = items;

                if (equalityComparer == null)
                    equalityComparer = EqualityComparer<T>.Default;
                _equalityComparer = equalityComparer;
            }

            public IChangeSet<T> GetChanges(IEnumerable<T> items)
            {
                var removed = _items.Except(items, _equalityComparer)
                    .Select(x => new Change<T>(ListChangeReason.Remove, x));
                var added = items.Except(_items, _equalityComparer)
                    .Select(x => new Change<T>(ListChangeReason.Add, x));
                return new ChangeSet<T>(removed.Concat(added));
            }
        }
    }
}
