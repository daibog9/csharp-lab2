namespace Core.Models;

public sealed record ParliamentEvent(
    string Type,
    string Title,
    DateTime Start,
    DateTime End,
    string? Location = null,
    string? Format = null) {
    public bool Overlaps(DateTime? from, DateTime? to) =>
        (from is null || End >= from) && (to is null || Start <= to);

    public IEnumerable<string?> SearchableTexts() {
        yield return Type;
        yield return Title;
        yield return Location;
        yield return Format;
    }
}