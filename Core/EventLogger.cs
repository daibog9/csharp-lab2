using System.Globalization;
using System.Text;

namespace Core.Logging;

public enum EventLevel {
    Filtering,
    Transformation,
    Saving
}

public sealed record LogEntry(DateTime Time, EventLevel Level, string Message) {
    public static string Title(EventLevel level) => level switch {
        EventLevel.Filtering => "Фільтрація",
        EventLevel.Transformation => "Трансформація",
        EventLevel.Saving => "Збереження",
        _ => level.ToString()
    };

    public override string ToString() =>
        $"{Time.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture)} {Title(Level)}. {Message}";
}

public sealed class EventLogger {
    private static readonly Lazy<EventLogger> LazyInstance = new(() => new EventLogger());
    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly object _sync = new();
    private string _filePath;

    private EventLogger() {
        _filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StudentParliament",
            "StudentParliament.log");
    }

    public static EventLogger Instance => LazyInstance.Value;

    public string FilePath {
        get {
            lock (_sync) return _filePath;
        }
    }

    public string? LastError { get; private set; }

    public void UseFile(string filePath) {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        lock (_sync) _filePath = Path.GetFullPath(filePath);
    }

    public LogEntry Log(EventLevel level, string message) {
        var entry = new LogEntry(DateTime.Now, level, message);
        lock (_sync) {
            try {
                string? directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.AppendAllText(_filePath, entry + Environment.NewLine, Utf8);
                LastError = null;
            } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
                LastError = ex.Message;
            }
        }
        return entry;
    }

    public IReadOnlyList<string> ReadAll() {
        lock (_sync) {
            return File.Exists(_filePath) ? File.ReadAllLines(_filePath, Utf8) : [];
        }
    }
}