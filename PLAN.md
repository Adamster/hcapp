# HealthCheck App (HCApp) - Implementation Plan

## Overview

A .NET MAUI desktop application (macOS + Windows) that monitors health check endpoints of web APIs following the [ASP.NET Core health check response format](https://learn.microsoft.com/en-us/aspnet/core/host-and-build-apps/health-checks). Supports multiple environments, configurable modules, and sends native OS notifications when a service goes unhealthy.

---

## 1. Core Concepts

### Environment
A named group of monitored endpoints (e.g., "Production", "Staging", "Dev"). Each environment has its own base URL and set of modules.

### Module
A logical health check endpoint. The URL is constructed as `{BaseUrl}/{ModuleName}`. Each module is polled independently.

### Health Check Response Format (ASP.NET Core)
The app expects the standard .NET health check JSON response:

```json
{
  "status": "Healthy",          // Healthy | Degraded | Unhealthy
  "totalDuration": "00:00:00.123",
  "entries": {
    "database": {
      "status": "Healthy",
      "duration": "00:00:00.050",
      "description": null,
      "data": {}
    },
    "redis": {
      "status": "Degraded",
      "duration": "00:00:00.073",
      "description": "High latency",
      "data": {}
    }
  }
}
```

The app also handles simple responses where only the HTTP status code matters (200 = Healthy, 503 = Unhealthy) and the body is just a string like `"Healthy"`.

---

## 2. Features

### P0 - MVP
- **Environment management**: Add/edit/delete environments (name + base URL)
- **Module management**: Add/edit/delete modules per environment (name maps to URL path)
- **Dashboard**: Grid/list view showing all modules per environment with color-coded status (green/yellow/red)
- **Polling**: Configurable polling interval per environment (default 30s)
- **Notifications**: Native OS notification (macOS + Windows) when a module transitions from Healthy to Unhealthy or Degraded
- **Detail view**: Tap a module to see the full health check response including all entries, durations, and descriptions
- **Persistence**: Save configuration (environments, modules, settings) to local JSON file

### P1 - Nice to Have
- **Health history**: Keep last N status changes per module with timestamps
- **Custom headers**: Support Authorization headers or custom headers per environment (e.g., API keys)
- **Import/Export**: Export/import configuration as JSON for sharing between machines
- **System tray**: Minimize to system tray with status indicator
- **Sound alerts**: Optional sound on status change

### P2 - Future
- **Grouped modules**: Tag/group modules within an environment
- **Response time graph**: Chart response times over time
- **Webhook/Slack integration**: Forward alerts to external channels

---

## 3. Architecture

### Project Structure

```
HCApp/
├── HCApp.sln
├── src/
│   └── HCApp/
│       ├── HCApp.csproj                  # .NET MAUI project (net9.0-maccatalyst;net9.0-windows10.0.19041.0)
│       ├── App.xaml / App.xaml.cs
│       ├── AppShell.xaml / AppShell.cs
│       ├── MauiProgram.cs                # DI registration
│       │
│       ├── Models/
│       │   ├── Environment.cs            # Name, BaseUrl, PollingIntervalSeconds, Modules list
│       │   ├── Module.cs                 # Name (path segment), last status, last checked
│       │   ├── HealthCheckResponse.cs    # Deserialization model for .NET HC format
│       │   └── HealthStatus.cs           # Enum: Unknown, Healthy, Degraded, Unhealthy
│       │
│       ├── Services/
│       │   ├── IHealthCheckService.cs    # Poll a single endpoint, return parsed result
│       │   ├── HealthCheckService.cs
│       │   ├── IMonitoringService.cs     # Orchestrates polling loops per environment
│       │   ├── MonitoringService.cs
│       │   ├── INotificationService.cs   # Send native OS notification
│       │   ├── NotificationService.cs
│       │   ├── IConfigurationStore.cs    # Load/save config from disk
│       │   └── ConfigurationStore.cs     # JSON file in AppData/Application Support
│       │
│       ├── ViewModels/
│       │   ├── DashboardViewModel.cs     # Main screen: list of environments + module statuses
│       │   ├── EnvironmentEditViewModel.cs
│       │   ├── ModuleDetailViewModel.cs  # Detail view for a single module's health response
│       │   └── SettingsViewModel.cs
│       │
│       ├── Views/
│       │   ├── DashboardPage.xaml        # Main page
│       │   ├── EnvironmentEditPage.xaml  # Add/edit environment + its modules
│       │   ├── ModuleDetailPage.xaml     # Full health check response detail
│       │   └── SettingsPage.xaml         # Global settings (polling defaults, notifications toggle)
│       │
│       ├── Converters/
│       │   └── StatusToColorConverter.cs # Healthy->Green, Degraded->Orange, Unhealthy->Red
│       │
│       └── Platforms/
│           ├── MacCatalyst/
│           └── Windows/
```

### Key Design Decisions

| Decision | Choice | Rationale |
|---|---|---|
| UI Framework | .NET MAUI | Cross-platform, native desktop, C# ecosystem |
| Target Platforms | `net9.0-maccatalyst`, `net9.0-windows10.0.19041.0` | macOS + Windows desktop only |
| MVVM | CommunityToolkit.Mvvm | Source generators, minimal boilerplate |
| HTTP | `HttpClient` via `IHttpClientFactory` | Proper connection pooling, timeout handling |
| Notifications | `Plugin.LocalNotification` or platform-specific | Native OS notifications |
| Persistence | `System.Text.Json` to file | Simple, no database needed |
| Polling | `PeriodicTimer` per environment | Async-friendly, cancellable |

### Data Flow

```
ConfigurationStore (JSON file)
        │
        ▼
 MonitoringService ──► starts PeriodicTimer per Environment
        │
        ▼
 HealthCheckService ──► HTTP GET {BaseUrl}/{ModuleName}
        │                      │
        ▼                      ▼
 Parse JSON response    Handle errors (timeout, DNS, 5xx)
        │
        ▼
 Update Module status ──► If status changed: NotificationService.Send()
        │
        ▼
 DashboardViewModel (ObservableCollection bound to UI)
```

---

## 4. NuGet Packages

| Package | Purpose |
|---|---|
| `CommunityToolkit.Mvvm` | MVVM source generators, ObservableObject, RelayCommand |
| `CommunityToolkit.Maui` | UI helpers, converters, behaviors |
| `Plugin.LocalNotification` | Cross-platform local notifications |
| `Microsoft.Extensions.Http` | IHttpClientFactory registration |

---

## 5. Configuration File Format

Stored at:
- **macOS**: `~/Library/Application Support/HCApp/config.json`
- **Windows**: `%APPDATA%/HCApp/config.json`

```json
{
  "environments": [
    {
      "id": "guid",
      "name": "Production",
      "baseUrl": "https://api.example.com/health",
      "pollingIntervalSeconds": 30,
      "headers": {
        "Authorization": "Bearer xxx"
      },
      "modules": [
        { "id": "guid", "name": "users-service" },
        { "id": "guid", "name": "payments-service" },
        { "id": "guid", "name": "notifications-service" }
      ]
    },
    {
      "id": "guid",
      "name": "Staging",
      "baseUrl": "https://staging-api.example.com/health",
      "pollingIntervalSeconds": 60,
      "headers": {},
      "modules": [
        { "id": "guid", "name": "users-service" }
      ]
    }
  ],
  "settings": {
    "notificationsEnabled": true,
    "defaultPollingIntervalSeconds": 30
  }
}
```

URLs are constructed as: `{baseUrl}/{moduleName}` -> `https://api.example.com/health/users-service`

---

## 6. UI Wireframes (Text)

### Dashboard Page
```
┌─────────────────────────────────────────────────────┐
│  HCApp                                    [⚙ Settings] │
├─────────────────────────────────────────────────────┤
│  [Production ▾]  [Staging]  [Dev]       [+ Environment] │
├─────────────────────────────────────────────────────┤
│                                                         │
│  ● users-service          Healthy    120ms   10s ago   │
│  ◐ payments-service       Degraded   450ms   10s ago   │
│  ○ notifications-service  Unhealthy  ---     10s ago   │
│  ● auth-service           Healthy     35ms   10s ago   │
│                                                         │
│                                        [+ Add Module]   │
└─────────────────────────────────────────────────────┘
```

### Module Detail Page
```
┌─────────────────────────────────────────────────────┐
│  ← Back    payments-service    [Edit] [Delete]          │
├─────────────────────────────────────────────────────┤
│  Status: Degraded          URL: .../health/payments     │
│  Total Duration: 450ms     Last Checked: 10:32:15       │
├─────────────────────────────────────────────────────┤
│  Entries:                                               │
│  ┌───────────────────────────────────────────────┐     │
│  │ ● database        Healthy    50ms              │     │
│  │ ◐ stripe-api      Degraded   380ms             │     │
│  │   "High response time from Stripe"             │     │
│  │ ● redis           Healthy    20ms              │     │
│  └───────────────────────────────────────────────┘     │
├─────────────────────────────────────────────────────┤
│  Recent History:                                        │
│  10:32 Degraded  │  10:31 Healthy  │  10:30 Healthy    │
└─────────────────────────────────────────────────────┘
```

---

## 7. Implementation Order

### Phase 1: Skeleton + Core Services
1. Create MAUI project targeting maccatalyst + windows
2. Set up DI in `MauiProgram.cs`
3. Implement `Models/` (Environment, Module, HealthCheckResponse, HealthStatus)
4. Implement `ConfigurationStore` (JSON load/save)
5. Implement `HealthCheckService` (single endpoint poll + parse)

### Phase 2: Monitoring + Dashboard
6. Implement `MonitoringService` (polling orchestration with PeriodicTimer)
7. Build `DashboardViewModel` with ObservableCollection
8. Build `DashboardPage.xaml` with environment tabs and module list
9. Wire up status colors via `StatusToColorConverter`

### Phase 3: CRUD + Detail
10. Build `EnvironmentEditPage` (add/edit environment + modules)
11. Build `ModuleDetailPage` (full response view)
12. Build `SettingsPage` (notifications toggle, default interval)

### Phase 4: Notifications + Polish
13. Integrate `Plugin.LocalNotification` for native notifications
14. Implement status transition detection (Healthy -> Unhealthy triggers notification)
15. Error handling: timeouts, DNS failures, invalid JSON
16. Persist window size/position
17. Test on both macOS and Windows
