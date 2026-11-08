using System.Diagnostics;
using Core.Logging;
using Core.Models;

namespace Core.Search;

public sealed class XmlSearchContext {
    private IXmlSearchStrategy _strategy;

    public XmlSearchContext(IXmlSearchStrategy strategy) {
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
    }

    public IXmlSearchStrategy Strategy => _strategy;

    public void SetStrategy(IXmlSearchStrategy strategy) {
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
    }

    public SearchResult Execute(string xmlPath, SearchCriteria criteria) {
        if (!File.Exists(xmlPath)) throw new FileNotFoundException("XML-файл не знайдено.", xmlPath);

        var stopwatch = Stopwatch.StartNew();
        IReadOnlyList<ParliamentMember> members = _strategy.Search(xmlPath, criteria);
        stopwatch.Stop();

        var result = new SearchResult(_strategy.Name, members, _strategy.DescribeQuery(criteria), stopwatch.Elapsed);
        EventLogger.Instance.Log(EventLevel.Filtering,
            $"Параметри: {criteria.Describe()}. Спосіб аналізу {result.StrategyName}, " +
            $"знайдено учасників {result.Members.Count}, записів про заходи {result.EventCount}");
        return result;
    }
}