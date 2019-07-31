using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Uavcan.NET.Drivers;
using Uavcan.NET.Dsdl;
using Uavcan.NET.Studio.Presentation.Converters;
using Uavcan.NET.Studio.Tools.BusMonitor.Presentation;

namespace Uavcan.NET.Studio.Tools.BusMonitor
{
    sealed class BusMonitorViewModel : ReactiveObject, IDisposable
    {
        readonly IDisposable _cleanUp;

        readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        readonly IEnumerable<ICanDriver> _drivers;
        readonly CanFramesProcessor _framesProcessor;
        readonly IUavcanTypeResolver _typeResolver;

        readonly LinkedList<(CanFrame Frame, FrameViewModel Model)> _framesMap = new LinkedList<(CanFrame, FrameViewModel)>();

        readonly SourceList<FrameViewModel> _itemsSource = new SourceList<FrameViewModel>();

        readonly ReadOnlyObservableCollection<FrameViewModel> _items;
        public ReadOnlyObservableCollection<FrameViewModel> Items => _items;

        public ReactiveCommand<Unit, Unit> AddFilter { get; }

        volatile bool _enabled = true;

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public ReactiveCommand<Unit, Unit> ClearItems { get; }

        public BusMonitorViewModel(UavcanInstance uavcan, TableFilterSetViewModel filter)
        {
            _typeResolver = uavcan.TypeResolver;
            _framesProcessor = new CanFramesProcessor(ShouldAcceptTransfer);

            _drivers = uavcan.Drivers.ToList();

            var messageReceived = _drivers.Select(
                driver => Observable.FromEventPattern<EventHandler<CanMessageEventArgs>, CanMessageEventArgs>(
                    handler => driver.MessageReceived += handler,
                    handler => driver.MessageReceived -= handler))
                .Merge()
                .Select(x => (FrameDirection.Rx, x.EventArgs));

            var messageTransmitted = _drivers.Select(
                driver => Observable.FromEventPattern<EventHandler<CanMessageEventArgs>, CanMessageEventArgs>(
                    handler => driver.MessageTransmitted += handler,
                    handler => driver.MessageTransmitted -= handler))
                .Merge()
                .Select(x => (FrameDirection.Tx, x.EventArgs));

            var messages = new[] { messageReceived, messageTransmitted }
                .Merge();

            var logItemsFiller = messages
                .Where(x => _enabled)
                .Select(ProcessFrame)
                .Subscribe(_itemsSource.Add);

            var filterObservable = filter.WhenValueChanged(t => t.Filter)
                .Select(BuildFilter);

            var sourceBuilder = _itemsSource
                .Connect()
                .Filter(filterObservable, ListFilterPolicy.ClearAndReplace)
                .Reverse()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _items)
                .Subscribe();

            AddFilter = ReactiveCommand.Create(() => filter.AddFilter());

            ClearItems = ReactiveCommand.Create(() => _itemsSource.Clear());

            _cleanUp = new CompositeDisposable(logItemsFiller, sourceBuilder, filter);
        }

        FrameViewModel ProcessFrame((FrameDirection, CanMessageEventArgs) input)
        {
            var frame = input.Item2.Message;
            var bytes = GetData(frame);

            var canIdInfo = new CanIdInfo(frame.Id);

            var model = new FrameViewModel
            {
                Direction = input.Item1,
                CanId = frame.Id,
                Data = bytes,
                SourceNodeId = canIdInfo.SourceId,
                Time = DateTime.Now,
            };

            if (canIdInfo.IsServiceNotMessage)
                model.DestinationNodeId = canIdInfo.DestinationId;

            ProcessFrames(frame, model);

            return model;
        }

        enum ProcessFramesResult
        {
            UpdatedNone,
            UpdatedSingle,
            UpdatedMultiple,
        }

        ProcessFramesResult ProcessFrames(CanFrame frame, FrameViewModel model)
        {
            var nowUs = (ulong)_stopwatch.ElapsedMilliseconds * 1000;
            CanFramesProcessingResult processorResult;
            try
            {
                processorResult = _framesProcessor.HandleRxFrame(frame, nowUs);
            }
            catch (CanFramesProcessingException ex)
            {
                Apply(ex.SourceFrames, m => m.AccociatedFrameProcessorException = ex);

                return ProcessFramesResult.UpdatedSingle;
            }

            if (processorResult.Transfer != null)
            {
                Apply(processorResult.Transfer, model);

                if (processorResult.SourceFrames.Count > 1 &&
                    _framesMap.Count != 0)
                {
                    Apply(processorResult.SourceFrames, m => Apply(processorResult.Transfer, m));

                    return ProcessFramesResult.UpdatedMultiple;
                }

                return ProcessFramesResult.UpdatedSingle;
            }
            else
            {
                _framesMap.AddLast((frame, model));
                return ProcessFramesResult.UpdatedNone;
            }
        }

        void Apply(IEnumerable<CanFrame> frames, Action<FrameViewModel> action)
        {
            var hashset = new HashSet<CanFrame>(frames);
            var node = _framesMap.First;
            while (node != null)
            {
                var next = node.Next;
                if (hashset.Contains(node.Value.Frame))
                {
                    _framesMap.Remove(node);
                    action(node.Value.Model);
                }
                node = next;
            }
        }

        void Apply(UavcanRxTransfer transfer, FrameViewModel frame)
        {
            frame.AssociatedTransfer = transfer;

            var typeKind = transfer.TransferType == UavcanTransferType.Broadcast ?
                UavcanTypeKind.Message :
                UavcanTypeKind.Service;
            var type = _typeResolver.TryResolveType((int)transfer.DataTypeId, typeKind);
            if (type != null)
            {
                frame.DataType = type;
            }
        }

        static byte[] GetData(CanFrame msg)
        {
            if (msg.DataOffset == 0 && msg.Data.Length == msg.DataLength)
            {
                return msg.Data;
            }
            else
            {
                var bytes = new byte[msg.DataLength];
                Buffer.BlockCopy(msg.Data, msg.DataOffset, bytes, 0, bytes.Length);
                return bytes;
            }
        }

        static Func<FrameViewModel, bool> BuildFilter(Predicate<IEnumerable<string>> func)
        {
            if (func == null)
                return x => true;

            return x => func(EnumerateStrings(x));
        }

        static IEnumerable<string> EnumerateStrings(FrameViewModel item)
        {
            yield return Convert.ToString(item.CanId.Value, 16);
            if (item.SourceNodeId != null)
                yield return item.SourceNodeId.Value.ToString();
            if (item.DestinationNodeId != null)
                yield return item.DestinationNodeId.ToString();
            if (item.DataType != null)
                yield return item.DataType.ToString();
            if (item.Data != null)
            {
                yield return ByteArrayToHexConverter.ToString(item.Data);
                yield return ByteArrayToTextConverter.ToString(item.Data);
            }
        }

        public void Dispose()
        {
            if (_itemsSource != null)
                _itemsSource.Dispose();

            _cleanUp.Dispose();
        }

        bool ShouldAcceptTransfer(out ulong dataTypeSignature, uint dataTypeId, UavcanTransferType transferType, byte sourceNodeId, byte destinationNodeId)
        {
            dataTypeSignature = 0;

            var typeKind = transferType == UavcanTransferType.Broadcast ?
                UavcanTypeKind.Message :
                UavcanTypeKind.Service;
            var type = _typeResolver.TryResolveType((int)dataTypeId, typeKind);
            if (type == null)
                return false;

            var signature = type.GetDataTypeSignature();
            if (signature == null)
                return false;

            dataTypeSignature = signature.Value;

            return true;
        }
    }
}
