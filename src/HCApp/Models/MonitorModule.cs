using System.Text.Json.Serialization;

namespace HCApp.Models;

public sealed record StatusHistoryEntry(HealthStatus Status, DateTime Timestamp);

public sealed class MonitorModule
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("healthCheckPath")]
    public string HealthCheckPath { get; set; } = string.Empty;

    [JsonIgnore]
    public HealthStatus LastStatus { get; set; } = HealthStatus.Unknown;

    [JsonIgnore]
    public DateTime? LastChecked { get; set; }

    [JsonIgnore]
    public string? LastDuration { get; set; }

    [JsonIgnore]
    public HealthCheckResponse? LastResponse { get; set; }

    [JsonIgnore]
    public string? LastError { get; set; }

    [JsonIgnore]
    public List<StatusHistoryEntry> StatusHistory { get; } = new();
}
