using System.Collections.ObjectModel;
using System.Xml.Linq;
using Core;
using Core.Export;
using Core.Logging;
using Core.Models;
using Core.Search;
using Core.Services;
using Core.Storage;
using Ui.Services;

namespace Ui.ViewModels;

public sealed class MainViewModel : ObservableObject {
    public const string AllOption = "(усі)";
    private const string SampleXml = "StudentParliament.xml";
    private const string DefaultXsl = "StudentParliament.xsl";
    private const int MaxDriveChoices = 25;

    private enum ExportFormat {
        Xml,
        Html
    }

    private readonly ISearchStrategyFactory _factory;
    private readonly XmlSearchContext _context;
    private readonly IFileService _files;
    private readonly IDialogService _dialogs;
    private readonly IHtmlPreviewService _preview;
    private readonly IDriveConnector _drive;
    private readonly XslHtmlTransformer _transformer;
    private readonly SearchResultXmlWriter _resultWriter;

    private string? _xmlPath;
    private string _xmlName = "";
    private string? _xslPath;
    private string _xslName = DefaultXsl;
    private XmlDataCatalog? _catalog;
    private SearchResult? _lastResult;
    private SearchCriteria? _lastCriteria;

    private StrategyOption? _selectedStrategy;
    private List<string> _faculties = [AllOption];
    private List<string> _departments = [AllOption];
    private List<string> _specialties = [AllOption];
    private List<string> _positions = [AllOption];
    private List<string> _eventTypes = [AllOption];
    private string? _selectedFaculty = AllOption;
    private string? _selectedDepartment = AllOption;
    private string? _selectedSpecialty = AllOption;
    private string? _selectedPosition = AllOption;
    private string? _selectedEventType = AllOption;
    private string _keywords = "";
    private bool _useDateRange;
    private DateTime? _dateFrom = DateTime.Today;
    private DateTime? _dateTo = DateTime.Today;
    private bool _isBusy;
    private string _fileSummary = "Файл не обрано. Відкрийте XML-файл, файл-приклад або файл з Google Drive.";
    private string _xslInfo = $"XSL-документ {DefaultXsl} (вбудований)";
    private string _driveStatus = "Не підключено. Під час першої дії відкриється вхід у Google.";
    private string _statusText = "Очікування файлу";
    private string _criteriaText = "";
    private string _queryText = "";
    private string _emptyText = "Тут з'являться результати пошуку.";

    public MainViewModel(
        ISearchStrategyFactory factory,
        IFileService files,
        IDialogService dialogs,
        IAppLifetimeService lifetime,
        IHtmlPreviewService preview,
        IDriveConnector drive,
        XslHtmlTransformer transformer,
        SearchResultXmlWriter resultWriter) {
        _factory = factory;
        _files = files;
        _dialogs = dialogs;
        _preview = preview;
        _drive = drive;
        _transformer = transformer;
        _resultWriter = resultWriter;

        StrategyOptions = factory.Available
            .Select(kind => new StrategyOption(kind, SearchStrategyFactory.DisplayName(kind)))
            .ToList();
        _selectedStrategy = StrategyOptions[0];
        _context = new XmlSearchContext(factory.Create(_selectedStrategy.Kind));

        OpenXmlCommand = new AsyncCommand(() => RunAsync("Не вдалося відкрити XML-файл", OpenXmlAsync), () => !IsBusy);
        OpenSampleCommand = new AsyncCommand(() => RunAsync("Не вдалося відкрити файл-приклад", OpenSampleAsync), () => !IsBusy);
        OpenXslCommand = new AsyncCommand(() => RunAsync("Не вдалося відкрити XSL-документ", OpenXslAsync), () => !IsBusy);
        SearchCommand = new AsyncCommand(() => RunAsync("Помилка під час аналізу", SearchAsync), () => IsFileLoaded && !IsBusy);
        TransformCommand = new AsyncCommand(() => RunAsync("Помилка XSL-трансформації", TransformAsync), () => IsFileLoaded && !IsBusy);
        ClearCommand = new AsyncCommand(ClearAsync);
        ExitCommand = new AsyncCommand(lifetime.RequestExitAsync);
        ShowLogCommand = new AsyncCommand(() => RunAsync("Не вдалося відкрити протокол", ShowLogAsync), () => !IsBusy);

        OpenXmlFromDriveCommand = new AsyncCommand(
            () => RunAsync("Google Drive", OpenXmlFromDriveAsync), () => !IsBusy);
        OpenXslFromDriveCommand = new AsyncCommand(
            () => RunAsync("Google Drive", OpenXslFromDriveAsync), () => !IsBusy);
        SaveSourcesToDriveCommand = new AsyncCommand(
            () => RunAsync("Google Drive", SaveSourcesToDriveAsync), () => IsFileLoaded && !IsBusy);
        ExportXmlToDriveCommand = new AsyncCommand(
            () => RunAsync("Google Drive", () => ExportToDriveAsync(ExportFormat.Xml)), () => HasSearchResult && !IsBusy);
        ExportHtmlToDriveCommand = new AsyncCommand(
            () => RunAsync("Google Drive", () => ExportToDriveAsync(ExportFormat.Html)), () => HasSearchResult && !IsBusy);
    }

