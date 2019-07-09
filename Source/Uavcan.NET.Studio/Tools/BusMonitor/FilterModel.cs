using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Uavcan.NET.Studio.Tools.BusMonitor
{
    sealed class FilterModel : INotifyPropertyChanged
    {
        bool _enabled;
        bool _negate;
        bool _regex;
        bool _caseSensitive;
        string _content;

        void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool Enabled { get => _enabled; set => SetField(ref _enabled, value); }
        public bool Negate { get => _negate; set => SetField(ref _negate, value); }
        public bool Regex { get => _regex; set => SetField(ref _regex, value); }
        public bool CaseSensitive { get => _caseSensitive; set => SetField(ref _caseSensitive, value); }
        public string Content { get => _content; set => SetField(ref _content, value); }

        public Regex RegexCache { get; set; }
    }
}
