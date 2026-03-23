using System.Globalization;
using HCApp.Models;

namespace HCApp.Converters;

public sealed class StatusToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not HealthStatus status)
            return Application.Current?.Resources.TryGetValue("StatusUnknown", out var fallback) == true
                ? fallback
                : Color.FromArgb("#6C757D");

        var key = status switch
        {
            HealthStatus.Healthy => "StatusHealthy",
            HealthStatus.Degraded => "StatusDegraded",
            HealthStatus.Unhealthy => "StatusUnhealthy",
            _ => "StatusUnknown"
        };

        return Application.Current?.Resources.TryGetValue(key, out var color) == true
            ? color
            : Color.FromArgb("#6C757D");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
