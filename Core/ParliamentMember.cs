namespace Core.Models;

public sealed class ParliamentMember {
    public ParliamentMember(
        IReadOnlyList<KeyValuePair<string, string>> attributes,
        string lastName,
        string firstName,
        string? patronymic,
        IReadOnlyList<ParliamentEvent> events) {
        Attributes = attributes;
        LastName = lastName;
        FirstName = firstName;
        Patronymic = string.IsNullOrWhiteSpace(patronymic) ? null : patronymic;
        Events = events;
    }

    public IReadOnlyList<KeyValuePair<string, string>> Attributes { get; }
    public string LastName { get; }
    public string FirstName { get; }
    public string? Patronymic { get; }

    public IReadOnlyList<ParliamentEvent> Events { get; }

    public string FullName =>
        string.Join(" ", new[] { LastName, FirstName, Patronymic }.Where(p => !string.IsNullOrWhiteSpace(p)));

    public string Id => GetAttribute(XmlNames.Id) ?? "";
    public string Faculty => GetAttribute(XmlNames.Faculty) ?? "";
    public string? Department => GetAttribute(XmlNames.Department);
    public string Specialty => GetAttribute(XmlNames.Specialty) ?? "";

    public string? GetAttribute(string name) {
        foreach (var (key, value) in Attributes) {
            if (key == name) return value;
        }
        return null;
    }

    public static IEnumerable<string?> SearchableTexts(
        IEnumerable<KeyValuePair<string, string>> attributes, string? lastName, string? firstName, string? patronymic) {
        foreach (var (_, value) in attributes) yield return value;
        yield return lastName;
        yield return firstName;
        yield return patronymic;
    }

    public string Signature() =>
        $"{Id}|{FullName}|{string.Join(",", Attributes.Select(a => a.Key + "=" + a.Value))}|" +
        string.Join(";", Events.Select(e => $"{e.Type}/{e.Title}/{e.Start:s}/{e.End:s}/{e.Location}/{e.Format}"));
}