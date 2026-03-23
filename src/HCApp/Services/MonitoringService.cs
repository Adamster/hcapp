using System.Collections.Concurrent;
using HCApp.Models;

namespace HCApp.Services;

public sealed class MonitoringService : IDisposable
{
    private readonly IHealthCheckService _healthCheckService;
    private readonly INotificationService _notificationService;
    private readonly IConfigurationStore _configStore;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _pollingTasks = new();

    public bool NotificationsEnabled { get; set; } = true;

    public event Action<string, MonitorModule>? ModuleStatusUpdated;

    public MonitoringService(
        IHealthCheckService healthCheckService,
        INotificationService notificationService,
        IConfigurationStore configStore)
    {
        _healthCheckService = healthCheckService;
        _notificationService = notificationService;
        _configStore = configStore;
    }

    public void StartMonitoring(MonitorEnvironment environment)
    {
        StopMonitoring(environment.Id);

        var cts = new CancellationTokenSource();
        _pollingTasks[environment.Id] = cts;

        _ = PollLoopAsync(environment, cts.Token);
    }

    public void StopMonitoring(string environmentId)
    {
        if (_pollingTasks.TryRemove(environmentId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    public void StopAll()
    {
        foreach (var id in _pollingTasks.Keys.ToList())
            StopMonitoring(id);
    }

    public async Task CheckModuleNowAsync(MonitorEnvironment environment, MonitorModule module)
    {
        var url = string.IsNullOrEmpty(module.HealthCheckPath)
            ? environment.BaseUrl.TrimEnd('/')
            : BuildUrl(environment.BaseUrl, module.HealthCheckPath);
        var result = await _healthCheckService.CheckAsync(url, environment.Headers).ConfigureAwait(false);
        ApplyResult(environment, module, result);
    }

    public async Task CheckAllNowAsync(MonitorEnvironment environment)
    {
        await PollAllModulesAsync(environment, CancellationToken.None).ConfigureAwait(false);
    }

private async Task PollLoopAsync(MonitorEnvironment environment, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(environment.PollingIntervalSeconds));

        while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
        {
            await PollAllModulesAsync(environment, ct).ConfigureAwait(false);
        }
    }

    private async Task PollAllModulesAsync(MonitorEnvironment environment, CancellationToken ct)
    {
        var modules = environment.GetEffectiveModules();
        var tasks = modules.Select(async module =>
        {
            var url = string.IsNullOrEmpty(module.HealthCheckPath)
                ? environment.BaseUrl.TrimEnd('/')
                : BuildUrl(environment.BaseUrl, module.HealthCheckPath);
            var result = await _healthCheckService.CheckAsync(url, environment.Headers, ct).ConfigureAwait(false);
            ApplyResult(environment, module, result);
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private void ApplyResult(MonitorEnvironment environment, MonitorModule module, HealthCheckResult result)
    {
        var previousStatus = module.LastStatus;
        module.LastStatus = result.Status;
        module.LastChecked = DateTime.Now;
        module.LastResponse = result.Response;
        module.LastError = result.Error;
        module.LastDuration = result.Response?.TotalDuration;

        // Notify on status transition (skip initial Unknown -> X)
        if (previousStatus != HealthStatus.Unknown && previousStatus != result.Status)
        {
            if (NotificationsEnabled)
                _notificationService.SendStatusChange(environment.Name, module.Name, previousStatus, result.Status);
        }

        module.StatusHistory.Insert(0, new StatusHistoryEntry(result.Status, DateTime.Now));
        if (module.StatusHistory.Count > 10)
            module.StatusHistory.RemoveAt(module.StatusHistory.Count - 1);

        ModuleStatusUpdated?.Invoke(environment.Id, module);
    }

    private static string BuildUrl(string baseUrl, string moduleName)
    {
        var trimmedBase = baseUrl.TrimEnd('/');
        var trimmedModule = moduleName.TrimStart('/');
        return $"{trimmedBase}/{trimmedModule}";
    }

    public void Dispose()
    {
        StopAll();
    }
}
