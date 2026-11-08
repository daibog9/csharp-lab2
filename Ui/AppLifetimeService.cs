namespace Ui.Services;

public interface IAppLifetimeService {
    bool IsExitConfirmed { get; }

    Task RequestExitAsync();
}

public sealed class AppLifetimeService(IDialogService dialogs) : IAppLifetimeService {
    private bool _asking;

    public bool IsExitConfirmed { get; private set; }

    public async Task RequestExitAsync() {
        if (_asking || IsExitConfirmed) return;

        _asking = true;
        try {
            bool confirmed = await dialogs.ConfirmAsync(
                "Завершення роботи",
                "Чи дійсно ви хочете завершити роботу з програмою?",
                "Так",
                "Ні");
            if (!confirmed) return;

            IsExitConfirmed = true;
            Application.Current?.Quit();
        } finally {
            _asking = false;
        }
    }
}