using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MonitoringAgent
{
    class Scanf
    {
        private static readonly string _shortsRegex;
        private static IReadOnlyDictionary<string, Type> _shorts = new Dictionary<string, Type>
        {
            ["s"] = (typeof(string)),
            ["i"] = (typeof(int)),
            ["d"] = (typeof(double)),
        };

        private static IReadOnlyDictionary<Type, string> _patterns = new Dictionary<Type, string>
        {
            [(typeof(string))]  = @"[\w\d\S]+",
            [(typeof(int))]     = @"[-]?[0-9]+",
            [(typeof(double))]  = @"[-]?[0-9]*[.]*[0-9]+",
        };

        private IReadOnlyList<Type> _types;
        private Regex _regex;

        static Scanf()
        {
            _shortsRegex = string.Join('|', _shorts.ToList().Select(pair => pair.Key));
        }

        private Scanf(Regex regex, IReadOnlyList<Type> types)
        {
            if (regex == null)
                throw new ArgumentNullException(nameof(regex));
            else if (types == null)
                throw new ArgumentNullException(nameof(types));

            _regex = regex;
            _types = types;
        }

        public static Scanf Create(string pattern)
        {
            List<Type> types = new List<Type>();

            pattern = Regex.Replace(pattern, $@"\%({_shortsRegex})", (m) =>
            {
                Console.WriteLine(m.Value);
                var type = _shorts[m.Groups[1].Value];
                Console.WriteLine(type);
                types.Add(type);
                return "(" + _patterns[type] + ")";
            });

            pattern = pattern.Replace(" ", @"\s+");

            return new Scanf(new Regex(pattern, RegexOptions.Compiled), types);
        }

        public void Matches(string input, object[] array)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            var match = _regex.Match(input);
            int count = match.Groups.Count;
            int argumentsCount = count - 1;
            if (array.Length < argumentsCount)
                throw new ArgumentException($"The array ({nameof(array)}) must contain at least {argumentsCount} elements!");
            else if (_types.Count < argumentsCount)
                throw new ScanfPatternCountException(_types.Count, argumentsCount);

            for (int i = 0; i < argumentsCount; i++)
            {
                var group = match.Groups[i + 1];
                array[i] = Convert.ChangeType(group.Value, _types[i]);
            }
        }

        public object[] Matches(string input)
        {
            var match = _regex.Match(input);
            int count = match.Groups.Count;
            int argumentsCount = count - 1;
            if (_types.Count < argumentsCount)
                throw new ScanfPatternCountException(_types.Count, argumentsCount);

            object[] array = new object[argumentsCount];
            for (int i = 0; i < argumentsCount; i++)
            {
                var group = match.Groups[i + 1];
                array[i] = Convert.ChangeType(group.Value, _types[i]);
            }

            return array;
        }
    }
}
