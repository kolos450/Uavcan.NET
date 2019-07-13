using ReactiveUI;
using System;
using System.ComponentModel.Composition;
using System.Reactive;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Uavcan.NET.Studio.Tools
{
    /// <summary>
    /// Interaction logic for TableFilterControl.xaml
    /// </summary>
    partial class TableFilterControl : ReactiveUserControl<TableFilterViewModel>, IDisposable
    {
        public TableFilterControl(ReactiveCommand<Unit, Unit> removeFilterCommand)
        {
            InitializeComponent();

            ViewModel = new TableFilterViewModel(removeFilterCommand);

            this.WhenActivated(disposableRegistration =>
            {
                this.BindCommand(ViewModel,
                    vm => vm.RemoveFilter,
                    v => v.RemoveButton)
                    .DisposeWith(disposableRegistration);

                this.Bind(ViewModel,
                    vm => vm.Content,
                    v => v.SearchTerm.Text)
                    .DisposeWith(disposableRegistration);

                this.Bind(ViewModel,
                    vm => vm.Enabled,
                    v => v.ApplyButton.IsChecked)
                    .DisposeWith(disposableRegistration);

                this.Bind(ViewModel,
                    vm => vm.Negate,
                    v => v.NegateButton.IsChecked)
                    .DisposeWith(disposableRegistration);

                this.Bind(ViewModel,
                    vm => vm.Regex,
                    v => v.RegexButton.IsChecked)
                    .DisposeWith(disposableRegistration);

                this.Bind(ViewModel,
                    vm => vm.CaseSensitive,
                    v => v.CaseButton.IsChecked)
                    .DisposeWith(disposableRegistration);
            });
        }

        public void Dispose()
        {
            ViewModel?.Dispose();
        }
    }
}
