using CommunityToolkit.Mvvm.ComponentModel;
using HCApp.Models;

namespace HCApp.ViewModels;

public partial class EnvironmentStatusViewModel : ObservableObject
{
    [ObservableProperty]
    private string _environmentId = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private HealthStatus _overallStatus = HealthStatus.Unknown;

    [ObservableProperty]
    private bool _isSelected;

    public void Recompute(IEnumerable<MonitorModule> modules)
    {
        var statuses = modules.Select(m => m.LastStatus).ToList();
        if (statuses.Contains(HealthStatus.Unhealthy))
            OverallStatus = HealthStatus.Unhealthy;
        else if (statuses.Contains(HealthStatus.Degraded))
            OverallStatus = HealthStatus.Degraded;
        else if (statuses.Contains(HealthStatus.Healthy))
            OverallStatus = HealthStatus.Healthy;
        else
            OverallStatus = HealthStatus.Unknown;
    }
}
