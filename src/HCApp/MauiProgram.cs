using CommunityToolkit.Maui;
using HCApp.Services;
using HCApp.ViewModels;
using HCApp.Views;
using Microsoft.Extensions.Logging;

namespace HCApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Services
		builder.Services.AddHttpClient("HealthCheck", c => c.Timeout = TimeSpan.FromSeconds(15));
		builder.Services.AddSingleton<IConfigurationStore, ConfigurationStore>();
		builder.Services.AddSingleton<IHealthCheckService, HealthCheckService>();
		builder.Services.AddSingleton<INotificationService, NotificationService>();
		builder.Services.AddSingleton<MonitoringService>();

		// ViewModels — Dashboard is singleton: one instance for the app lifetime
		builder.Services.AddSingleton<DashboardViewModel>();
		builder.Services.AddTransient<EnvironmentEditViewModel>();
		builder.Services.AddTransient<ModuleDetailViewModel>();
		builder.Services.AddTransient<SettingsViewModel>();

		// Pages — Dashboard is singleton to avoid re-creation on navigation
		builder.Services.AddSingleton<DashboardPage>();
		builder.Services.AddTransient<EnvironmentEditPage>();
		builder.Services.AddTransient<ModuleDetailPage>();
		builder.Services.AddTransient<SettingsPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
