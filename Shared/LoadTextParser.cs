using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Qks.Shared
{
    public sealed class ParsedLoad { public string Pattern { get; set; } = string.Empty; public double Value { get; set; } }
    public static class LoadTextParser
    {
        private const string Number = @"[-+]?(?:\d+(?:[.,]\d+)?|[.,]\d+)(?:[eE][-+]?\d+)?";
        public static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        public static List<ParsedLoad> Parse(string raw, string defaultPattern)
        {
            if (string.IsNullOrWhiteSpace(raw)) throw new FormatException("Empty load text; zero and delete must be explicit.");
            // Only formatting produced by this application is accepted; unknown MText control codes fail closed.
            string text = raw.Replace("\\P", "\n").Replace("\\~", " ").Trim();
            var result = new List<ParsedLoad>(); var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string line in text.Split(new[] { '\n', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var pair = Regex.Match(line.Trim(), @"^(?<p>[^:=\\{}]+?)\s*[:=]\s*(?<v>" + Number + @")\s*(?:kN/m(?:2|²|\^2)?)?$", RegexOptions.IgnoreCase);
                string pattern, number;
                if (pair.Success) { pattern = pair.Groups["p"].Value.Trim(); number = pair.Groups["v"].Value; }
                else
                {
                    var single = Regex.Match(line.Trim(), @"^(?<v>" + Number + @")\s*(?:kN/m(?:2|²|\^2)?)?$", RegexOptions.IgnoreCase);
                    if (!single.Success || string.IsNullOrWhiteSpace(defaultPattern)) throw new FormatException("Invalid or ambiguous load text: " + line);
                    pattern = defaultPattern.Trim(); number = single.Groups["v"].Value;
                }
                double value;
                if (!double.TryParse(number.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value) || !Finite(value) || !seen.Add(pattern))
                    throw new FormatException("Invalid number or duplicate pattern: " + pattern);
                result.Add(new ParsedLoad { Pattern = pattern, Value = value });
            }
            if (result.Count == 0) throw new FormatException("No load values.");
            return result;
        }
    }
}
