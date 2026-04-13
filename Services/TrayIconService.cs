using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AWSServerSelector.Models;
using AWSServerSelector.Services.Interfaces;
using Hardcodet.Wpf.TaskbarNotification;

namespace AWSServerSelector.Services;

public sealed class TrayIconService : ITrayIconService
{
    private readonly ILocalizationService _localizationService;
    private readonly IDispatcherTimerFactory _dispatcherTimerFactory;

    private TaskbarIcon? _taskbarIcon;
    private ITrayHost? _trayHost;
    private Window? _mainWindow;
    private DispatcherTimer? _statusTimer;
    private MenuItem? _toggleMenuItem;
    private MenuItem? _statusMenuItem;
    private bool _disposed;

    public TrayIconService(
        ILocalizationService localizationService,
        IDispatcherTimerFactory dispatcherTimerFactory)
    {
        _localizationService = localizationService;
        _dispatcherTimerFactory = dispatcherTimerFactory;
    }

    public void Initialize(Window mainWindow)
    {
        if (_taskbarIcon != null)
        {
            return;
        }

        if (mainWindow is not ITrayHost trayHost)
        {
            throw new InvalidOperationException("Main window must implement ITrayHost for tray integration.");
        }

        _mainWindow = mainWindow;
        _trayHost = trayHost;

        _taskbarIcon = new TaskbarIcon
        {
            IconSource = new BitmapImage(new Uri("pack://application:,,,/Resources/icon.ico", UriKind.Absolute)),
            ContextMenu = BuildContextMenu(),
            ToolTipText = BuildTooltip(TrayStatusSnapshot.Unavailable),
            Visibility = Visibility.Visible
        };

        _taskbarIcon.TrayMouseDoubleClick += OnTrayMouseDoubleClick;
        _localizationService.LanguageChanged += OnLanguageChanged;
        StartStatusTimer();
        RefreshStatus();
    }

    public void ShowMainWindow()
    {
        if (_trayHost == null)
        {
            return;
        }

        _trayHost.ShowFromTray();
        RefreshStatus();
    }

    public void HideMainWindow()
    {
        if (_trayHost == null)
        {
            return;
        }

        _trayHost.HideToTray();
        RefreshStatus();
    }

    public void RefreshStatus()
    {
        if (_taskbarIcon == null)
        {
            return;
        }

        var snapshot = GetSafeSnapshot();
        UpdateToggleHeader();
        UpdateStatusHeader(snapshot);
        _taskbarIcon.ToolTipText = BuildTooltip(snapshot);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _localizationService.LanguageChanged -= OnLanguageChanged;

        if (_statusTimer != null)
        {
            _statusTimer.Stop();
            _statusTimer = null;
        }

        if (_taskbarIcon != null)
        {
            _taskbarIcon.TrayMouseDoubleClick -= OnTrayMouseDoubleClick;
            _taskbarIcon.Dispose();
            _taskbarIcon = null;
        }
    }

