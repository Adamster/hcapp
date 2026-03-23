using System.Globalization;
using HCApp.Models;

namespace HCApp.Converters;

public sealed class StatusToGlowConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not HealthStatus status)
            return null;

        return status switch
        {
            HealthStatus.Healthy   => new Shadow { Brush = new SolidColorBrush(Color.FromArgb("#2DB87A")), Radius = 12, Opacity = 0.75f, Offset = new Point(0, 0) },
            HealthStatus.Degraded  => new Shadow { Brush = new SolidColorBrush(Color.FromArgb("#E8963A")), Radius = 12, Opacity = 0.75f, Offset = new Point(0, 0) },
            HealthStatus.Unhealthy => new Shadow { Brush = new SolidColorBrush(Color.FromArgb("#E0443A")), Radius = 14, Opacity = 0.80f, Offset = new Point(0, 0) },
            _                      => new Shadow { Brush = new SolidColorBrush(Color.FromArgb("#6C757D")), Radius = 4,  Opacity = 0.25f, Offset = new Point(0, 0) },
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
