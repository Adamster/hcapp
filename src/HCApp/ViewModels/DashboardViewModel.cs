using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HCApp.Models;
using HCApp.Services;

namespace HCApp.ViewModels;

public partial class DashboardViewModel : ObservableObject, IDisposable
{
    private readonly MonitoringService _monitoringService;
    private readonly IConfigurationStore _configStore;
    private AppConfiguration _config = new();
    private bool _initialized;

    [ObservableProperty]
    private ObservableCollection<MonitorEnvironment> _environments = [];

    [ObservableProperty]
    private MonitorEnvironment? _selectedEnvironment;

    [ObservableProperty]
    private ObservableCollection<ModuleStatusViewModel> _modules = [];

    [ObservableProperty]
    private ObservableCollection<EnvironmentStatusViewModel> _environmentStatuses = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isRefreshing;

    public DashboardViewModel(MonitoringService monitoringService, IConfigurationStore configStore)
    {
        _monitoringService = monitoringService;
        _configStore = configStore;
        _monitoringService.ModuleStatusUpdated += OnModuleStatusUpdated;
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;

        IsLoading = true;
        _config = await _configStore.LoadAsync();
        Environments = new ObservableCollection<MonitorEnvironment>(_config.Environments);

        if (Environments.Count > 0)
            SelectedEnvironment = Environments[0];

        RebuildEnvironmentStatuses();

        _monitoringService.NotificationsEnabled = _config.Settings.NotificationsEnabled;

        IsLoading = false;
    }

    partial void OnSelectedEnvironmentChanged(MonitorEnvironment? value)
    {
        foreach (var s in EnvironmentStatuses)
            s.IsSelected = s.EnvironmentId == value?.Id;

        RefreshModuleList();
    }

    [RelayCommand]
    private async Task AddEnvironmentAsync()
    {
        var env = new MonitorEnvironment
        {
            Name = $"Environment {_config.Environments.Count + 1}",
            PollingIntervalSeconds = _config.Settings.DefaultPollingIntervalSeconds
        };

        _config.Environments.Add(env);
        Environments.Add(env);
        RebuildEnvironmentStatuses();
        await _configStore.SaveAsync(_config);

        SelectedEnvironment = env;
        await Shell.Current.GoToAsync("environment-edit", new Dictionary<string, object>
        {
            ["Environment"] = env
        });
    }

    [RelayCommand]
    private async Task EditEnvironmentAsync()
    {
        if (SelectedEnvironment is null) return;
        await Shell.Current.GoToAsync("environment-edit", new Dictionary<string, object>
        {
            ["Environment"] = SelectedEnvironment
        });
    }

    [RelayCommand]
    private async Task CloneEnvironmentAsync()
    {
        if (SelectedEnvironment is null) return;

        var clone = new MonitorEnvironment
        {
            Name = $"{SelectedEnvironment.Name} (Copy)",
            BaseUrl = SelectedEnvironment.BaseUrl,
            PollingIntervalSeconds = SelectedEnvironment.PollingIntervalSeconds,
            Headers = new Dictionary<string, string>(SelectedEnvironment.Headers),
            Modules = SelectedEnvironment.Modules.Select(m => new MonitorModule
            {
                Name = m.Name,
                HealthCheckPath = m.HealthCheckPath
            }).ToList()
        };

        _config.Environments.Add(clone);
        Environments.Add(clone);
        RebuildEnvironmentStatuses();
        await _configStore.SaveAsync(_config);

        SelectedEnvironment = clone;
        await Shell.Current.GoToAsync("environment-edit", new Dictionary<string, object>
        {
            ["Environment"] = clone
        });
    }

    [RelayCommand]
    private async Task DeleteEnvironmentAsync()
    {
        if (SelectedEnvironment is null) return;

        var confirm = await Shell.Current.DisplayAlertAsync(
            "Delete Environment",
            $"Delete '{SelectedEnvironment.Name}' and all its modules?",
            "Delete", "Cancel");

        if (!confirm) return;

        _monitoringService.StopMonitoring(SelectedEnvironment.Id);
        _config.Environments.Remove(SelectedEnvironment);
        Environments.Remove(SelectedEnvironment);
        RebuildEnvironmentStatuses();
        await _configStore.SaveAsync(_config);

        SelectedEnvironment = Environments.FirstOrDefault();
    }

    [RelayCommand]
    private async Task ViewModuleDetailAsync(ModuleStatusViewModel moduleVm)
    {
        if (SelectedEnvironment is null) return;
        var module = SelectedEnvironment.Modules.FirstOrDefault(m => m.Id == moduleVm.ModuleId);
        if (module is null) return;

        await Shell.Current.GoToAsync("module-detail", new Dictionary<string, object>
        {
            ["Module"] = module,
            ["EnvironmentName"] = SelectedEnvironment.Name
        });
    }

    [RelayCommand]
    private async Task AddModuleAsync()
    {
        if (SelectedEnvironment is null) return;

        var name = await Shell.Current.DisplayPromptAsync(
            "Add Module", "Enter the module name (e.g. 'Users Service'):");

        if (string.IsNullOrWhiteSpace(name)) return;

        var path = await Shell.Current.DisplayPromptAsync(
            "Health Check Path", "Enter the health check endpoint path (e.g. 'health/users-service'):");

        if (string.IsNullOrWhiteSpace(path)) return;

        var module = new MonitorModule { Name = name.Trim(), HealthCheckPath = path.Trim() };
        SelectedEnvironment.Modules.Add(module);
        await _configStore.SaveAsync(_config);

        RefreshModuleList();
        _monitoringService.StartMonitoring(SelectedEnvironment);
    }