    public AsyncCommand OpenXmlCommand { get; }
    public AsyncCommand OpenSampleCommand { get; }
    public AsyncCommand OpenXslCommand { get; }
    public AsyncCommand SearchCommand { get; }
    public AsyncCommand TransformCommand { get; }
    public AsyncCommand ClearCommand { get; }
    public AsyncCommand ExitCommand { get; }
    public AsyncCommand ShowLogCommand { get; }
    public AsyncCommand OpenXmlFromDriveCommand { get; }
    public AsyncCommand OpenXslFromDriveCommand { get; }
    public AsyncCommand SaveSourcesToDriveCommand { get; }
    public AsyncCommand ExportXmlToDriveCommand { get; }
    public AsyncCommand ExportHtmlToDriveCommand { get; }

    public List<StrategyOption> StrategyOptions { get; }

    public StrategyOption? SelectedStrategy {
        get => _selectedStrategy;
        set {
            if (value is null || !SetProperty(ref _selectedStrategy, value)) return;
            _context.SetStrategy(_factory.Create(value.Kind));
            if (IsFileLoaded) StatusText = $"Обрано {value.Name}. Натисніть «Пошук».";
        }
    }

    public List<string> Faculties { get => _faculties; private set => SetProperty(ref _faculties, value); }
    public List<string> Departments { get => _departments; private set => SetProperty(ref _departments, value); }
    public List<string> Specialties { get => _specialties; private set => SetProperty(ref _specialties, value); }
    public List<string> Positions { get => _positions; private set => SetProperty(ref _positions, value); }
    public List<string> EventTypes { get => _eventTypes; private set => SetProperty(ref _eventTypes, value); }

    public string? SelectedFaculty { get => _selectedFaculty; set => SetProperty(ref _selectedFaculty, value); }
    public string? SelectedDepartment { get => _selectedDepartment; set => SetProperty(ref _selectedDepartment, value); }
    public string? SelectedSpecialty { get => _selectedSpecialty; set => SetProperty(ref _selectedSpecialty, value); }
    public string? SelectedPosition { get => _selectedPosition; set => SetProperty(ref _selectedPosition, value); }
    public string? SelectedEventType { get => _selectedEventType; set => SetProperty(ref _selectedEventType, value); }
    public string Keywords { get => _keywords; set => SetProperty(ref _keywords, value ?? ""); }
    public bool UseDateRange { get => _useDateRange; set => SetProperty(ref _useDateRange, value); }
    public DateTime? DateFrom { get => _dateFrom; set => SetProperty(ref _dateFrom, value); }
    public DateTime? DateTo { get => _dateTo; set => SetProperty(ref _dateTo, value); }

    public ObservableCollection<MemberCardViewModel> Results { get; } = [];

    public bool IsFileLoaded => _xmlPath is not null;
    public bool HasSearchResult => _lastResult is not null;
    public bool CanEditFilters => IsFileLoaded && !IsBusy;
    public bool IsIdle => !IsBusy;
    public bool HasQuery => QueryText.Length > 0;

    public bool IsBusy {
        get => _isBusy;
        private set {
            if (!SetProperty(ref _isBusy, value)) return;
            OnPropertyChanged(nameof(IsIdle));
            OnPropertyChanged(nameof(CanEditFilters));
            RefreshCommands();
        }
    }

