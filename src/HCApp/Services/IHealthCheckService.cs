using HCApp.Models;

namespace HCApp.Services;

public sealed record HealthCheckResult(
    HealthStatus Status,
    HealthCheckResponse? Response,
    string? Error);

public interface IHealthCheckService
{
    Task<HealthCheckResult> CheckAsync(string url, Dictionary<string, string>? headers, CancellationToken ct = default);
}
