using System.Windows.Forms;
using Microsoft.UI.Windowing;

namespace HCApp;

/// <summary>
/// Manages the Windows system-tray icon and window close/minimize interception.
/// Created once per app lifetime, initialized after the native window is available.
/// </summary>
public sealed class TrayService : IDisposable
{
    private NotifyIcon? _notifyIcon;
    private Microsoft.UI.Xaml.Window? _nativeWindow;
    private AppWindow? _appWindow;
    private bool _isExiting;

    public void Initialize(Microsoft.UI.Xaml.Window nativeWindow)
    {
        _nativeWindow = nativeWindow;

        // Resolve the WinUI AppWindow (needed for close interception and hide/show)
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        // Tray icon — reuse the process icon so it matches the taskbar entry
        _notifyIcon = new NotifyIcon
        {
            Text = "Health Monitor",
            Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? string.Empty)
                   ?? SystemIcons.Application,
            Visible = false
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Open Health Monitor", null, (_, _) => ShowWindow());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitApp());
        _notifyIcon.ContextMenuStrip = menu;
        _notifyIcon.DoubleClick += (_, _) => ShowWindow();

        // Intercept the close button — cancel and let the user decide
        _appWindow.Closing += OnClosing;

        // Intercept minimize — send straight to tray
        _appWindow.Changed += OnAppWindowChanged;
    }

    private async void OnClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_isExiting) return;

        // Cancel the native close synchronously; handle asynchronously after
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
            // "Cancel" → do nothing, window stays open
        }
    }

    private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (_isExiting) return;
        if (sender.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Minimized })
            HideToTray();
    }

    private void HideToTray()
    {
        _appWindow?.Hide();
        if (_notifyIcon is not null)
            _notifyIcon.Visible = true;
    }

    private void ShowWindow()
    {
        if (_notifyIcon is not null)
            _notifyIcon.Visible = false;
        _appWindow?.Show();
        _nativeWindow?.Activate();
    }

    private void ExitApp()
    {
        _isExiting = true;
        _notifyIcon?.Dispose();
        _notifyIcon = null;
        // Remove handlers before closing so the Closing event doesn't re-cancel
        if (_appWindow is not null)
        {
            _appWindow.Closing -= OnClosing;
            _appWindow.Changed -= OnAppWindowChanged;
        }
        _nativeWindow?.Close();
    }

    public void Dispose()
    {
        _notifyIcon?.Dispose();
        _notifyIcon = null;
    }
}
