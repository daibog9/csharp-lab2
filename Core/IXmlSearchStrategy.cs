using Core.Models;

namespace Core.Search;

public interface IXmlSearchStrategy {
    string Name { get; }

    IReadOnlyList<ParliamentMember> Search(string xmlPath, SearchCriteria criteria);

    string DescribeQuery(SearchCriteria criteria);
}