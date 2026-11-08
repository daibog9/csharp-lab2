using System.Xml.Linq;
using Core.Logging;
using Core.Services;
using Core.Storage;

namespace Core.Export;

public abstract class FragmentExporter(IRemoteStorage storage) {
    protected abstract IFragmentWriter CreateWriter();

    public async Task<RemoteFile> ExportAsync(XDocument fragment, string fileName, CancellationToken cancellationToken = default) {
        IFragmentWriter writer = CreateWriter();
        string name = WithExtension(fileName, writer.Extension);

        await using FileStream file = OpenFile();
        writer.Write(fragment, file, name);
        await file.FlushAsync(cancellationToken);
        file.Position = 0;

        RemoteFile saved = await storage.UploadAsync(name, file, writer.MimeType, cancellationToken);
        EventLogger.Instance.Log(EventLevel.Saving,
            $"Збережено відфільтрований фрагмент у файл {saved.Name} ({storage.Name})");
        return saved;
    }

    private static FileStream OpenFile() =>
        new(Path.Combine(Path.GetTempPath(), $"fragment-{Guid.NewGuid():N}.tmp"),
            FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 4096,
            FileOptions.DeleteOnClose | FileOptions.Asynchronous);

    private static readonly string[] KnownExtensions = [".xml", ".html", ".htm"];

    private static string WithExtension(string fileName, string extension) {
        string name = Path.GetFileName(fileName.Trim());
        if (KnownExtensions.Contains(Path.GetExtension(name), StringComparer.OrdinalIgnoreCase)) {
            name = Path.GetFileNameWithoutExtension(name);
        }
        if (string.IsNullOrWhiteSpace(name)) name = "fragment";
        return name + extension;
    }
}

public sealed class XmlFragmentExporter(IRemoteStorage storage) : FragmentExporter(storage) {
    protected override IFragmentWriter CreateWriter() => new XmlFragmentWriter();
}

public sealed class HtmlFragmentExporter(IRemoteStorage storage, string xslPath, XslHtmlTransformer transformer)
    : FragmentExporter(storage) {
    protected override IFragmentWriter CreateWriter() => new HtmlFragmentWriter(xslPath, transformer);
}