using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Core.Models;
using Core.Search;

namespace Core.Services;

public sealed class SearchResultXmlWriter {
    private const string DateFormat = "yyyy-MM-ddTHH:mm:ss";
    private static readonly string[] ServiceAttributes = ["strategy", "filter", "generated"];

    public XDocument Build(string sourceXmlPath, SearchResult result, SearchCriteria criteria) {
        var root = new XElement(XmlNames.Root,
            ReadRootAttributes(sourceXmlPath),
            new XAttribute("strategy", result.StrategyName),
            new XAttribute("filter", criteria.Describe()),
            new XAttribute("generated", DateTime.Now.ToString(DateFormat, CultureInfo.InvariantCulture)),
            result.Members.Select(ToElement));

        return new XDocument(new XDeclaration("1.0", "utf-8", null), root);
    }

    private static List<XAttribute> ReadRootAttributes(string path) {
        var attributes = new List<XAttribute>();
        using XmlReader reader = XmlReader.Create(path, XmlValues.ReaderSettings());
        reader.MoveToContent();
        if (reader.MoveToFirstAttribute()) {
            do {
                if (!ServiceAttributes.Contains(reader.Name)) attributes.Add(new XAttribute(reader.Name, reader.Value));
            } while (reader.MoveToNextAttribute());
        }
        return attributes;
    }

    private static XElement ToElement(ParliamentMember member) =>
        new(XmlNames.Member,
            member.Attributes.Select(a => new XAttribute(a.Key, a.Value)),
            new XElement(XmlNames.FullName,
                new XElement(XmlNames.LastName, member.LastName),
                new XElement(XmlNames.FirstName, member.FirstName),
                member.Patronymic is null ? null : new XElement(XmlNames.Patronymic, member.Patronymic)),
            member.Events.Select(ToElement));

    private static XElement ToElement(ParliamentEvent ev) =>
        new(XmlNames.Event,
            new XAttribute(XmlNames.Type, ev.Type),
            ev.Format is null ? null : new XAttribute(XmlNames.Format, ev.Format),
            new XElement(XmlNames.Title, ev.Title),
            new XElement(XmlNames.Start, ev.Start.ToString(DateFormat, CultureInfo.InvariantCulture)),
            new XElement(XmlNames.End, ev.End.ToString(DateFormat, CultureInfo.InvariantCulture)),
            ev.Location is null ? null : new XElement(XmlNames.Location, ev.Location));
}