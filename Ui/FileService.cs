namespace Ui.Services;

public sealed record PickedFile(string LocalPath, string DisplayName);

public interface IFileService {
    string LogFilePath { get; }
    Task<PickedFile?> PickXmlAsync();
    Task<PickedFile?> PickXslAsync();
    Task<PickedFile> CopyBundledAsync(string assetName);
    string CreateDownloadPath(string fileName);
    string CreateHtmlOutputPath(string baseName);
}

public sealed class FileService : IFileService {
    private static readonly FilePickerFileType XmlTypes = new(new Dictionary<DevicePlatform, IEnumerable<string>> {
        [DevicePlatform.MacCatalyst] = ["public.xml"],
        [DevicePlatform.iOS] = ["public.xml"],
        [DevicePlatform.WinUI] = [".xml"],
        [DevicePlatform.Android] = ["text/xml", "application/xml"]
    });

    private static readonly FilePickerFileType XslTypes = new(new Dictionary<DevicePlatform, IEnumerable<string>> {
        [DevicePlatform.MacCatalyst] = ["public.xml", "public.data"],
        [DevicePlatform.iOS] = ["public.xml", "public.data"],
        [DevicePlatform.WinUI] = [".xsl", ".xslt"],
        [DevicePlatform.Android] = ["*/*"]
    });

    public string LogFilePath => Path.Combine(OutputFolder(), "StudentParliament.log");

    public Task<PickedFile?> PickXmlAsync() => PickAsync("Оберіть XML-файл студентського парламенту", XmlTypes);

    public Task<PickedFile?> PickXslAsync() => PickAsync("Оберіть XSL-документ", XslTypes);

    public async Task<PickedFile> CopyBundledAsync(string assetName) {
        string target = Path.Combine(WorkFolder("bundled"), assetName);
        await using (Stream source = await FileSystem.OpenAppPackageFileAsync(assetName))
        await using (FileStream copy = File.Create(target)) {
            await source.CopyToAsync(copy);
        }
        return new PickedFile(target, assetName);
    }

    public string CreateDownloadPath(string fileName) =>
        Path.Combine(WorkFolder("drive"), SafeName(fileName, "drive-file"));

    public string CreateHtmlOutputPath(string baseName) =>
        Path.Combine(OutputFolder(), $"{SafeName(baseName, "StudentParliament")}_{DateTime.Now:yyyyMMdd_HHmmss}.html");

    private static async Task<PickedFile?> PickAsync(string title, FilePickerFileType types) {
        FileResult? result = await FilePicker.Default.PickAsync(new PickOptions {
            PickerTitle = title,
            FileTypes = types
        });
        if (result is null) return null;

        string target = Path.Combine(WorkFolder("input"), result.FileName);
        await using (Stream source = await result.OpenReadAsync())
        await using (FileStream copy = File.Create(target)) {
            await source.CopyToAsync(copy);
        }
        return new PickedFile(target, result.FileName);
    }

    private static string SafeName(string name, string fallback) {
        string safe = string.Concat(Path.GetFileName(name).Split(Path.GetInvalidFileNameChars()));
        return string.IsNullOrWhiteSpace(safe) ? fallback : safe;
    }

    private static string WorkFolder(string name) {
        string folder = Path.Combine(FileSystem.CacheDirectory, "lab2", name);
        Directory.CreateDirectory(folder);
        return folder;
    }

    private static string OutputFolder() {
        string downloads = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "StudentParliament");
        if (CanWrite(downloads)) return downloads;

        string appData = Path.Combine(FileSystem.AppDataDirectory, "output");
        Directory.CreateDirectory(appData);
        return appData;
    }

    private static bool CanWrite(string folder) {
        try {
            Directory.CreateDirectory(folder);
            string probe = Path.Combine(folder, ".write-test");
            File.WriteAllText(probe, "");
            File.Delete(probe);
            return true;
        } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            return false;
        }
    }
}