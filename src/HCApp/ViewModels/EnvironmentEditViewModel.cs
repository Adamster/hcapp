using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HCApp.Models;
using HCApp.Services;

namespace HCApp.ViewModels;

[QueryProperty(nameof(Environment), "Environment")]
public partial class EnvironmentEditViewModel : ObservableObject
{
    private readonly IConfigurationStore _configStore;
    private readonly MonitoringService _monitoringService;

    [ObservableProperty]
    private MonitorEnvironment _environment = new();

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _baseUrl = string.Empty;

    [ObservableProperty]
    private int _pollingInterval = 30;

    [ObservableProperty]
    private string _headersText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<MonitorModule> _modules = [];

    public EnvironmentEditViewModel(IConfigurationStore configStore, MonitoringService monitoringService)
    {
        _configStore = configStore;
        _monitoringService = monitoringService;
    }

    partial void OnEnvironmentChanged(MonitorEnvironment value)
    {
        Name = value.Name;
        BaseUrl = value.BaseUrl;
        PollingInterval = value.PollingIntervalSeconds;
        Modules = new ObservableCollection<MonitorModule>(value.Modules);
        HeadersText = string.Join("\n", value.Headers.Select(h => $"{h.Key}: {h.Value}"));
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        Environment.Name = Name;
        Environment.BaseUrl = BaseUrl;
        Environment.PollingIntervalSeconds = PollingInterval;
        Environment.Modules = [.. Modules];

        // Parse headers
        Environment.Headers.Clear();
        foreach (var line in HeadersText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var colonIdx = line.IndexOf(':');
            if (colonIdx > 0)
            {
                var key = line[..colonIdx].Trim();
                var val = line[(colonIdx + 1)..].Trim();
                Environment.Headers[key] = val;
            }
        }

        var config = await _configStore.LoadAsync();
        var existing = config.Environments.FirstOrDefault(e => e.Id == Environment.Id);
        if (existing is not null)
        {
            var idx = config.Environments.IndexOf(existing);
            config.Environments[idx] = Environment;
        }
        else
        {
            config.Environments.Add(Environment);
        }

        await _configStore.SaveAsync(config);
        _monitoringService.StartMonitoring(Environment);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task AddModuleAsync()
    {
        var name = await Shell.Current.DisplayPromptAsync(
            "Add Module", "Enter the module path (e.g. 'users-service'):");

        if (string.IsNullOrWhiteSpace(name)) return;
        Modules.Add(new MonitorModule { Name = name.Trim() });
    }

    [RelayCommand]
    private void RemoveModule(MonitorModule module)
    {
        Modules.Remove(module);
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
