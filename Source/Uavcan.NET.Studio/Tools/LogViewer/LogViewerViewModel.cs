using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Uavcan.NET.Dsdl;
using Uavcan.NET.Dsdl.DataTypes;
using Uavcan.NET.Studio.DataTypes.Protocol;
using Uavcan.NET.Studio.DataTypes.Protocol.Debug;

namespace Uavcan.NET.Studio.Tools.LogViewer
{
    sealed class LogViewerViewModel : ReactiveObject, IDisposable
    {
        readonly IDisposable _cleanUp;

        MessageType _logMessageType;
        MessageType _panicMessageType;

        readonly DsdlSerializer _serializer;

        readonly SourceList<LogViewerItemViewModel> _logItemsSource = new SourceList<LogViewerItemViewModel>();
        readonly ReadOnlyObservableCollection<LogViewerItemViewModel> _logItems;
        public ReadOnlyObservableCollection<LogViewerItemViewModel> LogItems => _logItems;

        public ReactiveCommand<Unit, Unit> AddFilter { get; }

        internal LogViewerViewModel(UavcanInstance uavcan, TableFilterSetViewModel filter)
        {
            _serializer = uavcan.Serializer;

            ResolveTypes(uavcan.TypeResolver);

            var messageReceived = Observable.FromEventPattern<EventHandler<TransferReceivedArgs>, TransferReceivedArgs>(
                handler => uavcan.MessageReceived += handler,
                handler => uavcan.MessageReceived -= handler);

            var logItemsFiller = messageReceived
                .Select(x => GetLogMessage(x))
                .Where(x => x != null)
                .Subscribe(m => _logItemsSource.Add(m));

            var filterObservable = filter.WhenValueChanged(t => t.Filter)
                .Select(BuildFilter);

            var sourceBuilder = _logItemsSource
                .Connect()
                .Filter(filterObservable)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _logItems)
                .Subscribe();

            AddFilter = ReactiveCommand.Create(() => filter.AddFilter());

            _cleanUp = new CompositeDisposable(logItemsFiller, sourceBuilder, filter);
        }

        static Func<LogViewerItemViewModel, bool> BuildFilter(Predicate<IEnumerable<string>> func)
        {
            if (func == null)
                return x => true;

            return x => func(GetStrings(x));
        }

        static IEnumerable<string> GetStrings(LogViewerItemViewModel x)
        {
            yield return x.NodeId.ToString();
            yield return x.Level.ToString();
            if (x.Source != null)
                yield return x.Source;
            yield return x.Text;
        }

        void ResolveTypes(IUavcanTypeResolver typeResolver)
        {
            _logMessageType = (MessageType)typeResolver.ResolveType("uavcan.protocol.debug", "LogMessage");
            _panicMessageType = (MessageType)typeResolver.ResolveType("uavcan.protocol", "Panic");
        }

        LogViewerItemViewModel GetLogMessage(EventPattern<TransferReceivedArgs> eventPattern)
        {
            var e = eventPattern.EventArgs;

            if (e.Type == _logMessageType)
            {
                var data = _serializer.Deserialize<LogMessage>(e.ContentBytes, 0, e.ContentBytes.Length);
                return new LogViewerItemViewModel(e.SourceNodeId, DateTime.Now, data);
            }
            else if (e.Type == _panicMessageType)
            {
                var data = _serializer.Deserialize<Panic>(e.ContentBytes, 0, e.ContentBytes.Length);
                return new LogViewerItemViewModel(e.SourceNodeId, DateTime.Now, data);
            }

            return null;
        }

        public void Dispose()
        {
            _cleanUp.Dispose();
        }
    }
}
