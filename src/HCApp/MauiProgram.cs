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
		builder.Services.AddHttpClient("HealthCheck");
		builder.Services.AddSingleton<IConfigurationStore, ConfigurationStore>();
		builder.Services.AddSingleton<IHealthCheckService, HealthCheckService>();
		builder.Services.AddSingleton<INotificationService, NotificationService>();
		builder.Services.AddSingleton<MonitoringService>();

		// ViewModels
		builder.Services.AddTransient<DashboardViewModel>();
		builder.Services.AddTransient<EnvironmentEditViewModel>();
		builder.Services.AddTransient<ModuleDetailViewModel>();

		// Pages
		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<EnvironmentEditPage>();
		builder.Services.AddTransient<ModuleDetailPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
