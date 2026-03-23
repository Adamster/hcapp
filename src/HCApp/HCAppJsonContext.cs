using System.Text.Json.Serialization;
using HCApp.Models;

namespace HCApp;

[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    WriteIndented = true)]
[JsonSerializable(typeof(AppConfiguration))]
[JsonSerializable(typeof(HealthCheckResponse))]
internal partial class HCAppJsonContext : JsonSerializerContext { }
