using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Uavcan.NET.Studio.Tools
{
    sealed class FilterPredicateBuilder
    {
        string _regexPattern;
        RegexOptions _regexOptions;
        Regex _regexCache { get; set; }

        public Predicate<IEnumerable<string>> BuildFilterPredicate(TableFilterViewModel filter)
        {
            if (filter.Regex)
            {
                var regexOptions = RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture;
                if (!filter.CaseSensitive)
                    regexOptions |= RegexOptions.IgnoreCase;
                var regexPattern = filter.Content ?? string.Empty;

                if (_regexOptions != regexOptions ||
                    !string.Equals(_regexPattern, regexPattern, StringComparison.Ordinal))
                {
                    try
                    {
                        _regexCache = new Regex(filter.Content ?? string.Empty, regexOptions);
                        _regexOptions = regexOptions;
                        _regexPattern = regexPattern;
                    }
                    catch (ArgumentException) { }
                }
            }

            return strings =>
            {
                if (filter == null)
                    return true;
                if (!filter.Enabled)
                    return true;
                var content = filter.Content;
                if (string.IsNullOrEmpty(content))
                    return true;

                var negate = filter.Negate;

                foreach (var str in strings)
                {
                    bool isMatch;
                    if (filter.Regex)
                    {
                        if (_regexCache == null)
                            return true;
                        isMatch = _regexCache.IsMatch(str);
                    }
                    else
                    {
                        var stringComparison = filter.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                        isMatch = str.IndexOf(filter.Content, stringComparison) != -1;
                    }

                    if (isMatch)
                    {
                        return !negate;
                    }
                }

                return negate;
            };
        }
    }
}
