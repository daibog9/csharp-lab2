using Core;
using Core.Export;
using Core.Logging;
using Core.Models;
using Core.Search;
using Core.Services;
using Core.Storage;

Console.OutputEncoding = System.Text.Encoding.UTF8;

string dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
string xmlPath = args.Length > 0 ? args[0] : Path.Combine(dataDir, "StudentParliament.xml");
string xslPath = args.Length > 1 ? args[1] : Path.Combine(dataDir, "StudentParliament.xsl");
string outDir = Path.Combine(Directory.GetCurrentDirectory(), "out");

Directory.CreateDirectory(outDir);
EventLogger.Instance.UseFile(Path.Combine(outDir, "StudentParliament.log"));
File.Delete(EventLogger.Instance.FilePath);

var catalog = XmlDataCatalog.Load(xmlPath);
Console.WriteLine($"Файл {Path.GetFileName(xmlPath)}");
Console.WriteLine($"  {catalog.University}, {catalog.AcademicYear}");
Console.WriteLine($"  учасників {catalog.MemberCount}, заходів {catalog.EventCount}, " +
                  $"атрибутів у Member від {catalog.MinAttributeCount} до {catalog.MaxAttributeCount}");
Console.WriteLine($"  факультети:      {string.Join(" | ", catalog.Faculties)}");
Console.WriteLine($"  види заходів:    {string.Join(" | ", catalog.EventTypes)}");
Console.WriteLine($"  посади:          {string.Join(" | ", catalog.Positions)}");
Console.WriteLine();

var cases = new List<(string Title, SearchCriteria Criteria)> {
    ("Усі записи", SearchCriteria.Empty),
    ("Факультет з апострофом у назві", new SearchCriteriaBuilder()
        .WhereMemberAttribute(XmlNames.Faculty, "Факультет комп'ютерних наук та кібернетики").Build()),
    ("Вид заходу «Хакатон»", new SearchCriteriaBuilder().WithEventType("Хакатон").Build()),
    ("Листопад 2026", new SearchCriteriaBuilder().Between(new DateTime(2026, 11, 1), new DateTime(2026, 11, 30)).Build()),
    ("Ключові слова «червоний актова»", new SearchCriteriaBuilder().WithKeywords("червоний, актова").Build()),
    ("Ключове слово з П.І.П. «шевчук»", new SearchCriteriaBuilder().WithKeywords("шевчук").Build()),
    ("Юридичний + Засідання + жовтень", new SearchCriteriaBuilder()
        .WhereMemberAttribute(XmlNames.Faculty, "Юридичний факультет")
        .WithEventType("Засідання")
        .Between(new DateTime(2026, 10, 31), new DateTime(2026, 10, 1))
        .Build()),
    ("Посада «Заступник голови»", new SearchCriteriaBuilder().WhereMemberAttribute(XmlNames.Position, "Заступник голови").Build()),
    ("Відділення коледжу + кафедра іншого факультету", new SearchCriteriaBuilder()
        .WhereMemberAttribute(XmlNames.Faculty, "Відділення комп'ютерних технологій фахового коледжу")
        .WhereMemberAttribute(XmlNames.Department, "Кафедра фінансів").Build()),
};

var factory = new SearchStrategyFactory();
var context = new XmlSearchContext(factory.Create(SearchStrategyKind.Sax));
bool allSame = true;
SearchResult? sample = null;
SearchCriteria? sampleCriteria = null;

foreach (var (title, criteria) in cases) {
    Console.WriteLine($"=== {title}");
    Console.WriteLine($"    параметри: {criteria.Describe()}");

    var results = new List<SearchResult>();
    foreach (SearchStrategyKind kind in factory.Available) {
        context.SetStrategy(factory.Create(kind));
        results.Add(context.Execute(xmlPath, criteria));
    }

    SearchResult first = results[0];
    foreach (ParliamentMember m in first.Members) {
        Console.WriteLine($"    • {m.FullName} ({m.Faculty})");
        foreach (ParliamentEvent e in m.Events) {
            Console.WriteLine($"        {e.Type}: {e.Title} [{e.Start:dd.MM.yyyy HH:mm} – {e.End:dd.MM.yyyy HH:mm}]");
        }
    }

    string expected = string.Join("\n", first.Members.Select(m => m.Signature()));
    foreach (SearchResult r in results) {
        bool same = string.Join("\n", r.Members.Select(m => m.Signature())) == expected;
        allSame &= same;
        Console.WriteLine($"    {r.StrategyName,-12} учасників {r.Members.Count,2}, заходів {r.EventCount,2}, " +
                          $"{r.Elapsed.TotalMilliseconds,7:0.00} мс  {(same ? "✓ збігається" : "✗ ВІДРІЗНЯЄТЬСЯ")}");
        Console.WriteLine("      запит: " + r.Query.Replace("\n", "\n             "));
    }
    Console.WriteLine();

    if (title.StartsWith("Листопад")) {
        sample = first;
        sampleCriteria = criteria;
    }
}

var transformer = new XslHtmlTransformer();
string fullHtml = Path.Combine(outDir, "StudentParliament.html");
transformer.TransformFile(xmlPath, xslPath, fullHtml);
Console.WriteLine($"HTML усього документа:    {fullHtml}");

if (sample is not null && sampleCriteria is not null) {
    var fragment = new SearchResultXmlWriter().Build(xmlPath, sample, sampleCriteria);
    string searchHtml = Path.Combine(outDir, "SearchResult.html");
    transformer.Transform(fragment, xslPath, searchHtml);
    Console.WriteLine($"HTML результатів пошуку: {searchHtml}");

    var storage = new LocalFolderStorage(Path.Combine(outDir, "storage"));
    FragmentExporter[] exporters = [
        new XmlFragmentExporter(storage),
        new HtmlFragmentExporter(storage, xslPath, transformer)
    ];
    foreach (FragmentExporter exporter in exporters) {
        RemoteFile saved = await exporter.ExportAsync(fragment, "листопад_2026");
        Console.WriteLine($"{exporter.GetType().Name,-22} → {saved.Id}");
    }
}

Console.WriteLine();
Console.WriteLine($"Протокол {EventLogger.Instance.FilePath}");
foreach (string line in EventLogger.Instance.ReadAll().TakeLast(6)) Console.WriteLine("  " + line);

Console.WriteLine(allSame ? "\nУсі три стратегії дали однакові результати." : "\nСТРАТЕГІЇ РОЗІЙШЛИСЯ!");
return allSame ? 0 : 1;