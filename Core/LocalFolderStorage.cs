namespace Core.Storage;

public sealed class LocalFolderStorage : IRemoteStorage {
    private readonly string _folder;

    public LocalFolderStorage(string folder) {
        _folder = Path.GetFullPath(folder);
        Directory.CreateDirectory(_folder);
    }

    public string Name => $"локальна папка {_folder}";

    public Task<IReadOnlyList<RemoteFile>> ListAsync(IReadOnlyCollection<string> extensions, CancellationToken cancellationToken = default) {
        IReadOnlyList<RemoteFile> files = Directory.EnumerateFiles(_folder)
            .Where(path => extensions.Any(e => path.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(File.GetLastWriteTime)
            .Select(path => new RemoteFile(path, Path.GetFileName(path)))
            .ToList();
        return Task.FromResult(files);
    }

    public async Task DownloadAsync(RemoteFile file, string localPath, CancellationToken cancellationToken = default) {
        await using FileStream source = File.OpenRead(file.Id);
        await using FileStream target = File.Create(localPath);
        await source.CopyToAsync(target, cancellationToken);
    }

    public async Task<RemoteFile> UploadAsync(string name, Stream content, string mimeType, CancellationToken cancellationToken = default) {
        string path = Path.Combine(_folder, Path.GetFileName(name));
        await using FileStream target = File.Create(path);
        await content.CopyToAsync(target, cancellationToken);
        return new RemoteFile(path, Path.GetFileName(path));
    }
}