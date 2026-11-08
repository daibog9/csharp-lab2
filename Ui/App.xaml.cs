namespace Ui;

public partial class App : Application {
    private readonly MainPage _mainPage;

    public App(MainPage mainPage) {
        InitializeComponent();
        _mainPage = mainPage;
    }

    protected override Window CreateWindow(IActivationState? activationState) {
        return new Window(_mainPage) {
            Title = "Студентський парламент · робота з XML",
            Width = 1320,
            Height = 860,
            MinimumWidth = 1000,
            MinimumHeight = 640
        };
    }
}