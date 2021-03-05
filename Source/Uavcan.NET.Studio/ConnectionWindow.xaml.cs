using MahApps.Metro.Controls;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;

namespace Uavcan.NET.Studio
{
    /// <summary>
    /// Interaction logic for ConnectionWindow.xaml
    /// </summary>
    partial class ConnectionWindow : MetroWindow, IViewFor<ConnectionViewModel>
    {
        public ConnectionWindow(ICompositionService compositionService)
        {
            InitializeComponent();

            ViewModel = new ConnectionViewModel(compositionService);

            this.WhenActivated(d =>
            {
                this.OneWayBind(ViewModel,
                    viewModel => viewModel.Busy,
                    view => view.bBusyIndicator.Visibility)
                    .DisposeWith(d);

                this.OneWayBind(ViewModel,
                    viewModel => viewModel.Interfaces,
                    view => view.cbInterfaces.ItemsSource)
                    .DisposeWith(d);

                this.OneWayBind(ViewModel,
                    viewModel => viewModel.Interface,
                    view => view.cbInterfaces.SelectedItem)
                    .DisposeWith(d);

                this.WhenAnyValue(v => v.upBitRate.Value)
                    .Select(x => x.HasValue ? (int?)x.Value : null)
                    .BindTo(this, v => v.ViewModel.BitRate)
                    .DisposeWith(d);

                this.BindCommand(ViewModel,
                    vm => vm.Connect,
                    v => v.bOk,
                    Observable.Return(this))
                    .DisposeWith(d);
            });
        }

        public ConnectionViewModel ViewModel { get; set; }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (ConnectionViewModel)value;
        }
    }
}
