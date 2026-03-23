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

    private bool _firstAppearance = true;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        bool isFirst = _firstAppearance;
        _firstAppearance = false;

        await _viewModel.InitializeAsync();
        _viewModel.StartAllMonitoring(pollImmediately: isFirst);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.StopAllMonitoring();
    }
}
