using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using DriveFile = Google.Apis.Drive.v3.Data.File;
using DriveFileList = Google.Apis.Drive.v3.Data.FileList;

namespace Core.Storage;

public sealed class GoogleDriveStorage : IRemoteStorage {
    private static readonly string[] Scopes = [DriveService.Scope.DriveReadonly, DriveService.Scope.DriveFile];
    private static readonly TimeSpan AuthorizationTimeout = TimeSpan.FromMinutes(3);

    private readonly string _clientSecretJson;
    private readonly string _tokenFolder;
    private readonly Action<string> _openBrowser;
    private readonly SemaphoreSlim _authorizationLock = new(1, 1);
    private DriveService? _service;

    public GoogleDriveStorage(string clientSecretJson, string tokenFolder, Action<string> openBrowser) {
        _clientSecretJson = clientSecretJson;
        _tokenFolder = tokenFolder;
        _openBrowser = openBrowser;
    }

    public string Name => "Google Drive";

    public bool IsAuthorized => _service is not null;

    public async Task<IReadOnlyList<RemoteFile>> ListAsync(IReadOnlyCollection<string> extensions, CancellationToken cancellationToken = default) {
        DriveService service = await GetServiceAsync(cancellationToken);
        try {
            FilesResource.ListRequest request = service.Files.List();
            request.Q = "trashed = false and mimeType != 'application/vnd.google-apps.folder'";
            request.Fields = "files(id, name)";
            request.OrderBy = "modifiedTime desc";
            request.PageSize = 200;

            DriveFileList result = await request.ExecuteAsync(cancellationToken);
            return (result.Files ?? [])
                .Where(f => f.Name is not null && extensions.Any(e => f.Name.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
                .Select(f => new RemoteFile(f.Id, f.Name))
                .ToList();
        } catch (Exception ex) when (ex is not OperationCanceledException and not RemoteStorageException) {
            throw new RemoteStorageException($"Не вдалося отримати список файлів Google Drive. {ex.Message}", ex);
        }
    }

    public async Task DownloadAsync(RemoteFile file, string localPath, CancellationToken cancellationToken = default) {
        DriveService service = await GetServiceAsync(cancellationToken);
        try {
            await using FileStream target = File.Create(localPath);
            IDownloadProgress progress = await service.Files.Get(file.Id).DownloadAsync(target, cancellationToken);
            if (progress.Status != DownloadStatus.Completed) {
                throw new RemoteStorageException($"Не вдалося завантажити «{file.Name}». {progress.Exception?.Message}");
            }
        } catch (Exception ex) when (ex is not OperationCanceledException and not RemoteStorageException) {
            throw new RemoteStorageException($"Не вдалося завантажити «{file.Name}» з Google Drive. {ex.Message}", ex);
        }
    }

    public async Task<RemoteFile> UploadAsync(string name, Stream content, string mimeType, CancellationToken cancellationToken = default) {
        DriveService service = await GetServiceAsync(cancellationToken);
        try {
            var metadata = new DriveFile { Name = name, MimeType = mimeType };
            FilesResource.CreateMediaUpload request = service.Files.Create(metadata, content, mimeType);
            IUploadProgress progress = await request.UploadAsync(cancellationToken);
            if (progress.Status != UploadStatus.Completed) {
                throw new RemoteStorageException($"Не вдалося передати «{name}». {progress.Exception?.Message}");
            }

            DriveFile? created = request.ResponseBody;
            return new RemoteFile(created?.Id ?? "", created?.Name ?? name);
        } catch (Exception ex) when (ex is not OperationCanceledException and not RemoteStorageException) {
            throw new RemoteStorageException($"Не вдалося зберегти «{name}» на Google Drive. {ex.Message}", ex);
        }
    }

    private async Task<DriveService> GetServiceAsync(CancellationToken cancellationToken) {
        if (_service is not null) return _service;

        await _authorizationLock.WaitAsync(cancellationToken);
        try {
            if (_service is not null) return _service;

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(AuthorizationTimeout);

            using var secrets = new MemoryStream(Encoding.UTF8.GetBytes(_clientSecretJson));
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer {
                ClientSecrets = GoogleClientSecrets.FromStream(secrets).Secrets,
                Scopes = Scopes,
                DataStore = new FileDataStore(_tokenFolder, true)
            });

            UserCredential credential = await new AuthorizationCodeInstalledApp(flow, new LoopbackCodeReceiver(_openBrowser))
                .AuthorizeAsync("user", timeout.Token);

            _service = new DriveService(new BaseClientService.Initializer {
                HttpClientInitializer = credential,
                ApplicationName = "StudentParliament"
            });
            return _service;
        } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            throw new RemoteStorageException("Час очікування авторизації Google вичерпано. Спробуйте ще раз.");
        } catch (Exception ex) when (ex is not OperationCanceledException and not RemoteStorageException) {
            throw new RemoteStorageException($"Не вдалося авторизуватися в Google Drive. {ex.Message}", ex);
        } finally {
            _authorizationLock.Release();
        }
    }
}