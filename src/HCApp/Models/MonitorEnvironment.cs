using System.Text.Json.Serialization;

namespace HCApp.Models;

public sealed class MonitorEnvironment
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("baseUrl")]
    public string BaseUrl { get; set; } = string.Empty;

    [JsonPropertyName("pollingIntervalSeconds")]
    public int PollingIntervalSeconds { get; set; } = 30;

    [JsonPropertyName("headers")]
    public Dictionary<string, string> Headers { get; set; } = [];

    [JsonPropertyName("modules")]
    public List<MonitorModule> Modules { get; set; } = [];

    [JsonIgnore]
    private MonitorModule? _baseUrlModule;

    [JsonIgnore]
    public MonitorModule BaseUrlModule => _baseUrlModule ??= new MonitorModule
    {
        Id = $"{Id}_base",
        Name = string.Empty
    };

    public List<MonitorModule> GetEffectiveModules()
        => Modules.Count > 0 ? Modules : [BaseUrlModule];
}
