using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HCApp.Models;
using HCApp.Services;

namespace HCApp.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly MonitoringService _monitoringService;
    private readonly IConfigurationStore _configStore;
    private AppConfiguration _config = new();

    [ObservableProperty]
    private ObservableCollection<MonitorEnvironment> _environments = [];

    [ObservableProperty]
    private MonitorEnvironment? _selectedEnvironment;

    [ObservableProperty]
    private ObservableCollection<ModuleStatusViewModel> _modules = [];

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
        IsLoading = true;
        _config = await _configStore.LoadAsync();
        Environments = new ObservableCollection<MonitorEnvironment>(_config.Environments);

        if (Environments.Count > 0)
            SelectedEnvironment = Environments[0];

        IsLoading = false;
    }

    partial void OnSelectedEnvironmentChanged(MonitorEnvironment? value)
    {
        RefreshModuleList();
        if (value is not null)
            _monitoringService.StartMonitoring(value);
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

    public void StartAllMonitoring()
    {
        foreach (var env in _config.Environments)
            _monitoringService.StartMonitoring(env);
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
        if (SelectedEnvironment?.Id != environmentId) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var existing = Modules.FirstOrDefault(m => m.ModuleId == module.Id);
            if (existing is not null)
            {
                existing.UpdateFrom(module, SelectedEnvironment?.BaseUrl);
            }
        });
    }
}
