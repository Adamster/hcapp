using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HCApp.Services;

namespace HCApp.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IConfigurationStore _configStore;
    private readonly MonitoringService _monitoringService;

    [ObservableProperty]
    private bool _notificationsEnabled = true;

    [ObservableProperty]
    private int _defaultPollingInterval = 30;

    public SettingsViewModel(IConfigurationStore configStore, MonitoringService monitoringService)
    {
        _configStore = configStore;
        _monitoringService = monitoringService;
    }

    public async Task LoadAsync()
    {
        var config = await _configStore.LoadAsync();
        NotificationsEnabled = config.Settings.NotificationsEnabled;
        DefaultPollingInterval = config.Settings.DefaultPollingIntervalSeconds;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var config = await _configStore.LoadAsync();
        config.Settings.NotificationsEnabled = NotificationsEnabled;
        config.Settings.DefaultPollingIntervalSeconds = DefaultPollingInterval;
        await _configStore.SaveAsync(config);
        _monitoringService.NotificationsEnabled = NotificationsEnabled;
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
