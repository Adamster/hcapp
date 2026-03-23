using System.Text.Json;
using HCApp.Models;

namespace HCApp.Services;

public sealed class ConfigurationStore : IConfigurationStore
{
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

        await using var stream = new FileStream(
            _filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        return await JsonSerializer.DeserializeAsync(stream, HCAppJsonContext.Default.AppConfiguration).ConfigureAwait(false)
               ?? new AppConfiguration();
    }

    public async Task SaveAsync(AppConfiguration config)
    {
        await using var stream = new FileStream(
            _filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
        await JsonSerializer.SerializeAsync(stream, config, HCAppJsonContext.Default.AppConfiguration).ConfigureAwait(false);
    }
}
