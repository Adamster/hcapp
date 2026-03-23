using System.Text.Json;
using System.Text.Json.Serialization;

namespace HCApp.Models;

public sealed class HealthCheckResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("totalDuration")]
    public string? TotalDuration { get; set; }

    [JsonPropertyName("entries")]
    public Dictionary<string, HealthCheckEntry>? Entries { get; set; }
}

public sealed class HealthCheckEntry
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("duration")]
    public string? Duration { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("exception")]
    public string? Exception { get; set; }

    [JsonPropertyName("data")]
    public Dictionary<string, JsonElement>? Data { get; set; }

    [JsonPropertyName("tags")]
    public string[]? Tags { get; set; }
}
