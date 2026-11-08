using Ui.ViewModels;

namespace Ui;

public partial class HtmlPreviewPage : ContentPage {
    public HtmlPreviewPage(HtmlPreviewViewModel viewModel) {
        InitializeComponent();
        BindingContext = viewModel;
    }
}