    public string FileSummary { get => _fileSummary; private set => SetProperty(ref _fileSummary, value); }
    public string XslInfo { get => _xslInfo; private set => SetProperty(ref _xslInfo, value); }
    public string DriveStatus { get => _driveStatus; private set => SetProperty(ref _driveStatus, value); }
    public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }
    public string CriteriaText { get => _criteriaText; private set => SetProperty(ref _criteriaText, value); }
    public string EmptyText { get => _emptyText; private set => SetProperty(ref _emptyText, value); }

    public string QueryText {
        get => _queryText;
        private set {
            if (SetProperty(ref _queryText, value)) OnPropertyChanged(nameof(HasQuery));
        }
    }

    private async Task OpenXmlAsync() {
        PickedFile? file = await _files.PickXmlAsync();
        if (file is not null) await LoadXmlAsync(file);
    }

    private async Task OpenSampleAsync() {
        PickedFile file = await _files.CopyBundledAsync(SampleXml);
        await LoadXmlAsync(file);
    }

    private async Task LoadXmlAsync(PickedFile file, string source = "") {
        XmlDataCatalog catalog = await Task.Run(() => XmlDataCatalog.Load(file.LocalPath));

        _xmlPath = file.LocalPath;
        _xmlName = file.DisplayName;
        _catalog = catalog;

        Faculties = WithAllOption(catalog.Faculties);
        Departments = WithAllOption(catalog.Departments);
        Specialties = WithAllOption(catalog.Specialties);
        Positions = WithAllOption(catalog.Positions);
        EventTypes = WithAllOption(catalog.EventTypes);

        ResetSearchParameters();
        ClearOutput();

        FileSummary = $"{file.DisplayName}{source}  ·  {catalog.University} {catalog.AcademicYear}  ·  " +
                      $"учасників {catalog.MemberCount}, заходів {catalog.EventCount}  ·  " +
                      $"атрибутів у Member від {catalog.MinAttributeCount} до {catalog.MaxAttributeCount}";
        StatusText = "Файл завантажено. Оберіть параметри й натисніть «Пошук».";

        OnPropertyChanged(nameof(IsFileLoaded));
        OnPropertyChanged(nameof(CanEditFilters));
        RefreshCommands();
    }

    private async Task OpenXslAsync() {
        PickedFile? file = await _files.PickXslAsync();
        if (file is null) return;

        await Task.Run(() => _transformer.ValidateStylesheet(file.LocalPath));
        UseXsl(file.LocalPath, file.DisplayName, "");
    }

    private async Task SearchAsync() {
        if (_xmlPath is null) return;

        string xmlPath = _xmlPath;
        SearchCriteria criteria = BuildCriteria();
        SearchResult result = await Task.Run(() => _context.Execute(xmlPath, criteria));

        SetLastResult(result, criteria);

        Results.Clear();
        int number = 1;
        foreach (ParliamentMember member in result.Members) {
            Results.Add(new MemberCardViewModel(member, number++));
        }

        StatusText = $"{result.StrategyName}  ·  знайдено учасників {result.Members.Count}, " +
                     $"записів про заходи {result.EventCount}  ·  {result.Elapsed.TotalMilliseconds:0.##} мс";
        CriteriaText = "Параметри пошуку  ·  " + criteria.Describe();
        QueryText = result.Query;
        EmptyText = "За заданими параметрами нічого не знайдено.";
    }

    private async Task TransformAsync() {
        if (_xmlPath is null) return;

        string xmlPath = _xmlPath;
        string xslPath = await GetXslPathAsync();
        string htmlPath = _files.CreateHtmlOutputPath(Path.GetFileNameWithoutExtension(_xmlName));
        SearchResult? result = _lastResult;
        SearchCriteria? criteria = _lastCriteria;

        await Task.Run(() => {
            if (result is not null && criteria is not null) {
                _transformer.Transform(_resultWriter.Build(xmlPath, result, criteria), xslPath, htmlPath);
            } else {
                _transformer.TransformFile(xmlPath, xslPath, htmlPath);
            }
        });

        string caption = result is null
            ? $"Увесь документ {_xmlName}"
            : $"Результати пошуку ({result.StrategyName})  ·  учасників {result.Members.Count}";
        await _preview.ShowAsync(htmlPath, caption);
    }

    private async Task OpenXmlFromDriveAsync() {
        RemoteFile? file = await ChooseDriveFileAsync("XML-файл на Google Drive", [".xml"]);
        if (file is null) return;

        string localPath = await DownloadAsync(file);
        await LoadXmlAsync(new PickedFile(localPath, file.Name), " (Google Drive)");
    }

    private async Task OpenXslFromDriveAsync() {
        RemoteFile? file = await ChooseDriveFileAsync("XSL-документ на Google Drive", [".xsl", ".xslt"]);
        if (file is null) return;

        string localPath = await DownloadAsync(file);
        await Task.Run(() => _transformer.ValidateStylesheet(localPath));
        UseXsl(localPath, file.Name, " (Google Drive)");
    }

    private async Task SaveSourcesToDriveAsync() {
        if (_xmlPath is null) return;

        IRemoteStorage drive = await _drive.GetStorageAsync();
        string xslPath = await GetXslPathAsync();
        var uploads = new[] {
            (Local: _xmlPath, Name: _xmlName, Mime: "application/xml"),
            (Local: xslPath, Name: _xslName, Mime: "application/xslt+xml")
        };

        var saved = new List<string>();
        foreach (var upload in uploads) {
            RemoteFile file = await Task.Run(async () => {
                await using FileStream stream = File.OpenRead(upload.Local);
                return await drive.UploadAsync(upload.Name, stream, upload.Mime);
            });
            EventLogger.Instance.Log(EventLevel.Saving, $"Збережено файл {file.Name} ({drive.Name})");
            saved.Add($"«{file.Name}»");
        }

        await _dialogs.ShowInfoAsync("Google Drive", $"На Google Drive збережено файли {string.Join(" і ", saved)}.");
    }

    private async Task ExportToDriveAsync(ExportFormat format) {
        if (_xmlPath is null || _lastResult is null || _lastCriteria is null) return;

        string extension = format == ExportFormat.Xml ? ".xml" : ".html";
        string? name = await _dialogs.PromptAsync(
            "Збереження на Google Drive",
            "Назва файлу для відфільтрованого фрагмента",
            $"{Path.GetFileNameWithoutExtension(_xmlName)}_фрагмент_{DateTime.Now:yyyyMMdd_HHmmss}{extension}");
        if (string.IsNullOrWhiteSpace(name)) return;

        string xmlPath = _xmlPath;
        SearchResult result = _lastResult;
        SearchCriteria criteria = _lastCriteria;

        IRemoteStorage drive = await _drive.GetStorageAsync();
        FragmentExporter exporter = format == ExportFormat.Xml
            ? new XmlFragmentExporter(drive)
            : new HtmlFragmentExporter(drive, await GetXslPathAsync(), _transformer);

        RemoteFile saved = await Task.Run(async () => {
            XDocument fragment = _resultWriter.Build(xmlPath, result, criteria);
            return await exporter.ExportAsync(fragment, name);
        });

        await _dialogs.ShowInfoAsync("Google Drive", $"Відфільтрований фрагмент збережено у файл «{saved.Name}».");
    }

    private async Task ShowLogAsync() {
        string path = EventLogger.Instance.FilePath;
        if (!File.Exists(path)) {
            string reason = EventLogger.Instance.LastError is { } error ? $" {error}" : "";
            await _dialogs.ShowInfoAsync("Протокол подій", $"Протокол ще порожній.{reason}");
            return;
        }
        await _preview.ShowTextAsync(path, "Протокол подій");
    }

    private Task ClearAsync() {
        ResetSearchParameters();
        ClearOutput();
        StatusText = IsFileLoaded ? "Поле виводу та параметри пошуку очищено." : "Очікування файлу";
        return Task.CompletedTask;
    }

    private async Task<RemoteFile?> ChooseDriveFileAsync(string title, string[] extensions) {
        IRemoteStorage drive = await _drive.GetStorageAsync();
        IReadOnlyList<RemoteFile> files = await Task.Run(() => drive.ListAsync(extensions));
        if (files.Count == 0) {
            await _dialogs.ShowInfoAsync("Google Drive", $"На Google Drive немає файлів {string.Join(", ", extensions)}.");
            return null;
        }

        var shown = files.Take(MaxDriveChoices).ToList();
        var labels = new List<string>();
        foreach (RemoteFile file in shown) {
            string label = file.Name;
            for (int copy = 2; labels.Contains(label); copy++) label = $"{file.Name} ({copy})";
            labels.Add(label);
        }

        string? choice = await _dialogs.ChooseAsync(title, labels);
        return choice is null ? null : shown[labels.IndexOf(choice)];
    }

    private async Task<string> DownloadAsync(RemoteFile file) {
        IRemoteStorage drive = await _drive.GetStorageAsync();
        string localPath = _files.CreateDownloadPath(file.Name);
        await Task.Run(() => drive.DownloadAsync(file, localPath));
        return localPath;
    }

    private async Task<string> GetXslPathAsync() =>
        _xslPath ?? (await _files.CopyBundledAsync(DefaultXsl)).LocalPath;

    private void UseXsl(string path, string name, string source) {
        _xslPath = path;
        _xslName = name;
        XslInfo = $"XSL-документ {name}{source}";
    }

    private SearchCriteria BuildCriteria() =>
        new SearchCriteriaBuilder()
            .WhereMemberAttribute(XmlNames.Faculty, Selected(SelectedFaculty))
            .WhereMemberAttribute(XmlNames.Department, Selected(SelectedDepartment))
            .WhereMemberAttribute(XmlNames.Specialty, Selected(SelectedSpecialty))
            .WhereMemberAttribute(XmlNames.Position, Selected(SelectedPosition))
            .WithEventType(Selected(SelectedEventType))
            .WithKeywords(Keywords)
            .Between(UseDateRange ? DateFrom : null, UseDateRange ? DateTo : null)
            .Build();

    private static string? Selected(string? value) => value is null or AllOption ? null : value;

    private static List<string> WithAllOption(IEnumerable<string> values) => [AllOption, .. values];

    private void SetLastResult(SearchResult? result, SearchCriteria? criteria) {
        _lastResult = result;
        _lastCriteria = criteria;
        OnPropertyChanged(nameof(HasSearchResult));
        RefreshCommands();
    }

    private void ResetSearchParameters() {
        _selectedFaculty = _selectedDepartment = _selectedSpecialty = _selectedPosition = _selectedEventType = AllOption;
        _keywords = "";
        _useDateRange = false;
        _dateFrom = _catalog?.FirstEventStart?.Date ?? DateTime.Today;
        _dateTo = _catalog?.LastEventEnd?.Date ?? DateTime.Today;

        foreach (string name in new[] {
                     nameof(SelectedFaculty), nameof(SelectedDepartment), nameof(SelectedSpecialty),
                     nameof(SelectedPosition), nameof(SelectedEventType), nameof(Keywords),
                     nameof(UseDateRange), nameof(DateFrom), nameof(DateTo) }) {
            OnPropertyChanged(name);
        }
    }

    private void ClearOutput() {
        Results.Clear();
        SetLastResult(null, null);
        CriteriaText = "";
        QueryText = "";
        EmptyText = IsFileLoaded
            ? "Натисніть «Пошук». Без параметрів буде показано всі записи."
            : "Тут з'являться результати пошуку.";
    }

    private void RefreshCommands() {
        OpenXmlCommand.RaiseCanExecuteChanged();
        OpenSampleCommand.RaiseCanExecuteChanged();
        OpenXslCommand.RaiseCanExecuteChanged();
        SearchCommand.RaiseCanExecuteChanged();
        TransformCommand.RaiseCanExecuteChanged();
        ShowLogCommand.RaiseCanExecuteChanged();
        OpenXmlFromDriveCommand.RaiseCanExecuteChanged();
        OpenXslFromDriveCommand.RaiseCanExecuteChanged();
        SaveSourcesToDriveCommand.RaiseCanExecuteChanged();
        ExportXmlToDriveCommand.RaiseCanExecuteChanged();
        ExportHtmlToDriveCommand.RaiseCanExecuteChanged();
    }

    private async Task RunAsync(string errorTitle, Func<Task> action) {
        IsBusy = true;
        try {
            await action();
        } catch (Exception ex) {
            await _dialogs.ShowErrorAsync(errorTitle, ex.Message);
        } finally {
            IsBusy = false;
            DriveStatus = _drive.IsAuthorized
                ? "Підключено."
                : "Не підключено. Під час першої дії відкриється вхід у Google.";
        }
    }
}