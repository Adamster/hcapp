using HCApp.Models;

namespace HCApp.Services;

public interface INotificationService
{
    void SendStatusChange(string environmentName, string moduleName, HealthStatus oldStatus, HealthStatus newStatus);
}
