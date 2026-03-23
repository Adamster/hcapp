namespace HCApp;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new AppShell());

#if WINDOWS
		var tray = new TrayService();
		window.HandlerChanged += (_, _) =>
		{
			if (window.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
				tray.Initialize(nativeWindow);
		};
		// Dispose tray when the window is destroyed (e.g. explicit app exit)
		window.Destroying += (_, _) => tray.Dispose();
#endif

		return window;
	}
}