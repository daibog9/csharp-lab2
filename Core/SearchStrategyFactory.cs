namespace Core.Search;

public enum SearchStrategyKind {
    Sax,
    Dom,
    Linq
}

public interface ISearchStrategyFactory {
    IReadOnlyList<SearchStrategyKind> Available { get; }
    IXmlSearchStrategy Create(SearchStrategyKind kind);
}

public sealed class SearchStrategyFactory : ISearchStrategyFactory {
    public IReadOnlyList<SearchStrategyKind> Available { get; } =
        [SearchStrategyKind.Sax, SearchStrategyKind.Dom, SearchStrategyKind.Linq];

    public IXmlSearchStrategy Create(SearchStrategyKind kind) => kind switch {
        SearchStrategyKind.Sax => new SaxSearchStrategy(),
        SearchStrategyKind.Dom => new DomSearchStrategy(),
        SearchStrategyKind.Linq => new LinqSearchStrategy(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Невідомий спосіб аналізу")
    };

    public static string DisplayName(SearchStrategyKind kind) => kind switch {
        SearchStrategyKind.Sax => "SAX API",
        SearchStrategyKind.Dom => "DOM API",
        SearchStrategyKind.Linq => "LINQ to XML",
        _ => kind.ToString()
    };
}