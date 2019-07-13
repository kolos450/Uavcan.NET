using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Uavcan.NET.Studio.Tools
{
    /// <summary>
    /// Interaction logic for TableFilterSetControl.xaml
    /// </summary>
    partial class TableFilterSetControl : ReactiveUserControl<TableFilterSetViewModel>, IDisposable
    {
        public TableFilterSetControl()
        {
            InitializeComponent();

            ViewModel = new TableFilterSetViewModel(this);

            this.WhenActivated(d =>
            {
                this.OneWayBind(ViewModel,
                    vm => vm.FilterViews,
                    v => v.icFilters.ItemsSource)
                    .DisposeWith(d);
            });
        }

        public void Dispose()
        {
            ViewModel?.Dispose();
        }
    }
}
