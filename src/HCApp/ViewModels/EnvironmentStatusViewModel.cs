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
        var worst = HealthStatus.Unknown;
        foreach (var m in modules)
        {
            var s = m.LastStatus;
            if (s == HealthStatus.Unhealthy) { worst = HealthStatus.Unhealthy; break; }
            if (s == HealthStatus.Degraded)  worst = HealthStatus.Degraded;
            else if (s == HealthStatus.Healthy && worst != HealthStatus.Degraded) worst = HealthStatus.Healthy;
        }
        OverallStatus = worst;
    }
}
