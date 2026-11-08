namespace Core.Models;

public sealed record SearchResult(
    string StrategyName,
    IReadOnlyList<ParliamentMember> Members,
    string Query,
    TimeSpan Elapsed) {
    public int EventCount => Members.Sum(m => m.Events.Count);
}