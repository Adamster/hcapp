using HCApp.Views;

namespace HCApp;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute("environment-edit", typeof(EnvironmentEditPage));
		Routing.RegisterRoute("module-detail", typeof(ModuleDetailPage));
		Routing.RegisterRoute("settings", typeof(SettingsPage));
	}
}
