using Core.Storage;

namespace Ui.Services;

public interface IDriveConnector {
    bool IsAuthorized { get; }
    Task<IRemoteStorage> GetStorageAsync();
}

public sealed class GoogleDriveConnector : IDriveConnector {
    private const string ClientSecretFile = "client_secret.json";

    private GoogleDriveStorage? _storage;

    public bool IsAuthorized => _storage?.IsAuthorized == true;

    public async Task<IRemoteStorage> GetStorageAsync() {
        if (_storage is not null) return _storage;

        string secret;
        try {
            await using Stream stream = await FileSystem.OpenAppPackageFileAsync(ClientSecretFile);
            using var reader = new StreamReader(stream);
            secret = await reader.ReadToEndAsync();
        } catch (Exception ex) {
            throw new RemoteStorageException(
                $"Не знайдено {ClientSecretFile}. Додайте файл OAuth-клієнта Google (тип Desktop app) " +
                "у папку Ui/Resources/Raw і перезберіть програму.", ex);
        }

        _storage = new GoogleDriveStorage(
            secret,
            Path.Combine(FileSystem.AppDataDirectory, "drive-token"),
            url => MainThread.BeginInvokeOnMainThread(() => Browser.Default.OpenAsync(url, BrowserLaunchMode.SystemPreferred)));
        return _storage;
    }
}