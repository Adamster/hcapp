using CommunityToolkit.Mvvm.ComponentModel;
using HCApp.Models;

namespace HCApp.ViewModels;

public partial class ModuleStatusViewModel : ObservableObject
{
    [ObservableProperty]
    private string _moduleId = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private HealthStatus _status = HealthStatus.Unknown;

    [ObservableProperty]
    private string _duration = "--";

    [ObservableProperty]
    private string _lastChecked = "Never";

    [ObservableProperty]
    private string? _error;

    [ObservableProperty]
    private HealthCheckResponse? _lastResponse;

    [ObservableProperty]
    private string _fullUrl = string.Empty;

    public void UpdateFrom(MonitorModule module, string? baseUrl = null)
    {
        ModuleId = module.Id;
        Name = string.IsNullOrEmpty(module.Name) ? "(Base URL)" : module.Name;
        Status = module.LastStatus;
        Duration = module.LastDuration ?? "--";
        LastChecked = module.LastChecked?.ToString("HH:mm:ss") ?? "Never";
        Error = module.LastError;
        LastResponse = module.LastResponse;

        if (baseUrl is not null)
        {
            FullUrl = string.IsNullOrEmpty(module.HealthCheckPath)
                ? baseUrl.TrimEnd('/')
                : $"{baseUrl.TrimEnd('/')}/{module.HealthCheckPath.TrimStart('/')}";
        }
    }
}
