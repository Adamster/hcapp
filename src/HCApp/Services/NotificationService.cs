using HCApp.Models;

namespace HCApp.Services;

public sealed class NotificationService : INotificationService
{
    public void SendStatusChange(string environmentName, string moduleName, HealthStatus oldStatus, HealthStatus newStatus)
    {
        var title = newStatus switch
        {
            HealthStatus.Unhealthy => $"[{environmentName}] {moduleName} is DOWN",
            HealthStatus.Degraded => $"[{environmentName}] {moduleName} is DEGRADED",
            HealthStatus.Healthy => $"[{environmentName}] {moduleName} is back UP",
            _ => $"[{environmentName}] {moduleName} status changed"
        };

        var body = $"Status changed from {oldStatus} to {newStatus}";

        MainThread.BeginInvokeOnMainThread(() =>
        {
#if MACCATALYST
            SendMacNotification(title, body);
#elif WINDOWS
            SendWindowsNotification(title, body);
#endif
        });
    }

#if MACCATALYST
    private static void SendMacNotification(string title, string body)
    {
        var content = new UserNotifications.UNMutableNotificationContent
        {
            Title = title,
            Body = body,
            Sound = UserNotifications.UNNotificationSound.Default
        };

        var trigger = UserNotifications.UNTimeIntervalNotificationTrigger.CreateTrigger(1, false);
        var request = UserNotifications.UNNotificationRequest.FromIdentifier(
            Guid.NewGuid().ToString(), content, trigger);

        UserNotifications.UNUserNotificationCenter.Current.AddNotificationRequest(request, null);
    }
#endif

#if WINDOWS
    private static void SendWindowsNotification(string title, string body)
    {
        try
        {
            var builder = new Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder()
                .AddText(title)
                .AddText(body);

            Microsoft.Windows.AppNotifications.AppNotificationManager.Default.Show(builder.BuildNotification());
        }
        catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException or InvalidOperationException)
        {
            // Notification infrastructure unavailable (e.g. running unpackaged without identity)
            System.Diagnostics.Debug.WriteLine($"[Notification] Failed to show toast: {ex.Message}");
        }
    }
#endif
}
