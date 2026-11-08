using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Core.Search;

namespace Core.Services;

public sealed class XmlDataCatalog {
    private XmlDataCatalog() { }

    public string? University { get; private init; }
    public string? AcademicYear { get; private init; }
    public int MemberCount { get; private init; }
    public int EventCount { get; private init; }
    public int MinAttributeCount { get; private init; }
    public int MaxAttributeCount { get; private init; }

    public IReadOnlyList<string> Faculties { get; private init; } = [];
    public IReadOnlyList<string> Departments { get; private init; } = [];
    public IReadOnlyList<string> Specialties { get; private init; } = [];
    public IReadOnlyList<string> Positions { get; private init; } = [];
    public IReadOnlyList<string> EventTypes { get; private init; } = [];

    public DateTime? FirstEventStart { get; private init; }
    public DateTime? LastEventEnd { get; private init; }

    public static XmlDataCatalog Load(string xmlPath) {
        XDocument document;
        try {
            using XmlReader reader = XmlReader.Create(xmlPath, XmlValues.ReaderSettings());
            document = XDocument.Load(reader);
        } catch (XmlException ex) {
            throw new InvalidDataException($"Файл не є коректним XML. {ex.Message}", ex);
        }

        XElement root = document.Root!;
        if (root.Name.LocalName != XmlNames.Root) {
            throw new InvalidDataException(
                $"Кореневий вузол має називатися <{XmlNames.Root}>, а у файлі знайдено <{root.Name.LocalName}>.");
        }

        var members = root.Elements(XmlNames.Member).ToList();
        if (members.Count == 0) {
            throw new InvalidDataException($"У файлі немає жодного вузла <{XmlNames.Member}>.");
        }
        var events = members.Elements(XmlNames.Event).ToList();

        var starts = events.Select(e => TryDate(e.Element(XmlNames.Start)?.Value)).OfType<DateTime>().ToList();
        var ends = events.Select(e => TryDate(e.Element(XmlNames.End)?.Value)).OfType<DateTime>().ToList();

        return new XmlDataCatalog {
            University = (string?)root.Attribute(XmlNames.University),
            AcademicYear = (string?)root.Attribute(XmlNames.AcademicYear),
            MemberCount = members.Count,
            EventCount = events.Count,
            MinAttributeCount = members.Min(m => m.Attributes().Count()),
            MaxAttributeCount = members.Max(m => m.Attributes().Count()),
            Faculties = Distinct(members.Select(m => (string?)m.Attribute(XmlNames.Faculty))),
            Departments = Distinct(members.Select(m => (string?)m.Attribute(XmlNames.Department))),
            Specialties = Distinct(members.Select(m => (string?)m.Attribute(XmlNames.Specialty))),
            Positions = Distinct(members.Select(m => (string?)m.Attribute(XmlNames.Position))),
            EventTypes = Distinct(events.Select(e => (string?)e.Attribute(XmlNames.Type))),
            FirstEventStart = starts.Count > 0 ? starts.Min() : null,
            LastEventEnd = ends.Count > 0 ? ends.Max() : null
        };
    }

    private static List<string> Distinct(IEnumerable<string?> values) =>
        values.Where(v => !string.IsNullOrWhiteSpace(v))
              .Select(v => v!)
              .Distinct()
              .Order(UkrainianComparer)
              .ToList();

    private static DateTime? TryDate(string? text) =>
        DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime value) ? value : null;

    private static readonly StringComparer UkrainianComparer = CreateComparer();

    private static StringComparer CreateComparer() {
        try {
            return StringComparer.Create(CultureInfo.GetCultureInfo("uk-UA"), ignoreCase: true);
        } catch (CultureNotFoundException) {
            return StringComparer.CurrentCultureIgnoreCase;
        }
    }
}