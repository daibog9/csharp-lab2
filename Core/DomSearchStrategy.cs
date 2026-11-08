using System.Xml;
using Core.Models;

namespace Core.Search;

public sealed class DomSearchStrategy : IXmlSearchStrategy {
    public string Name => "DOM API";

    public IReadOnlyList<ParliamentMember> Search(string xmlPath, SearchCriteria criteria) {
        var document = new XmlDocument();
        using (XmlReader reader = XmlReader.Create(xmlPath, XmlValues.ReaderSettings())) {
            document.Load(reader);
        }

        string memberQuery = XPathQueryBuilder.BuildMemberQuery(criteria);
        string eventQuery = XPathQueryBuilder.BuildEventQuery(criteria);

        var results = new List<ParliamentMember>();
        XmlNodeList? memberNodes = document.SelectNodes(memberQuery);
        if (memberNodes is null) return results;

        foreach (XmlElement memberNode in memberNodes.OfType<XmlElement>()) {
            var attributes = memberNode.Attributes
                .Cast<XmlAttribute>()
                .Select(a => new KeyValuePair<string, string>(a.Name, a.Value))
                .ToList();
            string id = memberNode.GetAttribute(XmlNames.Id);

            string lastName = Text(memberNode, $"{XmlNames.FullName}/{XmlNames.LastName}") ?? "";
            string firstName = Text(memberNode, $"{XmlNames.FullName}/{XmlNames.FirstName}") ?? "";
            string? patronymic = Text(memberNode, $"{XmlNames.FullName}/{XmlNames.Patronymic}");
            var memberTexts = ParliamentMember.SearchableTexts(attributes, lastName, firstName, patronymic).ToList();

            var matched = new List<ParliamentEvent>();
            foreach (XmlElement eventNode in (memberNode.SelectNodes(eventQuery) ?? EmptyList).OfType<XmlElement>()) {
                ParliamentEvent ev = ReadEvent(eventNode, id);
                if (KeywordMatcher.Matches(criteria.Keywords, memberTexts, ev.SearchableTexts())) matched.Add(ev);
            }

            bool hasEvents = memberNode.SelectSingleNode(XmlNames.Event) is not null;
            bool include = matched.Count > 0 ||
                           (!hasEvents && !criteria.HasEventFilter &&
                            KeywordMatcher.Matches(criteria.Keywords, memberTexts, []));

            if (include) results.Add(new ParliamentMember(attributes, lastName, firstName, patronymic, matched));
        }
        return results;
    }

    public string DescribeQuery(SearchCriteria criteria) {
        string query = "XPath учасників:  " + XPathQueryBuilder.BuildMemberQuery(criteria) +
                       "\nXPath заходів:    " + XPathQueryBuilder.BuildEventQuery(criteria) + "   (відносно Member)";
        if (criteria.Keywords.Count > 0) {
            query += "\nключові слова в InnerText вузлів: " + string.Join(", ", criteria.Keywords.Select(k => $"\"{k}\""));
        }
        return query;
    }

    private static readonly XmlNodeList EmptyList = new XmlDocument().ChildNodes;

    private static ParliamentEvent ReadEvent(XmlElement node, string memberId) => new(
        node.GetAttribute(XmlNames.Type),
        Text(node, XmlNames.Title) ?? "",
        XmlValues.ParseDateTime(Text(node, XmlNames.Start), XmlNames.Start, memberId),
        XmlValues.ParseDateTime(Text(node, XmlNames.End), XmlNames.End, memberId),
        Text(node, XmlNames.Location),
        node.HasAttribute(XmlNames.Format) ? node.GetAttribute(XmlNames.Format) : null);

    private static string? Text(XmlNode node, string xpath) {
        string? text = node.SelectSingleNode(xpath)?.InnerText.Trim();
        return string.IsNullOrEmpty(text) ? null : text;
    }
}