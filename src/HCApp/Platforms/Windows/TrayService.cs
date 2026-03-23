using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.UI.Windowing;

namespace HCApp;

public sealed class TrayService : IDisposable
{
    private TaskbarIcon?  _trayIcon;
    private AppWindow?    _appWindow;
    private Microsoft.UI.Xaml.Window? _nativeWindow;
    private bool          _isExiting;
    private bool          _disposed;

    public void Initialize(Microsoft.UI.Xaml.Window nativeWindow)
    {
        _nativeWindow = nativeWindow;

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
        _appWindow = AppWindow.GetFromWindowId(
            Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd));

        // Build the tray icon with a context menu
        _trayIcon = new TaskbarIcon
        {
            ToolTipText    = "Health Monitor",
            IconSource     = new GeneratedIconSource
            {
                Text            = "HM",
                BackgroundColor = Windows.UI.Color.FromArgb(255, 26, 32, 48),   // dark iron
                ForegroundColor = Windows.UI.Color.FromArgb(255, 200, 144, 32), // brass gold
            },
            ContextMenuMode = ContextMenuMode.PopupMenu,
        };

        // Context menu
        var menuOpen = new H.NotifyIcon.Core.PopupMenuItem("Open Health Monitor", (_, _) => ShowWindow());
        var menuSep  = new H.NotifyIcon.Core.PopupMenuSeparator();
        var menuExit = new H.NotifyIcon.Core.PopupMenuItem("Exit",                (_, _) => ExitApp());
        _trayIcon.ContextMenuItems.Add(menuOpen);
        _trayIcon.ContextMenuItems.Add(menuSep);
        _trayIcon.ContextMenuItems.Add(menuExit);

        _trayIcon.TrayMouseDoubleClick += (_, _) => ShowWindow();

        _trayIcon.ForceCreate(enablesActAsTaskbarWindow: false);

        // Intercept close button
        _appWindow.Closing += OnClosing;

        // Minimize → straight to tray
        _appWindow.Changed += OnAppWindowChanged;
    }

    private async void OnClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_isExiting) return;

        args.Cancel = true;

        var page = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null) { ExitApp(); return; }

        var result = await page.DisplayActionSheetAsync(
            "Health Monitor is still running",
            "Cancel",
            "Exit",
            "Minimize to Tray");

        switch (result)
        {
            case "Minimize to Tray": HideToTray(); break;
            case "Exit":             ExitApp();    break;
        }
    }

    private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (_isExiting) return;
        if (sender.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Minimized })
            HideToTray();
    }

    private void HideToTray()   => _appWindow?.Hide();
    private void ShowWindow()
    {
        _appWindow?.Show();
        _nativeWindow?.Activate();
    }

    private void ExitApp()
    {
        _isExiting = true;
        if (_appWindow is not null)
        {
            _appWindow.Closing -= OnClosing;
            _appWindow.Changed -= OnAppWindowChanged;
        }
        _trayIcon?.Dispose();
        _trayIcon = null;
        _nativeWindow?.Close();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _trayIcon?.Dispose();
    }
}
