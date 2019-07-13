using DynamicData;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET.Studio.Tools
{
    sealed class TableFilterSetViewModel : ReactiveObject, IDisposable
    {
        readonly IDisposable _cleanUp;
        readonly SourceList<TableFilterViewModel> _filters = new SourceList<TableFilterViewModel>();
        readonly SourceList<TableFilterControl> _filterViewsSource = new SourceList<TableFilterControl>();

        readonly ReadOnlyObservableCollection<TableFilterControl> _filterViews;
        public ReadOnlyObservableCollection<TableFilterControl> FilterViews => _filterViews;

        public TableFilterSetViewModel(TableFilterSetControl tableFilterSetControl)
        {
            var filterViewsFiller = _filterViewsSource
                .Connect()
                .Bind(out _filterViews)
                .Subscribe();

            var filtersConnect = _filters.Connect();

            var filtersWatcher = filtersConnect
                .WhenValueChanged(x => x.Filter)
                .Subscribe(x => BuildFilter());

            var filtersSetWatcher = filtersConnect
                .Subscribe(x => BuildFilter());


            _cleanUp = new CompositeDisposable(
                filtersWatcher, _filters, _filterViewsSource, filterViewsFiller, filtersSetWatcher);
        }

        void BuildFilter()
        {
            var predicates = _filters.Items
                .Select(x => x.Filter)
                .Where(x => x != null)
                .ToList();

            Filter = And(predicates);
        }

        static Predicate<T> And<T>(IEnumerable<Predicate<T>> predicates)
        {
            return delegate (T item)
            {
                foreach (Predicate<T> predicate in predicates)
                {
                    if (!predicate(item))
                    {
                        return false;
                    }
                }
                return true;
            };
        }

        Predicate<IEnumerable<string>> _filter;
        public Predicate<IEnumerable<string>> Filter
        {
            get => _filter;
            set => this.RaiseAndSetIfChanged(ref _filter, value);
        }

        public void AddFilter()
        {
            TableFilterControl control = null;

            var removeFilterCommand = ReactiveCommand.Create(() =>
            {
                _filterViewsSource.Remove(control);
                _filters.Remove(control.ViewModel);
                control.Dispose();
            });

            control = new TableFilterControl(removeFilterCommand);
            _filterViewsSource.Add(control);
            _filters.Add(control.ViewModel);
        }

        public void Dispose()
        {
            _cleanUp.Dispose();
        }
    }
}
