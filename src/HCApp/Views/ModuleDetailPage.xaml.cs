using HCApp.ViewModels;

namespace HCApp.Views;

public partial class ModuleDetailPage : ContentPage
{
    public ModuleDetailPage(ModuleDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
