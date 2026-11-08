using System.Globalization;

namespace Core.Models;

public sealed class SearchCriteria {
    public static readonly SearchCriteria Empty = new([], null, [], null, null);

    internal SearchCriteria(
        IReadOnlyList<KeyValuePair<string, string>> memberAttributes,
        string? eventType,
        IReadOnlyList<string> keywords,
        DateTime? from,
        DateTime? to) {
        MemberAttributes = memberAttributes;
        EventType = eventType;
        Keywords = keywords;
        From = from;
        To = to;
    }

    public IReadOnlyList<KeyValuePair<string, string>> MemberAttributes { get; }
    public string? EventType { get; }
    public IReadOnlyList<string> Keywords { get; }
    public DateTime? From { get; }
    public DateTime? To { get; }

    public bool HasEventFilter => EventType is not null || From is not null || To is not null;

    public bool IsEmpty => MemberAttributes.Count == 0 && !HasEventFilter && Keywords.Count == 0;

    public string Describe() {
        if (IsEmpty) return "без обмежень";

        var parts = new List<string>();
        foreach (var (name, value) in MemberAttributes) {
            parts.Add($"«{XmlNames.Label(name)}» – {value}");
        }
        if (EventType is not null) parts.Add($"«{XmlNames.Label(XmlNames.Type)}» – {EventType}");
        if (From is not null || To is not null) {
            string from = From?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? "…";
            string to = To?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? "…";
            parts.Add($"«Часові рамки» – {from}-{to}");
        }
        if (Keywords.Count > 0) parts.Add($"«Ключові слова» – {string.Join(" ", Keywords)}");
        return string.Join(", ", parts);
    }
}