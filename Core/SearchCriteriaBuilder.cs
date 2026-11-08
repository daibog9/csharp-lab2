namespace Core.Models;

public sealed class SearchCriteriaBuilder {
    private static readonly char[] KeywordSeparators = [' ', '\t', '\n', '\r', ',', ';', '"', '«', '»'];

    private readonly List<KeyValuePair<string, string>> _attributes = [];
    private readonly List<string> _keywords = [];
    private string? _eventType;
    private DateTime? _from;
    private DateTime? _to;

    public SearchCriteriaBuilder WhereMemberAttribute(string attributeName, string? value) {
        if (!string.IsNullOrWhiteSpace(value)) {
            _attributes.RemoveAll(a => a.Key == attributeName);
            _attributes.Add(new(attributeName, value));
        }
        return this;
    }

    public SearchCriteriaBuilder WithEventType(string? eventType) {
        _eventType = string.IsNullOrWhiteSpace(eventType) ? null : eventType;
        return this;
    }

    public SearchCriteriaBuilder WithKeywords(string? text) {
        _keywords.Clear();
        if (string.IsNullOrWhiteSpace(text)) return this;

        foreach (string word in text.Split(KeywordSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
            if (!_keywords.Contains(word, StringComparer.OrdinalIgnoreCase)) _keywords.Add(word);
        }
        return this;
    }

    public SearchCriteriaBuilder Between(DateTime? from, DateTime? to) {
        if (from is not null && to is not null && from.Value.Date > to.Value.Date) (from, to) = (to, from);
        _from = from?.Date;
        _to = to?.Date.AddDays(1).AddTicks(-1);
        return this;
    }

    public SearchCriteria Build() =>
        new([.. _attributes], _eventType, [.. _keywords], _from, _to);
}