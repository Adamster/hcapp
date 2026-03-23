using System.Text.Json;
using HCApp.Models;

namespace HCApp.Services;

public sealed class ConfigurationStore : IConfigurationStore
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public ConfigurationStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, "HCApp");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "config.json");
    }

    public async Task<AppConfiguration> LoadAsync()
    {
        if (!File.Exists(_filePath))
            return new AppConfiguration();

        await using var stream = File.OpenRead(_filePath);
        return await JsonSerializer.DeserializeAsync<AppConfiguration>(stream, s_jsonOptions)
               ?? new AppConfiguration();
    }

    public async Task SaveAsync(AppConfiguration config)
    {
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, config, s_jsonOptions);
    }
}
