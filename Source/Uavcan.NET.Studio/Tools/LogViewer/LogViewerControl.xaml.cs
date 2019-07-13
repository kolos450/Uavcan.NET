using ReactiveUI;
using System;
using System.ComponentModel.Composition;
using System.Reactive.Disposables;

namespace Uavcan.NET.Studio.Tools.LogViewer
{
    /// <summary>
    /// Interaction logic for LogViewerControl.xaml
    /// </summary>
    partial class LogViewerControl : ReactiveUserControl<LogViewerViewModel>, IDisposable
    {
        [Import]
        UavcanService _uavcan = null;

        public LogViewerControl()
        {
            InitializeComponent();

            ShellService.SatisfyImportsOnce(this);

            var uavcan = _uavcan.Engine;

            var filter = TableFilterSet.ViewModel;
            ViewModel = new LogViewerViewModel(uavcan, filter);

            this.WhenActivated(disposableRegistration =>
            {
                this.OneWayBind(ViewModel,
                    vm => vm.LogItems,
                    v => v.dgLogItems.ItemsSource)
                    .DisposeWith(disposableRegistration);

                this.BindCommand(ViewModel,
                    vm => vm.AddFilter,
                    v => v.AddFilterButton)
                    .DisposeWith(disposableRegistration);
            });
        }

        public void Dispose()
        {
            ViewModel?.Dispose();
        }
    }
}
