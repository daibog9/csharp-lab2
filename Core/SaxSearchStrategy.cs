using System.Text;
using Core.Models;

namespace Core.Search;

public sealed class SaxSearchStrategy : IXmlSearchStrategy {
    public string Name => "SAX API";

    public IReadOnlyList<ParliamentMember> Search(string xmlPath, SearchCriteria criteria) {
        var handler = new ParliamentSaxHandler(criteria);
        SaxParser.Parse(xmlPath, handler);
        return handler.Results;
    }

    public string DescribeQuery(SearchCriteria criteria) {
        var rules = new StringBuilder();

        rules.Append("startElement(Member) → ");
        rules.Append(criteria.MemberAttributes.Count == 0
            ? "приймаються всі учасники"
            : string.Join(" ∧ ", criteria.MemberAttributes.Select(a => $"@{a.Key} == \"{a.Value}\"")));

        rules.Append("\nstartElement(Event) → ");
        rules.Append(criteria.EventType is null ? "приймаються всі заходи" : $"@type == \"{criteria.EventType}\"");

        if (criteria.From is not null || criteria.To is not null) {
            rules.Append("\nendElement(Event) → End ≥ ")
                 .Append(criteria.From?.ToString("yyyy-MM-dd HH:mm") ?? "−∞")
                 .Append(" ∧ Start ≤ ")
                 .Append(criteria.To?.ToString("yyyy-MM-dd HH:mm") ?? "+∞");
        }

        if (criteria.Keywords.Count > 0) {
            rules.Append("\nendElement(Member) → текст містить ")
                 .Append(string.Join(" ∧ ", criteria.Keywords.Select(k => $"\"{k}\"")));
        }
        return rules.ToString();
    }

    private sealed class ParliamentSaxHandler(SearchCriteria criteria) : ISaxContentHandler {
        private readonly List<ParliamentMember> _results = [];
        private readonly StringBuilder _text = new();

        private bool _inMember;
        private bool _skipMember;
        private List<KeyValuePair<string, string>> _attributes = [];
        private string _lastName = "";
        private string _firstName = "";
        private string? _patronymic;
        private List<ParliamentEvent> _candidates = [];
        private int _eventCount;

        private bool _inEvent;
        private bool _skipEvent;
        private string _eventType = "";
        private string? _eventFormat;
        private string? _title;
        private string? _start;
        private string? _end;
        private string? _location;

        public IReadOnlyList<ParliamentMember> Results => _results;

        public void StartDocument() => _results.Clear();

        public void EndDocument() { }

        public void StartElement(string name, IReadOnlyList<KeyValuePair<string, string>> attributes) {
            _text.Clear();

            if (name == XmlNames.Member) {
                _inMember = true;
                _skipMember = !MatchesMemberAttributes(attributes);
                _attributes = [.. attributes];
                _lastName = _firstName = "";
                _patronymic = null;
                _candidates = [];
                _eventCount = 0;
                return;
            }

            if (name == XmlNames.Event && _inMember && !_skipMember) {
                _inEvent = true;
                _eventCount++;
                _eventType = Find(attributes, XmlNames.Type) ?? "";
                _eventFormat = Find(attributes, XmlNames.Format);
                _skipEvent = criteria.EventType is not null && _eventType != criteria.EventType;
                _title = _start = _end = _location = null;
            }
        }

        public void Characters(string text) {
            if (_inMember && !_skipMember) _text.Append(text);
        }

        public void EndElement(string name) {
            if (name == XmlNames.Member) {
                if (_inMember && !_skipMember) FinishMember();
                _inMember = false;
                _skipMember = false;
                return;
            }
            if (!_inMember || _skipMember) return;

            string text = _text.ToString().Trim();
            _text.Clear();

            switch (name) {
                case XmlNames.LastName: _lastName = text; break;
                case XmlNames.FirstName: _firstName = text; break;
                case XmlNames.Patronymic: _patronymic = text; break;
                case XmlNames.Title when _inEvent: _title = text; break;
                case XmlNames.Start when _inEvent: _start = text; break;
                case XmlNames.End when _inEvent: _end = text; break;
                case XmlNames.Location when _inEvent: _location = text; break;
                case XmlNames.Event when _inEvent:
                    FinishEvent();
                    _inEvent = false;
                    break;
            }
        }

        private void FinishEvent() {
            if (_skipEvent) return;

            string? id = Find(_attributes, XmlNames.Id);
            var ev = new ParliamentEvent(
                _eventType,
                _title ?? "",
                XmlValues.ParseDateTime(_start, XmlNames.Start, id),
                XmlValues.ParseDateTime(_end, XmlNames.End, id),
                string.IsNullOrEmpty(_location) ? null : _location,
                _eventFormat);

            if (ev.Overlaps(criteria.From, criteria.To)) _candidates.Add(ev);
        }

        private void FinishMember() {
            var memberTexts = ParliamentMember.SearchableTexts(_attributes, _lastName, _firstName, _patronymic).ToList();
            var matched = _candidates
                .Where(e => KeywordMatcher.Matches(criteria.Keywords, memberTexts, e.SearchableTexts()))
                .ToList();

            bool include = matched.Count > 0 ||
                           (_eventCount == 0 && !criteria.HasEventFilter &&
                            KeywordMatcher.Matches(criteria.Keywords, memberTexts, []));

            if (include) {
                _results.Add(new ParliamentMember(_attributes, _lastName, _firstName, _patronymic, matched));
            }
        }

        private bool MatchesMemberAttributes(IReadOnlyList<KeyValuePair<string, string>> attributes) {
            foreach (var (name, value) in criteria.MemberAttributes) {
                if (Find(attributes, name) != value) return false;
            }
            return true;
        }

        private static string? Find(IReadOnlyList<KeyValuePair<string, string>> attributes, string name) {
            foreach (var (key, value) in attributes) {
                if (key == name) return value;
            }
            return null;
        }
    }
}