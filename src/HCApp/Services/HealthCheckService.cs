using System.Text.Json;
using HCApp.Models;

namespace HCApp.Services;

public sealed class HealthCheckService : IHealthCheckService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HealthCheckService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HealthCheckResult> CheckAsync(string url, Dictionary<string, string>? headers, CancellationToken ct = default)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient("HealthCheck");

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (headers is not null)
            {
                foreach (var (key, value) in headers)
                    request.Headers.TryAddWithoutValidation(key, value);
            }

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);

            // Try to parse as structured health check response (stream — no intermediate string allocation)
            try
            {
                await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
                var hcResponse = await JsonSerializer.DeserializeAsync(stream, HCAppJsonContext.Default.HealthCheckResponse, ct).ConfigureAwait(false);
                if (hcResponse is not null && !string.IsNullOrEmpty(hcResponse.Status))
                    return new HealthCheckResult(ParseStatus(hcResponse.Status), hcResponse, null);
            }
            catch (JsonException)
            {
                // Not JSON — fall through to HTTP status
            }

            // Fallback: derive status from HTTP response code
            var simpleStatus = response.IsSuccessStatusCode ? HealthStatus.Healthy : HealthStatus.Unhealthy;
            return new HealthCheckResult(simpleStatus, null, response.IsSuccessStatusCode ? null : $"HTTP {(int)response.StatusCode}");
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return new HealthCheckResult(HealthStatus.Unhealthy, null, "Request timed out");
        }
        catch (HttpRequestException ex)
        {
            return new HealthCheckResult(HealthStatus.Unhealthy, null, ex.Message);
        }
    }

    private static HealthStatus ParseStatus(string status) =>
        status.Trim().Trim('"').ToLowerInvariant() switch
        {
            "healthy" => HealthStatus.Healthy,
            "degraded" => HealthStatus.Degraded,
            "unhealthy" => HealthStatus.Unhealthy,
            _ => HealthStatus.Unknown
        };
}
