using HCApp.ViewModels;

namespace HCApp.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _viewModel;

    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
        _viewModel.StartAllMonitoring();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.StopAllMonitoring();
    }
}
