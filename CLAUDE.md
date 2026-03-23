# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

HCApp is a .NET MAUI desktop application (macOS + Windows) that monitors health check endpoints of web APIs following the ASP.NET Core health check response format. See `PLAN.md` for full feature specs, wireframes, and implementation phases.

## Build & Run

```bash
# Build (macOS)
dotnet build src/HCApp/HCApp.csproj -f net10.0-maccatalyst

# Build (Windows)
dotnet build src/HCApp/HCApp.csproj -f net10.0-windows10.0.19041.0

# Run (macOS)
dotnet build src/HCApp/HCApp.csproj -f net10.0-maccatalyst -t:Run
```

## Solution Structure

Single-project solution (`HCApp.slnx`) targeting `net10.0-maccatalyst` and `net10.0-windows10.0.19041.0`. All source is under `src/HCApp/`.

## Architecture

MVVM pattern using **CommunityToolkit.Mvvm** (source generators). DI is configured in `MauiProgram.cs`.

- **Models/** — `MonitorEnvironment`, `MonitorModule`, `HealthCheckResponse`, `HealthStatus` enum
- **Services/** — `ConfigurationStore` (JSON persistence), `HealthCheckService` (HTTP polling + parsing), `MonitoringService` (polling orchestration with PeriodicTimer), `NotificationService` (native OS notifications)
- **ViewModels/** — `DashboardViewModel`, `EnvironmentEditViewModel`, `ModuleDetailViewModel`, `ModuleStatusViewModel`
- **Views/** — `DashboardPage`, `EnvironmentEditPage`, `ModuleDetailPage` (XAML + code-behind)
- **Converters/** — `StatusToColorConverter`, `StatusToBackgroundConverter`, `IsNotNullConverter`

### Data Flow

`ConfigurationStore` (JSON file) → `MonitoringService` (PeriodicTimer per environment) → `HealthCheckService` (HTTP GET `{BaseUrl}/{ModuleName}`) → status update → `NotificationService` on transition → `DashboardViewModel` (ObservableCollection bound to UI)

## Key Dependencies

- `CommunityToolkit.Mvvm` 8.4.1 — MVVM source generators
- `CommunityToolkit.Maui` 14.0.1 — UI helpers
- `Microsoft.Extensions.Http` — IHttpClientFactory for health check polling
- Target framework: .NET 10
