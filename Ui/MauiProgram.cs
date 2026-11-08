using Core.Logging;
using Core.Search;
using Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ui.Services;
using Ui.ViewModels;
#if WINDOWS
using Microsoft.Maui.LifecycleEvents;
#endif

namespace Ui;

public static class MauiProgram {
    public static MauiApp CreateMauiApp() {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<ISearchStrategyFactory, SearchStrategyFactory>();
        builder.Services.AddSingleton<XslHtmlTransformer>();
        builder.Services.AddSingleton<SearchResultXmlWriter>();

        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton<IAppLifetimeService, AppLifetimeService>();
        builder.Services.AddSingleton<IFileService, FileService>();
        builder.Services.AddSingleton<IHtmlPreviewService, HtmlPreviewService>();
        builder.Services.AddSingleton<IDriveConnector, GoogleDriveConnector>();

        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainPage>();

#if WINDOWS
        builder.ConfigureLifecycleEvents(events => events.AddWindows(windows => windows.OnWindowCreated(window => {
            window.AppWindow.Closing += async (_, args) => {
                var lifetime = IPlatformApplication.Current?.Services.GetService<IAppLifetimeService>();
                if (lifetime is null || lifetime.IsExitConfirmed) return;
                args.Cancel = true;
                await lifetime.RequestExitAsync();
            };
        })));
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif

        MauiApp app = builder.Build();
        EventLogger.Instance.UseFile(app.Services.GetRequiredService<IFileService>().LogFilePath);
        return app;
    }
}