    [RelayCommand]
    private async Task RefreshAllAsync()
    {
        if (SelectedEnvironment is null) return;
        IsRefreshing = true;
        await _monitoringService.CheckAllNowAsync(SelectedEnvironment);
        IsRefreshing = false;
    }

    [RelayCommand]
    private async Task RefreshModuleAsync(ModuleStatusViewModel moduleVm)
    {
        if (SelectedEnvironment is null) return;
        var module = SelectedEnvironment.GetEffectiveModules().FirstOrDefault(m => m.Id == moduleVm.ModuleId);
        if (module is null) return;
        await _monitoringService.CheckModuleNowAsync(SelectedEnvironment, module);
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        await Shell.Current.GoToAsync("settings");
    }

    [RelayCommand]
    private async Task ImportConfigAsync()
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Import configuration",
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.MacCatalyst, new[] { "public.json", "json" } },
                { DevicePlatform.WinUI, new[] { ".json" } }
            })
        });

        if (result is null) return;

        try
        {
            await using var stream = await result.OpenReadAsync();
            var imported = await System.Text.Json.JsonSerializer.DeserializeAsync(stream, HCAppJsonContext.Default.AppConfiguration);
            if (imported is null) return;

            _config = imported;
            await _configStore.SaveAsync(_config);

            _monitoringService.StopAll();
            Environments = new ObservableCollection<MonitorEnvironment>(_config.Environments);
            RebuildEnvironmentStatuses();
            SelectedEnvironment = Environments.FirstOrDefault();

            await Shell.Current.DisplayAlertAsync("Import", "Configuration imported successfully.", "OK");
        }
        catch
        {
            await Shell.Current.DisplayAlertAsync("Import Failed", "The selected file could not be imported.", "OK");
        }
    }

    [RelayCommand]
    private async Task ExportConfigAsync()
    {
        using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, _config, HCAppJsonContext.Default.AppConfiguration);
        stream.Position = 0;

        var result = await CommunityToolkit.Maui.Storage.FileSaver.Default.SaveAsync(
            "hcapp-config.json", stream, CancellationToken.None);

        if (result.IsSuccessful)
        {
            await Shell.Current.DisplayAlertAsync("Export", "Configuration exported successfully.", "OK");
        }
    }

    [RelayCommand]
    private void SelectEnvironment(EnvironmentStatusViewModel envStatus)
    {
        var env = _config.Environments.FirstOrDefault(e => e.Id == envStatus.EnvironmentId);
        if (env is not null)
            SelectedEnvironment = env;
    }

    private void RebuildEnvironmentStatuses()
    {
        EnvironmentStatuses = new ObservableCollection<EnvironmentStatusViewModel>(
            _config.Environments.Select(env =>
            {
                var vm = new EnvironmentStatusViewModel
                {
                    EnvironmentId = env.Id,
                    Name = env.Name.Length > 8 ? env.Name[..8].ToUpperInvariant() : env.Name.ToUpperInvariant(),
                    IsSelected = env.Id == SelectedEnvironment?.Id
                };
                vm.Recompute(env.GetEffectiveModules());
                return vm;
            }));
    }

    public void StartAllMonitoring(bool pollImmediately = false)
    {
        foreach (var env in _config.Environments)
            _monitoringService.StartMonitoring(env);

        if (pollImmediately)
        {
            var envs = _config.Environments.ToList();
            _ = Task.WhenAll(envs.Select(e => _monitoringService.CheckAllNowAsync(e)));
        }
    }

    public void StopAllMonitoring()
    {
        _monitoringService.StopAll();
    }

    public async Task SaveConfigAsync()
    {
        await _configStore.SaveAsync(_config);
    }

    public void RefreshModuleList()
    {
        if (SelectedEnvironment is null)
        {
            Modules.Clear();
            return;
        }

        Modules = new ObservableCollection<ModuleStatusViewModel>(
            SelectedEnvironment.GetEffectiveModules().Select(m =>
            {
                var vm = new ModuleStatusViewModel();
                vm.UpdateFrom(m, SelectedEnvironment.BaseUrl);
                return vm;
            }));
    }

    private void OnModuleStatusUpdated(string environmentId, MonitorModule module)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (SelectedEnvironment?.Id == environmentId)
            {
                var existing = Modules.FirstOrDefault(m => m.ModuleId == module.Id);
                if (existing is not null)
                    existing.UpdateFrom(module, SelectedEnvironment?.BaseUrl);
            }

            var envStatus = EnvironmentStatuses.FirstOrDefault(s => s.EnvironmentId == environmentId);
            if (envStatus is not null)
            {
                var env = _config.Environments.FirstOrDefault(e => e.Id == environmentId);
                if (env is not null)
                    envStatus.Recompute(env.GetEffectiveModules());
            }
        });
    }

    public void Dispose()
    {
        _monitoringService.ModuleStatusUpdated -= OnModuleStatusUpdated;
    }
}
