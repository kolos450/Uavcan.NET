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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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

        public BusMonitorControl(CanardInstance canardInstance)
        {
            _canardInstance = canardInstance;
            _framesProcessor = new CanFramesProcessor(CanardShouldAcceptTransfer);

            InitializeComponent();

            var driver = _canardInstance.CanDriver;
            driver.MessageReceived += Driver_MessageReceived;
            driver.MessageTransmitted += Driver_MessageTransmitted;

            dgFrames.ItemsSource = _items;
        }

        bool CanardShouldAcceptTransfer(out ulong dataTypeSignature, uint dataTypeId, CanardTransferType transferType, byte sourceNodeId, byte destinationNodeId)
        {
            dataTypeSignature = 0;

            var type = _typeResolver.TryResolveType((int)dataTypeId);
            if (type == null)
                return false;

            var signature = type.GetDataTypeSignature();
            if (signature == null)
                return false;

            dataTypeSignature = signature.Value;

            return true;
        }

        ObservableCollection<FrameModel> _items = new ObservableCollection<FrameModel>();

        void Driver_MessageTransmitted(object sender, CanMessageEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                var frame = e.Message;
                var bytes = GetData(frame);

                var canIdInfo = new CanIdInfo(frame.Id);

                var model = new FrameModel
                {
                    Direction = FrameDirection.Tx,
                    CanId = frame.Id,
                    Data = bytes,
                    DestinationNodeId = canIdInfo.DestinationId,
                    SourceNodeId = canIdInfo.SourceId,
                    Time = DateTime.Now,
                };

                _items.Add(model);

                ProcessFrames(frame, model);
            }));
        }

        void ProcessFrames(CanFrame frame, FrameModel model)
        {
            var nowUs = (ulong)_stopwatch.ElapsedMilliseconds * 1000;
            var processorResult = _framesProcessor.HandleRxFrame(frame, nowUs);
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
                    }
                }
            }
            else
            {
                _framesMap.AddLast((frame, model));
            }
        }

        void Apply(CanardRxTransfer transfer, FrameModel frame)
        {
            throw new NotImplementedException();
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
            Dispatcher.BeginInvoke((Action)(() =>
            {
                var frame = e.Message;
                var bytes = GetData(frame);

                var canIdInfo = new CanIdInfo(frame.Id);

                var model = new FrameModel
                {
                    Direction = FrameDirection.Rx,
                    CanId = frame.Id,
                    Data = bytes,
                    DestinationNodeId = canIdInfo.DestinationId,
                    SourceNodeId = canIdInfo.SourceId,
                    Time = DateTime.Now,
                };

                _items.Add(model);

                ProcessFrames(frame, model);
            }));
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
    }
}
