using ReactiveUI;
using System;
using System.Reactive.Disposables;
using System.Windows.Controls;
using Uavcan.NET.Dsdl;
using Uavcan.NET.Dsdl.DataTypes;
using Uavcan.NET.Studio.Tools.BusMonitor.Presentation;

namespace Uavcan.NET.Studio.Tools.BusMonitor
{
    /// <summary>
    /// Interaction logic for BusMonitorControl.xaml
    /// </summary>
    partial class BusMonitorControl : ReactiveUserControl<BusMonitorViewModel>, IDisposable
    {
        DsdlSerializer _serializer;

        public BusMonitorControl(UavcanInstance uavcan)
        {
            _serializer = uavcan.Serializer;

            InitializeComponent();

            var filter = TableFilterSet.ViewModel;
            ViewModel = new BusMonitorViewModel(uavcan, filter);

            this.WhenActivated(disposableRegistration =>
            {
                this.OneWayBind(ViewModel,
                    vm => vm.Items,
                    v => v.dgFrames.ItemsSource)
                    .DisposeWith(disposableRegistration);

                this.BindCommand(ViewModel,
                    vm => vm.AddFilter,
                    v => v.AddFilterButton)
                    .DisposeWith(disposableRegistration);

                this.BindCommand(ViewModel,
                    vm => vm.ClearItems,
                    v => v.ClearButton)
                    .DisposeWith(disposableRegistration);

                this.Bind(ViewModel,
                    vm => vm.Enabled,
                    v => v.EnabledButton.IsChecked)
                    .DisposeWith(disposableRegistration);
            });
        }

        public void Dispose()
        {
            ViewModel?.Dispose();
        }

        void DgFrames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedModel = dgFrames.SelectedItem as FrameViewModel;
            if (selectedModel != null)
            {
                var str = GetPayloadString(selectedModel) ?? string.Empty;
                runObjectView.Text = str;
            }
        }

        string GetPayloadString(FrameViewModel model)
        {
            var exception = model.AccociatedFrameProcessorException;
            if (exception != null)
                return exception.ToString();

            var transfer = model.AssociatedTransfer;
            if (transfer == null)
                return null;

            DsdlType scheme;
            switch (transfer.TransferType)
            {
                case UavcanTransferType.Broadcast:
                    scheme = model.DataType as MessageType;
                    break;
                case UavcanTransferType.Request:
                    scheme = (model.DataType as ServiceType)?.Request;
                    break;
                case UavcanTransferType.Response:
                    scheme = (model.DataType as ServiceType)?.Response;
                    break;
                default:
                    throw new InvalidOperationException();
            }

            if (scheme == null)
                return null;

            var bytes = transfer.Payload;
            var obj = _serializer.Deserialize(bytes, scheme);
            try
            {
                return ObjectPrinter.PrintToString(obj, scheme);
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }
    }
}
