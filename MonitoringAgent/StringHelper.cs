using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MonitoringAgent
{
    public static class StringHelper
    {
        private static string[] _emptyStringArray;
        private static char[] _spaceChars;
        private static char[] _lineChars;

        public static string[] EmptyStringArray => _emptyStringArray;

        static StringHelper()
        {
            _emptyStringArray = new string[0];
            _spaceChars = new[] { ' ', '\t' };
            _lineChars = new[] { '\n' };
        }

        public static string RemoveMultipleSpaces(this string data)
        {
            return Regex.Replace(data, "[ ]{2,}", " ", RegexOptions.Compiled);
        }

        public static string[] SplitLines(this string data)
        {
            if (!string.IsNullOrWhiteSpace(data))
            {
                return data.Split(_lineChars, StringSplitOptions.RemoveEmptyEntries);
            }

            return EmptyStringArray;
        }

        public static string[] SplitSpaces(this string data)
        {
            if (!string.IsNullOrWhiteSpace(data))
            {
                return data.Split(_spaceChars, StringSplitOptions.RemoveEmptyEntries);
            }

            return EmptyStringArray;
        }

        public static string JoinString(this IEnumerable<string> stringArray, string separator = "")
        {
            return string.Join(separator, stringArray);
        }

        public static string[] Trim(this string[] stringArray, params char[] trimChars)
        {
            if (stringArray.Length > 0)
            {
                return stringArray
                    .Select(str => str.Trim(trimChars))
                    .Where(x => !string.IsNullOrEmpty(x)).ToArray();
            }

            return stringArray;
        }
    }
}
