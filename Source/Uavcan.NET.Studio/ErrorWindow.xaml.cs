using MahApps.Metro.Controls;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Uavcan.NET.Studio
{
    /// <summary>
    /// Interaction logic for ErrorWindow.xaml
    /// </summary>
    partial class ErrorWindow : MetroWindow, IViewFor<ErrorViewModel>
    {
        public ErrorWindow(ErrorViewModel viewModel)
        {
            InitializeComponent();

            ViewModel = viewModel;

            this.WhenActivated(d =>
            {
                this.OneWayBind(ViewModel,
                    vm => vm.Description,
                    v => v.tbDescription.Text)
                    .DisposeWith(d);

                this.OneWayBind(ViewModel,
                    vm => vm.Details,
                    v => v.tbDetails.Text)
                    .DisposeWith(d);

                this.OneWayBind(ViewModel,
                    vm => vm.DetailsVisisble,
                    v => v.tbDetails.Visibility)
                    .DisposeWith(d);

                this.BindCommand(ViewModel,
                    vm => vm.ToggleDetailsVisibility,
                    v => v.bToggleDetailsVisibility)
                    .DisposeWith(d);
            });
        }

        public ErrorViewModel ViewModel { get; set; }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (ErrorViewModel)value;
        }

        public static void Show(string description, string details)
        {
            var vm = new ErrorViewModel
            {
                Description = description,
                Details = details,
            };

            new ErrorWindow(vm).ShowDialog();
        }
    }
}
