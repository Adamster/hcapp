using HCApp.Models;

namespace HCApp.Services;

public interface IConfigurationStore
{
    Task<AppConfiguration> LoadAsync();
    Task SaveAsync(AppConfiguration config);
}
