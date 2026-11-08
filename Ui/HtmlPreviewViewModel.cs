using Ui.Services;

namespace Ui.ViewModels;

public sealed class HtmlPreviewViewModel {
    private readonly IDialogService _dialogs;

    public HtmlPreviewViewModel(string filePath, string html, string caption, IDialogService dialogs, Func<Task> close) {
        _dialogs = dialogs;
        FilePath = filePath;
        Caption = caption;
        Source = new HtmlWebViewSource { Html = html };

        CloseCommand = new AsyncCommand(close);
        OpenInBrowserCommand = new AsyncCommand(OpenInBrowserAsync);
        CopyPathCommand = new AsyncCommand(CopyPathAsync);
    }

    public string Caption { get; }
    public string FilePath { get; }
    public HtmlWebViewSource Source { get; }

    public AsyncCommand CloseCommand { get; }
    public AsyncCommand OpenInBrowserCommand { get; }
    public AsyncCommand CopyPathCommand { get; }

    private async Task OpenInBrowserAsync() {
        try {
            if (DeviceInfo.Current.Platform == DevicePlatform.WinUI) {
                await Launcher.Default.OpenAsync(new OpenFileRequest("HTML-звіт", new ReadOnlyFile(FilePath)));
            } else {
                await Launcher.Default.OpenAsync(new Uri(FilePath));
            }
        } catch (Exception ex) {
            await _dialogs.ShowErrorAsync("Не вдалося відкрити браузер",
                $"{ex.Message}\n\nФайл збережено тут.\n{FilePath}");
        }
    }

    private async Task CopyPathAsync() {
        await Clipboard.Default.SetTextAsync(FilePath);
        await _dialogs.ShowInfoAsync("Шлях скопійовано", FilePath);
    }
}