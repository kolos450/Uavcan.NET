using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET.Studio
{
    sealed class ErrorViewModel : ReactiveObject
    {
        string _description;
        public string Description { get => _description; set => this.RaiseAndSetIfChanged(ref _description, value); }

        string _details;
        public string Details { get => _details; set => this.RaiseAndSetIfChanged(ref _details, value); }

        bool _detailsVisible;
        public bool DetailsVisisble { get => _detailsVisible; set => this.RaiseAndSetIfChanged(ref _detailsVisible, value); }

        public ReactiveCommand<Unit, Unit> ToggleDetailsVisibility { get; set; }

        public ErrorViewModel()
        {
            ToggleDetailsVisibility = ReactiveCommand.Create(() =>
            {
                DetailsVisisble = !DetailsVisisble;
            });
        }
    }
}
