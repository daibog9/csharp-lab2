using System.Globalization;
using System.Text;
using Core.Models;

namespace Core.Search;

public static class XPathQueryBuilder {
    public static string BuildMemberQuery(SearchCriteria criteria) {
        var xpath = new StringBuilder($"/{XmlNames.Root}/{XmlNames.Member}");

        foreach (var (name, value) in criteria.MemberAttributes) {
            xpath.Append($"[@{name}={Literal(value)}]");
        }

        if (criteria.HasEventFilter) {
            xpath.Append('[').Append(BuildEventQuery(criteria)).Append(']');
        }
        return xpath.ToString();
    }

    public static string BuildEventQuery(SearchCriteria criteria) {
        var xpath = new StringBuilder(XmlNames.Event);

        if (criteria.EventType is not null) {
            xpath.Append($"[@{XmlNames.Type}={Literal(criteria.EventType)}]");
        }
        if (criteria.From is { } from) {
            xpath.Append($"[{DateAsNumber(XmlNames.End)} >= {ToNumber(from)}]");
        }
        if (criteria.To is { } to) {
            xpath.Append($"[{DateAsNumber(XmlNames.Start)} <= {ToNumber(to)}]");
        }
        return xpath.ToString();
    }

    public static string Literal(string value) {
        if (!value.Contains('\'')) return $"'{value}'";
        if (!value.Contains('"')) return $"\"{value}\"";
        return "concat('" + value.Replace("'", "', \"'\", '") + "')";
    }

    private static string DateAsNumber(string node) =>
        $"number(substring(translate({node}, '-:T', ''), 1, 12))";

    private static string ToNumber(DateTime value) =>
        value.ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture);
}