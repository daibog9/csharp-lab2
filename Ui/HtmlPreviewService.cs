using System.Net;
using Ui.ViewModels;

namespace Ui.Services;

public interface IHtmlPreviewService {
    Task ShowAsync(string htmlPath, string caption);
    Task ShowTextAsync(string textPath, string caption);
}

public sealed class HtmlPreviewService(IDialogService dialogs) : IHtmlPreviewService {
    public async Task ShowAsync(string htmlPath, string caption) {
        string html = await File.ReadAllTextAsync(htmlPath);
        await ShowHtmlAsync(htmlPath, html, caption);
    }

    public async Task ShowTextAsync(string textPath, string caption) {
        string text = await File.ReadAllTextAsync(textPath);
        string html =
            "<html><head><meta charset=\"utf-8\"></head>" +
            "<body style=\"font-family:Menlo,Consolas,monospace;font-size:13px;line-height:1.5;padding:12px;white-space:pre-wrap\">" +
            WebUtility.HtmlEncode(text) +
            "</body></html>";
        await ShowHtmlAsync(textPath, html, caption);
    }

    private async Task ShowHtmlAsync(string path, string html, string caption) {
        Page host = Application.Current?.Windows.FirstOrDefault()?.Page
                    ?? throw new InvalidOperationException("Вікно програми ще не створене.");

        var viewModel = new HtmlPreviewViewModel(path, html, caption, dialogs,
            close: () => host.Navigation.PopModalAsync());

        await host.Navigation.PushModalAsync(new HtmlPreviewPage(viewModel));
    }
}