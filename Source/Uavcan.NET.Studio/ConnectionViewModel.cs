using DynamicData;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Uavcan.NET.Drivers;

namespace Uavcan.NET.Studio
{
    sealed class ConnectionViewModel : ReactiveObject, IDisposable
    {
        readonly IDisposable _cleanUp;
        readonly ChangesetHelper<ICanDriverPort> _changesetHelper;

        [ImportMany]
        IEnumerable<ICanDriverPortProvider> _driverProviders = null;

        public ConnectionViewModel(ICompositionService compositionService)
        {
            compositionService.SatisfyImportsOnce(this);

            _changesetHelper = new ChangesetHelper<ICanDriverPort>();

            var interfacesBindingDisposable = Observable.Timer(
                    TimeSpan.Zero,
                    TimeSpan.FromMilliseconds(500),
                    RxApp.MainThreadScheduler)
                .Select(_ => _changesetHelper.ApplyChanges(GetPorts()))
                .Bind(out _interfaces)
                .Subscribe(changeSet =>
                {
                    if (Interface != null && changeSet.Removes(Interface))
                        Interface = null;
                    if (Interface == null && changeSet.Adds > 0)
                        Interface = changeSet.GetFirstAddedItemOrDefault();
                });

            Connect = ReactiveCommand.Create<Window>(window =>
            {
                if (Interface == null)
                    return;
                if (BitRate == null)
                    return;

                window.DialogResult = true;
                window.Close();
            });

            _cleanUp = new CompositeDisposable(interfacesBindingDisposable);
        }

        IEnumerable<ICanDriverPort> GetPorts()
        {
            return _driverProviders
                .SelectMany(x => x.GetDriverPorts())
                .ToList();
        }

        readonly ReadOnlyObservableCollection<ICanDriverPort> _interfaces;
        public ReadOnlyObservableCollection<ICanDriverPort> Interfaces => _interfaces;

        bool _busy;
        public bool Busy { get => _busy; set => this.RaiseAndSetIfChanged(ref _busy, value); }

        ICanDriverPort _interface;
        public ICanDriverPort Interface { get => _interface; set => this.RaiseAndSetIfChanged(ref _interface, value); }

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

            public IChangeSet<T> ApplyChanges(IEnumerable<T> items)
            {
                var removed = _items.Except(items, _equalityComparer)
                    .Select(x => new Change<T>(ListChangeReason.Remove, x));
                var added = items.Except(_items, _equalityComparer)
                    .Select(x => new Change<T>(ListChangeReason.Add, x));
                _items = items;
                return new ChangeSet<T>(removed.Concat(added));
            }
        }
    }
}
