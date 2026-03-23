using System.Globalization;
using HCApp.Models;

namespace HCApp.Converters;

public sealed class StatusToBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not HealthStatus status)
            return GetColor("StatusUnknownSubtle", "StatusUnknownSubtleLight");

        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;

        var key = status switch
        {
            HealthStatus.Healthy => isDark ? "StatusHealthySubtle" : "StatusHealthySubtleLight",
            HealthStatus.Degraded => isDark ? "StatusDegradedSubtle" : "StatusDegradedSubtleLight",
            HealthStatus.Unhealthy => isDark ? "StatusUnhealthySubtle" : "StatusUnhealthySubtleLight",
            _ => isDark ? "StatusUnknownSubtle" : "StatusUnknownSubtleLight"
        };

        return Application.Current?.Resources.TryGetValue(key, out var color) == true
            ? color
            : Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static Color GetColor(string darkKey, string lightKey)
    {
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var key = isDark ? darkKey : lightKey;
        return Application.Current?.Resources.TryGetValue(key, out var c) == true && c is Color color
            ? color
            : Colors.Transparent;
    }
}
