using System.Text;
using System.Xml;
using System.Xml.Linq;
using Core.Models;

namespace Core.Search;

public sealed class LinqSearchStrategy : IXmlSearchStrategy {
    public string Name => "LINQ to XML";

    public IReadOnlyList<ParliamentMember> Search(string xmlPath, SearchCriteria criteria) {
        XDocument document;
        using (XmlReader reader = XmlReader.Create(xmlPath, XmlValues.ReaderSettings())) {
            document = XDocument.Load(reader);
        }
        XElement root = document.Root ?? throw new InvalidDataException("Документ не містить кореневого вузла.");

        return BuildQuery(root, criteria, description: null).ToList();
    }

    public string DescribeQuery(SearchCriteria criteria) {
        var description = new StringBuilder();
        _ = BuildQuery(new XElement(XmlNames.Root), criteria, description);
        return description.ToString();
    }

    private static IEnumerable<ParliamentMember> BuildQuery(XElement root, SearchCriteria criteria, StringBuilder? description) {
        IEnumerable<XElement> members = root.Elements(XmlNames.Member);
        description?.Append("root.Elements(\"Member\")");

        foreach (var (name, value) in criteria.MemberAttributes) {
            members = members.Where(m => (string?)m.Attribute(name) == value);
            description?.Append($"\n   .Where(m => (string)m.Attribute(\"{name}\") == \"{value}\")");
        }

        var eventFilters = new List<Func<XElement, bool>>();
        var eventFilterText = new List<string>();

        if (criteria.EventType is { } type) {
            eventFilters.Add(e => (string?)e.Attribute(XmlNames.Type) == type);
            eventFilterText.Add($"(string)e.Attribute(\"type\") == \"{type}\"");
        }
        if (criteria.From is { } from) {
            eventFilters.Add(e => ParseDate(e, XmlNames.End) >= from);
            eventFilterText.Add($"(DateTime)e.Element(\"End\") >= {from:yyyy-MM-dd HH:mm}");
        }
        if (criteria.To is { } to) {
            eventFilters.Add(e => ParseDate(e, XmlNames.Start) <= to);
            eventFilterText.Add($"(DateTime)e.Element(\"Start\") <= {to:yyyy-MM-dd HH:mm}");
        }

        if (eventFilters.Count > 0) {
            members = members.Where(m => m.Elements(XmlNames.Event).Any(e => eventFilters.All(f => f(e))));
            description?.Append($"\n   .Where(m => m.Elements(\"Event\").Any(e => {string.Join(" && ", eventFilterText)}))");
        }

        var projected =
            from m in members
            let attributes = m.Attributes().Select(a => new KeyValuePair<string, string>(a.Name.LocalName, a.Value)).ToList()
            let fullName = m.Element(XmlNames.FullName)
            let lastName = Value(fullName, XmlNames.LastName) ?? ""
            let firstName = Value(fullName, XmlNames.FirstName) ?? ""
            let patronymic = Value(fullName, XmlNames.Patronymic)
            let memberTexts = ParliamentMember.SearchableTexts(attributes, lastName, firstName, patronymic).ToList()
            let allEvents = m.Elements(XmlNames.Event).ToList()
            let events = (from e in allEvents
                          where eventFilters.All(f => f(e))
                          let ev = ToEvent(e)
                          where KeywordMatcher.Matches(criteria.Keywords, memberTexts, ev.SearchableTexts())
                          select ev).ToList()
            where events.Count > 0 ||
                  (allEvents.Count == 0 && !criteria.HasEventFilter &&
                   KeywordMatcher.Matches(criteria.Keywords, memberTexts, []))
            select new ParliamentMember(attributes, lastName, firstName, patronymic, events);

        description?.Append("\n   .Select(m => new ParliamentMember(… m.Elements(\"Event\").Where(ті самі умови)))");
        if (criteria.Keywords.Count > 0) {
            description?.Append($"\n   .Where(x => текст містить {string.Join(" && ", criteria.Keywords.Select(k => $"\"{k}\""))})");
        }
        return projected;
    }

    private static ParliamentEvent ToEvent(XElement e) {
        string? memberId = (string?)e.Parent?.Attribute(XmlNames.Id);
        return new ParliamentEvent(
            (string?)e.Attribute(XmlNames.Type) ?? "",
            Value(e, XmlNames.Title) ?? "",
            XmlValues.ParseDateTime(Value(e, XmlNames.Start), XmlNames.Start, memberId),
            XmlValues.ParseDateTime(Value(e, XmlNames.End), XmlNames.End, memberId),
            Value(e, XmlNames.Location),
            (string?)e.Attribute(XmlNames.Format));
    }

    private static DateTime ParseDate(XElement e, string node) =>
        XmlValues.ParseDateTime(Value(e, node), node, (string?)e.Parent?.Attribute(XmlNames.Id));

    private static string? Value(XElement? parent, string child) {
        string? text = parent?.Element(child)?.Value.Trim();
        return string.IsNullOrEmpty(text) ? null : text;
    }
}