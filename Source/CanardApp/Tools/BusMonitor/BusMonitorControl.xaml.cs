using CanardApp.Presentation.Converters;
using CanardApp.Tools.BusMonitor.Presentation;
using CanardSharp;
using CanardSharp.Drivers;
using CanardSharp.Dsdl;
using CanardSharp.Dsdl.DataTypes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CanardApp.Tools.BusMonitor
{
    /// <summary>
    /// Interaction logic for BusMonitorControl.xaml
    /// </summary>
    public partial class BusMonitorControl : UserControl, IDisposable
    {
        readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        CanardInstance _canardInstance;
        CanFramesProcessor _framesProcessor;
        IUavcanTypeResolver _typeResolver;

        LinkedList<(CanFrame Frame, FrameModel Model)> _framesMap = new LinkedList<(CanFrame, FrameModel)>();

        volatile bool _enabled = true;

        public BusMonitorControl(CanardInstance canardInstance)
        {
            _canardInstance = canardInstance;
            _typeResolver = canardInstance.TypeResolver;
            _framesProcessor = new CanFramesProcessor(CanardShouldAcceptTransfer);

            InitializeComponent();

            var driver = _canardInstance.CanDriver;
            driver.MessageReceived += Driver_MessageReceived;
            driver.MessageTransmitted += Driver_MessageTransmitted;

            _itemsViewSource = new CollectionViewSource() { Source = _items };
            var itemList = _itemsViewSource.View;
            itemList.Filter = FilterItem;
            dgFrames.ItemsSource = itemList;
        }

        bool CanardShouldAcceptTransfer(out ulong dataTypeSignature, uint dataTypeId, CanardTransferType transferType, byte sourceNodeId, byte destinationNodeId)
        {
            dataTypeSignature = 0;

            var typeKind = transferType == CanardTransferType.CanardTransferTypeBroadcast ?
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

        CollectionViewSource _itemsViewSource;
        ObservableStack<FrameModel> _items = new ObservableStack<FrameModel>();

        void Driver_MessageTransmitted(object sender, CanMessageEventArgs e)
        {
            if (!_enabled)
                return;

            Dispatcher.BeginInvoke((Action)(() =>
            {
                ProcessMessageCore(e, FrameDirection.Tx);
            }));
        }

        enum ProcessFramesResult
        {
            UpdatedNone,
            UpdatedSingle,
            UpdatedMultiple,
        }

        ProcessFramesResult ProcessFrames(CanFrame frame, FrameModel model)
        {
            var nowUs = (ulong)_stopwatch.ElapsedMilliseconds * 1000;
            CanFramesProcessingResult processorResult;
            try
            {
                processorResult = _framesProcessor.HandleRxFrame(frame, nowUs);
            }
            catch (CanFramesProcessingException)
            {
                return ProcessFramesResult.UpdatedSingle;
            }

            if (processorResult.Transfer != null)
            {
                Apply(processorResult.Transfer, model);

                if (processorResult.SourceFrames.Count > 1 &&
                    _framesMap.Count != 0)
                {
                    var hashset = new HashSet<CanFrame>(processorResult.SourceFrames);
                    var node = _framesMap.First;
                    while (node != null)
                    {
                        var next = node.Next;
                        if (hashset.Contains(node.Value.Frame))
                        {
                            _framesMap.Remove(node);
                            Apply(processorResult.Transfer, node.Value.Model);
                        }
                        node = next;
                    }

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

        void Apply(CanardRxTransfer transfer, FrameModel frame)
        {
            var typeKind = transfer.TransferType == CanardTransferType.CanardTransferTypeBroadcast ?
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

        void Driver_MessageReceived(object sender, CanMessageEventArgs e)
        {
            if (!_enabled)
                return;

            Dispatcher.BeginInvoke((Action)(() =>
            {
                ProcessMessageCore(e, FrameDirection.Rx);
            }));
        }

        void ProcessMessageCore(CanMessageEventArgs e, FrameDirection direction)
        {
            var frame = e.Message;
            var bytes = GetData(frame);

            var canIdInfo = new CanIdInfo(frame.Id);

            var model = new FrameModel
            {
                Direction = direction,
                CanId = frame.Id,
                Data = bytes,
                SourceNodeId = canIdInfo.SourceId,
                Time = DateTime.Now,
            };

            if (canIdInfo.IsServiceNotMessage)
                model.DestinationNodeId = canIdInfo.DestinationId;

            var processFramesResult = ProcessFrames(frame, model);

            _items.Push(model);

            if (processFramesResult == ProcessFramesResult.UpdatedMultiple &&
                _filters.Count > 0)
            {
                _itemsViewSource.View.Refresh();
            }
        }

        public void Dispose()
        {
            if (_canardInstance != null)
            {
                var driver = _canardInstance.CanDriver;
                driver.MessageReceived -= Driver_MessageReceived;
                driver.MessageTransmitted -= Driver_MessageTransmitted;
            }
        }

        void EnabledButton_Click(object sender, RoutedEventArgs e)
        {
            _enabled = ((ToggleButton)sender).IsChecked == true;
        }

        void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            _items.Clear();
        }

        List<FilterModel> _filters = new List<FilterModel>();

        bool FilterItem(object obj)
        {
            if (_filters.Count == 0)
                return true;

            var item = (FrameModel)obj;

            foreach (var filter in _filters)
            {
                if (!FilterItem(filter, item))
                    return false;
            }

            return true;
        }

        bool FilterItem(FilterModel filter, FrameModel item)
        {
            if (filter == null)
                return true;
            if (!filter.Enabled)
                return true;
            var content = filter.Content;
            if (string.IsNullOrEmpty(content))
                return true;

            var negate = filter.Negate;

            foreach (var str in EnumerateStrings(item))
            {
                bool isMatch;
                if (filter.Regex)
                {
                    if (filter.RegexCache == null)
                        return true;
                    isMatch = filter.RegexCache.IsMatch(str);
                }
                else
                {
                    var stringComparison = filter.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                    isMatch = str.IndexOf(filter.Content, stringComparison) != -1;
                }

                if (isMatch)
                {
                    return !negate;
                }
            }

            return negate;
        }

        IEnumerable<string> EnumerateStrings(FrameModel item)
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

        void AddFilterButton_Click(object sender, RoutedEventArgs e)
        {
            var filterControl = new TableFilterControl();

            spFilters.Children.Add(filterControl);

            filterControl.RemoveButtonClicked += (o, args) =>
            {
                _filters.Remove(filterControl.Value);
                spFilters.Children.Remove(filterControl);
                _itemsViewSource.View.Refresh();
            };

            filterControl.Value.PropertyChanged += (o, args) =>
            {
                var filter = filterControl.Value;

                if (string.Equals(args.PropertyName, nameof(FilterModel.Enabled), StringComparison.Ordinal))
                {
                    if (filter.Enabled)
                        _filters.Add(filter);
                    else
                        _filters.Remove(filter);
                }

                if (filter.Regex)
                {
                    var regexOptions = RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture;
                    if (!filter.CaseSensitive)
                        regexOptions |= RegexOptions.IgnoreCase;
                    try
                    {
                        filter.RegexCache = new Regex(filter.Content ?? string.Empty, regexOptions);
                    }
                    catch (ArgumentException) { }
                }

                _itemsViewSource.View.Refresh();
            };
        }
    }
}
