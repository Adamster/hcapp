using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HCApp.Models;

namespace HCApp.ViewModels;

[QueryProperty(nameof(Module), "Module")]
[QueryProperty(nameof(EnvironmentName), "EnvironmentName")]
public partial class ModuleDetailViewModel : ObservableObject
{
    [ObservableProperty]
    private MonitorModule _module = new();

    [ObservableProperty]
    private string _environmentName = string.Empty;

    [ObservableProperty]
    private string _statusText = "Unknown";

    [ObservableProperty]
    private HealthStatus _status = HealthStatus.Unknown;

    [ObservableProperty]
    private string _totalDuration = "--";

    [ObservableProperty]
    private string _lastChecked = "Never";

    [ObservableProperty]
    private string? _error;

    [ObservableProperty]
    private ObservableCollection<EntryViewModel> _entries = [];

    [ObservableProperty]
    private ObservableCollection<StatusHistoryEntry> _history = [];

    partial void OnModuleChanged(MonitorModule value)
    {
        StatusText = value.LastStatus.ToString();
        Status = value.LastStatus;
        TotalDuration = value.LastDuration ?? "--";
        LastChecked = value.LastChecked?.ToString("HH:mm:ss") ?? "Never";
        Error = value.LastError;

        History.Clear();
        foreach (var h in value.StatusHistory)
            History.Add(h);

        Entries.Clear();
        if (value.LastResponse?.Entries is not null)
        {
            foreach (var (name, entry) in value.LastResponse.Entries)
            {
                Entries.Add(new EntryViewModel
                {
                    Name = name,
                    Status = ParseStatus(entry.Status),
                    StatusText = entry.Status,
                    Duration = entry.Duration ?? "--",
                    Description = entry.Description
                });
            }
        }
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    private static HealthStatus ParseStatus(string status) =>
        status.ToLowerInvariant() switch
        {
            "healthy" => HealthStatus.Healthy,
            "degraded" => HealthStatus.Degraded,
            "unhealthy" => HealthStatus.Unhealthy,
            _ => HealthStatus.Unknown
        };
}

public partial class EntryViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private HealthStatus _status;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private string _duration = "--";

    [ObservableProperty]
    private string? _description;
}
