using System.Globalization;
using System.Xml;

namespace Core.Search;

internal static class KeywordMatcher {
    private const char Separator = '\u001F';

    public static bool Matches(IReadOnlyList<string> keywords, IEnumerable<string?> memberTexts, IEnumerable<string?> eventTexts) {
        if (keywords.Count == 0) return true;

        string haystack = string.Join(Separator, memberTexts.Concat(eventTexts).Where(t => !string.IsNullOrEmpty(t)));
        foreach (string keyword in keywords) {
            if (!haystack.Contains(keyword, StringComparison.OrdinalIgnoreCase)) return false;
        }
        return true;
    }
}

internal static class XmlValues {
    private static readonly string[] DateFormats = ["yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm"];

    public static XmlReaderSettings ReaderSettings() => new() {
        DtdProcessing = DtdProcessing.Ignore,
        IgnoreComments = true,
        IgnoreWhitespace = true,
        IgnoreProcessingInstructions = true
    };

    public static DateTime ParseDateTime(string? text, string nodeName, string? memberId) {
        if (text is not null &&
            DateTime.TryParseExact(text.Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime value)) {
            return value;
        }
        throw new InvalidDataException(
            $"Учасник «{memberId}» має некоректне значення {nodeName} «{text}». Очікується формат yyyy-MM-ddTHH:mm:ss.");
    }
}