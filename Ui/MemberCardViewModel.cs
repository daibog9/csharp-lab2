using System.Globalization;
using Core;
using Core.Models;
using Core.Search;

namespace Ui.ViewModels;

public sealed record StrategyOption(SearchStrategyKind Kind, string Name) {
    public override string ToString() => Name;
}

public sealed class MemberCardViewModel {
    public MemberCardViewModel(ParliamentMember member, int number) {
        Title = $"{number}. {member.FullName}";

        Subtitle = string.Join("  ·  ",
            new[] { member.Faculty, member.Department, member.Specialty }.Where(s => !string.IsNullOrWhiteSpace(s)));

        var extra = member.Attributes
            .Where(a => !XmlNames.MainMemberAttributes.Contains(a.Key))
            .Select(a => $"{XmlNames.Label(a.Key)}: {a.Value}")
            .Append($"атрибутів у вузлі: {member.Attributes.Count}");
        Details = string.Join("     ", extra);

        EventsText = member.Events.Count == 0
            ? "заходів немає"
            : string.Join(Environment.NewLine, member.Events.Select(Describe));
    }

    public string Title { get; }
    public string Subtitle { get; }
    public string Details { get; }
    public string EventsText { get; }

    private static string Describe(ParliamentEvent e) {
        string place = e.Location is null ? "" : $"  ·  {e.Location}";
        string format = e.Format is null ? "" : $" ({e.Format})";
        return $"▸ {e.Type}{format}  ·  {e.Title}  ·  {Period(e)}{place}";
    }

    private static string Period(ParliamentEvent e) {
        var c = CultureInfo.InvariantCulture;
        return e.Start.Date == e.End.Date
            ? $"{e.Start.ToString("dd.MM.yyyy HH:mm", c)}–{e.End.ToString("HH:mm", c)}"
            : $"{e.Start.ToString("dd.MM.yyyy HH:mm", c)} – {e.End.ToString("dd.MM.yyyy HH:mm", c)}";
    }
}