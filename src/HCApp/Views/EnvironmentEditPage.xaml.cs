using HCApp.ViewModels;

namespace HCApp.Views;

public partial class EnvironmentEditPage : ContentPage
{
    public EnvironmentEditPage(EnvironmentEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
