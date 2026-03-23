using System.Text.Json.Serialization;

namespace HCApp.Models;

public sealed class AppConfiguration
{
    [JsonPropertyName("environments")]
    public List<MonitorEnvironment> Environments { get; set; } = [];

    [JsonPropertyName("settings")]
    public AppSettings Settings { get; set; } = new();
}

public sealed class AppSettings
{
    [JsonPropertyName("notificationsEnabled")]
    public bool NotificationsEnabled { get; set; } = true;

    [JsonPropertyName("defaultPollingIntervalSeconds")]
    public int DefaultPollingIntervalSeconds { get; set; } = 30;
}
