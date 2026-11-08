namespace Core.Storage;

public sealed record RemoteFile(string Id, string Name);

public sealed class RemoteStorageException(string message, Exception? inner = null) : Exception(message, inner);

public interface IRemoteStorage {
    string Name { get; }

    Task<IReadOnlyList<RemoteFile>> ListAsync(IReadOnlyCollection<string> extensions, CancellationToken cancellationToken = default);

    Task DownloadAsync(RemoteFile file, string localPath, CancellationToken cancellationToken = default);

    Task<RemoteFile> UploadAsync(string name, Stream content, string mimeType, CancellationToken cancellationToken = default);
}