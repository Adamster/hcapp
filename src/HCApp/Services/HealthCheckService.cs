using System.Text.Json;
using HCApp.Models;

namespace HCApp.Services;

file static class JsonOptions
{
    public static readonly JsonSerializerOptions CaseInsensitive = new()
    {
        PropertyNameCaseInsensitive = true
    };
}

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
            client.Timeout = TimeSpan.FromSeconds(15);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (headers is not null)
            {
                foreach (var (key, value) in headers)
                    request.Headers.TryAddWithoutValidation(key, value);
            }

            using var response = await client.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            // Try to parse as structured health check response
            try
            {
                var hcResponse = JsonSerializer.Deserialize<HealthCheckResponse>(body, JsonOptions.CaseInsensitive);
                if (hcResponse is not null && !string.IsNullOrEmpty(hcResponse.Status))
                {
                    var status = ParseStatus(hcResponse.Status);
                    return new HealthCheckResult(status, hcResponse, null);
                }
            }
            catch (JsonException)
            {
                // Not JSON — fall through to simple status check
            }

            // Simple response: use HTTP status code, and try to parse body as a status string
            var simpleStatus = response.IsSuccessStatusCode
                ? ParseStatus(body)
                : HealthStatus.Unhealthy;

            // If HTTP was successful but body didn't parse to a known status, treat as Healthy
            if (simpleStatus == HealthStatus.Unknown && response.IsSuccessStatusCode)
                simpleStatus = HealthStatus.Healthy;

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
