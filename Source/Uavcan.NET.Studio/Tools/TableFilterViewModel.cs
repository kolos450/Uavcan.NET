using DynamicData.Binding;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET.Studio.Tools
{
    sealed class TableFilterViewModel : ReactiveObject, IDisposable
    {
        static readonly TimeSpan FilterThrottleTimeSpan = TimeSpan.FromMilliseconds(250);

        bool _enabled;
        bool _negate;
        bool _regex;
        bool _caseSensitive;
        string _content;

        public TableFilterViewModel(ReactiveCommand<Unit, Unit> removeFilterCommand)
        {
            RemoveFilter = removeFilterCommand;

            var predicateBuilder = new FilterPredicateBuilder();

            _filter = this.WhenAnyValue(
                    x => x.Enabled,
                    x => x.Negate,
                    x => x.Regex,
                    x => x.CaseSensitive,
                    x => x.Content)
                .Throttle(FilterThrottleTimeSpan)
                .Select(x => predicateBuilder.BuildFilterPredicate(this))
                .ToProperty(this, x => x.Filter);
        }

        public ReactiveCommand<Unit, Unit> RemoveFilter { get; }

        readonly ObservableAsPropertyHelper<Predicate<IEnumerable<string>>> _filter;
        public Predicate<IEnumerable<string>> Filter => _filter.Value;

        public bool Enabled
        {
            get => _enabled;
            set => this.RaiseAndSetIfChanged(ref _enabled, value);
        }

        public bool Negate
        {
            get => _negate;
            set => this.RaiseAndSetIfChanged(ref _negate, value);
        }

        public bool Regex
        {
            get => _regex;
            set => this.RaiseAndSetIfChanged(ref _regex, value);
        }

        public bool CaseSensitive
        {
            get => _caseSensitive;
            set => this.RaiseAndSetIfChanged(ref _caseSensitive, value);
        }

        public string Content
        {
            get => _content;
            set => this.RaiseAndSetIfChanged(ref _content, value);
        }

        public void Dispose()
        {
            if (_filter != null)
            {
                _filter.Dispose();
            }
        }
    }
}
