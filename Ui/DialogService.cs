namespace Ui.Services;

public interface IDialogService {
    Task ShowInfoAsync(string title, string message);
    Task ShowErrorAsync(string title, string message);
    Task<bool> ConfirmAsync(string title, string message, string accept = "Так", string cancel = "Ні");
    Task<string?> ChooseAsync(string title, IReadOnlyList<string> options);
    Task<string?> PromptAsync(string title, string message, string initialValue);
}

public sealed class DialogService : IDialogService {
    private const string Cancel = "Скасувати";

#pragma warning disable CS0618
    public Task ShowInfoAsync(string title, string message) =>
        MainThread.InvokeOnMainThreadAsync(() => TopPage().DisplayAlert(title, message, "Гаразд"));

    public Task ShowErrorAsync(string title, string message) =>
        MainThread.InvokeOnMainThreadAsync(() => TopPage().DisplayAlert(title, message, "Гаразд"));

    public Task<bool> ConfirmAsync(string title, string message, string accept = "Так", string cancel = "Ні") =>
        MainThread.InvokeOnMainThreadAsync(() => TopPage().DisplayAlert(title, message, accept, cancel));

    public Task<string?> ChooseAsync(string title, IReadOnlyList<string> options) =>
        MainThread.InvokeOnMainThreadAsync<string?>(async () => {
            string? choice = await TopPage().DisplayActionSheet(title, Cancel, null, options.ToArray());
            return choice is not null && options.Contains(choice) ? choice : null;
        });

    public Task<string?> PromptAsync(string title, string message, string initialValue) =>
        MainThread.InvokeOnMainThreadAsync<string?>(async () =>
            await TopPage().DisplayPromptAsync(title, message, "Зберегти", Cancel, initialValue: initialValue));
#pragma warning restore CS0618

    private static Page TopPage() {
        Page root = Application.Current?.Windows.FirstOrDefault()?.Page
                    ?? throw new InvalidOperationException("Вікно програми ще не створене.");
        return root.Navigation.ModalStack.LastOrDefault() ?? root;
    }
}