    private ContextMenu BuildContextMenu()
    {
        _toggleMenuItem = new MenuItem();
        _toggleMenuItem.Click += (_, _) => ToggleMainWindowVisibility();

        _statusMenuItem = new MenuItem
        {
            IsEnabled = false
        };

        var refreshMenuItem = new MenuItem();
        refreshMenuItem.Click += async (_, _) => await RefreshPingAsync();

        var applyMenuItem = new MenuItem();
        applyMenuItem.Click += (_, _) => _trayHost?.ApplySelectionFromTray();

        var resetMenuItem = new MenuItem();
        resetMenuItem.Click += (_, _) => _trayHost?.ResetSelectionFromTray();

        var connectionInfoMenuItem = new MenuItem();
        connectionInfoMenuItem.Click += (_, _) => _trayHost?.OpenConnectionInfoFromTray();

        var exitMenuItem = new MenuItem();
        exitMenuItem.Click += (_, _) => _trayHost?.ExitFromTray();

        var menu = new ContextMenu();
        menu.Items.Add(_toggleMenuItem);
        menu.Items.Add(_statusMenuItem);
        menu.Items.Add(refreshMenuItem);
        menu.Items.Add(applyMenuItem);
        menu.Items.Add(resetMenuItem);
        menu.Items.Add(connectionInfoMenuItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(exitMenuItem);

        ApplyLocalizedHeaders(
            refreshMenuItem,
            applyMenuItem,
            resetMenuItem,
            connectionInfoMenuItem,
            exitMenuItem);

        UpdateToggleHeader();
        UpdateStatusHeader(GetSafeSnapshot());
        return menu;
    }

    private void ApplyLocalizedHeaders(
        MenuItem refreshMenuItem,
        MenuItem applyMenuItem,
        MenuItem resetMenuItem,
        MenuItem connectionInfoMenuItem,
        MenuItem exitMenuItem)
    {
        refreshMenuItem.Header = _localizationService.GetString("TrayRefreshPing");
        applyMenuItem.Header = _localizationService.GetString("TrayApply");
        resetMenuItem.Header = _localizationService.GetString("TrayReset");
        connectionInfoMenuItem.Header = _localizationService.GetString("TrayConnectionInfo");
        exitMenuItem.Header = _localizationService.GetString("TrayExit");
    }

    private void UpdateToggleHeader()
    {
        if (_toggleMenuItem == null)
        {
            return;
        }

        var isVisible = _mainWindow?.IsVisible == true;
        _toggleMenuItem.Header = _localizationService.GetString(isVisible ? "TrayHide" : "TrayShow");
    }

    private void UpdateStatusHeader(TrayStatusSnapshot snapshot)
    {
        if (_statusMenuItem == null)
        {
            return;
        }

        _statusMenuItem.Header = _localizationService.GetString(
            "TrayStatusFormat",
            snapshot.MatchPing,
            snapshot.Region);
    }

    private string BuildTooltip(TrayStatusSnapshot snapshot)
    {
        var tooltip = _localizationService.GetString(
            "TrayTooltipFormat",
            snapshot.MatchPing,
            snapshot.Region);

        return tooltip.Length <= 63 ? tooltip : tooltip[..63];
    }

    private TrayStatusSnapshot GetSafeSnapshot()
    {
        try
        {
            return _trayHost?.GetTrayStatusSnapshot() ?? TrayStatusSnapshot.Unavailable;
        }
        catch (Exception ex)
        {
            AppLogger.Error("Failed to get tray status snapshot.", ex);
            return TrayStatusSnapshot.Unavailable;
        }
    }

    private async Task RefreshPingAsync()
    {
        if (_trayHost == null)
        {
            return;
        }

        try
        {
            await _trayHost.RefreshPingFromTrayAsync();
        }
        catch (Exception ex)
        {
            AppLogger.Error("Failed to refresh ping from tray.", ex);
        }
        finally
        {
            RefreshStatus();
        }
    }

    private void ToggleMainWindowVisibility()
    {
        _trayHost?.ToggleVisibilityFromTray();
        RefreshStatus();
    }

    private void StartStatusTimer()
    {
        _statusTimer = _dispatcherTimerFactory.Create(TimeSpan.FromSeconds(5));
        _statusTimer.Tick += (_, _) => RefreshStatus();
        _statusTimer.Start();
    }

    private void OnTrayMouseDoubleClick(object? sender, RoutedEventArgs e)
    {
        ToggleMainWindowVisibility();
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        if (_taskbarIcon?.ContextMenu is not ContextMenu menu)
        {
            return;
        }

        if (menu.Items.OfType<MenuItem>().ToList() is not { Count: >= 7 } items)
        {
            return;
        }

        // [0]: toggle, [1]: status, [2]: refresh, [3]: apply, [4]: reset, [5]: connection info, [6]: exit
        ApplyLocalizedHeaders(items[2], items[3], items[4], items[5], items[6]);
        RefreshStatus();
    }
}
